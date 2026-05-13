using AllocServer.DTOs.Revenues;
using AllocServer.Models;

namespace AllocServer.Interfaces.Revenues
{
    public interface IRevenueService
    {
        Task<PagedProjectRevenuesResponse> GetProjectRevenuesAsync(
            Project project,
            GetProjectRevenuesQuery query);
    }
}
