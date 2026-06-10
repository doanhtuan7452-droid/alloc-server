using AllocServer.Data;
using AllocServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using AllocServer.Models.Cache;

namespace AllocServer.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TaskAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public const string CurrentTaskItemKey = "CurrentTask";

        private readonly string? _requiredPermissionId;

        public TaskAuthorizeAttribute(string? requiredPermissionId = null)
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

            var accountIdStr = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                               ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(accountIdStr) || !int.TryParse(accountIdStr, out var accountId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!context.RouteData.Values.TryGetValue("taskId", out var taskIdObj)
                || !int.TryParse(taskIdObj?.ToString(), out var taskId))
            {
                context.Result = new BadRequestObjectResult(new { message = "Missing taskId in route." });
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

            var task = await dbContext.ProjectTasks
                .Include(item => item.Project)
                    .ThenInclude(project => project!.Workspace)
                .Where(item =>
                    item.TaskID == taskId
                    && item.Project != null
                    && item.Project.Workspace != null
                    && !item.Project.IsDeleted
                    && !item.Project.Workspace.IsDeleted)
                .FirstOrDefaultAsync();

            if (task == null || task.Project == null)
            {
                context.Result = new NotFoundObjectResult(new { message = "Khong tim thay Task." });
                return;
            }

            int workspaceId = task.Project.WorkspaceID;
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

            context.HttpContext.Items[CurrentTaskItemKey] = task;
            context.HttpContext.Items[ProjectAuthorizeAttribute.CurrentProjectItemKey] = task.Project;
        }
    }
}
