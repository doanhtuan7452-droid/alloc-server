using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Risks
{
    public class PagedProjectRisksResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("items")]
        public List<RiskListItemResponse> Items { get; set; } = new();
    }
}
