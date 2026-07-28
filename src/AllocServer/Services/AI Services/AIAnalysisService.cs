using AllocServer.Data;
using AllocServer.DTOs.AIInsights;
using AllocServer.Exceptions;
using AllocServer.Constants.Permissions;
using AllocServer.Filters;
using AllocServer.Interfaces;
using AllocServer.Interfaces.AI;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;

namespace AllocServer.Services.AI_Services
{
    public class AIAnalysisService : IAIAnalysisService
    {
        private const string AIChatQuotaFeatureCode = "AI_CHAT_QUOTA";
        private const string AIRiskManagementFeatureCode = "AI_RISK_MGT";

        private static readonly HashSet<string> AllowedAnalysisTypes = new(StringComparer.Ordinal)
        {
            "Risk Warning",
            "Resource Suggestion",
            "Budget Forecast"
        };

        private readonly ApplicationDbContext _context;
        private readonly IFeatureQuotaService _featureQuotaService;
        private readonly IAIProvider _aiProvider;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _llmClient;

        public AIAnalysisService(
            ApplicationDbContext context,
            IFeatureQuotaService featureQuotaService,
            IAIProvider aiProvider,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _featureQuotaService = featureQuotaService;
            _aiProvider = aiProvider;
            _configuration = configuration;
            _llmClient = httpClientFactory.CreateClient("PythonLLMClient");
        }

        public async Task<AIAskResponse> AnalyzeAsync(int accountId, AIAskRequest request)
        {
            var analysisType = NormalizeAnalysisType(request.AnalysisType);
            if (analysisType == null)
            {
                throw new ArgumentException(
                    "AnalysisType chi nhan Risk Warning, Resource Suggestion hoac Budget Forecast.");
            }

            if (analysisType == "Resource Suggestion" && !request.TargetEntityId.HasValue)
            {
                throw new ArgumentException("TargetEntityId (TaskId) la bat buoc doi voi Resource Suggestion.");
            }

            var project = await LoadProjectAsync(request.ProjectId);
            var membership = await ResolveActiveMembershipAsync(accountId, project.WorkspaceID);
            await EnsureAskPermissionAsync(membership);

            if (analysisType == "Risk Warning")
            {
                await EnsureRiskAiFeatureEnabledAsync(project.WorkspaceID);
            }

            var targetEntitySummary = await BuildTargetEntitySummaryAsync(project.ProjectID, request.TargetEntityId);
            var context = new AIAnalysisContext
            {
                AnalysisType = analysisType,
                Prompt = NormalizeNullableText(request.Prompt),
                ProjectSummary = await BuildProjectSummaryAsync(project),
                TargetEntitySummary = targetEntitySummary
            };

            var billingMonth = GetCurrentBillingMonth();
            var effectiveLimit = await ResolveEffectiveQuotaLimitAsync(project.WorkspaceID);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await EnsureMonthlyUsageRowAsync(project.WorkspaceID, billingMonth);

                var newCount = await TryConsumeAIQuotaAsync(
                    project.WorkspaceID,
                    billingMonth,
                    effectiveLimit);

                string content = string.Empty;
                List<AIAllocationAssessmentResultDto>? results = null;

                if (analysisType == "Risk Warning")
                {
                    content = await GetProjectRiskAnalysisAsync(project);
                }
                else if (analysisType == "Resource Suggestion")
                {
                    var allocationResult = await GetResourceAllocationAnalysisAsync(project, request.TargetEntityId, request.WorkspaceMemberIds);
                    content = allocationResult.Content;
                    results = allocationResult.Results;
                }
                else
                {
                    content = await _aiProvider.GenerateAnalysisAsync(context);
                }

                var now = DateTime.UtcNow;
                var log = new AILog
                {
                    ProjectID = project.ProjectID,
                    SuggestionType = analysisType,
                    SuggestionContent = content,
                    CreatedAt = now
                };

                _context.AILogs.Add(log);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new AIAskResponse
                {
                    LogId = log.LogID,
                    ProjectId = log.ProjectID,
                    AnalysisType = log.SuggestionType,
                    Content = log.SuggestionContent,
                    CreatedAt = log.CreatedAt,
                    RemainingQuota = CalculateRemainingQuota(effectiveLimit, newCount),
                    Results = results
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<string> GetProjectRiskAnalysisAsync(Project project)
        {
            // 1. Duration (anchor at project end and start dates)
            int projectDurationDays = Math.Max(project.EndDate.DayNumber - project.StartDate.DayNumber, 0);

            // 2. Expected Budget (normalized to USD)
            double expectedBudgetUsd = (double)(project.ExpectedBudget * project.ExchangeRateToUSD);

            // 3. Team Size (Strict Soft Delete & Member Check)
            int teamSize = await _context.TaskAssignees
                .AsNoTracking()
                .Where(ta => ta.Task.ProjectID == project.ProjectID 
                             && !ta.Task.IsDeleted
                             && ta.WorkspaceMember.Status == "Active"
                             && !ta.WorkspaceMember.Resource.IsDeleted)
                .Select(ta => ta.WorkspaceMemberID)
                .Distinct()
                .CountAsync();

            // 4. Average Team Skill Level (Scaled 1-10)
            var teamMembers = await _context.TaskAssignees
                .AsNoTracking()
                .Where(ta => ta.Task.ProjectID == project.ProjectID 
                             && !ta.Task.IsDeleted
                             && ta.WorkspaceMember.Status == "Active"
                             && !ta.WorkspaceMember.Resource.IsDeleted)
                .Select(ta => new { ta.WorkspaceMemberID, ta.WorkspaceMember.ResourceID })
                .Distinct()
                .ToListAsync();

            double avgTeamSkillLevel;
            if (!teamMembers.Any())
            {
                avgTeamSkillLevel = 6.0; // Fallback: default 3.0 scaled by 2.0
            }
            else
            {
                var resourceIds = teamMembers.Select(m => m.ResourceID).ToList();
                var memberSkills = await _context.ResourceSkills
                    .AsNoTracking()
                    .Where(rs => resourceIds.Contains(rs.ResourceID) && !rs.Skill.IsDeleted)
                    .ToListAsync();

                var skillAverages = new List<double>();
                foreach (var member in teamMembers)
                {
                    var levels = memberSkills
                        .Where(rs => rs.ResourceID == member.ResourceID)
                        .Select(rs => rs.Level)
                        .ToList();
                    
                    double memberAvg = levels.Any() ? levels.Average() : 3.0; // Default is 3.0
                    skillAverages.Add(memberAvg);
                }
                
                avgTeamSkillLevel = skillAverages.Average() * 2.0; // Scale 1-5 to 2-10
            }

            // 5. Complexity Score (Scaled 1-10)
            var complexities = await _context.ProjectTasks
                .AsNoTracking()
                .Where(t => t.ProjectID == project.ProjectID && !t.IsDeleted)
                .Select(t => t.Complexity)
                .ToListAsync();

            double avgComplexity = 5.0; // Medium fallback
            if (complexities.Any())
            {
                var scores = complexities.Select(c => c.ToUpperInvariant() switch
                {
                    "LOW" => 2.0,
                    "MEDIUM" => 5.0,
                    "HIGH" => 8.0,
                    "CRITICAL" => 10.0,
                    _ => 5.0
                });
                avgComplexity = scores.Average();
            }

            // 6. Budget Utilization (Including Labor Cost)
            var totalExpenses = await _context.Expenses
                .AsNoTracking()
                .Where(e => e.ProjectID == project.ProjectID && !e.IsDeleted)
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var totalLaborCost = await _context.Timesheets
                .AsNoTracking()
                .Where(t => t.Task.ProjectID == project.ProjectID && !t.IsDeleted && !t.Task.IsDeleted)
                .SumAsync(t => (decimal?)(t.NormalHours * t.LoggedHourlyRate + t.OTHours * t.LoggedOTRate)) ?? 0m;

            double budgetUtilization = 0.0;
            if (project.ExpectedBudget > 0m)
            {
                budgetUtilization = (double)((totalExpenses + totalLaborCost) / project.ExpectedBudget);
            }

            // 7. Methodology (One-Hot Encoded)
            int methodKanban = string.Equals(project.Methodology, "Kanban", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            int methodScrum = (string.Equals(project.Methodology, "Scrum", StringComparison.OrdinalIgnoreCase) || 
                               string.Equals(project.Methodology, "Agile", StringComparison.OrdinalIgnoreCase)) ? 1 : 0;
            int methodWaterfall = string.Equals(project.Methodology, "Waterfall", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            int methodHybrid = string.Equals(project.Methodology, "Hybrid", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            // 8. Resolve plan and map AI settings (Option 2 - System Decided model)
            var currentLimit = await _context.WorkspaceCurrentLimits
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.WorkspaceID == project.WorkspaceID);
            
            string planCode = currentLimit?.PlanCode ?? "FREE";
            
            string provider = "gemini";
            string model = "gemini-2.0-flash";
            if (planCode.Equals("PRO", StringComparison.OrdinalIgnoreCase))
            {
                model = "gemini-2.0-flash";
            }

            // 9. Prepare Python Project Risk request payload
            var payload = new PythonProjectRiskRequest
            {
                ProjectDurationDays = projectDurationDays,
                ExpectedBudget = expectedBudgetUsd,
                TeamSize = teamSize,
                AvgTeamSkillLevel = avgTeamSkillLevel,
                ComplexityScore = avgComplexity,
                BudgetUtilization = budgetUtilization,
                MethodologyUsedKanban = methodKanban,
                MethodologyUsedScrum = methodScrum,
                MethodologyUsedWaterfall = methodWaterfall,
                MethodologyUsedHybrid = methodHybrid,
                Provider = provider,
                Model = model,
                Temperature = 0.5
            };

            // 10. Call Python API
            var response = await _llmClient.PostAsJsonAsync("api/v1/project-risk/assess", payload);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Loi tu AI Server: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PythonProjectRiskResponse>();
            if (result == null)
            {
                throw new InvalidOperationException("Khong the doc phan hoi tu AI Server.");
            }

            // Format SuggestionContent
            return string.Join(
                Environment.NewLine,
                $"**Kết quả phân tích rủi ro (Model: {model}):** {result.PredictionLabel} (Độ tin cậy: {result.ConfidenceScore * 100:F1}%)",
                $"**Trạng thái:** {result.BusinessStatusText}",
                "",
                "**Nhận định chi tiết:**",
                result.LlmInsight,
                "",
                "**Các yếu tố thành công:**",
                result.SuccessFactors != null && result.SuccessFactors.Any() ? string.Join(Environment.NewLine, result.SuccessFactors.Select(f => $"- {f}")) : "- Không ghi nhận",
                "",
                "**Thử thách/Nguy cơ tiềm ẩn:**",
                result.PotentialChallenges != null && result.PotentialChallenges.Any() ? string.Join(Environment.NewLine, result.PotentialChallenges.Select(c => $"- {c}")) : "- Không ghi nhận"
            );
        }

        private async Task<(string Content, List<AIAllocationAssessmentResultDto> Results)> GetResourceAllocationAnalysisAsync(
            Project project,
            int? taskId,
            List<int>? workspaceMemberIds)
        {
            if (taskId == null)
            {
                throw new ArgumentException("TaskId la bat buoc doi voi Resource Suggestion.");
            }

            // 1. Load Task
            var task = await _context.ProjectTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TaskID == taskId.Value && t.ProjectID == project.ProjectID && !t.IsDeleted);

            if (task == null)
            {
                throw new KeyNotFoundException("Khong tim thay Task.");
            }

            // 2. Map task fields
            string taskComplexity = task.Complexity.ToLowerInvariant(); // Keep "critical" as Python supports it!

            int deadlineDays = 14;
            if (task.StartDate.HasValue && task.EndDate.HasValue)
            {
                deadlineDays = task.EndDate.Value.DayNumber - task.StartDate.Value.DayNumber;
            }
            else if (task.EndDate.HasValue)
            {
                deadlineDays = task.EndDate.Value.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
            }

            if (deadlineDays <= 0)
            {
                deadlineDays = 1;
            }

            string requiredSkillLevel = task.RequiredSkillLevel.ToLowerInvariant();

            double workloadHours = 40.0;
            if (!string.IsNullOrEmpty(task.DurationType))
            {
                if (string.Equals(task.DurationType, "Hour", StringComparison.OrdinalIgnoreCase))
                {
                    workloadHours = (double)task.EstimatedValue;
                }
                else if (string.Equals(task.DurationType, "Day", StringComparison.OrdinalIgnoreCase) || 
                         string.Equals(task.DurationType, "StoryPoint", StringComparison.OrdinalIgnoreCase))
                {
                    workloadHours = (double)(task.EstimatedValue * 8.0m);
                }
            }

            string taskPriority = task.Priority.ToLowerInvariant();
            int teamSize = task.ExpectedTeamSize;

            // 3. Resolve plan/model
            var currentLimit = await _context.WorkspaceCurrentLimits
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.WorkspaceID == project.WorkspaceID);
            
            string planCode = currentLimit?.PlanCode ?? "FREE";
            
            string provider = "gemini";
            string model = "gemini-2.0-flash";
            if (planCode.Equals("PRO", StringComparison.OrdinalIgnoreCase))
            {
                model = "gemini-2.0-flash";
            }

            // 4. Load Workspace Members (apply filter if selected)
            var activeMemberData = await (from m in _context.WorkspaceMembers.Include(m => m.Resource)
                                          join p in _context.WorkspaceMemberProfiles on m.WorkspaceMemberID equals p.WorkspaceMemberID
                                          where m.WorkspaceID == project.WorkspaceID 
                                             && m.Status == "Active"
                                             && m.Resource.IsDeleted == false
                                             && p.IsDeleted == false
                                          select new { Member = m, Profile = p })
                                         .ToListAsync();

            if (workspaceMemberIds != null && workspaceMemberIds.Any())
            {
                activeMemberData = activeMemberData
                    .Where(d => workspaceMemberIds.Contains(d.Member.WorkspaceMemberID))
                    .ToList();
            }

            if (!activeMemberData.Any())
            {
                throw new InvalidOperationException("Khong co nhan su nao dang hoat dong trong Workspace de danh gia.");
            }

            // 5. Construct Python request payload with ALL profile fields to prevent confidence penalties
            var employeesPayload = new List<PythonEmployeeAssessmentInfo>();
            foreach (var data in activeMemberData)
            {
                var m = data.Member;
                var p = data.Profile;

                double expYears = (double)p.ExperienceYears;
                double techScore = (double)p.TechnicalSkillScore;
                double commScore = (double)p.CommunicationScore;
                double leadScore = (double)p.LeadershipScore;
                double pbScore = (double)p.ProblemSolvingScore;
                double attRate = (double)p.AttendanceRate;
                double confRate = (double)p.ConflictRate;
                string eduLevel = p.EducationLevel?.ToLowerInvariant() ?? "bachelor";

                string sLevel = "medium";
                if (techScore < 40.0)
                {
                    sLevel = "low";
                }
                else if (techScore >= 75.0)
                {
                    sLevel = "high";
                }

                string perfRating = "good";
                if (!string.IsNullOrEmpty(p.PerformanceRating))
                {
                    perfRating = p.PerformanceRating.ToUpperInvariant() switch
                    {
                        "POOR" => "poor",
                        "AVERAGE" => "good",
                        "EXCELLENT" or "OUTSTANDING" => "excellent",
                        _ => "good"
                    };
                }

                employeesPayload.Add(new PythonEmployeeAssessmentInfo
                {
                    EmployeeId = m.EmployeeCode,
                    EmployeeName = m.Resource.FullName,
                    ExperienceYears = expYears,
                    SkillLevel = sLevel,
                    TechnicalSkillScore = techScore,
                    CommunicationScore = commScore,
                    EducationLevel = eduLevel,
                    LeadershipScore = leadScore,
                    ProblemSolvingScore = pbScore,
                    AttendanceRate = attRate,
                    ConflictRate = confRate,
                    PerformanceRating = perfRating
                });
            }

            var payload = new PythonBulkAssessmentRequest
            {
                RequestType = "bulk",
                TaskComplexity = taskComplexity,
                DeadlineDays = deadlineDays,
                RequiredSkillLevel = requiredSkillLevel,
                WorkloadHours = workloadHours,
                TaskPriority = taskPriority,
                TeamSize = teamSize,
                Employees = employeesPayload,
                Provider = provider,
                Model = model,
                Temperature = 0.7
            };

            // 6. Call Python API
            var response = await _llmClient.PostAsJsonAsync("api/v1/allocation/assess", payload);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Loi tu AI Server: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PythonBulkAssessmentResponse>();
            if (result == null)
            {
                throw new InvalidOperationException("Khong the doc phan hoi tu AI Server.");
            }

            // Match back employee names and codes if missing
            for (int i = 0; i < result.Results.Count; i++)
            {
                var res = result.Results[i];
                if (string.IsNullOrEmpty(res.EmployeeId) && i < payload.Employees.Count)
                {
                    res.EmployeeId = payload.Employees[i].EmployeeId;
                }
                if (string.IsNullOrEmpty(res.EmployeeName) && i < payload.Employees.Count)
                {
                    res.EmployeeName = payload.Employees[i].EmployeeName;
                }
            }

            var rankedResults = result.Results
                .OrderByDescending(r => r.FitPercentage)
                .ToList();

            // 7. Map to DTO results for Client
            var resultsDtoList = new List<AIAllocationAssessmentResultDto>();
            foreach (var res in rankedResults)
            {
                var memberInfo = activeMemberData.FirstOrDefault(d => d.Member.EmployeeCode == res.EmployeeId);
                if (memberInfo != null)
                {
                    resultsDtoList.Add(new AIAllocationAssessmentResultDto
                    {
                        WorkspaceMemberId = memberInfo.Member.WorkspaceMemberID,
                        EmployeeCode = res.EmployeeId ?? string.Empty,
                        FullName = memberInfo.Member.Resource.FullName,
                        FitPercentage = res.FitPercentage,
                        PredictionLabel = res.PredictionLabel,
                        BusinessStatusText = res.BusinessStatusText,
                        LlmInsight = res.LlmInsight,
                        SuccessFactors = res.SuccessFactors ?? new(),
                        PotentialChallenges = res.PotentialChallenges ?? new()
                    });
                }
            }

            // 8. Generate Audit Markdown for database logs
            var tableBuilder = new StringBuilder();
            tableBuilder.AppendLine("| Hạng | Mã nhân viên | Tên nhân sự | Đánh giá | Điểm phù hợp | Độ tin cậy | Trạng thái |");
            tableBuilder.AppendLine("|---|---|---|---|---|---|---|");

            for (int i = 0; i < rankedResults.Count; i++)
            {
                var res = rankedResults[i];
                tableBuilder.AppendLine($"| {i + 1} | {res.EmployeeId} | {res.EmployeeName} | {res.PredictionLabel} | {res.FitPercentage:F1}% | {res.ConfidenceScore * 100:F1}% | {res.BusinessStatusText} |");
            }

            var detailsBuilder = new StringBuilder();
            foreach (var res in rankedResults)
            {
                string successStr = res.SuccessFactors != null && res.SuccessFactors.Any() ? string.Join(", ", res.SuccessFactors) : "Không ghi nhận";
                string challengesStr = res.PotentialChallenges != null && res.PotentialChallenges.Any() ? string.Join(", ", res.PotentialChallenges) : "Không ghi nhận";

                detailsBuilder.AppendLine($"""
                    #### 👤 {res.EmployeeName} ({res.EmployeeId})
                    - **Đánh giá:** {res.PredictionLabel} ({res.BusinessStatusText}) - **Điểm phù hợp:** {res.FitPercentage:F1}%
                    - **Nhận định:** {res.LlmInsight}
                    - **Yếu tố thuận lợi:** {successStr}
                    - **Thử thách:** {challengesStr}
                    
                    """);
            }

            string content = $"""
                ### 📊 Bảng Xếp Hạng Mức Độ Phù Hợp Nhân Sự (Model: {model})
                
                {tableBuilder}
                
                ### 💡 Chi tiết nhận định từ AI:
                
                {detailsBuilder}
                """;

            return (content, resultsDtoList);
        }

        private async Task<Project> LoadProjectAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(item => item.Workspace)
                .FirstOrDefaultAsync(item =>
                    item.ProjectID == projectId
                    && item.Workspace != null
                    && !item.Workspace.IsDeleted);

            if (project == null)
            {
                throw new KeyNotFoundException("Khong tim thay Project.");
            }

            return project;
        }

        private async Task<ActiveMembership> ResolveActiveMembershipAsync(
            int accountId,
            int workspaceId)
        {
            var membership = await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member =>
                    member.WorkspaceID == workspaceId
                    && member.Resource.AccountID == accountId
                    && member.Status == "Active"
                    && !member.Workspace.IsDeleted
                    && !member.Resource.IsDeleted)
                .Select(member => new ActiveMembership
                {
                    WorkspaceMemberID = member.WorkspaceMemberID,
                    WorkspaceRoleID = member.WorkspaceRoleID,
                    RoleName = member.WorkspaceRole.RoleName
                })
                .FirstOrDefaultAsync();

            if (membership == null)
            {
                throw new UnauthorizedAccessException(
                    "Ban khong phai thanh vien active cua workspace chua Project nay.");
            }

            return membership;
        }

        private async Task EnsureAskPermissionAsync(ActiveMembership membership)
        {
            if (string.Equals(membership.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var hasPermission = await _context.RolePermissions
                .AsNoTracking()
                .AnyAsync(rolePermission =>
                    rolePermission.WorkspaceRoleID == membership.WorkspaceRoleID
                    && rolePermission.PermissionID == AIPermissionIds.Ask);

            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Ban khong co quyen su dung AI trong workspace nay.");
            }
        }

        private async Task EnsureRiskAiFeatureEnabledAsync(int workspaceId)
        {
            var hasFeature = await _featureQuotaService.CheckFeatureQuotaAsync(
                workspaceId,
                AIRiskManagementFeatureCode);

            if (!hasFeature)
            {
                throw new QuotaExceededException(
                    "Goi cuoc hien tai chua ho tro tinh nang Quan tri rui ro AI.",
                    "FEATURE_NOT_INCLUDED");
            }
        }

        private async Task<int> ResolveEffectiveQuotaLimitAsync(int workspaceId)
        {
            var isStrictCheckEnabled = _configuration.GetValue<bool>("FeatureToggles:EnableStrictQuotaCheck");
            if (!isStrictCheckEnabled)
            {
                return -1;
            }

            var limit = await _featureQuotaService.GetFeatureLimitAsync(
                workspaceId,
                AIChatQuotaFeatureCode);

            if (limit == null)
            {
                throw new QuotaExceededException("Khong tim thay cau hinh quota AI cho workspace nay.");
            }

            if (limit.LimitValue == -1)
            {
                return -1;
            }

            if (limit.LimitValue <= 0)
            {
                throw new QuotaExceededException("Workspace da het quota hoi dap AI cua goi cuoc hien tai.");
            }

            return limit.LimitValue;
        }

        private async Task<string> BuildProjectSummaryAsync(Project project)
        {
            var taskCount = await _context.ProjectTasks
                .AsNoTracking()
                .CountAsync(task => task.ProjectID == project.ProjectID);

            var openRiskCount = await _context.Risks
                .AsNoTracking()
                .CountAsync(risk =>
                    risk.ProjectID == project.ProjectID
                    && risk.Status != "Closed");

            return string.Join(
                "; ",
                $"ProjectID={project.ProjectID}",
                $"Name={project.ProjectName}",
                $"Status={project.Status}",
                $"ExpectedBudget={project.ExpectedBudget}",
                $"OriginalCurrencyCode={project.OriginalCurrencyCode}",
                $"ExchangeRateToUSD={project.ExchangeRateToUSD}",
                $"TotalRevenue={project.TotalRevenue}",
                $"Methodology={project.Methodology}",
                $"StartDate={project.StartDate:yyyy-MM-dd}",
                $"EndDate={project.EndDate:yyyy-MM-dd}",
                $"TaskCount={taskCount}",
                $"OpenRiskCount={openRiskCount}");
        }

        private async Task<string?> BuildTargetEntitySummaryAsync(int projectId, int? targetEntityId)
        {
            if (targetEntityId == null)
            {
                return null;
            }

            var task = await _context.ProjectTasks
                .AsNoTracking()
                .Where(item =>
                    item.TaskID == targetEntityId.Value
                    && item.ProjectID == projectId)
                .Select(item => new
                {
                    item.TaskID,
                    item.TaskName,
                    item.Status,
                    item.DurationType,
                    item.EstimatedValue,
                    item.StartDate,
                    item.EndDate
                })
                .FirstOrDefaultAsync();

            if (task == null)
            {
                throw new ArgumentException("targetEntityId khong ton tai hoac khong thuoc Project nay.");
            }

            return string.Join(
                "; ",
                $"TaskID={task.TaskID}",
                $"TaskName={task.TaskName}",
                $"Status={task.Status}",
                $"DurationType={task.DurationType}",
                $"EstimatedValue={task.EstimatedValue}",
                $"StartDate={task.StartDate:yyyy-MM-dd}",
                $"EndDate={task.EndDate:yyyy-MM-dd}");
        }

        private async Task EnsureMonthlyUsageRowAsync(int workspaceId, DateOnly billingMonth)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (
    SELECT 1
    FROM WorkspaceMonthlyUsages WITH (UPDLOCK, HOLDLOCK)
    WHERE WorkspaceID = {workspaceId} AND BillingMonth = {billingMonth}
)
BEGIN
    INSERT INTO WorkspaceMonthlyUsages (WorkspaceID, BillingMonth, AIQueryCount, StorageUsedMB, UpdatedAt)
    VALUES ({workspaceId}, {billingMonth}, 0, 0, SYSUTCDATETIME())
END");
        }

        private async Task<int> TryConsumeAIQuotaAsync(
            int workspaceId,
            DateOnly billingMonth,
            int effectiveLimit)
        {
            var affectedRows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE WorkspaceMonthlyUsages
SET AIQueryCount = ISNULL(AIQueryCount, 0) + 1,
    UpdatedAt = SYSUTCDATETIME()
WHERE WorkspaceID = {workspaceId}
  AND BillingMonth = {billingMonth}
  AND ({effectiveLimit} = -1 OR ISNULL(AIQueryCount, 0) < {effectiveLimit})");

            if (affectedRows == 0)
            {
                throw new QuotaExceededException("Workspace da vuot quota hoi dap AI cua thang hien tai.");
            }

            return await _context.WorkspaceMonthlyUsages
                .AsNoTracking()
                .Where(usage =>
                    usage.WorkspaceID == workspaceId
                    && usage.BillingMonth == billingMonth)
                .Select(usage => usage.AIQueryCount)
                .SingleAsync();
        }

        private static int? CalculateRemainingQuota(int effectiveLimit, int newCount)
        {
            return effectiveLimit == -1
                ? null
                : Math.Max(effectiveLimit - newCount, 0);
        }

        private static DateOnly GetCurrentBillingMonth()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return new DateOnly(today.Year, today.Month, 1);
        }

        private static string? NormalizeAnalysisType(string? value)
        {
            var normalized = NormalizeOptionalString(value);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "RISK" or "RISK WARNING" or "RISK_WARNING" or "RISK-WARNING" => "Risk Warning",
                "RESOURCE" or "RESOURCE SUGGESTION" or "RESOURCE_SUGGESTION" or "RESOURCE-SUGGESTION" => "Resource Suggestion",
                "BUDGET" or "BUDGET FORECAST" or "BUDGET_FORECAST" or "BUDGET-FORECAST" => "Budget Forecast",
                _ => normalized
            };

            return AllowedAnalysisTypes.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string? NormalizeNullableText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private sealed class ActiveMembership
        {
            public int WorkspaceMemberID { get; set; }
            public int WorkspaceRoleID { get; set; }
            public string RoleName { get; set; } = string.Empty;
        }
    }
}
