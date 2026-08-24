using System.Collections.Generic;
using System.Threading.Tasks;
using AllocServer.DTOs.Skills;

namespace AllocServer.Interfaces.Skills
{
    public interface ISkillService
    {
        Task<PagedSkillsResponse> GetSkillsPagedAsync(GetSkillsQuery query);
        Task<List<SkillResponse>> GetAllSkillsAsync(string? searchTerm = null);
        Task<SkillResponse?> GetSkillByIdAsync(int skillId);
        Task<SkillResponse> CreateSkillAsync(CreateSkillRequest request, int currentAccountId);
        Task<SkillResponse> UpdateSkillAsync(int skillId, UpdateSkillRequest request, int currentAccountId);
        Task<bool> DeleteSkillAsync(int skillId, int currentAccountId);
    }
}
