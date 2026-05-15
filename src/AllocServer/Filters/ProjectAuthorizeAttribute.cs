using AllocServer.Data;
using AllocServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AllocServer.Filters
{
    public static class ProjectPermissionIds
    {
        public const string Update = "project:update";
        public const string Delete = "project:delete";
    }

    public static class TaskPermissionIds
    {
        public const string Create = "task:create";
        public const string Update = "task:update";
        public const string Delete = "task:delete";
    }

    public static class TimesheetPermissionIds
    {
        public const string ViewAll = "timesheet:view_all";
        public const string EditAll = "timesheet:edit_all";
    }

    public static class ExpensePermissionIds
    {
        public const string View = "expense:view";
        public const string Create = "expense:create";
    }

    public static class RevenuePermissionIds
    {
        public const string View = "revenue:view";
    }

    public static class RiskPermissionIds
    {
        public const string View = "risk:view";
        public const string Create = "risk:create";
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ProjectAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public const string CurrentProjectItemKey = "CurrentProject";

        private readonly string? _requiredPermissionId;

        public ProjectAuthorizeAttribute(string? requiredPermissionId = null)
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

            if (!context.RouteData.Values.TryGetValue("projectId", out var projectIdObj)
                || !int.TryParse(projectIdObj?.ToString(), out var projectId))
            {
                context.Result = new BadRequestObjectResult(new { message = "Missing projectId in route." });
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

            var project = await dbContext.Projects
                .Include(item => item.Workspace)
                .Where(item =>
                    item.ProjectID == projectId
                    && item.Workspace != null
                    && !item.Workspace.IsDeleted)
                .FirstOrDefaultAsync();

            if (project == null)
            {
                context.Result = new NotFoundObjectResult(new { message = "Khong tim thay Project." });
                return;
            }

            var membership = await dbContext.WorkspaceMembers
                .AsNoTracking()
                .Where(member =>
                    member.WorkspaceID == project.WorkspaceID
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

            context.HttpContext.Items[CurrentProjectItemKey] = project;
        }
    }
}
