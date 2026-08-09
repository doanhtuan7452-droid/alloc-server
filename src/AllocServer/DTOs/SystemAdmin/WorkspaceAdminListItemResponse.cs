using System;

namespace AllocServer.DTOs.SystemAdmin
{
    public class WorkspaceAdminListItemResponse
    {
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal StandardHours { get; set; }
        public string ActivePlanCode { get; set; } = string.Empty;
        public string ActivePlanName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
