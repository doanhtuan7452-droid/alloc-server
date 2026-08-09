using AllocServer.Data;
using AllocServer.DTOs.Workspaces;
using AllocServer.Interfaces;
using AllocServer.Interfaces.Workspaces;
using AllocServer.Models;
using AllocServer.Exceptions;
using AllocServer.Events;
using AllocServer.Events.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace AllocServer.Services.Workspace_Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeatureQuotaService _featureQuotaService;
        private readonly IDistributedCache _cache;
        private readonly IEventPublisher _eventPublisher;

        public WorkspaceService(
            ApplicationDbContext context,
            IFeatureQuotaService featureQuotaService,
            IDistributedCache cache,
            IEventPublisher eventPublisher)
        {
            _context = context;
            _featureQuotaService = featureQuotaService;
            _cache = cache;
            _eventPublisher = eventPublisher;
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
                    item.CreatedAt,
                    item.StandardHours
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
                StandardHours = workspace.StandardHours,
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

            var queryResult = from project in projectsQuery
                              join s in _context.ProjectProgressStats on project.ProjectID equals s.ProjectID into statsGroup
                              from pgStat in statsGroup.DefaultIfEmpty()
                              select new { Project = project, Stat = pgStat };

            return await queryResult
                .OrderByDescending(item => item.Project.CreatedAt)
                .ThenByDescending(item => item.Project.ProjectID)
                .Select(item => new WorkspaceProjectListItemResponse
                {
                    ProjectID = item.Project.ProjectID,
                    ProjectName = item.Project.ProjectName,
                    ExpectedBudget = item.Project.ExpectedBudget,
                    TotalRevenue = item.Project.TotalRevenue,
                    StartDate = item.Project.StartDate,
                    EndDate = item.Project.EndDate,
                    Status = item.Project.Status,
                    OriginalCurrencyCode = item.Project.OriginalCurrencyCode,
                    ExchangeRateToUSD = item.Project.ExchangeRateToUSD,
                    Methodology = item.Project.Methodology,
                    CreatedAt = item.Project.CreatedAt,
                    Progress = (double?)item.Stat.WeightedProgress ?? 0.0
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
                throw new UnauthorizedAccessException("OwnerProjectCreationOnly");
            }

            if (request.StartDate == null || request.EndDate == null)
            {
                throw new ArgumentException("StartEndDateRequired");
            }

            if (request.EndDate.Value < request.StartDate.Value)
            {
                throw new ArgumentException("EndDateBeforeStartDate");
            }

            var projectName = request.ProjectName.Trim();

            var methodology = NormalizeOptionalString(request.Methodology) ?? "Agile";
            if (!IsAllowedMethodology(methodology))
            {
                throw new ArgumentException("InvalidMethodology");
            }

            var currencyCode = NormalizeOptionalString(request.OriginalCurrencyCode)?.ToUpperInvariant() ?? "USD";
            if (currencyCode.Length > 5)
            {
                throw new ArgumentException("CurrencyCodeTooLong");
            }

            if (request.ExchangeRateToUSD <= 0 || request.ExchangeRateToUSD > 999999.999999999999m)
            {
                throw new ArgumentException("InvalidExchangeRate");
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
                
                var creatorMemberId = currentUserMembership.WorkspaceMemberID;
                await _eventPublisher.PublishAsync(new ProjectCreatedEvent(project.ProjectID, project.ProjectName, creatorMemberId, workspaceId));
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && 
                    (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    throw new InvalidOperationException("ProjectNameExists");
                }
                throw; 
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
                CreatedAt = project.CreatedAt,
                Progress = 0.0
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
                throw new UnauthorizedAccessException("OwnerWorkspaceUpdateOnly");

            // Lấy Workspace để cập nhật
            var workspace = await _context.Workspaces
                .Where(w => w.WorkspaceID == workspaceId && !w.IsDeleted)
                .FirstOrDefaultAsync();

            if (workspace == null)
                return false;

            // Cập nhật các trường được phép
            workspace.Name = request.Name;
            if (request.StandardHours.HasValue)
            {
                workspace.StandardHours = request.StandardHours.Value;
            }
            
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
                throw new UnauthorizedAccessException("OwnerWorkspaceDeleteOnly");

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
                throw new ArgumentException("EmailRequired");

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
                throw new KeyNotFoundException("RoleNotFound");

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
                throw new KeyNotFoundException("AccountProfileNotFound");

            var isExistingMember = await _context.WorkspaceMembers
                .AsNoTracking()
                .AnyAsync(item =>
                    item.WorkspaceID == workspaceId
                    && item.ResourceID == resource.ResourceID);

            if (isExistingMember)
                throw new InvalidOperationException("MemberAlreadyExists");

            await EnsureMemberQuotaAvailableAsync(workspaceId);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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

                // Auto-create default profile inside transaction
                var profile = new WorkspaceMemberProfile
                {
                    WorkspaceMemberID = workspaceMember.WorkspaceMemberID,
                    ExperienceYears = 0,
                    EducationLevel = "Bachelor",
                    LastEvaluatedAt = DateTime.UtcNow
                };
                _context.WorkspaceMemberProfiles.Add(profile);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

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
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateMemberStatusAsync(
            int accountId,
            int workspaceId,
            int targetMemberId,
            UpdateMemberStatusRequest request)
        {
            var targetStatus = NormalizeMemberStatus(request.Status);
            if (targetStatus == null)
                throw new ArgumentException("InvalidMemberStatus");

            var currentUserMembership = await GetActiveOwnerMembershipAsync(accountId, workspaceId);
            if (currentUserMembership == null)
                return false;

            if (targetStatus == "Deactivated"
                && currentUserMembership.WorkspaceMemberID == targetMemberId)
            {
                throw new InvalidOperationException("OwnerCannotDeactivateSelf");
            }

            var targetMember = await _context.WorkspaceMembers
                .Include(item => item.Resource)
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

            if (targetMember.Resource?.AccountID != null)
            {
                var cacheKey = $"workspace_auth_{targetMember.Resource.AccountID}_{workspaceId}";
                await _cache.RemoveAsync(cacheKey);
            }

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
                throw new UnauthorizedAccessException("OwnerMemberManagementOnly");

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
                throw new QuotaExceededException("MemberQuotaExceeded");
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

        public async Task<WorkspaceRoleSummaryResponse> CreateWorkspaceRoleAsync(int accountId, int workspaceId, CreateWorkspaceRoleRequest request)
        {
            var currentUserMembership = await GetActiveOwnerMembershipAsync(accountId, workspaceId);
            if (currentUserMembership == null)
            {
                var workspaceExists = await _context.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
                if (!workspaceExists)
                {
                    throw new KeyNotFoundException("WorkspaceNotFound");
                }
                throw new UnauthorizedAccessException("OwnerMemberManagementOnly");
            }

            var roleName = NormalizeOptionalString(request.RoleName);
            if (string.IsNullOrEmpty(roleName))
            {
                throw new ArgumentException("RoleNameRequired");
            }

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var isDuplicate = await _context.WorkspaceRoles.AnyAsync(r => r.WorkspaceID == workspaceId && r.RoleName == roleName && !r.IsDeleted);
                if (isDuplicate)
                {
                    throw new InvalidOperationException("RoleNameExists");
                }

                var newRole = new WorkspaceRole
                {
                    WorkspaceID = workspaceId,
                    RoleName = roleName,
                    IsTemplate = false,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.WorkspaceRoles.Add(newRole);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new WorkspaceRoleSummaryResponse
                {
                    WorkspaceRoleID = newRole.WorkspaceRoleID,
                    RoleName = newRole.RoleName
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateWorkspaceRoleAsync(int accountId, int workspaceId, int roleId, UpdateWorkspaceRoleRequest request)
        {
            var currentUserMembership = await GetActiveOwnerMembershipAsync(accountId, workspaceId);
            if (currentUserMembership == null)
            {
                var workspaceExists = await _context.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
                if (!workspaceExists)
                {
                    throw new KeyNotFoundException("WorkspaceNotFound");
                }
                throw new UnauthorizedAccessException("OwnerMemberManagementOnly");
            }

            var roleName = NormalizeOptionalString(request.RoleName);
            if (string.IsNullOrEmpty(roleName))
            {
                throw new ArgumentException("RoleNameRequired");
            }

            var role = await _context.WorkspaceRoles.FirstOrDefaultAsync(r => r.WorkspaceRoleID == roleId && (r.WorkspaceID == workspaceId || r.WorkspaceID == null) && !r.IsDeleted);
            if (role == null)
            {
                throw new KeyNotFoundException("RoleNotFound");
            }

            if (role.IsTemplate || role.WorkspaceID == null || string.Equals(role.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("CannotModifySystemRole");
            }

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var isDuplicate = await _context.WorkspaceRoles.AnyAsync(r => r.WorkspaceID == workspaceId && r.RoleName == roleName && r.WorkspaceRoleID != roleId && !r.IsDeleted);
                if (isDuplicate)
                {
                    throw new InvalidOperationException("RoleNameExists");
                }

                role.RoleName = roleName;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await _eventPublisher.PublishAsync(new WorkspaceRoleUpdatedEvent(workspaceId, roleId, "RoleNameUpdated"));
            return true;
        }

        public async Task<bool> DeleteWorkspaceRoleAsync(int accountId, int workspaceId, int roleId)
        {
            var currentUserMembership = await GetActiveOwnerMembershipAsync(accountId, workspaceId);
            if (currentUserMembership == null)
            {
                var workspaceExists = await _context.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
                if (!workspaceExists)
                {
                    throw new KeyNotFoundException("WorkspaceNotFound");
                }
                throw new UnauthorizedAccessException("OwnerMemberManagementOnly");
            }

            var role = await _context.WorkspaceRoles.FirstOrDefaultAsync(r => r.WorkspaceRoleID == roleId && (r.WorkspaceID == workspaceId || r.WorkspaceID == null) && !r.IsDeleted);
            if (role == null)
            {
                throw new KeyNotFoundException("RoleNotFound");
            }

            if (role.IsTemplate || role.WorkspaceID == null || string.Equals(role.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("CannotDeleteSystemRole");
            }

            var isAssigned = await _context.WorkspaceMembers.AnyAsync(m => m.WorkspaceRoleID == roleId && m.Status != "Deactivated");
            if (isAssigned)
            {
                throw new InvalidOperationException("RoleCannotDeleteAssigned");
            }

            role.IsDeleted = true;
            role.DeletedAt = DateTime.UtcNow;
            role.DeletedBy = accountId;

            await _context.SaveChangesAsync();

            await _eventPublisher.PublishAsync(new WorkspaceRoleUpdatedEvent(workspaceId, roleId, "RoleDeleted"));
            return true;
        }

        public async Task<bool> UpdateRolePermissionsAsync(int accountId, int workspaceId, int roleId, UpdateRolePermissionsRequest request)
        {
            var currentUserMembership = await GetActiveOwnerMembershipAsync(accountId, workspaceId);
            if (currentUserMembership == null)
            {
                var workspaceExists = await _context.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
                if (!workspaceExists)
                {
                    throw new KeyNotFoundException("WorkspaceNotFound");
                }
                throw new UnauthorizedAccessException("OwnerMemberManagementOnly");
            }

            var role = await _context.WorkspaceRoles.FirstOrDefaultAsync(r => r.WorkspaceRoleID == roleId && (r.WorkspaceID == workspaceId || r.WorkspaceID == null) && !r.IsDeleted);
            if (role == null)
            {
                throw new KeyNotFoundException("RoleNotFound");
            }

            if (role.IsTemplate || role.WorkspaceID == null || string.Equals(role.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("CannotModifySystemRole");
            }

            var validPermissions = await _context.WorkspacePermissions
                .Select(p => p.PermissionID)
                .ToListAsync();

            var invalidPermission = request.PermissionIds.FirstOrDefault(p => !validPermissions.Contains(p));
            if (invalidPermission != null)
            {
                throw new ArgumentException("PermissionNotFound");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingPermissions = await _context.RolePermissions
                    .Where(rp => rp.WorkspaceRoleID == roleId)
                    .ToListAsync();

                var toRemove = existingPermissions
                    .Where(ep => !request.PermissionIds.Contains(ep.PermissionID))
                    .ToList();

                var existingIds = existingPermissions.Select(ep => ep.PermissionID).ToList();
                var toAdd = request.PermissionIds
                    .Where(pId => !existingIds.Contains(pId))
                    .Select(pId => new RolePermission
                    {
                        WorkspaceRoleID = roleId,
                        PermissionID = pId
                    })
                    .ToList();

                if (toRemove.Any())
                {
                    _context.RolePermissions.RemoveRange(toRemove);
                }

                if (toAdd.Any())
                {
                    _context.RolePermissions.AddRange(toAdd);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await _eventPublisher.PublishAsync(new WorkspaceRoleUpdatedEvent(workspaceId, roleId, "PermissionsUpdated"));
            return true;
        }

        public async Task<WorkspaceRoleDetailResponse?> GetWorkspaceRoleDetailsAsync(int accountId, int workspaceId, int roleId)
        {
            var isMember = await _context.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceID == workspaceId && m.Resource.AccountID == accountId && m.Status == "Active");
            if (!isMember)
            {
                var workspaceExists = await _context.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
                if (!workspaceExists)
                {
                    throw new KeyNotFoundException("WorkspaceNotFound");
                }
                throw new UnauthorizedAccessException("NotWorkspaceMember");
            }

            var role = await _context.WorkspaceRoles
                .Where(r => r.WorkspaceRoleID == roleId && (r.WorkspaceID == workspaceId || r.WorkspaceID == null) && !r.IsDeleted)
                .FirstOrDefaultAsync();

            if (role == null)
            {
                throw new KeyNotFoundException("RoleNotFound");
            }

            var permissions = await _context.RolePermissions
                .Where(rp => rp.WorkspaceRoleID == roleId)
                .Select(rp => rp.PermissionID)
                .ToListAsync();

            return new WorkspaceRoleDetailResponse
            {
                WorkspaceRoleID = role.WorkspaceRoleID,
                WorkspaceID = role.WorkspaceID,
                RoleName = role.RoleName,
                IsTemplate = role.IsTemplate,
                Permissions = permissions
            };
        }

        public async Task<List<WorkspacePermissionResponse>> GetAvailablePermissionsAsync(int accountId, int workspaceId)
        {
            var isMember = await _context.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceID == workspaceId && m.Resource.AccountID == accountId && m.Status == "Active");
            if (!isMember)
            {
                var workspaceExists = await _context.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
                if (!workspaceExists)
                {
                    throw new KeyNotFoundException("WorkspaceNotFound");
                }
                throw new UnauthorizedAccessException("NotWorkspaceMember");
            }

            return await _context.WorkspacePermissions
                .Select(p => new WorkspacePermissionResponse
                {
                    PermissionID = p.PermissionID,
                    DisplayName = p.DisplayName
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateMemberRoleAsync(
            int accountId,
            int workspaceId,
            int targetMemberId,
            UpdateMemberRoleRequest request)
        {
            var workspaceExists = await _context.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
            if (!workspaceExists)
            {
                throw new KeyNotFoundException("WorkspaceNotFound");
            }

            var targetMember = await _context.WorkspaceMembers
                .Include(m => m.Resource)
                .Include(m => m.WorkspaceRole)
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == targetMemberId && m.WorkspaceID == workspaceId);

            if (targetMember == null)
            {
                throw new KeyNotFoundException("WorkspaceMemberNotFound");
            }

            var targetRole = await _context.WorkspaceRoles
                .FirstOrDefaultAsync(r => r.WorkspaceRoleID == request.WorkspaceRoleID && r.WorkspaceID == workspaceId && !r.IsDeleted);

            if (targetRole == null)
            {
                throw new KeyNotFoundException("WorkspaceRoleNotFound");
            }

            if (string.Equals(targetRole.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("CannotAssignOwnerRole");
            }

            if (string.Equals(targetMember.WorkspaceRole.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("CannotModifyOwnerRole");
            }

            targetMember.WorkspaceRoleID = request.WorkspaceRoleID;
            await _context.SaveChangesAsync();

            if (targetMember.Resource?.AccountID != null)
            {
                var cacheKey = $"workspace_auth_{targetMember.Resource.AccountID}_{workspaceId}";
                await _cache.RemoveAsync(cacheKey);
            }

            return true;
        }

        public async Task<bool> UpdateMemberSalaryOTAsync(
            int accountId,
            int workspaceId,
            int targetMemberId,
            UpdateMemberSalaryOTRequest request)
        {
            var workspaceExists = await _context.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
            if (!workspaceExists)
            {
                throw new KeyNotFoundException("WorkspaceNotFound");
            }

            var targetMember = await _context.WorkspaceMembers
                .Include(m => m.Resource)
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == targetMemberId && m.WorkspaceID == workspaceId);

            if (targetMember == null)
            {
                throw new KeyNotFoundException("WorkspaceMemberNotFound");
            }

            targetMember.BaseSalaryMonth = request.BaseSalaryMonth;
            targetMember.OTRatePerHour = request.OTRatePerHour;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<MemberSalaryOTResponse?> GetMemberSalaryOTAsync(
            int accountId,
            int workspaceId,
            int targetMemberId)
        {
            var workspaceExists = await _context.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
            if (!workspaceExists)
            {
                throw new KeyNotFoundException("WorkspaceNotFound");
            }

            var targetMember = await _context.WorkspaceMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == targetMemberId && m.WorkspaceID == workspaceId);

            if (targetMember == null)
            {
                throw new KeyNotFoundException("WorkspaceMemberNotFound");
            }

            return new MemberSalaryOTResponse
            {
                WorkspaceMemberID = targetMember.WorkspaceMemberID,
                BaseSalaryMonth = targetMember.BaseSalaryMonth,
                OTRatePerHour = targetMember.OTRatePerHour
            };
        }

        public async Task<object> SearchWorkspaceAsync(int workspaceId, int currentAccountId, GetWorkspaceSearchQuery query)
        {
            var workspaceExists = await _context.Workspaces.AnyAsync(w => w.WorkspaceID == workspaceId && !w.IsDeleted);
            if (!workspaceExists)
            {
                throw new KeyNotFoundException("WorkspaceNotFound");
            }

            var memberInfo = await _context.WorkspaceMembers
                .AsNoTracking()
                .Include(wm => wm.WorkspaceRole)
                .FirstOrDefaultAsync(wm => wm.WorkspaceID == workspaceId 
                    && wm.Resource.AccountID == currentAccountId 
                    && wm.Status == "Active");

            if (memberInfo == null)
            {
                throw new UnauthorizedAccessException("User has no active membership in this workspace.");
            }

            var roleName = memberInfo.WorkspaceRole?.RoleName;
            bool isAdminOrOwner = string.Equals(roleName, "Owner", StringComparison.OrdinalIgnoreCase) 
                                  || string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase);

            var q = query.Q?.Trim().ToLower() ?? string.Empty;
            
            // 1. Projects Query
            var projectsQuery = _context.Projects
                .AsNoTracking()
                .Where(p => p.WorkspaceID == workspaceId && !p.IsDeleted);

            if (!isAdminOrOwner)
            {
                projectsQuery = projectsQuery.Where(p => _context.TaskAssignees.Any(ta => 
                    ta.WorkspaceMemberID == memberInfo.WorkspaceMemberID 
                    && ta.Task.ProjectID == p.ProjectID 
                    && !ta.Task.IsDeleted));
            }

            if (!string.IsNullOrEmpty(q))
            {
                projectsQuery = projectsQuery.Where(p => p.ProjectName.ToLower().Contains(q));
            }

            // 2. Tasks Query
            var tasksQuery = _context.ProjectTasks
                .AsNoTracking()
                .Include(t => t.Project)
                .Where(t => t.Project!.WorkspaceID == workspaceId && !t.IsDeleted && !t.Project.IsDeleted);

            if (!isAdminOrOwner)
            {
                tasksQuery = tasksQuery.Where(t => _context.TaskAssignees.Any(ta => 
                    ta.WorkspaceMemberID == memberInfo.WorkspaceMemberID 
                    && ta.Task.ProjectID == t.ProjectID 
                    && !ta.Task.IsDeleted));
            }

            if (!string.IsNullOrEmpty(q))
            {
                tasksQuery = tasksQuery.Where(t => t.TaskName.ToLower().Contains(q));
            }

            // 3. Employees Query
            IQueryable<WorkspaceMember>? membersQuery = null;
            if (isAdminOrOwner)
            {
                membersQuery = _context.WorkspaceMembers
                    .AsNoTracking()
                    .Include(m => m.Resource)
                    .Where(m => m.WorkspaceID == workspaceId && !m.Resource.IsDeleted);

                if (!string.IsNullOrEmpty(q))
                {
                    membersQuery = membersQuery.Where(m => m.EmployeeCode.ToLower().Contains(q) 
                                                           || m.Resource.FullName.ToLower().Contains(q));
                }
            }

            var type = query.Type?.ToLower() ?? "all";

            if (type == "all")
            {
                var projectsListTask = projectsQuery.OrderByDescending(p => p.CreatedAt).Take(5).Select(p => new SearchProjectItemResponse
                {
                    ProjectID = p.ProjectID,
                    ProjectName = p.ProjectName,
                    Status = p.Status,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                }).ToListAsync();

                var tasksListTask = tasksQuery.OrderByDescending(t => t.CreatedAt).Take(5).Select(t => new SearchTaskItemResponse
                {
                    TaskID = t.TaskID,
                    TaskName = t.TaskName,
                    ProjectID = t.ProjectID,
                    ProjectName = t.Project!.ProjectName,
                    Status = t.Status
                }).ToListAsync();

                Task<List<SearchEmployeeItemResponse>> employeesListTask;
                if (isAdminOrOwner && membersQuery != null)
                {
                    employeesListTask = membersQuery.OrderBy(m => m.EmployeeCode).Take(5).Select(m => new SearchEmployeeItemResponse
                    {
                        WorkspaceMemberID = m.WorkspaceMemberID,
                        EmployeeCode = m.EmployeeCode,
                        FullName = m.Resource.FullName,
                        AvatarURL = m.Resource.AvatarURL
                    }).ToListAsync();
                }
                else
                {
                    employeesListTask = Task.FromResult(new List<SearchEmployeeItemResponse>());
                }

                await Task.WhenAll(projectsListTask, tasksListTask, employeesListTask);

                return new WorkspaceSearchResponse
                {
                    Projects = projectsListTask.Result,
                    Tasks = tasksListTask.Result,
                    Employees = employeesListTask.Result
                };
            }
            else if (type == "project")
            {
                var page = Math.Max(query.Page, 1);
                var pageSize = Math.Clamp(query.PageSize, 1, 100);

                var totalItems = await projectsQuery.CountAsync();
                var items = await projectsQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new SearchProjectItemResponse
                    {
                        ProjectID = p.ProjectID,
                        ProjectName = p.ProjectName,
                        Status = p.Status,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate
                    })
                    .ToListAsync();

                return new 
                {
                    page,
                    pageSize,
                    totalItems,
                    totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                    items
                };
            }
            else if (type == "task")
            {
                var page = Math.Max(query.Page, 1);
                var pageSize = Math.Clamp(query.PageSize, 1, 100);

                var totalItems = await tasksQuery.CountAsync();
                var items = await tasksQuery
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new SearchTaskItemResponse
                    {
                        TaskID = t.TaskID,
                        TaskName = t.TaskName,
                        ProjectID = t.ProjectID,
                        ProjectName = t.Project!.ProjectName,
                        Status = t.Status
                    })
                    .ToListAsync();

                return new 
                {
                    page,
                    pageSize,
                    totalItems,
                    totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                    items
                };
            }
            else if (type == "employee")
            {
                var page = Math.Max(query.Page, 1);
                var pageSize = Math.Clamp(query.PageSize, 1, 100);

                if (!isAdminOrOwner || membersQuery == null)
                {
                    return new 
                    {
                        page,
                        pageSize,
                        totalItems = 0,
                        totalPages = 0,
                        items = new List<SearchEmployeeItemResponse>()
                    };
                }

                var totalItems = await membersQuery.CountAsync();
                var items = await membersQuery
                    .OrderBy(m => m.EmployeeCode)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new SearchEmployeeItemResponse
                    {
                        WorkspaceMemberID = m.WorkspaceMemberID,
                        EmployeeCode = m.EmployeeCode,
                        FullName = m.Resource.FullName,
                        AvatarURL = m.Resource.AvatarURL
                    })
                    .ToListAsync();

                return new 
                {
                    page,
                    pageSize,
                    totalItems,
                    totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                    items
                };
            }

            throw new ArgumentException("InvalidSearchType");
        }
    }
}
