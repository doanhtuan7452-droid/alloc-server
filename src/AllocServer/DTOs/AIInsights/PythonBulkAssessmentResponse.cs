using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class PythonBulkAssessmentResponse
    {
        [JsonPropertyName("results")]
        public List<PythonPersonnelAssessmentResponse> Results { get; set; } = new();

        [JsonPropertyName("usage")]
        public PythonUsageInfo? Usage { get; set; }
    }

    public class PythonUsageInfo
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
