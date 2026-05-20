using AllocServer.DTOs.Projects;
using AllocServer.Models;

namespace AllocServer.Interfaces.ProjectAssets
{
    public interface IProjectAssetService
    {
        Task<ProjectAssetResponseDto> UploadProjectAssetAsync(
            int accountId,
            Project project,
            UploadAssetRequestDto request);

        Task<ProjectAssetResponseDto> UploadWorkspaceAssetAsync(
            int accountId,
            int workspaceId,
            UploadAssetRequestDto request);

        Task<PagedProjectAssetsResponse> GetProjectAssetsAsync(
            Project project,
            GetProjectAssetsQuery query);

        Task<ProjectAssetDownloadResponseDto> GetAssetDownloadUrlAsync(
            int accountId,
            int assetId);

        Task DeleteAssetAsync(
            int accountId,
            int assetId);
    }
}
