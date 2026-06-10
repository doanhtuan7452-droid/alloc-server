using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    public class WorkspaceMemberProfile
    {
        [Key]
        public int ProfileID { get; set; }

        [Required]
        public int WorkspaceMemberID { get; set; }

        [ForeignKey("WorkspaceMemberID")]
        public WorkspaceMember WorkspaceMember { get; set; } = null!;

        /// <summary>
        /// Represents the years of experience before joining the workspace (Prior Experience).
        /// </summary>
        public int ExperienceYears { get; set; } = 0;

        [StringLength(50)]
        public string? EducationLevel { get; set; } // 'High School', 'Diploma', 'Bachelor', 'Master', 'PhD'

        public decimal TechnicalSkillScore { get; set; } = 0;
        public decimal CommunicationScore { get; set; } = 0;
        public decimal LeadershipScore { get; set; } = 0;
        public decimal ProblemSolvingScore { get; set; } = 0;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal AvgSoftSkillScore { get; private set; }

        public decimal AttendanceRate { get; set; } = 100.00m;
        public decimal ConflictRate { get; set; } = 0.00m;

        [StringLength(20)]
        public string PerformanceRating { get; set; } = "Average"; // 'Poor', 'Average', 'Excellent', 'Outstanding'

        public DateTime LastEvaluatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}
