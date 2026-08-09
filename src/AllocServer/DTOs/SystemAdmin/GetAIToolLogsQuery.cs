namespace AllocServer.DTOs.SystemAdmin
{
    public class GetAIToolLogsQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int? WorkspaceId { get; set; }
        public string? ToolName { get; set; }
        public bool? IsSuccess { get; set; }
    }
}
