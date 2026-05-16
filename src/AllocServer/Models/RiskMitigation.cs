using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("RiskMitigations")]
    public class RiskMitigation
    {
        [Key]
        public int MitigationID { get; set; }

        [Required]
        public int RiskID { get; set; }

        [ForeignKey("RiskID")]
        public Risk? Risk { get; set; }

        [Required]
        [StringLength(50)]
        public string StrategyType { get; set; } = "Avoid";

        [Required]
        public string ActionPlan { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MitigationCost { get; set; }

        public int? AssignedMemberID { get; set; }

        [ForeignKey("AssignedMemberID")]
        public WorkspaceMember? AssignedMember { get; set; }

        public DateOnly? TargetDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Planned";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
