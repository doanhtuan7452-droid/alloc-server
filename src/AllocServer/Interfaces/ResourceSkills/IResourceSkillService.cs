using System.Collections.Generic;
using System.Threading.Tasks;
using AllocServer.DTOs.ResourceSkills;

namespace AllocServer.Interfaces.ResourceSkills
{
    public interface IResourceSkillService
    {
        Task<List<ResourceSkillResponse>> GetResourceSkillsAsync(int resourceId);
        Task<List<ResourceSkillResponse>> GetMySkillsAsync(int currentAccountId);
        Task<ResourceSkillResponse> AssignSkillAsync(int resourceId, AssignResourceSkillRequest request, int currentAccountId, bool isSystemAccount);
        Task<ResourceSkillResponse> UpdateSkillLevelAsync(int resourceId, int skillId, UpdateResourceSkillLevelRequest request, int currentAccountId, bool isSystemAccount);
        Task<bool> RemoveSkillAsync(int resourceId, int skillId, int currentAccountId, bool isSystemAccount);
        Task<List<ResourceSkillResponse>> BatchUpsertSkillsAsync(int resourceId, BatchUpsertResourceSkillsRequest request, int currentAccountId, bool isSystemAccount);
    }
}
