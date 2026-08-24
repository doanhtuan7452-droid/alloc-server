using System.Collections.Generic;

namespace AllocServer.DTOs.MLExport
{
    public class PersonnelSkillExportDto
    {
        public string SkillName { get; set; } = string.Empty;
        public int Level { get; set; }
    }

    public class PersonnelTrainingRow
    {
        public string TaskName { get; set; } = string.Empty;
        public List<PersonnelSkillExportDto> Skills { get; set; } = new();
        public double ExperienceYears { get; set; }
        public string EducationLevel { get; set; } = string.Empty;
        public string SkillLevel { get; set; } = string.Empty;
        public double TechnicalSkillScore { get; set; }
        public double CommunicationScore { get; set; }
        public double LeadershipScore { get; set; }
        public double ProblemSolvingScore { get; set; }
        public string TaskComplexity { get; set; } = string.Empty;
        public string RequiredSkillLevel { get; set; } = string.Empty;
        public int DeadlineDays { get; set; }
        public double WorkloadHours { get; set; }
        public string TaskPriority { get; set; } = string.Empty;
        public int TeamSize { get; set; }
        public double AttendanceRate { get; set; }
        public string PerformanceRating { get; set; } = string.Empty;
        public double ConflictRate { get; set; }
        public double HoursPerDay { get; set; }
        public double SkillGap { get; set; }
        public double AvgSoftSkill { get; set; }

        // Periodic evaluation fields
        public double LatestManagerCommScore { get; set; }
        public double LatestManagerLeadScore { get; set; }
        public double LatestManagerProbScore { get; set; }
        public double EvaluationGapManagerSelf { get; set; }

        // Target labels (ground truth)
        public double? FitPercentage { get; set; } = null;
        public string? AllocationStatus { get; set; } = null;
    }

    public class ProjectRiskTrainingRow
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int ProjectDurationDays { get; set; }
        public double ExpectedBudget { get; set; }
        public int TeamSize { get; set; }
        public double AvgTeamSkillLevel { get; set; }
        public double ComplexityScore { get; set; }
        public double BudgetUtilization { get; set; }
        public int MethodologyUsedHybrid { get; set; }
        public int MethodologyUsedKanban { get; set; }
        public int MethodologyUsedScrum { get; set; }
        public int MethodologyUsedWaterfall { get; set; }

        public double OverallRiskScore { get; set; }
        public int? PredictionCode { get; set; } = null;
    }

    public class PagedMLExportResponse<T>
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<T> Data { get; set; } = new();
    }
}
