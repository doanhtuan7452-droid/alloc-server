namespace AllocServer.DTOs.SystemAdmin
{
    public class GetWorkspacesQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Keyword { get; set; }
        public string? PlanCode { get; set; }
        public string? Type { get; set; }
    }
}
