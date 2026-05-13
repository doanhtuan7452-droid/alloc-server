using AllocServer.Data;
using AllocServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

            var membership = await dbContext.WorkspaceMembers
                .AsNoTracking()
                .Where(member =>
                    member.WorkspaceID == task.Project.WorkspaceID
                    && member.Resource.AccountID == accountId
                    && member.Status == "Active"
                    && !member.Workspace.IsDeleted)
                .Select(member => new
                {
                    member.WorkspaceRoleID,
                    member.WorkspaceRole.RoleName
                })
                .FirstOrDefaultAsync();

            if (membership == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            if (!string.IsNullOrWhiteSpace(_requiredPermissionId))
            {
                var hasPermission = await dbContext.RolePermissions
                    .AsNoTracking()
                    .AnyAsync(rolePermission =>
                        rolePermission.WorkspaceRoleID == membership.WorkspaceRoleID
                        && rolePermission.PermissionID == _requiredPermissionId);

                var isOwnerFallback = string.Equals(
                    membership.RoleName,
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
