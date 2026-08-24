using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllocServer.Data;
using AllocServer.DTOs.Skills;
using AllocServer.Interfaces.Skills;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AllocServer.Services.SkillServices
{
    public class SkillService : ISkillService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SkillService> _logger;

        public SkillService(ApplicationDbContext context, ILogger<SkillService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedSkillsResponse> GetSkillsPagedAsync(GetSkillsQuery query)
        {
            var pageNumber = Math.Max(query.PageNumber, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var skillsQuery = _context.Skills.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim();
                skillsQuery = skillsQuery.Where(s => s.SkillName.Contains(term));
            }

            var totalCount = await skillsQuery.CountAsync();

            var skillsWithUsage = await (from s in skillsQuery
                                         join rs in _context.ResourceSkills on s.SkillID equals rs.SkillID into rsGroup
                                         orderby s.SkillName
                                         select new SkillResponse
                                         {
                                             SkillID = s.SkillID,
                                             SkillName = s.SkillName,
                                             UsageCount = rsGroup.Count()
                                         })
                                         .Skip((pageNumber - 1) * pageSize)
                                         .Take(pageSize)
                                         .ToListAsync();

            return new PagedSkillsResponse
            {
                Items = skillsWithUsage,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<List<SkillResponse>> GetAllSkillsAsync(string? searchTerm = null)
        {
            var skillsQuery = _context.Skills.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                skillsQuery = skillsQuery.Where(s => s.SkillName.Contains(term));
            }

            return await (from s in skillsQuery
                          join rs in _context.ResourceSkills on s.SkillID equals rs.SkillID into rsGroup
                          orderby s.SkillName
                          select new SkillResponse
                          {
                              SkillID = s.SkillID,
                              SkillName = s.SkillName,
                              UsageCount = rsGroup.Count()
                          })
                          .ToListAsync();
        }

        public async Task<SkillResponse?> GetSkillByIdAsync(int skillId)
        {
            return await (from s in _context.Skills.AsNoTracking()
                          where s.SkillID == skillId
                          join rs in _context.ResourceSkills on s.SkillID equals rs.SkillID into rsGroup
                          select new SkillResponse
                          {
                              SkillID = s.SkillID,
                              SkillName = s.SkillName,
                              UsageCount = rsGroup.Count()
                          })
                          .FirstOrDefaultAsync();
        }

        public async Task<SkillResponse> CreateSkillAsync(CreateSkillRequest request, int currentAccountId)
        {
            if (string.IsNullOrWhiteSpace(request.SkillName))
            {
                throw new ArgumentException("SkillNameRequired");
            }

            var trimmedName = request.SkillName.Trim();

            // Sử dụng .IgnoreQueryFilters() để kiểm tra sự tồn tại trong CSDL (kể cả bản ghi đã xóa mềm)
            var existingSkill = await _context.Skills
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.SkillName.ToLower() == trimmedName.ToLower());

            if (existingSkill != null)
            {
                if (existingSkill.IsDeleted)
                {
                    // Tự động khôi phục nếu đã bị xóa mềm trước đó
                    existingSkill.IsDeleted = false;
                    existingSkill.DeletedAt = null;
                    existingSkill.DeletedBy = null;
                    existingSkill.SkillName = trimmedName; // Chuẩn hóa hoa thường nếu có thay đổi

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Skill '{SkillName}' (ID: {SkillID}) was restored by Account {AccountID}", trimmedName, existingSkill.SkillID, currentAccountId);

                    var usageCount = await _context.ResourceSkills.CountAsync(rs => rs.SkillID == existingSkill.SkillID);
                    return new SkillResponse
                    {
                        SkillID = existingSkill.SkillID,
                        SkillName = existingSkill.SkillName,
                        UsageCount = usageCount
                    };
                }

                throw new InvalidOperationException("SkillNameAlreadyExists");
            }

            var newSkill = new Skill
            {
                SkillName = trimmedName,
                IsDeleted = false
            };

            _context.Skills.Add(newSkill);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Skill '{SkillName}' (ID: {SkillID}) created by Account {AccountID}", trimmedName, newSkill.SkillID, currentAccountId);

            return new SkillResponse
            {
                SkillID = newSkill.SkillID,
                SkillName = newSkill.SkillName,
                UsageCount = 0
            };
        }

        public async Task<SkillResponse> UpdateSkillAsync(int skillId, UpdateSkillRequest request, int currentAccountId)
        {
            if (string.IsNullOrWhiteSpace(request.SkillName))
            {
                throw new ArgumentException("SkillNameRequired");
            }

            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.SkillID == skillId);
            if (skill == null)
            {
                throw new KeyNotFoundException("SkillNotFound");
            }

            var trimmedName = request.SkillName.Trim();

            // Kiểm tra xem tên mới có bị trùng với Skill khác đang active hay không
            var isDuplicate = await _context.Skills
                .AnyAsync(s => s.SkillID != skillId && s.SkillName.ToLower() == trimmedName.ToLower());

            if (isDuplicate)
            {
                throw new InvalidOperationException("SkillNameAlreadyExists");
            }

            skill.SkillName = trimmedName;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Skill ID {SkillID} renamed to '{SkillName}' by Account {AccountID}", skillId, trimmedName, currentAccountId);

            var usageCount = await _context.ResourceSkills.CountAsync(rs => rs.SkillID == skillId);
            return new SkillResponse
            {
                SkillID = skill.SkillID,
                SkillName = skill.SkillName,
                UsageCount = usageCount
            };
        }

        public async Task<bool> DeleteSkillAsync(int skillId, int currentAccountId)
        {
            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.SkillID == skillId);
            if (skill == null)
            {
                throw new KeyNotFoundException("SkillNotFound");
            }

            // Safe Delete: Kiểm tra nếu có nhân sự đang sử dụng kỹ năng này thì từ chối xóa
            var isSkillInUse = await _context.ResourceSkills.AnyAsync(rs => rs.SkillID == skillId);
            if (isSkillInUse)
            {
                throw new InvalidOperationException("SkillInUseCannotDelete");
            }

            skill.IsDeleted = true;
            skill.DeletedAt = DateTime.UtcNow;
            skill.DeletedBy = currentAccountId;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Skill ID {SkillID} soft-deleted by Account {AccountID}", skillId, currentAccountId);

            return true;
        }
    }
}
