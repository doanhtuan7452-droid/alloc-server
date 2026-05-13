using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Timesheets")]
    public class Timesheet
    {
        [Key]
        public int TimesheetID { get; set; }

        [Required]
        public int TaskID { get; set; }

        [ForeignKey("TaskID")]
        public ProjectTask? Task { get; set; }

        [Required]
        public int WorkspaceMemberID { get; set; }

        [ForeignKey("WorkspaceMemberID")]
        public WorkspaceMember? WorkspaceMember { get; set; }

        public DateOnly WorkDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal NormalHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal OTHours { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LoggedHourlyRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LoggedOTRate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
