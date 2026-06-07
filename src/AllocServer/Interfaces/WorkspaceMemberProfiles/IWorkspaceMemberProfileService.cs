using AllocServer.DTOs.WorkspaceMemberProfiles;

namespace AllocServer.Interfaces.WorkspaceMemberProfiles
{
    public interface IWorkspaceMemberProfileService
    {
        Task<WorkspaceMemberProfileResponse?> GetProfileAsync(int workspaceId, int memberId);
        
        Task<WorkspaceMemberProfileResponse> CreateProfileAsync(int workspaceId, int memberId, CreateMemberProfileRequest request);
        
        Task<WorkspaceMemberProfileResponse> UpdateProfileAsync(int workspaceId, int memberId, UpdateMemberProfileRequest request);
        
        Task<bool> DeleteProfileAsync(int workspaceId, int memberId);

        Task RecalculateProfileScoresAsync(int memberId);

        Task RecalculateAttendanceRateForMonthAsync(int memberId, int year, int month);

        Task<List<ReviewCycleResponse>> GetReviewCyclesAsync(int workspaceId);
        Task<ReviewCycleResponse> CreateReviewCycleAsync(int workspaceId, CreateReviewCycleRequest request, int createdByMemberId);
        Task<ReviewCycleResponse> StartReviewCycleAsync(int workspaceId, int cycleId);
        Task<ReviewCycleResponse> CompleteReviewCycleAsync(int workspaceId, int cycleId);
        Task<MemberEvaluationResponse> SubmitMemberEvaluationAsync(int workspaceId, int cycleId, SubmitEvaluationRequest request);
    }
}
