using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllocServer.Data;
using AllocServer.DTOs.Accounts;
using AllocServer.DTOs.SystemAdmin;
using AllocServer.Interfaces.AI;
using AllocServer.Interfaces.Auth;
using AllocServer.Interfaces.Notifications;
using AllocServer.Interfaces.SystemAdmin;
using AllocServer.Interfaces.WorkspaceMemberProfiles;
using AllocServer.Models;
using AllocServer.Services.Notification_Services;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.SystemAdmin_Services
{
    public class SystemAdminService : ISystemAdminService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISessionService _sessionService;
        private readonly INotificationQueue _notificationQueue;
        private readonly INotificationCompensationQueue _notificationCompensationQueue;
        private readonly IProfileCalculationQueue _profileCalculationQueue;
        private readonly IAIQuotaCompensationQueue _aiQuotaCompensationQueue;

        public SystemAdminService(
            ApplicationDbContext dbContext,
            ISessionService sessionService,
            INotificationQueue notificationQueue,
            INotificationCompensationQueue notificationCompensationQueue,
            IProfileCalculationQueue profileCalculationQueue,
            IAIQuotaCompensationQueue aiQuotaCompensationQueue)
        {
            _dbContext = dbContext;
            _sessionService = sessionService;
            _notificationQueue = notificationQueue;
            _notificationCompensationQueue = notificationCompensationQueue;
            _profileCalculationQueue = profileCalculationQueue;
            _aiQuotaCompensationQueue = aiQuotaCompensationQueue;
        }

        public async Task<AccountAdminDetailResponse?> GetAccountDetailsForAdminAsync(int accountId)
        {
            var accountInfo = await (
                from account in _dbContext.Accounts.AsNoTracking()
                join resource in _dbContext.Resources.AsNoTracking() on account.AccountID equals resource.AccountID into resourceGroup
                from resource in resourceGroup.DefaultIfEmpty()
                where account.AccountID == accountId
                select new { Account = account, Resource = resource }
            ).FirstOrDefaultAsync();

            if (accountInfo == null) return null;

            var workspaces = await (
                from member in _dbContext.WorkspaceMembers.AsNoTracking()
                join ws in _dbContext.Workspaces.AsNoTracking() on member.WorkspaceID equals ws.WorkspaceID
                join role in _dbContext.WorkspaceRoles.AsNoTracking() on member.WorkspaceRoleID equals role.WorkspaceRoleID
                where member.Resource.AccountID == accountId
                select new WorkspaceMemberListItemDto
                {
                    WorkspaceId = ws.WorkspaceID,
                    WorkspaceName = ws.Name,
                    WorkspaceType = ws.Type,
                    RoleName = role.RoleName,
                    MemberStatus = member.Status,
                    JoinedAt = member.JoinedAt
                }
            ).ToListAsync();

            return new AccountAdminDetailResponse
            {
                AccountId = accountInfo.Account.AccountID,
                Email = accountInfo.Account.Email,
                AuthType = accountInfo.Account.AuthType,
                IsEmailVerified = accountInfo.Account.IsEmailVerified,
                AccountStatus = accountInfo.Account.AccountStatus,
                IsSystemAccount = accountInfo.Account.IsSystemAccount,
                LastLoginAt = accountInfo.Account.LastLoginAt,
                CreatedAt = accountInfo.Account.CreatedAt,
                UpdatedAt = accountInfo.Account.UpdatedAt,
                Profile = accountInfo.Resource == null ? null : new ResourceProfileResponse
                {
                    ResourceID = accountInfo.Resource.ResourceID,
                    FullName = accountInfo.Resource.FullName,
                    PhoneNumber = accountInfo.Resource.PhoneNumber,
                    AvatarURL = accountInfo.Resource.AvatarURL,
                    Timezone = accountInfo.Resource.Timezone,
                    CreatedAt = accountInfo.Resource.CreatedAt
                },
                Workspaces = workspaces
            };
        }

        public async Task<bool> UpdateAccountStatusAsync(int accountId, UpdateAccountStatusRequest request)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.AccountID == accountId);
            if (account == null) return false;

            account.AccountStatus = request.Status;
            account.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            if (request.Status == "Locked" || request.Status == "Suspended")
            {
                await _sessionService.RevokeAllSessionsByAccountIdAsync(accountId);
            }

            return true;
        }

        public async Task<bool> UpdateSystemAccountRoleAsync(int accountId, UpdateSystemRoleRequest request)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.AccountID == accountId);
            if (account == null) return false;

            account.IsSystemAccount = request.IsSystemAccount;
            account.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<PagedWorkspacesResponse> GetWorkspacesForAdminAsync(GetWorkspacesQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var workspacesQuery = _dbContext.Workspaces.AsNoTracking().Where(w => !w.IsDeleted);

            if (!string.IsNullOrEmpty(query.Keyword))
            {
                workspacesQuery = workspacesQuery.Where(w => w.Name.Contains(query.Keyword));
            }

            if (!string.IsNullOrEmpty(query.Type))
            {
                workspacesQuery = workspacesQuery.Where(w => w.Type == query.Type);
            }

            var joinedQuery = 
                from w in workspacesQuery
                join sub in _dbContext.WorkspaceSubscriptions.AsNoTracking().Where(s => s.Status == "Active" && !s.IsDeleted) 
                    on w.WorkspaceID equals sub.WorkspaceID into subGroup
                from sub in subGroup.DefaultIfEmpty()
                join plan in _dbContext.SubscriptionPlans.AsNoTracking().Where(p => !p.IsDeleted) 
                    on sub.PlanID equals plan.PlanID into planGroup
                from plan in planGroup.DefaultIfEmpty()
                select new
                {
                    Workspace = w,
                    PlanCode = plan != null ? plan.PlanCode : "FREE",
                    PlanName = plan != null ? plan.PlanName : "Gói Cơ Bản",
                    MemberCount = _dbContext.WorkspaceMembers.AsNoTracking().Count(m => m.WorkspaceID == w.WorkspaceID && m.Status != "Deactivated")
                };

            if (!string.IsNullOrEmpty(query.PlanCode))
            {
                joinedQuery = joinedQuery.Where(item => item.PlanCode == query.PlanCode);
            }

            var totalItems = await joinedQuery.CountAsync();
            var items = await joinedQuery
                .OrderByDescending(item => item.Workspace.CreatedAt)
                .ThenByDescending(item => item.Workspace.WorkspaceID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new WorkspaceAdminListItemResponse
                {
                    WorkspaceId = item.Workspace.WorkspaceID,
                    Name = item.Workspace.Name,
                    Type = item.Workspace.Type,
                    StandardHours = item.Workspace.StandardHours,
                    ActivePlanCode = item.PlanCode,
                    ActivePlanName = item.PlanName,
                    MemberCount = item.MemberCount,
                    CreatedAt = item.Workspace.CreatedAt
                })
                .ToListAsync();

            return new PagedWorkspacesResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        public async Task<WorkspaceAdminDetailResponse?> GetWorkspaceDetailsForAdminAsync(int workspaceId)
        {
            var ws = await _dbContext.Workspaces.AsNoTracking().FirstOrDefaultAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
            if (ws == null) return null;

            var activeSub = await (
                from sub in _dbContext.WorkspaceSubscriptions.AsNoTracking()
                join plan in _dbContext.SubscriptionPlans.AsNoTracking() on sub.PlanID equals plan.PlanID
                where sub.WorkspaceID == workspaceId && sub.Status == "Active" && !sub.IsDeleted && !plan.IsDeleted
                select new ActiveSubscriptionInfo
                {
                    SubscriptionId = sub.SubscriptionID,
                    PlanCode = plan.PlanCode,
                    PlanName = plan.PlanName,
                    BillingCycle = sub.BillingCycle,
                    StartDate = sub.StartDate,
                    EndDate = sub.EndDate,
                    Status = sub.Status
                }
            ).FirstOrDefaultAsync();

            var members = await (
                from m in _dbContext.WorkspaceMembers.AsNoTracking()
                join r in _dbContext.Resources.AsNoTracking() on m.ResourceID equals r.ResourceID
                join a in _dbContext.Accounts.AsNoTracking() on r.AccountID equals a.AccountID
                join role in _dbContext.WorkspaceRoles.AsNoTracking() on m.WorkspaceRoleID equals role.WorkspaceRoleID
                where m.WorkspaceID == workspaceId && m.Status != "Deactivated" && !r.IsDeleted && !a.IsDeleted && !role.IsDeleted
                select new WorkspaceMemberAdminSummaryResponse
                {
                    WorkspaceMemberId = m.WorkspaceMemberID,
                    FullName = r.FullName,
                    Email = a.Email,
                    RoleName = role.RoleName,
                    Status = m.Status
                }
            ).ToListAsync();

            var usages = await _dbContext.WorkspaceMonthlyUsages.AsNoTracking()
                .Where(u => u.WorkspaceID == workspaceId)
                .OrderByDescending(u => u.BillingMonth)
                .Select(u => new WorkspaceMonthlyUsageResponse
                {
                    BillingMonth = u.BillingMonth.ToString("yyyy-MM"),
                    AIQueryCount = u.AIQueryCount,
                    StorageUsedMB = u.StorageUsedMB
                })
                .ToListAsync();

            return new WorkspaceAdminDetailResponse
            {
                WorkspaceId = ws.WorkspaceID,
                Name = ws.Name,
                Type = ws.Type,
                StandardHours = ws.StandardHours,
                CreatedAt = ws.CreatedAt,
                ActiveSubscription = activeSub,
                Members = members,
                Usages = usages
            };
        }

        public async Task<bool> UpdateWorkspaceSubscriptionAsync(int workspaceId, UpdateWorkspaceSubscriptionRequest request)
        {
            var wsExists = await _dbContext.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
            if (!wsExists) return false;

            var plan = await _dbContext.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.PlanCode == request.PlanCode && !p.IsDeleted);

            if (plan == null) return false;

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Hủy các subscription Active cũ
                var activeSubs = await _dbContext.WorkspaceSubscriptions
                    .Where(s => s.WorkspaceID == workspaceId && s.Status == "Active" && !s.IsDeleted)
                    .ToListAsync();

                foreach (var sub in activeSubs)
                {
                    sub.Status = "Cancelled";
                    sub.EndDate = DateTime.UtcNow;
                }

                // Tạo subscription mới
                var newSub = new WorkspaceSubscription
                {
                    WorkspaceID = workspaceId,
                    PlanID = plan.PlanID,
                    BillingCycle = request.BillingCycle,
                    StartDate = DateTime.UtcNow,
                    EndDate = null,
                    Status = "Active",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.WorkspaceSubscriptions.Add(newSub);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<PagedAIToolLogsResponse> GetAIToolLogsAsync(GetAIToolLogsQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var logsQuery = _dbContext.AIToolExecutionLogs.AsNoTracking().Where(l => !l.IsDeleted);

            if (query.WorkspaceId.HasValue)
            {
                logsQuery = logsQuery.Where(l => l.WorkspaceID == query.WorkspaceId.Value);
            }

            if (!string.IsNullOrEmpty(query.ToolName))
            {
                logsQuery = logsQuery.Where(l => l.ToolName == query.ToolName);
            }

            if (query.IsSuccess.HasValue)
            {
                logsQuery = logsQuery.Where(l => l.IsSuccess == query.IsSuccess.Value);
            }

            var totalItems = await logsQuery.CountAsync();
            var items = await (
                from l in logsQuery
                join ws in _dbContext.Workspaces.AsNoTracking() on l.WorkspaceID equals ws.WorkspaceID into wsGroup
                from ws in wsGroup.DefaultIfEmpty()
                join a in _dbContext.Accounts.AsNoTracking() on l.AccountID equals a.AccountID into aGroup
                from a in aGroup.DefaultIfEmpty()
                select new AIToolLogListItemResponse
                {
                    LogId = l.LogID,
                    WorkspaceId = l.WorkspaceID,
                    WorkspaceName = ws != null ? ws.Name : null,
                    AccountId = l.AccountID,
                    Email = a != null ? a.Email : null,
                    ToolName = l.ToolName,
                    Arguments = l.Arguments,
                    IsSuccess = l.IsSuccess,
                    StatusCode = l.StatusCode,
                    ErrorCode = l.ErrorCode,
                    ErrorMessage = l.ErrorMessage,
                    ExecutionTimeMs = l.ExecutionTimeMs,
                    CreatedAt = l.CreatedAt
                }
            )
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

            return new PagedAIToolLogsResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        public async Task<List<AIUsageAdminResponse>> GetAIUsagesForAdminAsync()
        {
            var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var usages = await (
                from usage in _dbContext.WorkspaceMonthlyUsages.AsNoTracking()
                join ws in _dbContext.Workspaces.AsNoTracking() on usage.WorkspaceID equals ws.WorkspaceID
                where usage.BillingMonth == currentMonth && !ws.IsDeleted
                select new
                {
                    WorkspaceId = usage.WorkspaceID,
                    WorkspaceName = ws.Name,
                    AIQueryCount = usage.AIQueryCount,
                    StorageUsedMB = usage.StorageUsedMB,
                    UpdatedAt = usage.UpdatedAt
                }
            ).ToListAsync();

            var limits = await _dbContext.WorkspaceCurrentLimits.AsNoTracking()
                .Where(l => l.FeatureCode == "AI_CHAT_QUOTA")
                .ToListAsync();

            var result = new List<AIUsageAdminResponse>();

            foreach (var u in usages)
            {
                var limit = limits.FirstOrDefault(l => l.WorkspaceID == u.WorkspaceId);
                result.Add(new AIUsageAdminResponse
                {
                    WorkspaceId = u.WorkspaceId,
                    WorkspaceName = u.WorkspaceName,
                    PlanCode = limit?.PlanCode ?? "FREE",
                    AIQueryCount = u.AIQueryCount,
                    MaxQuota = limit?.LimitValue ?? 10,
                    StorageUsedMB = u.StorageUsedMB,
                    UpdatedAt = u.UpdatedAt
                });
            }

            return result.OrderByDescending(r => r.AIQueryCount).ToList();
        }

        public async Task<BackgroundJobStatsResponse> GetBackgroundJobStatsAsync()
        {
            return new BackgroundJobStatsResponse
            {
                NotificationQueueLength = _notificationQueue.Count,
                NotificationCompensationQueueLength = _notificationCompensationQueue.Count,
                ProfileCalculationQueueLength = _profileCalculationQueue.Count,
                AIQuotaCompensationQueueLength = _aiQuotaCompensationQueue.Count
            };
        }
    }
}
