using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Tasks")]
    public class ProjectTask
    {
        [Key]
        public int TaskID { get; set; }

        [Required]
        public int ProjectID { get; set; }

        [ForeignKey("ProjectID")]
        public Project? Project { get; set; }

        [Required]
        [StringLength(255)]
        public string TaskName { get; set; } = string.Empty;

        [StringLength(50)]
        public string Status { get; set; } = "To-do";

        [StringLength(20)]
        public string? DurationType { get; set; }

        [Required]
        [StringLength(20)]
        public string Complexity { get; set; } = "Medium";

        [Required]
        [StringLength(20)]
        public string RequiredSkillLevel { get; set; } = "Medium";

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [Required]
        public int ExpectedTeamSize { get; set; } = 1;

        public decimal EstimatedValue { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
