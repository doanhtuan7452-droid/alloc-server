using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models.AI
{
    /// <summary>
    /// Bảng nhật ký gọi Webhook của hệ thống Python AI.
    /// Khác với AILogs (lưu gợi ý AI/Insight theo Project).
    /// </summary>
    [Table("AIToolExecutionLogs")]
    public class AIToolExecutionLog
    {
        [Key]
        public int LogID { get; set; }

        public int? WorkspaceID { get; set; }

        [ForeignKey("WorkspaceID")]
        public Workspace? Workspace { get; set; }

        public int? AccountID { get; set; }

        [ForeignKey("AccountID")]
        public Account? Account { get; set; }

        [Required]
        [StringLength(100)]
        public string ToolName { get; set; } = string.Empty;

        [Required]
        public string Arguments { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }

        public int StatusCode { get; set; }

        [StringLength(50)]
        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public long ExecutionTimeMs { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
