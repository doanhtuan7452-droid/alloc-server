using System.Collections.Generic;

namespace AllocServer.DTOs.SystemAdmin
{
    public class PagedAIToolLogsResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<AIToolLogListItemResponse> Items { get; set; } = new();
    }
}
