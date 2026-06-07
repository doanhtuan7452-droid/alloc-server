using AllocServer.Data;
using AllocServer.DTOs.WorkspaceMemberProfiles;
using AllocServer.Interfaces.WorkspaceMemberProfiles;
using AllocServer.Models;
using AllocServer.Events;
using AllocServer.Events.DomainEvents;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AllocServer.Services.WorkspaceMemberProfile_Services
{
    public class WorkspaceMemberProfileService : IWorkspaceMemberProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEventPublisher _eventPublisher;

        public WorkspaceMemberProfileService(ApplicationDbContext context, IEventPublisher eventPublisher)
        {
            _context = context;
            _eventPublisher = eventPublisher;
        }

        public async Task<WorkspaceMemberProfileResponse?> GetProfileAsync(int workspaceId, int memberId)
        {
            var profile = await _context.WorkspaceMemberProfiles
                .Include(p => p.WorkspaceMember)
                .FirstOrDefaultAsync(p => p.WorkspaceMemberID == memberId 
                                       && p.WorkspaceMember.WorkspaceID == workspaceId);

            if (profile == null)
            {
                return null;
            }

            return MapToResponse(profile);
        }

        public async Task<WorkspaceMemberProfileResponse> CreateProfileAsync(int workspaceId, int memberId, CreateMemberProfileRequest request)
        {
            // Check if member exists, is active and belongs to the workspace
            var member = await _context.WorkspaceMembers
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == memberId 
                                       && m.WorkspaceID == workspaceId 
                                       && m.Status == "Active");

            if (member == null)
            {
                throw new KeyNotFoundException("Khong tim thay nhan su hoac nhan su khong active trong Workspace.");
            }

            // Check if profile already exists in any state (active or soft-deleted)
            var existingProfile = await _context.WorkspaceMemberProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.WorkspaceMemberID == memberId);

            if (existingProfile != null)
            {
                throw new InvalidOperationException("Profile da ton tai cho nhan su nay. Vui long cap nhat hoac khoi phuc profile thay vi tao moi.");
            }

            var profile = new WorkspaceMemberProfile
            {
                WorkspaceMemberID = memberId,
                ExperienceYears = request.PriorExperienceYears,
                EducationLevel = request.EducationLevel,
                LastEvaluatedAt = DateTime.UtcNow
            };

            _context.WorkspaceMemberProfiles.Add(profile);
            await _context.SaveChangesAsync();

            // Reload profile with member relationship
            profile.WorkspaceMember = member;

            return MapToResponse(profile);
        }

        public async Task<WorkspaceMemberProfileResponse> UpdateProfileAsync(int workspaceId, int memberId, UpdateMemberProfileRequest request)
        {
            // Retrieve profile regardless of IsDeleted status to support restore
            var profile = await _context.WorkspaceMemberProfiles
                .IgnoreQueryFilters()
                .Include(p => p.WorkspaceMember)
                .FirstOrDefaultAsync(p => p.WorkspaceMemberID == memberId 
                                       && p.WorkspaceMember.WorkspaceID == workspaceId);

            if (profile == null)
            {
                throw new KeyNotFoundException("Khong tim thay profile cua nhan su.");
            }

            profile.ExperienceYears = request.PriorExperienceYears;
            profile.EducationLevel = request.EducationLevel;
            profile.IsDeleted = false; // Restore if it was soft-deleted
            profile.LastEvaluatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToResponse(profile);
        }

        public async Task<bool> DeleteProfileAsync(int workspaceId, int memberId)
        {
            var profile = await _context.WorkspaceMemberProfiles
                .Include(p => p.WorkspaceMember)
                .FirstOrDefaultAsync(p => p.WorkspaceMemberID == memberId 
                                       && p.WorkspaceMember.WorkspaceID == workspaceId);

            if (profile == null)
            {
                return false;
            }

            profile.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task RecalculateProfileScoresAsync(int memberId)
        {
            var profile = await _context.WorkspaceMemberProfiles
                .FirstOrDefaultAsync(p => p.WorkspaceMemberID == memberId);

            if (profile == null)
            {
                return;
            }

            var member = await _context.WorkspaceMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == memberId);

            if (member == null)
            {
                return;
            }

            // 1. Recalculate Technical Skill Score
            var skills = await _context.ResourceSkills
                .AsNoTracking()
                .Where(rs => rs.ResourceID == member.ResourceID)
                .ToListAsync();

            decimal baseTechnicalScore = 0;
            if (skills.Count > 0)
            {
                decimal sumOfLevels = skills.Sum(s => s.Level);
                baseTechnicalScore = (sumOfLevels / (skills.Count * 5.0m)) * 100.0m;
            }

            var completedTasks = await _context.TaskAssignees
                .AsNoTracking()
                .Include(ta => ta.Task)
                .Where(ta => ta.WorkspaceMemberID == memberId 
                          && ta.AssigneeType == "Assignee" 
                          && ta.Task.Status == "Done"
                          && !ta.Task.IsDeleted)
                .Select(ta => ta.Task.Complexity)
                .ToListAsync();

            decimal technicalBonus = 0;
            foreach (var complexity in completedTasks)
            {
                if (string.Equals(complexity, "High", StringComparison.OrdinalIgnoreCase))
                {
                    technicalBonus += 2.0m;
                }
                else if (string.Equals(complexity, "Critical", StringComparison.OrdinalIgnoreCase))
                {
                    technicalBonus += 5.0m;
                }
            }

            profile.TechnicalSkillScore = Math.Min(baseTechnicalScore + technicalBonus, 100.00m);

            // 2. Recalculate Soft Skills from completed cycles
            var evaluations = await _context.MemberEvaluations
                .AsNoTracking()
                .Include(e => e.ReviewCycle)
                .Where(e => e.RevieweeID == memberId 
                         && e.Status == "Submitted" 
                         && e.ReviewCycle.Status == "Completed")
                .ToListAsync();

            if (evaluations.Count > 0)
            {
                profile.CommunicationScore = evaluations.Average(e => e.CommunicationScore);
                profile.LeadershipScore = evaluations.Average(e => e.LeadershipScore);
                profile.ProblemSolvingScore = evaluations.Average(e => e.ProblemSolvingScore);
            }

            // 3. Recalculate Attendance Rate for the current month
            // 3. Recalculate Attendance Rate for the current month
            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateOnly(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            int standardWorkingDays = GetWorkingDaysInMonth(today.Year, today.Month);

            int timesheetDays = await _context.Timesheets
                .AsNoTracking()
                .Where(t => t.WorkspaceMemberID == memberId 
                         && t.WorkDate >= startOfMonth 
                         && t.WorkDate <= endOfMonth)
                .Select(t => t.WorkDate)
                .Distinct()
                .CountAsync();

            var leaveRequests = await _context.LeaveRequests
                .AsNoTracking()
                .Where(l => l.WorkspaceMemberID == memberId 
                         && l.Status == "Approved" 
                         && l.StartDate <= endOfMonth 
                         && l.EndDate >= startOfMonth)
                .ToListAsync();

            int approvedLeaveDays = 0;
            foreach (var leave in leaveRequests)
            {
                var overlapStart = leave.StartDate < startOfMonth ? startOfMonth : leave.StartDate;
                var overlapEnd = leave.EndDate > endOfMonth ? endOfMonth : leave.EndDate;
                approvedLeaveDays += GetWorkingDaysInRange(overlapStart, overlapEnd);
            }

            decimal attendanceRate = 100.00m;
            int divisor = standardWorkingDays - approvedLeaveDays;
            if (divisor > 0)
            {
                attendanceRate = Math.Min((timesheetDays / (decimal)divisor) * 100.00m, 100.00m);
            }
            profile.AttendanceRate = attendanceRate;

            profile.LastEvaluatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RecalculateAttendanceRateForMonthAsync(int memberId, int year, int month)
        {
            var profile = await _context.WorkspaceMemberProfiles
                .FirstOrDefaultAsync(p => p.WorkspaceMemberID == memberId);

            if (profile == null)
            {
                return;
            }

            var startOfMonth = new DateOnly(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            int standardWorkingDays = GetWorkingDaysInMonth(year, month);

            int timesheetDays = await _context.Timesheets
                .AsNoTracking()
                .Where(t => t.WorkspaceMemberID == memberId 
                         && t.WorkDate >= startOfMonth 
                         && t.WorkDate <= endOfMonth)
                .Select(t => t.WorkDate)
                .Distinct()
                .CountAsync();

            var leaveRequests = await _context.LeaveRequests
                .AsNoTracking()
                .Where(l => l.WorkspaceMemberID == memberId 
                         && l.Status == "Approved" 
                         && l.StartDate <= endOfMonth 
                         && l.EndDate >= startOfMonth)
                .ToListAsync();

            int approvedLeaveDays = 0;
            foreach (var leave in leaveRequests)
            {
                var overlapStart = leave.StartDate < startOfMonth ? startOfMonth : leave.StartDate;
                var overlapEnd = leave.EndDate > endOfMonth ? endOfMonth : leave.EndDate;
                approvedLeaveDays += GetWorkingDaysInRange(overlapStart, overlapEnd);
            }

            decimal attendanceRate = 100.00m;
            int divisor = standardWorkingDays - approvedLeaveDays;
            if (divisor > 0)
            {
                attendanceRate = Math.Min((timesheetDays / (decimal)divisor) * 100.00m, 100.00m);
            }
            profile.AttendanceRate = attendanceRate;
            profile.LastEvaluatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private static int GetWorkingDaysInMonth(int year, int month)
        {
            int days = DateTime.DaysInMonth(year, month);
            int workingDays = 0;
            for (int i = 1; i <= days; i++)
            {
                var date = new DateTime(year, month, i);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
            }
            return workingDays;
        }

        private static int GetWorkingDaysInRange(DateOnly start, DateOnly end)
        {
            int workingDays = 0;
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
            }
            return workingDays;
        }

        private static WorkspaceMemberProfileResponse MapToResponse(WorkspaceMemberProfile profile)
        {
            // Dynamic experience calculation: Prior + years since JoinedAt
            int yearsInWorkspace = 0;
            if (profile.WorkspaceMember != null)
            {
                var joinedAt = profile.WorkspaceMember.JoinedAt;
                var totalDays = (DateTime.UtcNow - joinedAt).TotalDays;
                yearsInWorkspace = (int)(totalDays / 365.25);
                if (yearsInWorkspace < 0) yearsInWorkspace = 0;
            }

            return new WorkspaceMemberProfileResponse
            {
                ProfileID = profile.ProfileID,
                WorkspaceMemberID = profile.WorkspaceMemberID,
                PriorExperienceYears = profile.ExperienceYears,
                TotalExperienceYears = profile.ExperienceYears + yearsInWorkspace,
                EducationLevel = profile.EducationLevel,
                TechnicalSkillScore = profile.TechnicalSkillScore,
                CommunicationScore = profile.CommunicationScore,
                LeadershipScore = profile.LeadershipScore,
                ProblemSolvingScore = profile.ProblemSolvingScore,
                AvgSoftSkillScore = profile.AvgSoftSkillScore,
                AttendanceRate = profile.AttendanceRate,
                ConflictRate = profile.ConflictRate,
                PerformanceRating = profile.PerformanceRating,
                LastEvaluatedAt = profile.LastEvaluatedAt
            };
        }

        public async Task<List<ReviewCycleResponse>> GetReviewCyclesAsync(int workspaceId)
        {
            var cycles = await _context.ReviewCycles
                .Where(rc => rc.WorkspaceID == workspaceId && !rc.IsDeleted)
                .ToListAsync();

            return cycles.Select(MapToReviewCycleResponse).ToList();
        }

        public async Task<ReviewCycleResponse> CreateReviewCycleAsync(int workspaceId, CreateReviewCycleRequest request, int createdByMemberId)
        {
            var workspaceExists = await _context.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
            if (!workspaceExists)
            {
                throw new KeyNotFoundException("Khong tim thay Workspace.");
            }

            var creatorExists = await _context.WorkspaceMembers.AnyAsync(m => m.WorkspaceMemberID == createdByMemberId && m.WorkspaceID == workspaceId && m.Status == "Active");
            if (!creatorExists)
            {
                throw new KeyNotFoundException("Nguoi tao khong phai la thanh vien active cua Workspace.");
            }

            if (request.StartDate > request.EndDate)
            {
                throw new ArgumentException("Ngay bat dau phai nho hon hoac bang ngay ket thuc.");
            }

            var cycle = new ReviewCycle
            {
                WorkspaceID = workspaceId,
                CycleName = request.CycleName,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = "Draft",
                CreatedBy = createdByMemberId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReviewCycles.Add(cycle);
            await _context.SaveChangesAsync();

            return MapToReviewCycleResponse(cycle);
        }

        public async Task<ReviewCycleResponse> StartReviewCycleAsync(int workspaceId, int cycleId)
        {
            var cycle = await _context.ReviewCycles
                .FirstOrDefaultAsync(rc => rc.CycleID == cycleId && rc.WorkspaceID == workspaceId && !rc.IsDeleted);

            if (cycle == null)
            {
                throw new KeyNotFoundException("Khong tim thay chu ky danh gia.");
            }

            if (cycle.Status != "Draft")
            {
                throw new InvalidOperationException("Chi co the bat dau chu ky danh gia dang o trang thai Draft.");
            }

            cycle.Status = "Active";
            await _context.SaveChangesAsync();

            return MapToReviewCycleResponse(cycle);
        }

        public async Task<ReviewCycleResponse> CompleteReviewCycleAsync(int workspaceId, int cycleId)
        {
            var cycle = await _context.ReviewCycles
                .FirstOrDefaultAsync(rc => rc.CycleID == cycleId && rc.WorkspaceID == workspaceId && !rc.IsDeleted);

            if (cycle == null)
            {
                throw new KeyNotFoundException("Khong tim thay chu ky danh gia.");
            }

            if (cycle.Status != "Active")
            {
                throw new InvalidOperationException("Chi co the hoan thanh chu ky danh gia dang o trang thai Active.");
            }

            cycle.Status = "Completed";
            await _context.SaveChangesAsync();

            // Publish the domain event to trigger calculations
            await _eventPublisher.PublishAsync(new ReviewCycleCompletedEvent(cycleId, workspaceId));

            return MapToReviewCycleResponse(cycle);
        }

        public async Task<MemberEvaluationResponse> SubmitMemberEvaluationAsync(int workspaceId, int cycleId, SubmitEvaluationRequest request)
        {
            var cycle = await _context.ReviewCycles
                .FirstOrDefaultAsync(rc => rc.CycleID == cycleId && rc.WorkspaceID == workspaceId && !rc.IsDeleted);

            if (cycle == null)
            {
                throw new KeyNotFoundException("Khong tim thay chu ky danh gia.");
            }

            if (cycle.Status != "Active")
            {
                throw new InvalidOperationException("Chi co the nop danh gia trong chu ky dang hoat dong (Active).");
            }

            var revieweeExists = await _context.WorkspaceMembers.AnyAsync(m => m.WorkspaceMemberID == request.RevieweeID && m.WorkspaceID == workspaceId && m.Status == "Active");
            var reviewerExists = await _context.WorkspaceMembers.AnyAsync(m => m.WorkspaceMemberID == request.ReviewerID && m.WorkspaceID == workspaceId && m.Status == "Active");

            if (!revieweeExists || !reviewerExists)
            {
                throw new KeyNotFoundException("Nhan vien duoc danh gia hoac nguoi danh gia khong hop le trong Workspace.");
            }

            if (!new[] { "Self", "Manager", "Peer" }.Contains(request.EvaluationType))
            {
                throw new ArgumentException("Loai danh gia khong hop le (chi chap nhan Self, Manager, Peer).");
            }

            var evaluation = await _context.MemberEvaluations
                .FirstOrDefaultAsync(e => e.CycleID == cycleId && e.RevieweeID == request.RevieweeID && e.ReviewerID == request.ReviewerID && e.EvaluationType == request.EvaluationType);

            if (evaluation == null)
            {
                evaluation = new MemberEvaluation
                {
                    CycleID = cycleId,
                    RevieweeID = request.RevieweeID,
                    ReviewerID = request.ReviewerID,
                    EvaluationType = request.EvaluationType
                };
                _context.MemberEvaluations.Add(evaluation);
            }

            evaluation.CommunicationScore = request.CommunicationScore;
            evaluation.LeadershipScore = request.LeadershipScore;
            evaluation.ProblemSolvingScore = request.ProblemSolvingScore;
            evaluation.FeedbackNotes = request.FeedbackNotes;
            evaluation.SubmittedAt = DateTime.UtcNow;
            evaluation.Status = "Submitted";

            await _context.SaveChangesAsync();

            return MapToMemberEvaluationResponse(evaluation);
        }

        private static ReviewCycleResponse MapToReviewCycleResponse(ReviewCycle rc)
        {
            return new ReviewCycleResponse
            {
                CycleID = rc.CycleID,
                WorkspaceID = rc.WorkspaceID,
                CycleName = rc.CycleName,
                StartDate = rc.StartDate,
                EndDate = rc.EndDate,
                Status = rc.Status,
                CreatedBy = rc.CreatedBy,
                CreatedAt = rc.CreatedAt
            };
        }

        private static MemberEvaluationResponse MapToMemberEvaluationResponse(MemberEvaluation me)
        {
            return new MemberEvaluationResponse
            {
                EvaluationID = me.EvaluationID,
                CycleID = me.CycleID,
                RevieweeID = me.RevieweeID,
                ReviewerID = me.ReviewerID,
                EvaluationType = me.EvaluationType,
                CommunicationScore = me.CommunicationScore,
                LeadershipScore = me.LeadershipScore,
                ProblemSolvingScore = me.ProblemSolvingScore,
                FeedbackNotes = me.FeedbackNotes,
                SubmittedAt = me.SubmittedAt,
                Status = me.Status
            };
        }
    }
}
