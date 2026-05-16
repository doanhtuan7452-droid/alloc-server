using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class AIAskResponse
    {
        [JsonPropertyName("logId")]
        public int LogId { get; set; }

        [JsonPropertyName("projectId")]
        public int? ProjectId { get; set; }

        [JsonPropertyName("analysisType")]
        public string AnalysisType { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("remainingQuota")]
        public int? RemainingQuota { get; set; }
    }
}
