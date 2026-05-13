using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using AllocServer.Data;

namespace AllocServer.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class WorkspaceAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
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
                // If the route doesn't have workspaceId, we can't authorize. 
                // Return Bad Request or Forbid.
                context.Result = new BadRequestObjectResult(new { message = "Missing workspaceId in route." });
                return;
            }

            // Check against database
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

            var hasAccess = await dbContext.WorkspaceMembers
                .Include(m => m.Resource)
                .Include(m => m.Workspace)
                .AnyAsync(m => m.WorkspaceID == workspaceId 
                            && m.Resource.AccountID == accountId 
                            && m.Status == "Active"
                            && !m.Workspace.IsDeleted);

            if (!hasAccess)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
