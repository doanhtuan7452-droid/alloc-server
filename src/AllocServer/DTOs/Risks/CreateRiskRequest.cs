using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Risks
{
    public class CreateRiskRequest
    {
        [JsonPropertyName("taskId")]
        public int? TaskId { get; set; }

        [Required(ErrorMessage = "RiskName la bat buoc.")]
        [StringLength(255, ErrorMessage = "RiskName toi da 255 ky tu.")]
        [JsonPropertyName("riskName")]
        public string RiskName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [Required(ErrorMessage = "Probability la bat buoc.")]
        [Range(1, 5, ErrorMessage = "Probability phai tu 1 den 5.")]
        [JsonPropertyName("probability")]
        public int Probability { get; set; }

        [Required(ErrorMessage = "Impact la bat buoc.")]
        [Range(1, 5, ErrorMessage = "Impact phai tu 1 den 5.")]
        [JsonPropertyName("impact")]
        public int Impact { get; set; }

        [Range(0, 9999999999999999.99, ErrorMessage = "EstimatedFinancialImpact phai >= 0.")]
        [JsonPropertyName("estimatedFinancialImpact")]
        public decimal EstimatedFinancialImpact { get; set; }

        [JsonPropertyName("ownerId")]
        public int? OwnerId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
