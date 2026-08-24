using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Skills
{
    public class PagedSkillsResponse
    {
        [JsonPropertyName("items")]
        public List<SkillResponse> Items { get; set; } = new();

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
}
