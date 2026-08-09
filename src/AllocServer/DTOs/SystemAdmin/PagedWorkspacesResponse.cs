using System.Collections.Generic;

namespace AllocServer.DTOs.SystemAdmin
{
    public class PagedWorkspacesResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<WorkspaceAdminListItemResponse> Items { get; set; } = new();
    }
}
