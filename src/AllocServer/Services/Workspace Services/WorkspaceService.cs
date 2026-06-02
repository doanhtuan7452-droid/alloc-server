using AllocServer.Data;
using AllocServer.DTOs.Workspaces;
using AllocServer.Interfaces;
using AllocServer.Interfaces.Workspaces;
using AllocServer.Models;
using AllocServer.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Workspace_Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeatureQuotaService _featureQuotaService;

        public WorkspaceService(
            ApplicationDbContext context,
            IFeatureQuotaService featureQuotaService)
        {
            _context = context;
            _featureQuotaService = featureQuotaService;
        }

        public async Task<List<WorkspaceListItemResponse>> GetCurrentUserWorkspacesAsync(int accountId)
        {
            return await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member =>
                    member.Status == "Active"
                    && member.Resource.AccountID == accountId)
                .OrderByDescending(member => member.JoinedAt)
                .ThenByDescending(member => member.WorkspaceID)
                .Select(member => new WorkspaceListItemResponse
                {
                    WorkspaceID = member.Workspace.WorkspaceID,
                    Name = member.Workspace.Name,
                    Type = member.Workspace.Type,
                    CreatedAt = member.Workspace.CreatedAt,
                    Membership = new WorkspaceMembershipResponse
                    {
                        WorkspaceMemberID = member.WorkspaceMemberID,
                        ResourceID = member.ResourceID,
                        EmployeeCode = member.EmployeeCode,
                        Status = member.Status,
                        JoinedAt = member.JoinedAt,
                        Role = new WorkspaceRoleSummaryResponse
                        {
                            WorkspaceRoleID = member.WorkspaceRole.WorkspaceRoleID,
                            RoleName = member.WorkspaceRole.RoleName
                        }
                    }
                })
                .ToListAsync();
        }

        public async Task<WorkspaceDetailResponse?> GetWorkspaceDetailsAsync(
            int accountId,
            int workspaceId)
        {
            var workspace = await _context.Workspaces
                .AsNoTracking()
                .Where(item => item.WorkspaceID == workspaceId && !item.IsDeleted)
                .Select(item => new
                {
                    item.WorkspaceID,
                    item.Name,
                    item.Type,
                    item.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (workspace == null)
                return null;

            var currentUserMembership = await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member =>
                    member.WorkspaceID == workspaceId
                    && member.Status == "Active"
                    && member.Resource.AccountID == accountId)
                .Select(member => new WorkspaceMembershipResponse
                {
                    WorkspaceMemberID = member.WorkspaceMemberID,
                    ResourceID = member.ResourceID,
                    EmployeeCode = member.EmployeeCode,
                    Status = member.Status,
                    JoinedAt = member.JoinedAt,
                    Role = new WorkspaceRoleSummaryResponse
                    {
                        WorkspaceRoleID = member.WorkspaceRole.WorkspaceRoleID,
                        RoleName = member.WorkspaceRole.RoleName
                    }
                })
                .FirstOrDefaultAsync();

            if (currentUserMembership == null)
                return null;

            var memberStatuses = await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member => member.WorkspaceID == workspaceId)
                .Select(member => member.Status)
                .ToListAsync();

            var projectStatuses = await _context.Projects
                .AsNoTracking()
                .Where(project => project.WorkspaceID == workspaceId && !project.IsDeleted)
                .Select(project => project.Status)
                .ToListAsync();

            return new WorkspaceDetailResponse
            {
                WorkspaceID = workspace.WorkspaceID,
                Name = workspace.Name,
                Type = workspace.Type,
                CreatedAt = workspace.CreatedAt,
                CurrentUserMembership = currentUserMembership,
                MemberSummary = new WorkspaceMemberSummaryResponse
                {
                    TotalMembers = memberStatuses.Count,
                    ActiveMembers = memberStatuses.Count(status => status == "Active"),
                    PendingInvites = memberStatuses.Count(status => status == "Pending_Invite"),
                    DeactivatedMembers = memberStatuses.Count(status => status == "Deactivated")
                },
                ProjectSummary = new WorkspaceProjectSummaryResponse
                {
                    TotalProjects = projectStatuses.Count,
                    PlanningProjects = projectStatuses.Count(status => status == "Planning"),
                    InProgressProjects = projectStatuses.Count(status => status == "In Progress"),
                    CompletedProjects = projectStatuses.Count(status => status == "Completed"),
                    OnHoldProjects = projectStatuses.Count(status => status == "On Hold"),
                    CancelledProjects = projectStatuses.Count(status => status == "Cancelled")
                },
                CurrentPlan = await GetCurrentPlanAsync(workspaceId)
            };
        }

        public async Task<PagedWorkspaceMembersResponse> GetWorkspaceMembersAsync(
            int workspaceId,
            GetWorkspaceMembersQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var search = NormalizeOptionalString(query.Search);

            var membersQuery = _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member => member.WorkspaceID == workspaceId);

            if (!string.IsNullOrEmpty(search))
            {
                membersQuery = membersQuery.Where(member =>
                    member.EmployeeCode.Contains(search)
                    || member.Resource.FullName.Contains(search));
            }

            var totalItems = await membersQuery.CountAsync();
            var items = await membersQuery
                .OrderBy(member => member.Status == "Active" ? 0
                    : member.Status == "Pending_Invite" ? 1
                    : 2)
                .ThenBy(member => member.Resource.FullName)
                .ThenBy(member => member.WorkspaceMemberID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(member => new WorkspaceMemberDetailResponse
                {
                    WorkspaceMemberID = member.WorkspaceMemberID,
                    Resource = new WorkspaceMemberResourceResponse
                    {
                        ResourceID = member.Resource.ResourceID,
                        FullName = member.Resource.FullName,
                        PhoneNumber = member.Resource.PhoneNumber,
                        AvatarURL = member.Resource.AvatarURL,
                        Timezone = member.Resource.Timezone
                    },
                    EmployeeCode = member.EmployeeCode,
                    Status = member.Status,
                    JoinedAt = member.JoinedAt,
                    Role = new WorkspaceRoleSummaryResponse
                    {
                        WorkspaceRoleID = member.WorkspaceRole.WorkspaceRoleID,
                        RoleName = member.WorkspaceRole.RoleName
                    }
                })
                .ToListAsync();

            return new PagedWorkspaceMembersResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        public async Task<List<WorkspaceProjectListItemResponse>> GetWorkspaceProjectsAsync(
            int workspaceId,
            GetWorkspaceProjectsQuery query)
        {
            var status = NormalizeProjectStatus(query.Status);

            var projectsQuery = _context.Projects
                .AsNoTracking()
                .Where(project => project.WorkspaceID == workspaceId && !project.IsDeleted);

            if (!string.IsNullOrEmpty(status))
            {
                projectsQuery = projectsQuery.Where(project => project.Status == status);
            }

            return await projectsQuery
                .OrderByDescending(project => project.CreatedAt)
                .ThenByDescending(project => project.ProjectID)
                .Select(project => new WorkspaceProjectListItemResponse
                {
                    ProjectID = project.ProjectID,
                    ProjectName = project.ProjectName,
                    ExpectedBudget = project.ExpectedBudget,
                    TotalRevenue = project.TotalRevenue,
                    StartDate = project.StartDate,
                    EndDate = project.EndDate,
                    Status = project.Status,
                    OriginalCurrencyCode = project.OriginalCurrencyCode,
                    ExchangeRateToUSD = project.ExchangeRateToUSD,
                    Methodology = project.Methodology,
                    CreatedAt = project.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ProjectDetailResponse> CreateProjectAsync(
            int accountId,
            int workspaceId,
            CreateProjectRequest request)
        {
            var currentUserMembership = await GetActiveOwnerMembershipAsync(accountId, workspaceId);
            if (currentUserMembership == null)
            {
                throw new UnauthorizedAccessException("Chỉ Owner mới có quyền tạo dự án.");
            }

            if (request.StartDate == null || request.EndDate == null)
            {
                throw new ArgumentException("Ngay bat dau va ngay ket thuc la bat buoc.");
            }

            if (request.EndDate.Value < request.StartDate.Value)
            {
                throw new ArgumentException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
            }

            var projectName = request.ProjectName.Trim();

            var methodology = NormalizeOptionalString(request.Methodology) ?? "Agile";
            if (!IsAllowedMethodology(methodology))
            {
                throw new ArgumentException("Methodology chi nhan Agile, Waterfall, Scrum, Kanban hoac Hybrid.");
            }

            var currencyCode = NormalizeOptionalString(request.OriginalCurrencyCode)?.ToUpperInvariant() ?? "USD";
            if (currencyCode.Length > 5)
            {
                throw new ArgumentException("OriginalCurrencyCode khong duoc vuot qua 5 ky tu.");
            }

            if (request.ExchangeRateToUSD <= 0 || request.ExchangeRateToUSD > 999999.9999m)
            {
                throw new ArgumentException("ExchangeRateToUSD phai lon hon 0 va nho hon hoac bang 999999.9999.");
            }

            var project = new Project
            {
                WorkspaceID = workspaceId,
                ProjectName = projectName,
                ExpectedBudget = request.ExpectedBudget,
                TotalRevenue = 0,
                StartDate = request.StartDate.Value,
                EndDate = request.EndDate.Value,
                Status = "Planning",
                OriginalCurrencyCode = currencyCode,
                ExchangeRateToUSD = request.ExchangeRateToUSD,
                Methodology = methodology
            };

            _context.Projects.Add(project);
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // SQL Server Error 2601 or 2627: Unique constraint violation
                if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && 
                    (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    throw new InvalidOperationException("Tên dự án đã tồn tại trong Workspace.");
                }
                throw; // Ném lại lỗi nếu không phải lỗi trùng tên
            }

            return new ProjectDetailResponse
            {
                ProjectID = project.ProjectID,
                WorkspaceID = project.WorkspaceID,
                ProjectName = project.ProjectName,
                ExpectedBudget = project.ExpectedBudget,
                TotalRevenue = project.TotalRevenue,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status,
                OriginalCurrencyCode = project.OriginalCurrencyCode,
                ExchangeRateToUSD = project.ExchangeRateToUSD,
                Methodology = project.Methodology,
                BaselineData = project.BaselineData,
                CreatedAt = project.CreatedAt
            };
        }

        public async Task<List<WorkspaceRoleSummaryResponse>> GetWorkspaceRolesAsync(int workspaceId)
        {
            return await _context.WorkspaceRoles
                .AsNoTracking()
                .Where(role => role.WorkspaceID == workspaceId && !role.IsDeleted)
                .OrderBy(role => role.RoleName)
                .Select(role => new WorkspaceRoleSummaryResponse
                {
                    WorkspaceRoleID = role.WorkspaceRoleID,
                    RoleName = role.RoleName
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateWorkspaceAsync(int accountId, int workspaceId, UpdateWorkspaceRequest request)
        {
            // Kiểm tra Role của người dùng hiện tại
            var currentUserMembership = await _context.WorkspaceMembers
                .AsNoTracking()
                .Include(m => m.WorkspaceRole)
                .Include(m => m.Resource)
                .Where(m => m.WorkspaceID == workspaceId 
                         && m.Resource.AccountID == accountId 
                         && m.Status == "Active")
                .FirstOrDefaultAsync();

            if (currentUserMembership == null)
                return false; // Không phải thành viên hoặc tài khoản không hợp lệ

            // BẢO MẬT: Chỉ cho phép Owner cập nhật
            if (currentUserMembership.WorkspaceRole.RoleName != "Owner")
                throw new UnauthorizedAccessException("Chỉ Owner mới có quyền cập nhật thông tin Workspace.");

            // Lấy Workspace để cập nhật
            var workspace = await _context.Workspaces
                .Where(w => w.WorkspaceID == workspaceId && !w.IsDeleted)
                .FirstOrDefaultAsync();

            if (workspace == null)
                return false;

            // Cập nhật các trường được phép
            workspace.Name = request.Name;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteWorkspaceAsync(int accountId, int workspaceId)
        {
            var currentUserMembership = await _context.WorkspaceMembers
                .AsNoTracking()
                .Include(m => m.Workspace)
                .Include(m => m.WorkspaceRole)
                .Include(m => m.Resource)
                .Where(m => m.WorkspaceID == workspaceId
                         && m.Resource.AccountID == accountId
                         && m.Status == "Active"
                         && !m.Workspace.IsDeleted)
                .FirstOrDefaultAsync();

            if (currentUserMembership == null)
                return false;

            if (currentUserMembership.WorkspaceRole.RoleName != "Owner")
                throw new UnauthorizedAccessException("Chi Owner moi co quyen xoa Workspace.");

            var deletedAt = DateTime.UtcNow;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var affectedWorkspaces = await _context.Workspaces
                    .Where(w => w.WorkspaceID == workspaceId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(w => w.IsDeleted, true)
                        .SetProperty(w => w.DeletedAt, deletedAt)
                        .SetProperty(w => w.DeletedBy, accountId));

                if (affectedWorkspaces == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await _context.WorkspaceMembers
                    .Where(m => m.WorkspaceID == workspaceId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(m => m.Status, "Deactivated"));

                await _context.Projects
                    .Where(p => p.WorkspaceID == workspaceId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.IsDeleted, true)
                        .SetProperty(p => p.DeletedAt, deletedAt)
                        .SetProperty(p => p.DeletedBy, accountId));

                await _context.WorkspaceRoles
                    .Where(r => r.WorkspaceID == workspaceId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(r => r.IsDeleted, true)
                        .SetProperty(r => r.DeletedAt, deletedAt)
                        .SetProperty(r => r.DeletedBy, accountId));

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<WorkspaceMemberDetailResponse?> InviteMemberAsync(
            int accountId,
            int workspaceId,
            InviteWorkspaceMemberRequest request)
        {
            var currentUserMembership = await GetActiveOwnerMembershipAsync(accountId, workspaceId);
            if (currentUserMembership == null)
                return null;

            var email = NormalizeOptionalString(request.Email)?.ToLowerInvariant();
            if (email == null)
                throw new ArgumentException("Email khong duoc de trong.");

            var role = await _context.WorkspaceRoles
                .AsNoTracking()
                .Where(item =>
                    item.WorkspaceRoleID == request.WorkspaceRoleID
                    && item.WorkspaceID == workspaceId
                    && !item.IsDeleted)
                .Select(item => new
                {
                    item.WorkspaceRoleID,
                    item.RoleName
                })
                .FirstOrDefaultAsync();

            if (role == null)
                throw new KeyNotFoundException("Khong tim thay vai tro trong Workspace.");

            var resource = await _context.Resources
                .AsNoTracking()
                .Include(item => item.Account)
                .Where(item =>
                    item.Account != null
                    && item.Account.Email.ToLower() == email)
                .Select(item => new
                {
                    item.ResourceID,
                    item.FullName,
                    item.PhoneNumber,
                    item.AvatarURL,
                    item.Timezone
                })
                .FirstOrDefaultAsync();

            if (resource == null)
                throw new KeyNotFoundException("Khong tim thay tai khoan hoac profile Resource.");

            var isExistingMember = await _context.WorkspaceMembers
                .AsNoTracking()
                .AnyAsync(item =>
                    item.WorkspaceID == workspaceId
                    && item.ResourceID == resource.ResourceID);

            if (isExistingMember)
                throw new InvalidOperationException("Nhan su da ton tai trong Workspace.");

            await EnsureMemberQuotaAvailableAsync(workspaceId);

            var workspaceMember = new WorkspaceMember
            {
                WorkspaceID = workspaceId,
                ResourceID = resource.ResourceID,
                EmployeeCode = $"EMP{resource.ResourceID:D4}",
                WorkspaceRoleID = role.WorkspaceRoleID,
                BaseSalaryMonth = request.BaseSalaryMonth ?? 0,
                OTRatePerHour = request.OTRatePerHour ?? 0,
                Status = "Active"
            };

            _context.WorkspaceMembers.Add(workspaceMember);
            await _context.SaveChangesAsync();

            return new WorkspaceMemberDetailResponse
            {
                WorkspaceMemberID = workspaceMember.WorkspaceMemberID,
                Resource = new WorkspaceMemberResourceResponse
                {
                    ResourceID = resource.ResourceID,
                    FullName = resource.FullName,
                    PhoneNumber = resource.PhoneNumber,
                    AvatarURL = resource.AvatarURL,
                    Timezone = resource.Timezone
                },
                EmployeeCode = workspaceMember.EmployeeCode,
                Status = workspaceMember.Status,
                JoinedAt = workspaceMember.JoinedAt,
                Role = new WorkspaceRoleSummaryResponse
                {
                    WorkspaceRoleID = role.WorkspaceRoleID,
                    RoleName = role.RoleName
                }
            };
        }

        public async Task<bool> UpdateMemberStatusAsync(
            int accountId,
            int workspaceId,
            int targetMemberId,
            UpdateMemberStatusRequest request)
        {
            var targetStatus = NormalizeMemberStatus(request.Status);
            if (targetStatus == null)
                throw new ArgumentException("Status chi nhan Active hoac Deactivated.");

            var currentUserMembership = await GetActiveOwnerMembershipAsync(accountId, workspaceId);
            if (currentUserMembership == null)
                return false;

            if (targetStatus == "Deactivated"
                && currentUserMembership.WorkspaceMemberID == targetMemberId)
            {
                throw new InvalidOperationException("Owner khong the tu vo hieu hoa chinh minh.");
            }

            var targetMember = await _context.WorkspaceMembers
                .Where(item =>
                    item.WorkspaceMemberID == targetMemberId
                    && item.WorkspaceID == workspaceId)
                .FirstOrDefaultAsync();

            if (targetMember == null)
                return false;

            if (targetMember.Status == targetStatus)
                return true;

            if (targetMember.Status == "Deactivated" && targetStatus == "Active")
            {
                await EnsureMemberQuotaAvailableAsync(workspaceId);
            }

            targetMember.Status = targetStatus;
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<WorkspaceMember?> GetActiveOwnerMembershipAsync(int accountId, int workspaceId)
        {
            var membership = await _context.WorkspaceMembers
                .AsNoTracking()
                .Include(item => item.Workspace)
                .Include(item => item.WorkspaceRole)
                .Include(item => item.Resource)
                .Where(item =>
                    item.WorkspaceID == workspaceId
                    && item.Resource.AccountID == accountId
                    && item.Status == "Active"
                    && !item.Workspace.IsDeleted)
                .FirstOrDefaultAsync();

            if (membership == null)
                return null;

            if (membership.WorkspaceRole.RoleName != "Owner")
                throw new UnauthorizedAccessException("Chi Owner moi co quyen quan tri nhan su Workspace.");

            return membership;
        }

        private async Task EnsureMemberQuotaAvailableAsync(int workspaceId)
        {
            var currentMemberCount = await _context.WorkspaceMembers
                .AsNoTracking()
                .CountAsync(item =>
                    item.WorkspaceID == workspaceId
                    && (item.Status == "Active" || item.Status == "Pending_Invite"));

            var hasQuota = await _featureQuotaService.CheckFeatureQuotaAsync(
                workspaceId,
                "MAX_MEMBERS",
                currentMemberCount);

            if (!hasQuota)
            {
                throw new QuotaExceededException("Workspace da dat gioi han so luong thanh vien cua goi cuoc.");
            }
        }

        private async Task<WorkspacePlanSummaryResponse?> GetCurrentPlanAsync(int workspaceId)
        {
            var currentLimits = await _context.WorkspaceCurrentLimits
                .AsNoTracking()
                .Where(limit => limit.WorkspaceID == workspaceId)
                .OrderBy(limit => limit.FeatureCode)
                .Select(limit => new
                {
                    limit.PlanCode,
                    Limit = new WorkspaceFeatureLimitResponse
                    {
                        FeatureCode = limit.FeatureCode,
                        IsIncluded = limit.IsIncluded,
                        LimitValue = limit.LimitValue
                    }
                })
                .ToListAsync();

            if (currentLimits.Count == 0)
                return null;

            return new WorkspacePlanSummaryResponse
            {
                PlanCode = currentLimits[0].PlanCode,
                Limits = currentLimits.Select(item => item.Limit).ToList()
            };
        }

        private static string? NormalizeProjectStatus(string? status)
        {
            var normalized = NormalizeOptionalString(status);
            if (normalized == null)
                return null;

            return normalized.ToUpperInvariant() switch
            {
                "INPROGRESS" or "IN PROGRESS" => "In Progress",
                "ONHOLD" or "ON HOLD" => "On Hold",
                "PLANNING" => "Planning",
                "COMPLETED" => "Completed",
                "CANCELLED" => "Cancelled",
                _ => normalized
            };
        }

        private static string? NormalizeMemberStatus(string? status)
        {
            var normalized = NormalizeOptionalString(status);
            if (normalized == null)
                return null;

            return normalized.ToUpperInvariant() switch
            {
                "ACTIVE" => "Active",
                "DEACTIVATED" => "Deactivated",
                _ => null
            };
        }

        private static bool IsAllowedMethodology(string methodology)
        {
            return methodology is "Agile" or "Waterfall" or "Scrum" or "Kanban" or "Hybrid";
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
