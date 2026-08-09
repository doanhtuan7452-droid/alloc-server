using System;
using System.Collections.Generic;

namespace AllocServer.DTOs.SystemAdmin
{
    public class WorkspaceAdminDetailResponse
    {
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal StandardHours { get; set; }
        public DateTime CreatedAt { get; set; }
        public ActiveSubscriptionInfo? ActiveSubscription { get; set; }
        public List<WorkspaceMemberAdminSummaryResponse> Members { get; set; } = new();
        public List<WorkspaceMonthlyUsageResponse> Usages { get; set; } = new();
    }

    public class ActiveSubscriptionInfo
    {
        public int SubscriptionId { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class WorkspaceMemberAdminSummaryResponse
    {
        public int WorkspaceMemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class WorkspaceMonthlyUsageResponse
    {
        public string BillingMonth { get; set; } = string.Empty;
        public int AIQueryCount { get; set; }
        public decimal StorageUsedMB { get; set; }
    }
}
