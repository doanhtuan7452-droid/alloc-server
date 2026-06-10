using AllocServer.Data;
using AllocServer.DTOs.Requests;
using AllocServer.Filters;
using AllocServer.Interfaces.Requests;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Request_Services
{
    public class RequestService : IRequestService
    {
        private const string PendingStatus = "Pending";
        private const string ApprovedStatus = "Approved";
        private const string RejectedStatus = "Rejected";
        private const string LeaveType = "Leave";
        private const string OTType = "OT";
        private const decimal MaxHoursPerDay = 24m;

        private readonly ApplicationDbContext _context;

        public RequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LeaveRequestResponse> CreateLeaveRequestAsync(
            int accountId,
            int workspaceId,
            CreateLeaveRequestRequest request)
        {
            ValidatePositiveId(workspaceId, "workspaceId");

            if (request.StartDate == null)
            {
                throw new ArgumentException("StartDateRequired");
            }

            if (request.EndDate == null)
            {
                throw new ArgumentException("EndDateRequired");
            }

            if (request.EndDate.Value < request.StartDate.Value)
            {
                throw new ArgumentException("InvalidDateRange");
            }

            var membership = await GetActiveMembershipAsync(accountId, workspaceId);
            var now = DateTime.UtcNow;

            var leaveRequest = new LeaveRequest
            {
                WorkspaceMemberID = membership.WorkspaceMemberID,
                StartDate = request.StartDate.Value,
                EndDate = request.EndDate.Value,
                Reason = NormalizeNullableText(request.Reason),
                Status = PendingStatus,
                CreatedAt = now
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            return await MapLeaveRequestAsync(leaveRequest.RequestID);
        }

        public async Task<OTRequestResponse> CreateOTRequestAsync(
            int accountId,
            int workspaceId,
            CreateOTRequestRequest request)
        {
            ValidatePositiveId(workspaceId, "workspaceId");

            if (request.RequestedDate == null)
            {
                throw new ArgumentException("RequestedDateRequired");
            }

            ValidateExpectedHours(request.ExpectedHours);

            if (request.TaskId is <= 0)
            {
                throw new ArgumentException("InvalidTaskId");
            }

            var membership = await GetActiveMembershipAsync(accountId, workspaceId);

            if (request.TaskId != null)
            {
                await ValidateTaskForOTRequestAsync(
                    workspaceId,
                    request.TaskId.Value,
                    request.RequestedDate.Value);
            }

            var now = DateTime.UtcNow;
            var otRequest = new OvertimeRequest
            {
                WorkspaceMemberID = membership.WorkspaceMemberID,
                TaskID = request.TaskId,
                RequestedDate = request.RequestedDate.Value,
                ExpectedHours = request.ExpectedHours,
                Status = PendingStatus,
                CreatedAt = now
            };

            _context.OTRequests.Add(otRequest);
            await _context.SaveChangesAsync();

            return await MapOTRequestAsync(otRequest.OTRequestID);
        }

        public async Task<RequestReviewResponse> ReviewRequestAsync(
            int accountId,
            string requestType,
            int requestId,
            ReviewRequestRequest request)
        {
            ValidatePositiveId(requestId, "requestId");

            var normalizedType = NormalizeRequestType(requestType);
            var normalizedStatus = NormalizeReviewStatus(request.Status);
            var approvalNote = NormalizeNullableText(request.ApprovalNote);
            var reviewedAt = DateTime.UtcNow;

            return normalizedType == LeaveType
                ? await ReviewLeaveRequestAsync(accountId, requestId, normalizedStatus, approvalNote, reviewedAt)
                : await ReviewOTRequestAsync(accountId, requestId, normalizedStatus, approvalNote, reviewedAt);
        }

        private async Task<RequestReviewResponse> ReviewLeaveRequestAsync(
            int accountId,
            int requestId,
            string status,
            string? approvalNote,
            DateTime reviewedAt)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(item => item.WorkspaceMember)
                .FirstOrDefaultAsync(item => item.RequestID == requestId);

            if (leaveRequest == null || leaveRequest.WorkspaceMember == null)
            {
                throw new KeyNotFoundException("LeaveRequestNotFound");
            }

            EnsurePending(leaveRequest.Status);

            var reviewer = await GetReviewerMembershipAsync(
                accountId,
                leaveRequest.WorkspaceMember.WorkspaceID);

            leaveRequest.Status = status;
            leaveRequest.ApproverID = reviewer.WorkspaceMemberID;
            leaveRequest.ApprovalNote = approvalNote;
            leaveRequest.ReviewedAt = reviewedAt;

            await _context.SaveChangesAsync();

            return new RequestReviewResponse
            {
                RequestType = LeaveType,
                RequestId = leaveRequest.RequestID,
                Status = leaveRequest.Status,
                ApproverId = reviewer.WorkspaceMemberID,
                ApprovalNote = leaveRequest.ApprovalNote,
                ReviewedAt = reviewedAt
            };
        }

        private async Task<RequestReviewResponse> ReviewOTRequestAsync(
            int accountId,
            int requestId,
            string status,
            string? approvalNote,
            DateTime reviewedAt)
        {
            var otRequest = await _context.OTRequests
                .Include(item => item.WorkspaceMember)
                .FirstOrDefaultAsync(item => item.OTRequestID == requestId);

            if (otRequest == null || otRequest.WorkspaceMember == null)
            {
                throw new KeyNotFoundException("OTRequestNotFound");
            }

            EnsurePending(otRequest.Status);

            var reviewer = await GetReviewerMembershipAsync(
                accountId,
                otRequest.WorkspaceMember.WorkspaceID);

            otRequest.Status = status;
            otRequest.ApproverID = reviewer.WorkspaceMemberID;
            otRequest.ApprovalNote = approvalNote;
            otRequest.ReviewedAt = reviewedAt;

            await _context.SaveChangesAsync();

            return new RequestReviewResponse
            {
                RequestType = OTType,
                RequestId = otRequest.OTRequestID,
                Status = otRequest.Status,
                ApproverId = reviewer.WorkspaceMemberID,
                ApprovalNote = otRequest.ApprovalNote,
                ReviewedAt = reviewedAt
            };
        }

        private async Task<ActiveMembership> GetActiveMembershipAsync(
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
                    WorkspaceID = member.WorkspaceID,
                    WorkspaceRoleID = member.WorkspaceRoleID,
                    RoleName = member.WorkspaceRole.RoleName
                })
                .FirstOrDefaultAsync();

            if (membership == null)
            {
                throw new UnauthorizedAccessException("UnauthorizedWorkspaceMember");
            }

            return membership;
        }

        private async Task<ActiveMembership> GetReviewerMembershipAsync(
            int accountId,
            int workspaceId)
        {
            var membership = await GetActiveMembershipAsync(accountId, workspaceId);

            if (string.Equals(membership.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                return membership;
            }

            var hasApprovePermission = await _context.RolePermissions
                .AsNoTracking()
                .AnyAsync(rolePermission =>
                    rolePermission.WorkspaceRoleID == membership.WorkspaceRoleID
                    && rolePermission.PermissionID == RequestPermissionIds.Approve);

            if (!hasApprovePermission)
            {
                throw new UnauthorizedAccessException("UnauthorizedRequestApproval");
            }

            return membership;
        }

        private async Task ValidateTaskForOTRequestAsync(
            int workspaceId,
            int taskId,
            DateOnly requestedDate)
        {
            var task = await _context.ProjectTasks
                .AsNoTracking()
                .Include(item => item.Project)
                    .ThenInclude(project => project!.Workspace)
                .FirstOrDefaultAsync(item =>
                    item.TaskID == taskId
                    && item.Project != null
                    && item.Project.WorkspaceID == workspaceId
                    && item.Project.Workspace != null
                    && !item.Project.Workspace.IsDeleted);

            if (task == null)
            {
                throw new KeyNotFoundException("TaskNotFoundInWorkspace");
            }

            if (task.StartDate != null && requestedDate < task.StartDate.Value)
            {
                throw new ArgumentException("RequestedDateBeforeTaskStart");
            }

            if (task.EndDate != null && requestedDate > task.EndDate.Value)
            {
                throw new ArgumentException("RequestedDateAfterTaskEnd");
            }
        }

        private async Task<LeaveRequestResponse> MapLeaveRequestAsync(int requestId)
        {
            return await _context.LeaveRequests
                .AsNoTracking()
                .Where(item => item.RequestID == requestId)
                .Select(item => new LeaveRequestResponse
                {
                    RequestId = item.RequestID,
                    WorkspaceId = item.WorkspaceMember != null ? item.WorkspaceMember.WorkspaceID : 0,
                    WorkspaceMemberId = item.WorkspaceMemberID,
                    RequesterName = item.WorkspaceMember != null && item.WorkspaceMember.Resource != null
                        ? item.WorkspaceMember.Resource.FullName
                        : string.Empty,
                    ApproverId = item.ApproverID,
                    ApproverName = item.Approver != null && item.Approver.Resource != null
                        ? item.Approver.Resource.FullName
                        : null,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    Reason = item.Reason,
                    Status = item.Status,
                    ApprovalNote = item.ApprovalNote,
                    ReviewedAt = item.ReviewedAt,
                    CreatedAt = item.CreatedAt
                })
                .FirstAsync();
        }

        private async Task<OTRequestResponse> MapOTRequestAsync(int requestId)
        {
            return await _context.OTRequests
                .AsNoTracking()
                .Where(item => item.OTRequestID == requestId)
                .Select(item => new OTRequestResponse
                {
                    RequestId = item.OTRequestID,
                    WorkspaceId = item.WorkspaceMember != null ? item.WorkspaceMember.WorkspaceID : 0,
                    WorkspaceMemberId = item.WorkspaceMemberID,
                    RequesterName = item.WorkspaceMember != null && item.WorkspaceMember.Resource != null
                        ? item.WorkspaceMember.Resource.FullName
                        : string.Empty,
                    TaskId = item.TaskID,
                    TaskName = item.Task != null ? item.Task.TaskName : null,
                    ProjectId = item.Task != null ? item.Task.ProjectID : null,
                    ProjectName = item.Task != null && item.Task.Project != null
                        ? item.Task.Project.ProjectName
                        : null,
                    RequestedDate = item.RequestedDate,
                    ExpectedHours = item.ExpectedHours,
                    ApproverId = item.ApproverID,
                    ApproverName = item.Approver != null && item.Approver.Resource != null
                        ? item.Approver.Resource.FullName
                        : null,
                    Status = item.Status,
                    ApprovalNote = item.ApprovalNote,
                    ReviewedAt = item.ReviewedAt,
                    CreatedAt = item.CreatedAt
                })
                .FirstAsync();
        }

        private static string NormalizeRequestType(string requestType)
        {
            if (string.IsNullOrWhiteSpace(requestType))
            {
                throw new ArgumentException("RequestTypeRequired");
            }

            return requestType.Trim().ToLowerInvariant() switch
            {
                "leave" or "leave-request" or "leave-requests" => LeaveType,
                "ot" or "ot-request" or "ot-requests" => OTType,
                _ => throw new ArgumentException("requestType chi nhan leave hoac ot.")
            };
        }

        private static string NormalizeReviewStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("StatusRequired");
            }

            if (string.Equals(status.Trim(), ApprovedStatus, StringComparison.OrdinalIgnoreCase))
            {
                return ApprovedStatus;
            }

            if (string.Equals(status.Trim(), RejectedStatus, StringComparison.OrdinalIgnoreCase))
            {
                return RejectedStatus;
            }

            throw new ArgumentException("InvalidRequestStatus");
        }

        private static void EnsurePending(string status)
        {
            if (!string.Equals(status, PendingStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("RequestAlreadyProcessed");
            }
        }

        private static void ValidateExpectedHours(decimal expectedHours)
        {
            if (expectedHours <= 0 || expectedHours > MaxHoursPerDay)
            {
                throw new ArgumentException("InvalidExpectedHours");
            }
        }

        private static void ValidatePositiveId(int value, string fieldName)
        {
            if (value <= 0)
            {
                throw new ArgumentException($"{fieldName} phai lon hon 0.");
            }
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
            public int WorkspaceID { get; set; }
            public int WorkspaceRoleID { get; set; }
            public string RoleName { get; set; } = string.Empty;
        }
    }
}
