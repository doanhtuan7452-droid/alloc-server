using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class AIAllocationAssessmentResultDto
    {
        [JsonPropertyName("workspaceMemberId")]
        public int WorkspaceMemberId { get; set; }

        [JsonPropertyName("employeeCode")]
        public string EmployeeCode { get; set; } = string.Empty;

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("fitPercentage")]
        public double FitPercentage { get; set; }

        [JsonPropertyName("predictionLabel")]
        public string PredictionLabel { get; set; } = string.Empty;

        [JsonPropertyName("businessStatusText")]
        public string BusinessStatusText { get; set; } = string.Empty;

        [JsonPropertyName("llmInsight")]
        public string LlmInsight { get; set; } = string.Empty;

        [JsonPropertyName("successFactors")]
        public List<string> SuccessFactors { get; set; } = new();

        [JsonPropertyName("potentialChallenges")]
        public List<string> PotentialChallenges { get; set; } = new();

        [JsonPropertyName("matchedSkills")]
        public List<PythonSkillItemDto> MatchedSkills { get; set; } = new();

        [JsonPropertyName("semanticSkillScore")]
        public double? SemanticSkillScore { get; set; }

        [JsonPropertyName("isMarginalMatch")]
        public bool IsMarginalMatch { get; set; }
    }
}
