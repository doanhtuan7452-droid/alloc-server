using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceMemberDetailResponse
    {
        [JsonPropertyName("workspaceMemberId")]
        public int WorkspaceMemberID { get; set; }

        [JsonPropertyName("resource")]
        public WorkspaceMemberResourceResponse Resource { get; set; } = new();

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
