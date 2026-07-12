using System;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class CreateTaskToolPayload
    {
        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("taskName")]
        public string TaskName { get; set; } = string.Empty;

        [JsonPropertyName("estimatedValue")]
        public decimal EstimatedValue { get; set; }

        [JsonPropertyName("durationType")]
        public string DurationType { get; set; } = string.Empty;

        [JsonPropertyName("startDate")]
        public DateOnly? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly? EndDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "To-do";

        [JsonPropertyName("complexity")]
        public string Complexity { get; set; } = "Medium";

        [JsonPropertyName("requiredSkillLevel")]
        public string RequiredSkillLevel { get; set; } = "Medium";

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "Medium";

        [JsonPropertyName("expectedTeamSize")]
        public int ExpectedTeamSize { get; set; } = 1;
    }
}
