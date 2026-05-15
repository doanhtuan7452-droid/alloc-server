using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Risks
{
    public class GetProjectRisksQuery
    {
        [Range(1, int.MaxValue)]
        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 20;

        [JsonPropertyName("search")]
        public string? Search { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("taskId")]
        public int? TaskId { get; set; }

        [JsonPropertyName("ownerId")]
        public int? OwnerId { get; set; }

        [JsonPropertyName("minScore")]
        public int? MinScore { get; set; }

        [JsonPropertyName("maxScore")]
        public int? MaxScore { get; set; }
    }
}
