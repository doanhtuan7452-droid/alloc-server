using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Risks
{
    public class RiskMitigationResponse
    {
        [JsonPropertyName("mitigationId")]
        public int MitigationId { get; set; }

        [JsonPropertyName("riskId")]
        public int RiskId { get; set; }

        [JsonPropertyName("strategyType")]
        public string StrategyType { get; set; } = string.Empty;

        [JsonPropertyName("actionPlan")]
        public string ActionPlan { get; set; } = string.Empty;

        [JsonPropertyName("mitigationCost")]
        public decimal MitigationCost { get; set; }

        [JsonPropertyName("assignedMemberId")]
        public int? AssignedMemberId { get; set; }

        [JsonPropertyName("targetDate")]
        public DateOnly? TargetDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
