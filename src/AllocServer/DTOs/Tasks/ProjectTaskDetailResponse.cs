using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class ProjectTaskDetailResponse
    {
        [JsonPropertyName("taskId")]
        public int TaskID { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectID { get; set; }

        [JsonPropertyName("taskName")]
        public string TaskName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("durationType")]
        public string? DurationType { get; set; }

        [JsonPropertyName("estimatedValue")]
        public decimal EstimatedValue { get; set; }

        [JsonPropertyName("startDate")]
        public DateOnly? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly? EndDate { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
