using System;

namespace AllocServer.DTOs.WorkspaceMemberProfiles
{
    public class MemberEvaluationResponse
    {
        public int EvaluationID { get; set; }
        public int CycleID { get; set; }
        public int RevieweeID { get; set; }
        public int ReviewerID { get; set; }
        public string EvaluationType { get; set; } = null!;
        public decimal CommunicationScore { get; set; }
        public decimal LeadershipScore { get; set; }
        public decimal ProblemSolvingScore { get; set; }
        public string? FeedbackNotes { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string Status { get; set; } = null!;
    }
}
