using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AllocServer.Data;
using AllocServer.Models;
using AllocServer.Constants.Permissions;
using AllocServer.Filters;

namespace AllocServer.Extensions
{
    public static class DatabaseSeedExtensions
    {
        private static readonly (string PermissionID, string DisplayName)[] SeedPermissions = new[]
        {
            // Timesheets
            (TimesheetPermissionIds.ViewAll, "View all workspace timesheets"),
            (TimesheetPermissionIds.EditAll, "Edit all workspace timesheets"),
            // Expenses
            (ExpensePermissionIds.View, "View project expenses"),
            (ExpensePermissionIds.Create, "Create project expenses"),
            // Revenues
            (RevenuePermissionIds.View, "View project revenues"),
            // Requests
            (RequestPermissionIds.Approve, "Approve or reject workspace requests"),
            // Risks
            (RiskPermissionIds.View, "View project risks"),
            (RiskPermissionIds.Create, "Create project risks"),
            // AI
            (AIPermissionIds.View, "View project AI insights"),
            (AIPermissionIds.Ask, "Ask project AI assistant"),
            // Assets
            (AssetPermissionIds.View, "View project assets"),
            (AssetPermissionIds.Create, "Upload project assets"),
            (AssetPermissionIds.Delete, "Delete project assets"),
            // Tasks
            (TaskPermissionIds.View, "View project tasks"),
            (TaskPermissionIds.Create, "Create project tasks"),
            (TaskPermissionIds.Update, "Update project tasks"),
            (TaskPermissionIds.Delete, "Delete project tasks"),
            // Conversations
            (ConversationPermissionIds.Manage, "Manage workspace conversations"),
            // Member Profiles
            (MemberProfilePermissionIds.View, "View workspace member profiles"),
            (MemberProfilePermissionIds.Manage, "Manage and edit workspace member profiles")
        };

        public static async Task SeedDatabasePermissionsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<WebApplication>>();
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            int maxRetries = 3;
            int delaySeconds = 5;

            for (int retry = 1; retry <= maxRetries; retry++)
            {
                try
                {
                    logger.LogInformation("Database Seeding: Attempt {Attempt} of {MaxRetries} starting...", retry, maxRetries);
                    
                    // Run the seeding logic within a transaction to guarantee atomic execution
                    await using var transaction = await dbContext.Database.BeginTransactionAsync();
                    
                    // 1. Bulk check and upsert permissions
                    var permissionIds = SeedPermissions.Select(p => p.PermissionID).ToList();
                    var existingPermissions = await dbContext.WorkspacePermissions
                        .Where(p => permissionIds.Contains(p.PermissionID))
                        .ToDictionaryAsync(p => p.PermissionID);

                    bool hasChanges = false;

                    foreach (var (permissionId, displayName) in SeedPermissions)
                    {
                        if (!existingPermissions.TryGetValue(permissionId, out var existing))
                        {
                            dbContext.WorkspacePermissions.Add(new WorkspacePermission
                            {
                                PermissionID = permissionId,
                                DisplayName = displayName
                            });
                            hasChanges = true;
                        }
                        else if (existing.DisplayName != displayName)
                        {
                            existing.DisplayName = displayName;
                            hasChanges = true;
                        }
                    }

                    if (hasChanges)
                    {
                        await dbContext.SaveChangesAsync();
                    }

                    // 2. Fetch Active Owner Roles
                    var ownerRoleIds = await dbContext.WorkspaceRoles
                        .Where(role => role.RoleName == "Owner" && !role.IsDeleted)
                        .Select(role => role.WorkspaceRoleID)
                        .ToListAsync();

                    if (ownerRoleIds.Any())
                    {
                        // Fetch existing RolePermissions mapping for these Owner roles to avoid duplicates
                        var existingMappings = await dbContext.RolePermissions
                            .Where(rp => ownerRoleIds.Contains(rp.WorkspaceRoleID) && permissionIds.Contains(rp.PermissionID))
                            .Select(rp => new { rp.WorkspaceRoleID, rp.PermissionID })
                            .ToListAsync();

                        var existingMappingSet = existingMappings
                            .Select(m => (m.WorkspaceRoleID, m.PermissionID))
                            .ToHashSet();

                        bool hasMappingChanges = false;

                        foreach (var ownerRoleId in ownerRoleIds)
                        {
                            foreach (var (permissionId, _) in SeedPermissions)
                            {
                                if (!existingMappingSet.Contains((ownerRoleId, permissionId)))
                                {
                                    dbContext.RolePermissions.Add(new RolePermission
                                    {
                                        WorkspaceRoleID = ownerRoleId,
                                        PermissionID = permissionId
                                    });
                                    hasMappingChanges = true;
                                }
                            }
                        }

                        if (hasMappingChanges)
                        {
                            await dbContext.SaveChangesAsync();
                        }
                    }

                    await transaction.CommitAsync();
                    logger.LogInformation("Database Seeding: Permissions and Owner mappings successfully seeded.");
                    return; // Success, exit retry loop
                }
                catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
                {
                    // If a concurrent instance is seeding the exact same values, a unique constraint/PK exception might occur.
                    // This is safe to ignore because the target state is identical (Idempotent seed).
                    logger.LogWarning("Database Seeding: Concurrency unique constraint conflict detected. This is expected if multiple app instances are starting up concurrently. Skipping remaining inserts.");
                    return; // Target state is satisfied, exit safely
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Database Seeding: Failure on attempt {Attempt} of {MaxRetries}.", retry, maxRetries);
                    if (retry == maxRetries)
                    {
                        logger.LogCritical("Database Seeding: Maximum retries reached. Failing startup.");
                        throw; // Crash container/app on startup if connection is permanently lost
                    }
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var sqlException = ex.InnerException as Microsoft.Data.SqlClient.SqlException;
            if (sqlException != null)
            {
                // SQL Server Error Codes: 
                // 2601 - Cannot insert duplicate key row in object with unique index.
                // 2627 - Violation of %ls constraint. Cannot insert duplicate key.
                return sqlException.Number == 2601 || sqlException.Number == 2627;
            }
            return false;
        }
    }
}
