using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Risks")]
    public class Risk
    {
        [Key]
        public int RiskID { get; set; }

        [Required]
        public int ProjectID { get; set; }

        [ForeignKey("ProjectID")]
        public Project? Project { get; set; }

        public int? TaskID { get; set; }

        [ForeignKey("TaskID")]
        public ProjectTask? Task { get; set; }

        public int? AILogID { get; set; }

        [ForeignKey("AILogID")]
        public AILog? AILog { get; set; }

        [Required]
        [StringLength(255)]
        public string RiskName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public int Probability { get; set; }

        public int Impact { get; set; }

        public int RiskScore { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedFinancialImpact { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualFinancialImpact { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Identified";

        public int? OwnerID { get; set; }

        [ForeignKey("OwnerID")]
        public WorkspaceMember? Owner { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
