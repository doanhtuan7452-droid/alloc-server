using System;

namespace AllocServer.DTOs.SystemAdmin
{
    public class AIToolLogListItemResponse
    {
        public int LogId { get; set; }
        public int? WorkspaceId { get; set; }
        public string? WorkspaceName { get; set; }
        public int? AccountId { get; set; }
        public string? Email { get; set; }
        public string ToolName { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public long ExecutionTimeMs { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
