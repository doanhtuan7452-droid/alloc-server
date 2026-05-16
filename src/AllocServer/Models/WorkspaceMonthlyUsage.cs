using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("WorkspaceMonthlyUsages")]
    public class WorkspaceMonthlyUsage
    {
        [Key]
        public int UsageID { get; set; }

        [Required]
        public int WorkspaceID { get; set; }

        [ForeignKey("WorkspaceID")]
        public Workspace? Workspace { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateOnly BillingMonth { get; set; }

        public int AIQueryCount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StorageUsedMB { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
