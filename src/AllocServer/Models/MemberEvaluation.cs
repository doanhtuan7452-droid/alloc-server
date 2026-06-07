using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    public class MemberEvaluation
    {
        [Key]
        public int EvaluationID { get; set; }

        [Required]
        public int CycleID { get; set; }

        [ForeignKey("CycleID")]
        public ReviewCycle ReviewCycle { get; set; } = null!;

        [Required]
        public int RevieweeID { get; set; }

        [ForeignKey("RevieweeID")]
        public WorkspaceMember Reviewee { get; set; } = null!;

        [Required]
        public int ReviewerID { get; set; }

        [ForeignKey("ReviewerID")]
        public WorkspaceMember Reviewer { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string EvaluationType { get; set; } = null!; // 'Self', 'Manager', 'Peer'

        public decimal CommunicationScore { get; set; } = 0;
        public decimal LeadershipScore { get; set; } = 0;
        public decimal ProblemSolvingScore { get; set; } = 0;

        public string? FeedbackNotes { get; set; }

        public DateTime? SubmittedAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // 'Pending', 'Submitted'
    }
}
