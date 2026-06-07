using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.WorkspaceMemberProfiles
{
    public class SubmitEvaluationRequest
    {
        [Required(ErrorMessage = "RevieweeID la bat buoc.")]
        public int RevieweeID { get; set; }

        [Required(ErrorMessage = "ReviewerID la bat buoc.")]
        public int ReviewerID { get; set; }

        [Required(ErrorMessage = "Loai danh gia (Self, Manager, Peer) la bat buoc.")]
        [StringLength(50)]
        public string EvaluationType { get; set; } = null!; // 'Self', 'Manager', 'Peer'

        [Range(0, 100, ErrorMessage = "Diem Communication phai tu 0 den 100.")]
        public decimal CommunicationScore { get; set; }

        [Range(0, 100, ErrorMessage = "Diem Leadership phai tu 0 den 100.")]
        public decimal LeadershipScore { get; set; }

        [Range(0, 100, ErrorMessage = "Diem Problem Solving phai tu 0 den 100.")]
        public decimal ProblemSolvingScore { get; set; }

        public string? FeedbackNotes { get; set; }
    }
}
