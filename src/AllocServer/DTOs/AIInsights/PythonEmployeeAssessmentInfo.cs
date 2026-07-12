using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class PythonEmployeeAssessmentInfo
    {
        [JsonPropertyName("employee_id")]
        public string EmployeeId { get; set; } = string.Empty;

        [JsonPropertyName("employee_name")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonPropertyName("experience_years")]
        public double ExperienceYears { get; set; }

        [JsonPropertyName("skill_level")]
        public string SkillLevel { get; set; } = string.Empty;

        [JsonPropertyName("technical_skill_score")]
        public double TechnicalSkillScore { get; set; }

        [JsonPropertyName("communication_score")]
        public double CommunicationScore { get; set; }
    }
}
