using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Timesheets
{
    public class GetTimesheetsQuery
    {
        [Range(1, int.MaxValue)]
        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 20;

        [JsonPropertyName("fromDate")]
        public DateOnly? FromDate { get; set; }

        [JsonPropertyName("toDate")]
        public DateOnly? ToDate { get; set; }

        [JsonPropertyName("workspaceId")]
        public int? WorkspaceId { get; set; }

        [JsonPropertyName("projectId")]
        public int? ProjectId { get; set; }

        [JsonPropertyName("taskId")]
        public int? TaskId { get; set; }

        [JsonPropertyName("memberId")]
        public int? MemberId { get; set; }
    }
}
