using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class AIInsightResponse
    {
        [JsonPropertyName("logId")]
        public int LogId { get; set; }

        [JsonPropertyName("projectId")]
        public int? ProjectId { get; set; }

        [JsonPropertyName("suggestionType")]
        public string SuggestionType { get; set; } = string.Empty;

        [JsonPropertyName("suggestionContent")]
        public string SuggestionContent { get; set; } = string.Empty;

        [JsonPropertyName("userFeedback")]
        public string? UserFeedback { get; set; }

        [JsonPropertyName("correctedRiskLevel")]
        public int? CorrectedRiskLevel { get; set; }

        [JsonPropertyName("isVerified")]
        public bool IsVerified { get; set; }

        [JsonPropertyName("verifiedBy")]
        public int? VerifiedBy { get; set; }

        [JsonPropertyName("verifiedAt")]
        public DateTime? VerifiedAt { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
