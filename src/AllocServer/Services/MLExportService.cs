using AllocServer.Data;
using AllocServer.DTOs.MLExport;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AllocServer.Services
{
    public class MLExportService
    {
        private readonly ApplicationDbContext _context;

        public MLExportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedMLExportResponse<PersonnelTrainingRow>> GetPersonnelTrainingDataAsync(
            int? workspaceId,
            int? projectId,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize)
        {
            // Query completed tasks assignments (Tasks.Status == "Done")
            var query = from assignee in _context.TaskAssignees
                        join task in _context.ProjectTasks on assignee.TaskID equals task.TaskID
                        join project in _context.Projects on task.ProjectID equals project.ProjectID
                        join member in _context.WorkspaceMembers.Include(m => m.Resource) on assignee.WorkspaceMemberID equals member.WorkspaceMemberID
                        join profile in _context.WorkspaceMemberProfiles on member.WorkspaceMemberID equals profile.WorkspaceMemberID
                        where task.Status == "Done"
                           && !task.IsDeleted
                           && !project.IsDeleted
                           && member.Status == "Active"
                           && !member.Resource.IsDeleted
                           && !profile.IsDeleted
                        select new { assignee, task, project, member, profile };

            if (workspaceId.HasValue)
            {
                query = query.Where(x => x.project.WorkspaceID == workspaceId.Value);
            }

            if (projectId.HasValue)
            {
                query = query.Where(x => x.project.ProjectID == projectId.Value);
            }

            if (startDate.HasValue)
            {
                var startOnly = DateOnly.FromDateTime(startDate.Value);
                query = query.Where(x => (x.task.EndDate >= startOnly) || (x.task.EndDate == null && x.task.CreatedAt >= startDate.Value));
            }

            if (endDate.HasValue)
            {
                var endOnly = DateOnly.FromDateTime(endDate.Value);
                query = query.Where(x => (x.task.EndDate <= endOnly) || (x.task.EndDate == null && x.task.CreatedAt <= endDate.Value));
            }

            int totalCount = await query.CountAsync();

            var dbData = await query
                .OrderBy(x => x.task.TaskID)
                .ThenBy(x => x.member.WorkspaceMemberID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dataRows = new List<PersonnelTrainingRow>();

            var memberIds = dbData.Select(x => x.member.WorkspaceMemberID).Distinct().ToList();

            var evaluations = await _context.MemberEvaluations
                .Include(e => e.ReviewCycle)
                .Where(e => memberIds.Contains(e.RevieweeID) && e.Status == "Submitted")
                .ToListAsync();

            foreach (var item in dbData)
            {
                var t = item.task;
                var p = item.project;
                var m = item.member;
                var prof = item.profile;

                // Base employee fields
                double expYears = (double)prof.ExperienceYears;
                string eduLevel = prof.EducationLevel?.ToLowerInvariant() ?? "bachelor";
                double techScore = (double)prof.TechnicalSkillScore;
                double commScore = (double)prof.CommunicationScore;
                double leadScore = (double)prof.LeadershipScore;
                double pbScore = (double)prof.ProblemSolvingScore;
                double attRate = (double)prof.AttendanceRate;
                double confRate = (double)prof.ConflictRate;

                // Derived Skill level
                string sLevel = "medium";
                if (techScore < 40.0)
                {
                    sLevel = "low";
                }
                else if (techScore >= 75.0)
                {
                    sLevel = "high";
                }

                // Performance rating mapping
                string perfRating = "good";
                if (!string.IsNullOrEmpty(prof.PerformanceRating))
                {
                    perfRating = prof.PerformanceRating.ToUpperInvariant() switch
                    {
                        "POOR" => "poor",
                        "AVERAGE" => "good",
                        "EXCELLENT" or "OUTSTANDING" => "excellent",
                        _ => "good"
                    };
                }

                // Task fields
                string taskComplexity = t.Complexity?.ToLowerInvariant() ?? "medium";
                string requiredSkillLevel = t.RequiredSkillLevel?.ToLowerInvariant() ?? "medium";
                string taskPriority = t.Priority?.ToLowerInvariant() ?? "medium";
                int teamSize = t.ExpectedTeamSize <= 0 ? 3 : t.ExpectedTeamSize;

                // Deadline days calculation
                int deadlineDays = 14;
                if (t.StartDate.HasValue && t.EndDate.HasValue)
                {
                    deadlineDays = t.EndDate.Value.DayNumber - t.StartDate.Value.DayNumber;
                }
                if (deadlineDays <= 0)
                {
                    deadlineDays = 1;
                }

                // Workload hours calculation
                double workloadHours = 40.0;
                if (!string.IsNullOrEmpty(t.DurationType))
                {
                    if (string.Equals(t.DurationType, "Hour", StringComparison.OrdinalIgnoreCase))
                    {
                        workloadHours = (double)t.EstimatedValue;
                    }
                    else if (string.Equals(t.DurationType, "Day", StringComparison.OrdinalIgnoreCase) || 
                             string.Equals(t.DurationType, "StoryPoint", StringComparison.OrdinalIgnoreCase))
                    {
                        workloadHours = (double)(t.EstimatedValue * 8.0m);
                    }
                }

                // Derived ML features
                double hoursPerDay = workloadHours / (deadlineDays + 1e-5);
                double avgSoftSkill = (commScore + leadScore + pbScore) / 3.0;

                // Skill codes for skill gap
                int skillCode = sLevel switch { "low" => 0, "medium" => 1, "high" => 2, "expert" => 3, _ => 1 };
                int reqSkillCode = requiredSkillLevel switch { "low" => 0, "medium" => 1, "high" => 2, "expert" => 3, _ => 1 };
                double skillGap = skillCode - reqSkillCode;

                // Fetch latest manager evaluation for this member
                var memberEvals = evaluations.Where(e => e.RevieweeID == m.WorkspaceMemberID).ToList();
                double latestManagerComm = commScore;
                double latestManagerLead = leadScore;
                double latestManagerProb = pbScore;
                double evalGapManagerSelf = 0.0;

                if (memberEvals.Any())
                {
                    var latestEvalByManager = memberEvals
                        .Where(e => e.EvaluationType == "Manager")
                        .OrderByDescending(e => e.ReviewCycle.EndDate)
                        .FirstOrDefault();

                    if (latestEvalByManager != null)
                    {
                        latestManagerComm = (double)latestEvalByManager.CommunicationScore;
                        latestManagerLead = (double)latestEvalByManager.LeadershipScore;
                        latestManagerProb = (double)latestEvalByManager.ProblemSolvingScore;

                        var selfEval = memberEvals.FirstOrDefault(e => e.CycleID == latestEvalByManager.CycleID && e.EvaluationType == "Self");
                        if (selfEval != null)
                        {
                            double mgrAvg = (latestManagerComm + latestManagerLead + latestManagerProb) / 3.0;
                            double selfAvg = ((double)selfEval.CommunicationScore + (double)selfEval.LeadershipScore + (double)selfEval.ProblemSolvingScore) / 3.0;
                            evalGapManagerSelf = mgrAvg - selfAvg;
                        }
                    }
                }

                // Targets from AILogs feedback (if exists for this task)
                double? fitPercentage = null;
                string? allocationStatus = null;

                // Check if there is an AILog entry that represents a prediction for this task/member
                var aiLog = await _context.AILogs
                    .Where(l => l.ProjectID == p.ProjectID && l.SuggestionType == "Resource Suggestion" && l.ModelOutputJson != null)
                    .OrderByDescending(l => l.CreatedAt)
                    .FirstOrDefaultAsync();

                if (aiLog != null && !string.IsNullOrEmpty(aiLog.ModelOutputJson))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(aiLog.ModelOutputJson);
                        if (doc.RootElement.TryGetProperty("results", out var resultsProp) && resultsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var itemNode in resultsProp.EnumerateArray())
                            {
                                if (itemNode.TryGetProperty("employee_id", out var empIdProp) && empIdProp.GetString() == m.EmployeeCode)
                                {
                                    if (itemNode.TryGetProperty("fit_percentage", out var fitProp))
                                    {
                                        fitPercentage = fitProp.GetDouble();
                                    }
                                    if (itemNode.TryGetProperty("prediction_label", out var labelProp))
                                    {
                                        allocationStatus = labelProp.GetString();
                                    }
                                    break;
                                }
                            }
                        }
                        else if (doc.RootElement.TryGetProperty("fit_percentage", out var fitProp) && doc.RootElement.TryGetProperty("prediction_label", out var labelProp))
                        {
                            // In case of single assessment output
                            fitPercentage = fitProp.GetDouble();
                            allocationStatus = labelProp.GetString();
                        }
                    }
                    catch
                    {
                        // Ignore parse errors, fallback to null targets
                    }
                }

                dataRows.Add(new PersonnelTrainingRow
                {
                    ExperienceYears = expYears,
                    EducationLevel = eduLevel,
                    SkillLevel = sLevel,
                    TechnicalSkillScore = techScore,
                    CommunicationScore = commScore,
                    LeadershipScore = leadScore,
                    ProblemSolvingScore = pbScore,
                    TaskComplexity = taskComplexity,
                    RequiredSkillLevel = requiredSkillLevel,
                    DeadlineDays = deadlineDays,
                    WorkloadHours = workloadHours,
                    TaskPriority = taskPriority,
                    TeamSize = teamSize,
                    AttendanceRate = attRate,
                    PerformanceRating = perfRating,
                    ConflictRate = confRate,
                    HoursPerDay = hoursPerDay,
                    SkillGap = skillGap,
                    AvgSoftSkill = avgSoftSkill,
                    LatestManagerCommScore = latestManagerComm,
                    LatestManagerLeadScore = latestManagerLead,
                    LatestManagerProbScore = latestManagerProb,
                    EvaluationGapManagerSelf = evalGapManagerSelf,
                    FitPercentage = fitPercentage,
                    AllocationStatus = allocationStatus
                });
            }

            return new PagedMLExportResponse<PersonnelTrainingRow>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Data = dataRows
            };
        }

        public async Task<PagedMLExportResponse<ProjectRiskTrainingRow>> GetProjectRiskTrainingDataAsync(
            int? workspaceId,
            string? status,
            int page,
            int pageSize)
        {
            var query = _context.ProjectRiskFeatures.AsQueryable();

            if (workspaceId.HasValue)
            {
                query = from f in query
                        join p in _context.Projects on f.ProjectID equals p.ProjectID
                        where p.WorkspaceID == workspaceId.Value
                        select f;
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = from f in query
                        join p in _context.Projects on f.ProjectID equals p.ProjectID
                        where p.Status == status
                        select f;
            }

            int totalCount = await query.CountAsync();

            var dbData = await query
                .OrderBy(x => x.ProjectID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dataRows = new List<ProjectRiskTrainingRow>();

            foreach (var item in dbData)
            {
                string methodology = item.Methodology_Used?.ToLowerInvariant() ?? "agile";

                int methodKanban = methodology == "kanban" ? 1 : 0;
                int methodScrum = (methodology == "scrum" || methodology == "agile") ? 1 : 0;
                int methodWaterfall = methodology == "waterfall" ? 1 : 0;
                int methodHybrid = methodology == "hybrid" ? 1 : 0;

                // Try to find the target prediction_code from AILogs
                int? predictionCode = null;
                var aiLog = await _context.AILogs
                    .Where(l => l.ProjectID == item.ProjectID && l.SuggestionType == "Risk Warning" && l.ModelOutputJson != null)
                    .OrderByDescending(l => l.CreatedAt)
                    .FirstOrDefaultAsync();

                if (aiLog != null && !string.IsNullOrEmpty(aiLog.ModelOutputJson))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(aiLog.ModelOutputJson);
                        if (doc.RootElement.TryGetProperty("prediction_code", out var codeProp))
                        {
                            predictionCode = codeProp.GetInt32();
                        }
                    }
                    catch
                    {
                        // Ignore parse error
                    }
                }

                dataRows.Add(new ProjectRiskTrainingRow
                {
                    ProjectID = item.ProjectID,
                    ProjectName = item.Project_Name,
                    ProjectDurationDays = item.Project_Duration_Days,
                    ExpectedBudget = (double)item.Expected_Budget,
                    TeamSize = item.Team_Size,
                    AvgTeamSkillLevel = item.Avg_Team_Skill_Level,
                    ComplexityScore = item.Raw_Complexity_Score,
                    BudgetUtilization = (double)item.Budget_Utilization_Rate,
                    MethodologyUsedHybrid = methodHybrid,
                    MethodologyUsedKanban = methodKanban,
                    MethodologyUsedScrum = methodScrum,
                    MethodologyUsedWaterfall = methodWaterfall,
                    OverallRiskScore = item.Overall_Risk_Score,
                    PredictionCode = predictionCode
                });
            }

            return new PagedMLExportResponse<ProjectRiskTrainingRow>
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Data = dataRows
            };
        }
    }
}
