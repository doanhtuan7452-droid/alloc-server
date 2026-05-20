using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace AllocServer.DTOs.Projects
{
    public class GetProjectAssetsQuery
    {
        [Range(1, int.MaxValue)]
        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 20;

        [JsonPropertyName("search")]
        public string? Search { get; set; }

        [JsonPropertyName("assetType")]
        public string? AssetType { get; set; }
    }

    public class UploadAssetRequestDto
    {
        [Required]
        [JsonPropertyName("file")]
        public IFormFile? File { get; set; }
    }

    public class ProjectAssetResponseDto
    {
        [JsonPropertyName("assetId")]
        public int AssetID { get; set; }

        [JsonPropertyName("projectId")]
        public int? ProjectID { get; set; }

        [JsonPropertyName("workspaceId")]
        public int WorkspaceID { get; set; }

        [JsonPropertyName("assetName")]
        public string AssetName { get; set; } = string.Empty;

        [JsonPropertyName("assetType")]
        public string AssetType { get; set; } = string.Empty;

        [JsonPropertyName("fileSizeKB")]
        public int FileSizeKB { get; set; }

        [JsonPropertyName("uploadedBy")]
        public int UploadedBy { get; set; }

        [JsonPropertyName("uploadedByName")]
        public string? UploadedByName { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    public class PagedProjectAssetsResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("items")]
        public List<ProjectAssetResponseDto> Items { get; set; } = new();
    }

    public class ProjectAssetDownloadResponseDto
    {
        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }
    }
}
