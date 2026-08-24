using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllocServer.Data;
using AllocServer.DTOs.ResourceSkills;
using AllocServer.Interfaces.ResourceSkills;
using AllocServer.Interfaces.WorkspaceMemberProfiles;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AllocServer.Services.ResourceSkillServices
{
    public class ResourceSkillService : IResourceSkillService
    {
        private readonly ApplicationDbContext _context;
        private readonly IProfileCalculationQueue _profileCalculationQueue;
        private readonly ILogger<ResourceSkillService> _logger;

        public ResourceSkillService(
            ApplicationDbContext context,
            IProfileCalculationQueue profileCalculationQueue,
            ILogger<ResourceSkillService> logger)
        {
            _context = context;
            _profileCalculationQueue = profileCalculationQueue;
            _logger = logger;
        }

        public async Task<List<ResourceSkillResponse>> GetResourceSkillsAsync(int resourceId)
        {
            var resourceExists = await _context.Resources.AnyAsync(r => r.ResourceID == resourceId);
            if (!resourceExists)
            {
                throw new KeyNotFoundException("ResourceNotFound");
            }

            return await _context.ResourceSkills
                .AsNoTracking()
                .Include(rs => rs.Skill)
                .Where(rs => rs.ResourceID == resourceId && !rs.Skill.IsDeleted)
                .OrderBy(rs => rs.Skill.SkillName)
                .Select(rs => new ResourceSkillResponse
                {
                    ResourceID = rs.ResourceID,
                    SkillID = rs.SkillID,
                    SkillName = rs.Skill.SkillName,
                    Level = rs.Level
                })
                .ToListAsync();
        }

        public async Task<List<ResourceSkillResponse>> GetMySkillsAsync(int currentAccountId)
        {
            var resource = await _context.Resources
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.AccountID == currentAccountId);

            if (resource == null)
            {
                throw new KeyNotFoundException("ResourceNotFound");
            }

            return await GetResourceSkillsAsync(resource.ResourceID);
        }

        public async Task<ResourceSkillResponse> AssignSkillAsync(
            int resourceId,
            AssignResourceSkillRequest request,
            int currentAccountId,
            bool isSystemAccount)
        {
            var resource = await _context.Resources.FirstOrDefaultAsync(r => r.ResourceID == resourceId);
            if (resource == null)
            {
                throw new KeyNotFoundException("ResourceNotFound");
            }

            // Chống IDOR: Chỉ cho phép chính chủ hoặc System Admin
            if (resource.AccountID != currentAccountId && !isSystemAccount)
            {
                throw new UnauthorizedAccessException("ForbiddenAccessResourceSkills");
            }

            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.SkillID == request.SkillID);
            if (skill == null)
            {
                throw new KeyNotFoundException("SkillNotFound");
            }

            var existingResourceSkill = await _context.ResourceSkills
                .FirstOrDefaultAsync(rs => rs.ResourceID == resourceId && rs.SkillID == request.SkillID);

            if (existingResourceSkill != null)
            {
                throw new InvalidOperationException("SkillAlreadyAssignedToResource");
            }

            var newResourceSkill = new ResourceSkill
            {
                ResourceID = resourceId,
                SkillID = request.SkillID,
                Level = request.Level
            };

            _context.ResourceSkills.Add(newResourceSkill);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Assigned Skill {SkillID} (Level {Level}) to Resource {ResourceID}", request.SkillID, request.Level, resourceId);

            // Bất đồng bộ hóa tính toán lại hồ sơ năng lực các thành viên liên quan
            await EnqueueAffectedMembersAsync(resourceId);

            return new ResourceSkillResponse
            {
                ResourceID = resourceId,
                SkillID = skill.SkillID,
                SkillName = skill.SkillName,
                Level = newResourceSkill.Level
            };
        }

        public async Task<ResourceSkillResponse> UpdateSkillLevelAsync(
            int resourceId,
            int skillId,
            UpdateResourceSkillLevelRequest request,
            int currentAccountId,
            bool isSystemAccount)
        {
            var resource = await _context.Resources.FirstOrDefaultAsync(r => r.ResourceID == resourceId);
            if (resource == null)
            {
                throw new KeyNotFoundException("ResourceNotFound");
            }

            // Chống IDOR
            if (resource.AccountID != currentAccountId && !isSystemAccount)
            {
                throw new UnauthorizedAccessException("ForbiddenAccessResourceSkills");
            }

            var resourceSkill = await _context.ResourceSkills
                .Include(rs => rs.Skill)
                .FirstOrDefaultAsync(rs => rs.ResourceID == resourceId && rs.SkillID == skillId);

            if (resourceSkill == null)
            {
                throw new KeyNotFoundException("ResourceSkillNotFound");
            }

            resourceSkill.Level = request.Level;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated Skill {SkillID} Level to {Level} for Resource {ResourceID}", skillId, request.Level, resourceId);

            await EnqueueAffectedMembersAsync(resourceId);

            return new ResourceSkillResponse
            {
                ResourceID = resourceId,
                SkillID = resourceSkill.SkillID,
                SkillName = resourceSkill.Skill.SkillName,
                Level = resourceSkill.Level
            };
        }

        public async Task<bool> RemoveSkillAsync(
            int resourceId,
            int skillId,
            int currentAccountId,
            bool isSystemAccount)
        {
            var resource = await _context.Resources.FirstOrDefaultAsync(r => r.ResourceID == resourceId);
            if (resource == null)
            {
                throw new KeyNotFoundException("ResourceNotFound");
            }

            // Chống IDOR
            if (resource.AccountID != currentAccountId && !isSystemAccount)
            {
                throw new UnauthorizedAccessException("ForbiddenAccessResourceSkills");
            }

            var resourceSkill = await _context.ResourceSkills
                .FirstOrDefaultAsync(rs => rs.ResourceID == resourceId && rs.SkillID == skillId);

            if (resourceSkill == null)
            {
                throw new KeyNotFoundException("ResourceSkillNotFound");
            }

            _context.ResourceSkills.Remove(resourceSkill);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Removed Skill {SkillID} from Resource {ResourceID}", skillId, resourceId);

            await EnqueueAffectedMembersAsync(resourceId);

            return true;
        }

        public async Task<List<ResourceSkillResponse>> BatchUpsertSkillsAsync(
            int resourceId,
            BatchUpsertResourceSkillsRequest request,
            int currentAccountId,
            bool isSystemAccount)
        {
            var resource = await _context.Resources.FirstOrDefaultAsync(r => r.ResourceID == resourceId);
            if (resource == null)
            {
                throw new KeyNotFoundException("ResourceNotFound");
            }

            // Chống IDOR
            if (resource.AccountID != currentAccountId && !isSystemAccount)
            {
                throw new UnauthorizedAccessException("ForbiddenAccessResourceSkills");
            }

            // Validate chống trùng lặp SkillID trong cùng request
            if (request.Skills.Select(s => s.SkillID).Distinct().Count() != request.Skills.Count)
            {
                throw new ArgumentException("DuplicateSkillIdsInRequest");
            }

            // Kiểm tra các SkillID có tồn tại không
            var skillIds = request.Skills.Select(s => s.SkillID).ToList();
            var validSkills = await _context.Skills
                .Where(s => skillIds.Contains(s.SkillID))
                .ToDictionaryAsync(s => s.SkillID);

            if (validSkills.Count != skillIds.Count)
            {
                throw new KeyNotFoundException("SkillNotFound");
            }

            // Bọc toàn bộ thao tác trong Database Transaction để đảm bảo tính nguyên tử (Atomicity)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Xóa toàn bộ ResourceSkills hiện tại của Resource
                var existingSkills = await _context.ResourceSkills
                    .Where(rs => rs.ResourceID == resourceId)
                    .ToListAsync();
                
                if (existingSkills.Any())
                {
                    _context.ResourceSkills.RemoveRange(existingSkills);
                }

                // 2. Thêm danh sách ResourceSkills mới
                var newSkills = request.Skills.Select(s => new ResourceSkill
                {
                    ResourceID = resourceId,
                    SkillID = s.SkillID,
                    Level = s.Level
                }).ToList();

                if (newSkills.Any())
                {
                    await _context.ResourceSkills.AddRangeAsync(newSkills);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Batch updated {Count} skills for Resource {ResourceID}", newSkills.Count, resourceId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to batch upsert skills for Resource {ResourceID}", resourceId);
                throw;
            }

            // Sau khi commit thành công, enqueue các thành viên bị ảnh hưởng
            await EnqueueAffectedMembersAsync(resourceId);

            return await GetResourceSkillsAsync(resourceId);
        }

        private async Task EnqueueAffectedMembersAsync(int resourceId)
        {
            try
            {
                var affectedMemberIds = await _context.WorkspaceMembers
                    .AsNoTracking()
                    .Where(m => m.ResourceID == resourceId && m.Status == "Active")
                    .Select(m => m.WorkspaceMemberID)
                    .ToListAsync();

                foreach (var memberId in affectedMemberIds)
                {
                    await _profileCalculationQueue.QueueProfileCalculationAsync(memberId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueuing profile calculation for Resource {ResourceID}", resourceId);
            }
        }
    }
}
