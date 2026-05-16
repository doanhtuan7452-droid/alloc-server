using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class GetProjectAIInsightsQuery
    {
        [Range(1, int.MaxValue)]
        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 20;

        [StringLength(50)]
        [JsonPropertyName("suggestionType")]
        public string? SuggestionType { get; set; }

        [StringLength(50)]
        [JsonPropertyName("userFeedback")]
        public string? UserFeedback { get; set; }
    }
}
