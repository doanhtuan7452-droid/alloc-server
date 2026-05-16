using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Risks
{
    public class CreateRiskMitigationRequest
    {
        [Required(ErrorMessage = "StrategyType la bat buoc.")]
        [JsonPropertyName("strategyType")]
        public string StrategyType { get; set; } = string.Empty;

        [Required(ErrorMessage = "ActionPlan la bat buoc.")]
        [JsonPropertyName("actionPlan")]
        public string ActionPlan { get; set; } = string.Empty;

        [Range(0, 9999999999999999.99, ErrorMessage = "MitigationCost phai tu 0 den 9999999999999999.99.")]
        [JsonPropertyName("mitigationCost")]
        public decimal MitigationCost { get; set; }

        [JsonPropertyName("assignedMemberId")]
        public int? AssignedMemberId { get; set; }

        [JsonPropertyName("targetDate")]
        public DateOnly? TargetDate { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
