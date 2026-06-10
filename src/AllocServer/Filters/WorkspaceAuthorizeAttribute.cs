using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using AllocServer.Data;
using AllocServer.Models.Cache;

namespace AllocServer.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class WorkspaceAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string? _requiredPermissionId;

        public WorkspaceAuthorizeAttribute(string? requiredPermissionId = null)
        {
            _requiredPermissionId = requiredPermissionId;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Extract AccountId from token
            var accountIdStr = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                               ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(accountIdStr) || !int.TryParse(accountIdStr, out var accountId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Extract WorkspaceId from Route (e.g. [Route("api/workspaces/{workspaceId}/...")])
            if (!context.RouteData.Values.TryGetValue("workspaceId", out var workspaceIdObj) 
                || !int.TryParse(workspaceIdObj?.ToString(), out var workspaceId))
            {
                context.Result = new BadRequestObjectResult(new { message = "Missing workspaceId in route." });
                return;
            }

            // Check against database or cache
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();

            var cacheKey = $"workspace_auth_{accountId}_{workspaceId}";
            var cachedData = await cache.GetStringAsync(cacheKey);

            WorkspaceAuthCacheModel? authData = null;

            if (!string.IsNullOrEmpty(cachedData))
            {
                authData = JsonSerializer.Deserialize<WorkspaceAuthCacheModel>(cachedData);
            }

            if (authData == null)
            {
                var member = await dbContext.WorkspaceMembers
                    .AsNoTracking()
                    .Where(m => m.WorkspaceID == workspaceId 
                                && m.Resource.AccountID == accountId 
                                && m.Status == "Active"
                                && !m.Workspace.IsDeleted)
                    .Select(m => new WorkspaceAuthCacheModel
                    {
                        WorkspaceRoleID = m.WorkspaceRoleID,
                        RoleName = m.WorkspaceRole.RoleName,
                        Permissions = dbContext.RolePermissions
                            .Where(rp => rp.WorkspaceRoleID == m.WorkspaceRoleID)
                            .Select(rp => rp.PermissionID)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (member == null)
                {
                    context.Result = new ForbidResult();
                    return;
                }

                authData = member;
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };
                await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(authData), cacheOptions);
            }

            if (!string.IsNullOrWhiteSpace(_requiredPermissionId))
            {
                var hasPermission = authData.Permissions.Contains(_requiredPermissionId);

                var isOwnerFallback = string.Equals(
                    authData.RoleName,
                    "Owner",
                    StringComparison.OrdinalIgnoreCase);

                if (!hasPermission && !isOwnerFallback)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }
    }
}
