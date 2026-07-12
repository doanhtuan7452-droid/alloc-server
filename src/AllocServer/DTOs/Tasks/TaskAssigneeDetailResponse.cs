using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class TaskAssigneeDetailResponse
    {
        [JsonPropertyName("taskId")]
        public int TaskId { get; set; }

        [JsonPropertyName("memberId")]
        public int MemberId { get; set; }

        [JsonPropertyName("employeeCode")]
        public string EmployeeCode { get; set; } = string.Empty;

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("avatarUrl")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("workspaceRoleName")]
        public string WorkspaceRoleName { get; set; } = string.Empty;

        [JsonPropertyName("memberStatus")]
        public string MemberStatus { get; set; } = string.Empty;

        [JsonPropertyName("assigneeTypes")]
        public List<string> AssigneeTypes { get; set; } = new();

        [JsonPropertyName("oldestAssignedAt")]
        public DateTime OldestAssignedAt { get; set; }
    }
}
