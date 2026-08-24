using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class SubTaskResponse
    {
        [JsonPropertyName("subTaskId")]
        public int SubTaskID { get; set; }

        [JsonPropertyName("taskId")]
        public int TaskID { get; set; }

        [JsonPropertyName("subTaskName")]
        public string SubTaskName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "To-do";

        [JsonPropertyName("workspaceMemberId")]
        public int? WorkspaceMemberID { get; set; }

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}
