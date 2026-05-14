using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("LeaveRequests")]
    public class LeaveRequest
    {
        [Key]
        public int RequestID { get; set; }

        [Required]
        public int WorkspaceMemberID { get; set; }

        [ForeignKey("WorkspaceMemberID")]
        public WorkspaceMember? WorkspaceMember { get; set; }

        public int? ApproverID { get; set; }

        [ForeignKey("ApproverID")]
        public WorkspaceMember? Approver { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public string? Reason { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? ApprovalNote { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
