using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceListItemResponse
    {
        [JsonPropertyName("workspaceId")]
        public int WorkspaceID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("membership")]
        public WorkspaceMembershipResponse Membership { get; set; } = new();
    }

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

    public class WorkspaceRoleSummaryResponse
    {
        [JsonPropertyName("workspaceRoleId")]
        public int WorkspaceRoleID { get; set; }

        [JsonPropertyName("roleName")]
        public string RoleName { get; set; } = string.Empty;
    }
}
