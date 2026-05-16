using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("RiskLifecycle")]
    public class RiskLifecycle
    {
        [Key]
        public int HistoryID { get; set; }

        [Required]
        public int RiskID { get; set; }

        [ForeignKey("RiskID")]
        public Risk? Risk { get; set; }

        [Required]
        public int ChangedByMemberID { get; set; }

        [ForeignKey("ChangedByMemberID")]
        public WorkspaceMember? ChangedByMember { get; set; }

        [StringLength(50)]
        public string? OldStatus { get; set; }

        [StringLength(50)]
        public string? NewStatus { get; set; }

        public int? OldScore { get; set; }

        public int? NewScore { get; set; }

        public string? ChangeNote { get; set; }

        public DateTime ChangeDate { get; set; } = DateTime.UtcNow;
    }
}
