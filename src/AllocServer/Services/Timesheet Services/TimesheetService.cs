using AllocServer.Data;
using AllocServer.DTOs.Timesheets;
using AllocServer.Constants.Permissions;
using AllocServer.Filters;
using AllocServer.Interfaces.Timesheets;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Timesheet_Services
{
    public class TimesheetService : ITimesheetService
    {
        private const int DefaultStandardHoursPerMonth = 160;
        private const decimal MaxHoursPerDay = 24m;

        private readonly ApplicationDbContext _context;
        private readonly int _standardHoursPerMonth;

        public TimesheetService(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _standardHoursPerMonth = Math.Max(
                configuration.GetValue<int?>("TimesheetSettings:StandardHoursPerMonth")
                    ?? DefaultStandardHoursPerMonth,
                1);
        }

        public async Task<PagedTimesheetsResponse> GetTimesheetsAsync(
            int accountId,
            GetTimesheetsQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var (fromDate, toDate) = ResolveDateRange(query.FromDate, query.ToDate);

            if (toDate < fromDate)
            {
                throw new ArgumentException("toDate phai lon hon hoac bang fromDate.");
            }

            ValidatePositiveId(query.WorkspaceId, "workspaceId");
            ValidatePositiveId(query.ProjectId, "projectId");
            ValidatePositiveId(query.TaskId, "taskId");
            ValidatePositiveId(query.MemberId, "memberId");

            var memberIds = await ResolveReadableMemberIdsAsync(accountId, query.MemberId, query.WorkspaceId);

            if (memberIds.Count == 0)
            {
                return EmptyPagedResponse(page, pageSize, fromDate, toDate);
            }

            var timesheetsQuery = _context.Timesheets
                .AsNoTracking()
                .Where(item =>
                    memberIds.Contains(item.WorkspaceMemberID)
                    && item.WorkDate >= fromDate
                    && item.WorkDate <= toDate);

            if (query.TaskId != null)
            {
                timesheetsQuery = timesheetsQuery.Where(item => item.TaskID == query.TaskId.Value);
            }

            if (query.ProjectId != null)
            {
                timesheetsQuery = timesheetsQuery.Where(item =>
                    item.Task != null
                    && item.Task.ProjectID == query.ProjectId.Value);
            }

            if (query.WorkspaceId != null)
            {
                timesheetsQuery = timesheetsQuery.Where(item =>
                    item.Task != null
                    && item.Task.Project != null
                    && item.Task.Project.WorkspaceID == query.WorkspaceId.Value);
            }

            var totalItems = await timesheetsQuery.CountAsync();
            var items = await timesheetsQuery
                .OrderByDescending(item => item.WorkDate)
                .ThenByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.TimesheetID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new TimesheetListItemResponse
                {
                    TimesheetId = item.TimesheetID,
                    TaskId = item.TaskID,
                    TaskName = item.Task != null ? item.Task.TaskName : string.Empty,
                    ProjectId = item.Task != null ? item.Task.ProjectID : 0,
                    ProjectName = item.Task != null && item.Task.Project != null
                        ? item.Task.Project.ProjectName
                        : string.Empty,
                    WorkspaceId = item.Task != null && item.Task.Project != null
                        ? item.Task.Project.WorkspaceID
                        : 0,
                    WorkspaceMemberId = item.WorkspaceMemberID,
                    MemberName = item.WorkspaceMember != null && item.WorkspaceMember.Resource != null
                        ? item.WorkspaceMember.Resource.FullName
                        : string.Empty,
                    WorkDate = item.WorkDate,
                    NormalHours = item.NormalHours,
                    OTHours = item.OTHours,
                    LoggedHourlyRate = item.LoggedHourlyRate,
                    LoggedOTRate = item.LoggedOTRate,
                    TotalCost = (item.NormalHours * item.LoggedHourlyRate)
                        + (item.OTHours * item.LoggedOTRate),
                    CreatedAt = item.CreatedAt
                })
                .ToListAsync();

            return new PagedTimesheetsResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                FromDate = fromDate,
                ToDate = toDate,
                Items = items
            };
        }

        public async Task<(TimesheetDetailResponse Timesheet, bool Created)> UpsertTimesheetAsync(
            int accountId,
            CreateTimesheetRequest request)
        {
            ValidateHours(request.NormalHours, request.OTHours);

            var task = await _context.ProjectTasks
                .Include(item => item.Project)
                .FirstOrDefaultAsync(item =>
                    item.TaskID == request.TaskId
                    && item.Project != null);

            if (task == null || task.Project == null)
            {
                throw new KeyNotFoundException("Khong tim thay Task.");
            }

            ValidateWorkDate(task, request.WorkDate);

            var membership = await _context.WorkspaceMembers
                .Include(item => item.Resource)
                .FirstOrDefaultAsync(item =>
                    item.WorkspaceID == task.Project.WorkspaceID
                    && item.Resource.AccountID == accountId
                    && item.Status == "Active"
                    && !item.Workspace.IsDeleted
                    && !item.Resource.IsDeleted);

            if (membership == null)
            {
                throw new UnauthorizedAccessException("Ban khong phai thanh vien active cua workspace chua Task nay.");
            }

            var loggedHourlyRate = CalculateHourlyRate(membership.BaseSalaryMonth);
            var loggedOTRate = membership.OTRatePerHour;
            var now = DateTime.UtcNow;

            var timesheet = await _context.Timesheets
                .FirstOrDefaultAsync(item =>
                    item.TaskID == task.TaskID
                    && item.WorkspaceMemberID == membership.WorkspaceMemberID
                    && item.WorkDate == request.WorkDate);

            var created = timesheet == null;
            if (timesheet == null)
            {
                timesheet = new Timesheet
                {
                    TaskID = task.TaskID,
                    WorkspaceMemberID = membership.WorkspaceMemberID,
                    WorkDate = request.WorkDate,
                    NormalHours = request.NormalHours,
                    OTHours = request.OTHours,
                    LoggedHourlyRate = loggedHourlyRate,
                    LoggedOTRate = loggedOTRate,
                    CreatedAt = now
                };

                _context.Timesheets.Add(timesheet);
            }
            else
            {
                timesheet.NormalHours = request.NormalHours;
                timesheet.OTHours = request.OTHours;
                timesheet.LoggedHourlyRate = loggedHourlyRate;
                timesheet.LoggedOTRate = loggedOTRate;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (!IsUniqueConstraintViolation(ex))
                {
                    throw;
                }

                _context.Entry(timesheet).State = EntityState.Detached;

                timesheet = await _context.Timesheets
                    .FirstAsync(item =>
                        item.TaskID == task.TaskID
                        && item.WorkspaceMemberID == membership.WorkspaceMemberID
                        && item.WorkDate == request.WorkDate);

                created = false;
                timesheet.NormalHours = request.NormalHours;
                timesheet.OTHours = request.OTHours;
                timesheet.LoggedHourlyRate = loggedHourlyRate;
                timesheet.LoggedOTRate = loggedOTRate;

                await _context.SaveChangesAsync();
            }

            return (await MapTimesheetAsync(timesheet.TimesheetID), created);
        }

        private async Task<List<int>> ResolveReadableMemberIdsAsync(
            int accountId,
            int? requestedMemberId,
            int? workspaceId)
        {
            if (requestedMemberId != null)
            {
                var requestedMember = await _context.WorkspaceMembers
                    .AsNoTracking()
                    .Where(member =>
                        member.WorkspaceMemberID == requestedMemberId.Value
                        && member.Status == "Active"
                        && !member.Workspace.IsDeleted
                        && !member.Resource.IsDeleted)
                    .Select(member => new
                    {
                        member.WorkspaceMemberID,
                        member.WorkspaceID,
                        member.Resource.AccountID
                    })
                    .FirstOrDefaultAsync();

                if (requestedMember == null)
                {
                    throw new KeyNotFoundException("Khong tim thay member active.");
                }

                if (workspaceId != null && requestedMember.WorkspaceID != workspaceId.Value)
                {
                    throw new ArgumentException("memberId khong thuoc workspaceId da truyen.");
                }

                if (requestedMember.AccountID == accountId)
                {
                    return new List<int> { requestedMember.WorkspaceMemberID };
                }

                var canViewAll = await HasWorkspacePermissionAsync(
                    accountId,
                    requestedMember.WorkspaceID,
                    TimesheetPermissionIds.ViewAll);

                if (!canViewAll)
                {
                    throw new UnauthorizedAccessException("Ban khong co quyen xem timesheet cua member khac.");
                }

                return new List<int> { requestedMember.WorkspaceMemberID };
            }

            var currentMembershipsQuery = _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member =>
                    member.Resource.AccountID == accountId
                    && member.Status == "Active"
                    && !member.Workspace.IsDeleted
                    && !member.Resource.IsDeleted);

            if (workspaceId != null)
            {
                currentMembershipsQuery = currentMembershipsQuery.Where(member => member.WorkspaceID == workspaceId.Value);
            }

            var memberIds = await currentMembershipsQuery
                .Select(member => member.WorkspaceMemberID)
                .ToListAsync();

            if (workspaceId != null && memberIds.Count == 0)
            {
                throw new UnauthorizedAccessException("Ban khong phai thanh vien active cua workspace nay.");
            }

            return memberIds;
        }

        private async Task<bool> HasWorkspacePermissionAsync(
            int accountId,
            int workspaceId,
            string permissionId)
        {
            var membership = await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member =>
                    member.WorkspaceID == workspaceId
                    && member.Resource.AccountID == accountId
                    && member.Status == "Active"
                    && !member.Workspace.IsDeleted)
                .Select(member => new
                {
                    member.WorkspaceRoleID,
                    member.WorkspaceRole.RoleName
                })
                .FirstOrDefaultAsync();

            if (membership == null)
            {
                return false;
            }

            if (string.Equals(membership.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return await _context.RolePermissions
                .AsNoTracking()
                .AnyAsync(rolePermission =>
                    rolePermission.WorkspaceRoleID == membership.WorkspaceRoleID
                    && rolePermission.PermissionID == permissionId);
        }

        private async Task<TimesheetDetailResponse> MapTimesheetAsync(int timesheetId)
        {
            return await _context.Timesheets
                .AsNoTracking()
                .Where(item => item.TimesheetID == timesheetId)
                .Select(item => new TimesheetDetailResponse
                {
                    TimesheetId = item.TimesheetID,
                    TaskId = item.TaskID,
                    TaskName = item.Task != null ? item.Task.TaskName : string.Empty,
                    ProjectId = item.Task != null ? item.Task.ProjectID : 0,
                    ProjectName = item.Task != null && item.Task.Project != null
                        ? item.Task.Project.ProjectName
                        : string.Empty,
                    WorkspaceId = item.Task != null && item.Task.Project != null
                        ? item.Task.Project.WorkspaceID
                        : 0,
                    WorkspaceMemberId = item.WorkspaceMemberID,
                    MemberName = item.WorkspaceMember != null && item.WorkspaceMember.Resource != null
                        ? item.WorkspaceMember.Resource.FullName
                        : string.Empty,
                    WorkDate = item.WorkDate,
                    NormalHours = item.NormalHours,
                    OTHours = item.OTHours,
                    LoggedHourlyRate = item.LoggedHourlyRate,
                    LoggedOTRate = item.LoggedOTRate,
                    TotalCost = (item.NormalHours * item.LoggedHourlyRate)
                        + (item.OTHours * item.LoggedOTRate),
                    CreatedAt = item.CreatedAt
                })
                .FirstAsync();
        }

        private static (DateOnly FromDate, DateOnly ToDate) ResolveDateRange(
            DateOnly? requestedFromDate,
            DateOnly? requestedToDate)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var defaultFromDate = new DateOnly(today.Year, today.Month, 1);
            var defaultToDate = defaultFromDate.AddMonths(1).AddDays(-1);

            return (
                requestedFromDate ?? defaultFromDate,
                requestedToDate ?? defaultToDate);
        }

        private static void ValidatePositiveId(int? value, string fieldName)
        {
            if (value is <= 0)
            {
                throw new ArgumentException($"{fieldName} phai lon hon 0.");
            }
        }

        private static void ValidateHours(decimal normalHours, decimal otHours)
        {
            if (normalHours < 0 || otHours < 0)
            {
                throw new ArgumentException("So gio lam viec khong duoc am.");
            }

            if (normalHours + otHours <= 0)
            {
                throw new ArgumentException("Tong so gio lam viec phai lon hon 0.");
            }

            if (normalHours + otHours > MaxHoursPerDay)
            {
                throw new ArgumentException("Tong so gio lam viec trong ngay khong duoc vuot qua 24.");
            }
        }

        private static void ValidateWorkDate(ProjectTask task, DateOnly workDate)
        {
            if (task.StartDate != null && workDate < task.StartDate.Value)
            {
                throw new ArgumentException("Ngay lam viec khong duoc nho hon ngay bat dau task.");
            }

            if (task.EndDate != null && workDate > task.EndDate.Value)
            {
                throw new ArgumentException("Ngay lam viec khong duoc lon hon ngay ket thuc task.");
            }
        }

        private decimal CalculateHourlyRate(decimal baseSalaryMonth)
        {
            return Math.Round(baseSalaryMonth / _standardHoursPerMonth, 2, MidpointRounding.AwayFromZero);
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlException
                   && (sqlException.Number == 2601 || sqlException.Number == 2627);
        }

        private static PagedTimesheetsResponse EmptyPagedResponse(
            int page,
            int pageSize,
            DateOnly fromDate,
            DateOnly toDate)
        {
            return new PagedTimesheetsResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = 0,
                TotalPages = 0,
                FromDate = fromDate,
                ToDate = toDate,
                Items = new List<TimesheetListItemResponse>()
            };
        }
    }
}
