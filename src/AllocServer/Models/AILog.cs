using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("AILogs")]
    public class AILog
    {
        [Key]
        public int LogID { get; set; }

        public int? ProjectID { get; set; }

        [ForeignKey("ProjectID")]
        public Project? Project { get; set; }

        [Required]
        [StringLength(50)]
        public string SuggestionType { get; set; } = string.Empty;

        [Required]
        public string SuggestionContent { get; set; } = string.Empty;

        [StringLength(50)]
        public string? UserFeedback { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
