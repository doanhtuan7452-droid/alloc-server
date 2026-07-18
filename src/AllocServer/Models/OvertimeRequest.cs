using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("OTRequests")]
    public class OvertimeRequest
    {
        [Key]
        public int OTRequestID { get; set; }

        [Required]
        public int WorkspaceMemberID { get; set; }

        [ForeignKey("WorkspaceMemberID")]
        public WorkspaceMember? WorkspaceMember { get; set; }

        public int? TaskID { get; set; }

        [ForeignKey("TaskID")]
        public ProjectTask? Task { get; set; }

        public int? ProjectID { get; set; }

        [ForeignKey("ProjectID")]
        public Project? Project { get; set; }

        public DateOnly RequestedDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal ExpectedHours { get; set; }

        public int? ApproverID { get; set; }

        [ForeignKey("ApproverID")]
        public WorkspaceMember? Approver { get; set; }

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
