using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class PythonPersonnelAssessmentRequest
    {
        [JsonPropertyName("request_type")]
        public string RequestType { get; set; } = "single";

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = "openai";

        [JsonPropertyName("model")]
        public string Model { get; set; } = "gpt-4o-mini";

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("experience_years")]
        public double ExperienceYears { get; set; }

        [JsonPropertyName("education_level")]
        public string EducationLevel { get; set; } = string.Empty;

        [JsonPropertyName("skill_level")]
        public string SkillLevel { get; set; } = string.Empty;

        [JsonPropertyName("technical_skill_score")]
        public double TechnicalSkillScore { get; set; }

        [JsonPropertyName("communication_score")]
        public double CommunicationScore { get; set; }

        [JsonPropertyName("leadership_score")]
        public double LeadershipScore { get; set; }

        [JsonPropertyName("problem_solving_score")]
        public double ProblemSolvingScore { get; set; }

        [JsonPropertyName("task_name")]
        public string TaskName { get; set; } = string.Empty;

        [JsonPropertyName("task_complexity")]
        public string TaskComplexity { get; set; } = string.Empty;

        [JsonPropertyName("required_skill_level")]
        public string RequiredSkillLevel { get; set; } = string.Empty;

        [JsonPropertyName("deadline_days")]
        public int DeadlineDays { get; set; }

        [JsonPropertyName("workload_hours")]
        public double WorkloadHours { get; set; }

        [JsonPropertyName("task_priority")]
        public string TaskPriority { get; set; } = string.Empty;

        [JsonPropertyName("team_size")]
        public int TeamSize { get; set; }

        [JsonPropertyName("attendance_rate")]
        public double AttendanceRate { get; set; }

        [JsonPropertyName("performance_rating")]
        public string PerformanceRating { get; set; } = string.Empty;

        [JsonPropertyName("conflict_rate")]
        public double ConflictRate { get; set; }

        [JsonPropertyName("skills")]
        public List<PythonSkillItemDto> Skills { get; set; } = new();
    }
}
