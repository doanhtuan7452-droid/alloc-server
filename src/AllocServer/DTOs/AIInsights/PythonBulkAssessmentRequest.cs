using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class PythonBulkAssessmentRequest
    {
        [JsonPropertyName("request_type")]
        public string RequestType { get; set; } = "bulk";

        [JsonPropertyName("task_name")]
        public string TaskName { get; set; } = string.Empty;

        [JsonPropertyName("task_complexity")]
        public string TaskComplexity { get; set; } = string.Empty;

        [JsonPropertyName("deadline_days")]
        public int DeadlineDays { get; set; }

        [JsonPropertyName("required_skill_level")]
        public string RequiredSkillLevel { get; set; } = string.Empty;

        [JsonPropertyName("workload_hours")]
        public double WorkloadHours { get; set; }

        [JsonPropertyName("task_priority")]
        public string TaskPriority { get; set; } = string.Empty;

        [JsonPropertyName("team_size")]
        public int TeamSize { get; set; }

        [JsonPropertyName("employees")]
        public List<PythonEmployeeAssessmentInfo> Employees { get; set; } = new();

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = "openai";

        [JsonPropertyName("model")]
        public string Model { get; set; } = "gpt-4o-mini";

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }
}
