using System;

namespace AllocServer.DTOs.WorkspaceMemberProfiles
{
    public class WorkspaceMemberProfileResponse
    {
        public int ProfileID { get; set; }
        public int WorkspaceMemberID { get; set; }
        public int PriorExperienceYears { get; set; }
        public int TotalExperienceYears { get; set; }
        public string? EducationLevel { get; set; }
        public decimal TechnicalSkillScore { get; set; }
        public decimal CommunicationScore { get; set; }
        public decimal LeadershipScore { get; set; }
        public decimal ProblemSolvingScore { get; set; }
        public decimal AvgSoftSkillScore { get; set; }
        public decimal AttendanceRate { get; set; }
        public decimal ConflictRate { get; set; }
        public string PerformanceRating { get; set; } = null!;
        public DateTime LastEvaluatedAt { get; set; }
    }
}
