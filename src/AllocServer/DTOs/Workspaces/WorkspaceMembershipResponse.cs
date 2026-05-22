using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceMembershipResponse
    {
        [JsonPropertyName("workspaceMemberId")]
        public int WorkspaceMemberID { get; set; }

        [JsonPropertyName("resourceId")]
        public int ResourceID { get; set; }

        [JsonPropertyName("employeeCode")]
        public string EmployeeCode { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("joinedAt")]
        public DateTime JoinedAt { get; set; }

        [JsonPropertyName("role")]
        public WorkspaceRoleSummaryResponse Role { get; set; } = new();
    }
}
