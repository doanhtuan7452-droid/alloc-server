using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Workspaces
{
    public class GetWorkspaceMembersQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public string? Search { get; set; }
    }
}
