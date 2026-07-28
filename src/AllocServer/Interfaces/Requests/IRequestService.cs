using AllocServer.DTOs.Requests;

namespace AllocServer.Interfaces.Requests
{
    public interface IRequestService
    {
        Task<LeaveRequestResponse> CreateLeaveRequestAsync(
            int accountId,
            int workspaceId,
            CreateLeaveRequestRequest request);

        Task<OTRequestResponse> CreateOTRequestAsync(
            int accountId,
            int workspaceId,
            CreateOTRequestRequest request);

        Task<RequestReviewResponse> ReviewRequestAsync(
            int accountId,
            string requestType,
            int requestId,
            ReviewRequestRequest request);

        Task<List<LeaveRequestResponse>> GetWorkspaceLeaveRequestsAsync(
            int accountId,
            int workspaceId);

        Task<List<OTRequestResponse>> GetWorkspaceOTRequestsAsync(
            int accountId,
            int workspaceId);
    }
}
