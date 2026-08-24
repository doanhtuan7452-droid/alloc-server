using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class UpdateAILogFeedbackRequest
    {
        [JsonPropertyName("userFeedback")]
        public string? UserFeedback { get; set; }

        [Range(0, 3, ErrorMessage = "CorrectedRiskLevel must be between 0 (Low) and 3 (Critical).")]
        [JsonPropertyName("correctedRiskLevel")]
        public int? CorrectedRiskLevel { get; set; }

        [JsonPropertyName("isVerified")]
        public bool? IsVerified { get; set; }
    }
}
