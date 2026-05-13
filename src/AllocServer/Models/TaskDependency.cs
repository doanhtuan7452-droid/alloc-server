using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("TaskDependencies")]
    public class TaskDependency
    {
        [Key]
        public int DependencyID { get; set; }

        [Required]
        public int PredecessorTaskID { get; set; }

        [ForeignKey("PredecessorTaskID")]
        public ProjectTask? PredecessorTask { get; set; }

        [Required]
        public int SuccessorTaskID { get; set; }

        [ForeignKey("SuccessorTaskID")]
        public ProjectTask? SuccessorTask { get; set; }

        [Required]
        [StringLength(10)]
        public string DependencyType { get; set; } = "FS";
    }
}
