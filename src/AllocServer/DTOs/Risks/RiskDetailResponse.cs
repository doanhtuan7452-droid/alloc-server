using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Risks
{
    public class RiskDetailResponse
    {
        [JsonPropertyName("riskId")]
        public int RiskId { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("taskId")]
        public int? TaskId { get; set; }

        [JsonPropertyName("riskName")]
        public string RiskName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("probability")]
        public int Probability { get; set; }

        [JsonPropertyName("impact")]
        public int Impact { get; set; }

        [JsonPropertyName("riskScore")]
        public int RiskScore { get; set; }

        [JsonPropertyName("estimatedFinancialImpact")]
        public decimal EstimatedFinancialImpact { get; set; }

        [JsonPropertyName("actualFinancialImpact")]
        public decimal ActualFinancialImpact { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("ownerId")]
        public int? OwnerId { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
