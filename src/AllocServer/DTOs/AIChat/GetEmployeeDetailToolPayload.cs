using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class GetEmployeeDetailToolPayload
    {
        [JsonPropertyName("workspaceMemberId")]
        public int? WorkspaceMemberId { get; set; }

        [JsonPropertyName("employeeCode")]
        public string? EmployeeCode { get; set; }

        [JsonPropertyName("workspaceId")]
        public int WorkspaceId { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }
    }
}
