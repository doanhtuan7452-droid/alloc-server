using System;

namespace AllocServer.DTOs.SystemAdmin
{
    public class AIUsageAdminResponse
    {
        public int WorkspaceId { get; set; }
        public string WorkspaceName { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public int AIQueryCount { get; set; }
        public int MaxQuota { get; set; }
        public decimal StorageUsedMB { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
