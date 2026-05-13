using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class TaskAssigneeResponse
    {
        [JsonPropertyName("taskId")]
        public int TaskId { get; set; }

        [JsonPropertyName("memberId")]
        public int MemberId { get; set; }

        [JsonPropertyName("assigneeType")]
        public string AssigneeType { get; set; } = string.Empty;

        [JsonPropertyName("assignedAt")]
        public DateTime AssignedAt { get; set; }
    }
}
