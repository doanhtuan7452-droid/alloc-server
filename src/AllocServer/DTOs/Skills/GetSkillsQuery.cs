using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Skills
{
    public class GetSkillsQuery
    {
        [JsonPropertyName("searchTerm")]
        public string? SearchTerm { get; set; }

        [JsonPropertyName("pageNumber")]
        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        [JsonPropertyName("pageSize")]
        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 20;

        [JsonPropertyName("all")]
        public bool All { get; set; } = false;
    }
}
