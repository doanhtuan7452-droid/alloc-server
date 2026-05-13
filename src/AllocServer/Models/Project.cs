using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Projects")]
    public class Project
    {
        [Key]
        public int ProjectID { get; set; }

        [Required]
        public int WorkspaceID { get; set; }

        [ForeignKey("WorkspaceID")]
        public Workspace? Workspace { get; set; }

        [Required]
        [StringLength(255)]
        public string ProjectName { get; set; } = string.Empty;

        public decimal ExpectedBudget { get; set; }

        public decimal TotalRevenue { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Planning";

        public string? BaselineData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
