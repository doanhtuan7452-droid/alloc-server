using AllocServer.Data;
using AllocServer.DTOs.Projects;
using AllocServer.Constants.Permissions;
using AllocServer.Filters;
using AllocServer.Interfaces.ProjectAssets;
using AllocServer.Interfaces.Storage;
using AllocServer.Models;
using AllocServer.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.ProjectAsset_Services
{
    public class ProjectAssetService : IProjectAssetService
    {
        private const long MaxFileSizeBytes = 50L * 1024L * 1024L;
        private static readonly TimeSpan DownloadUrlExpiration = TimeSpan.FromHours(1);

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp"
        };

        private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx",
            ".ppt",
            ".pptx",
            ".txt",
            ".csv"
        };

        private static readonly HashSet<string> FileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip"
        };

        private static readonly HashSet<string> AllowedAssetTypes = new(StringComparer.Ordinal)
        {
            "File",
            "Image",
            "Document"
        };

        private readonly ApplicationDbContext _context;
        private readonly StorageFactory _storageFactory;

        public ProjectAssetService(
            ApplicationDbContext context,
            StorageFactory storageFactory)
        {
            _context = context;
            _storageFactory = storageFactory;
        }

        public async Task<ProjectAssetResponseDto> UploadProjectAssetAsync(
            int accountId,
            Project project,
            UploadAssetRequestDto request)
        {
            var file = ValidateUploadFile(request.File);
            var originalFileName = Path.GetFileName(file.FileName).Trim();
            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                throw new ArgumentException("InvalidFileName");
            }
            if (originalFileName.Length > 255)
            {
                throw new ArgumentException("FileNameTooLong");
            }

            var extension = Path.GetExtension(originalFileName);
            var assetType = ResolveAssetType(extension);
            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

            var uploaderMembership = await ResolveActiveMembershipAsync(accountId, project.WorkspaceID);
            var sanitizedFileName = SanitizeFileName(originalFileName);
            var blobPath = BuildBlobPath(project.WorkspaceID, project.ProjectID, sanitizedFileName);
            var storageStrategy = _storageFactory.Create();

            await using (var fileStream = file.OpenReadStream())
            {
                await storageStrategy.UploadFileAsync(fileStream, contentType, blobPath);
            }

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                var now = DateTime.UtcNow;
                var asset = new ProjectAsset
                {
                    WorkspaceID = project.WorkspaceID,
                    ProjectID = project.ProjectID,
                    UploadedBy = uploaderMembership.WorkspaceMemberID,
                    AssetType = assetType,
                    AssetName = originalFileName,
                    AssetURL = blobPath,
                    FileSizeKB = CalculateFileSizeKB(file.Length),
                    CreatedAt = now
                };

                _context.ProjectAssets.Add(asset);
                await _context.SaveChangesAsync();

                await AddMonthlyUploadUsageAsync(
                    project.WorkspaceID,
                    GetCurrentBillingMonth(now),
                    CalculateFileSizeMB(file.Length));

                await transaction.CommitAsync();

                return new ProjectAssetResponseDto
                {
                    AssetID = asset.AssetID,
                    ProjectID = asset.ProjectID,
                    WorkspaceID = asset.WorkspaceID,
                    AssetName = asset.AssetName,
                    AssetType = asset.AssetType,
                    FileSizeKB = asset.FileSizeKB,
                    UploadedBy = asset.UploadedBy,
                    UploadedByName = uploaderMembership.FullName,
                    CreatedAt = asset.CreatedAt
                };
            }
            catch
            {
                await TryDeleteUploadedBlobAsync(storageStrategy, blobPath);
                throw;
            }
        }

        public async Task<ProjectAssetResponseDto> UploadWorkspaceAssetAsync(
            int accountId,
            int workspaceId,
            UploadAssetRequestDto request)
        {
            var file = ValidateUploadFile(request.File);
            var originalFileName = Path.GetFileName(file.FileName).Trim();
            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                throw new ArgumentException("InvalidFileName");
            }
            if (originalFileName.Length > 255)
            {
                throw new ArgumentException("FileNameTooLong");
            }

            var extension = Path.GetExtension(originalFileName);
            var assetType = ResolveAssetType(extension);
            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

            var uploaderMembership = await ResolveActiveMembershipAsync(accountId, workspaceId);
            var sanitizedFileName = SanitizeFileName(originalFileName);
            var blobPath = BuildWorkspaceBlobPath(workspaceId, sanitizedFileName);
            var storageStrategy = _storageFactory.Create();

            await using (var fileStream = file.OpenReadStream())
            {
                await storageStrategy.UploadFileAsync(fileStream, contentType, blobPath);
            }

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                var now = DateTime.UtcNow;
                var asset = new ProjectAsset
                {
                    WorkspaceID = workspaceId,
                    ProjectID = null,
                    UploadedBy = uploaderMembership.WorkspaceMemberID,
                    AssetType = assetType,
                    AssetName = originalFileName,
                    AssetURL = blobPath,
                    FileSizeKB = CalculateFileSizeKB(file.Length),
                    CreatedAt = now
                };

                _context.ProjectAssets.Add(asset);
                await _context.SaveChangesAsync();

                await AddMonthlyUploadUsageAsync(
                    workspaceId,
                    GetCurrentBillingMonth(now),
                    CalculateFileSizeMB(file.Length));

                await transaction.CommitAsync();

                return new ProjectAssetResponseDto
                {
                    AssetID = asset.AssetID,
                    WorkspaceID = asset.WorkspaceID,
                    ProjectID = asset.ProjectID,
                    AssetName = asset.AssetName,
                    AssetType = asset.AssetType,
                    FileSizeKB = asset.FileSizeKB,
                    UploadedBy = asset.UploadedBy,
                    UploadedByName = uploaderMembership.FullName,
                    CreatedAt = asset.CreatedAt
                };
            }
            catch
            {
                await TryDeleteUploadedBlobAsync(storageStrategy, blobPath);
                throw;
            }
        }

        public async Task<PagedProjectAssetsResponse> GetProjectAssetsAsync(
            Project project,
            GetProjectAssetsQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var search = NormalizeOptionalString(query.Search);
            var assetType = NormalizeAssetType(query.AssetType);

            if (!string.IsNullOrWhiteSpace(query.AssetType) && assetType == null)
            {
                throw new ArgumentException("InvalidAssetType");
            }

            var assetsQuery = _context.ProjectAssets
                .AsNoTracking()
                .Where(asset => asset.ProjectID == project.ProjectID);

            if (search != null)
            {
                assetsQuery = assetsQuery.Where(asset => asset.AssetName.Contains(search));
            }

            if (assetType != null)
            {
                assetsQuery = assetsQuery.Where(asset => asset.AssetType == assetType);
            }

            var totalItems = await assetsQuery.CountAsync();
            var items = await assetsQuery
                .OrderByDescending(asset => asset.CreatedAt)
                .ThenByDescending(asset => asset.AssetID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(asset => new ProjectAssetResponseDto
                {
                    AssetID = asset.AssetID,
                    ProjectID = asset.ProjectID,
                    WorkspaceID = asset.WorkspaceID,
                    AssetName = asset.AssetName,
                    AssetType = asset.AssetType,
                    FileSizeKB = asset.FileSizeKB,
                    UploadedBy = asset.UploadedBy,
                    UploadedByName = asset.UploadedByMember != null
                        ? asset.UploadedByMember.Resource.FullName
                        : null,
                    CreatedAt = asset.CreatedAt
                })
                .ToListAsync();

            return new PagedProjectAssetsResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        public async Task<ProjectAssetDownloadResponseDto> GetAssetDownloadUrlAsync(
            int accountId,
            int assetId)
        {
            var asset = await LoadAssetForAccessAsync(
                accountId,
                assetId,
                AssetPermissionIds.View,
                asTracking: false);

            var expiresAt = DateTime.UtcNow.Add(DownloadUrlExpiration);
            var downloadUrl = await _storageFactory
                .Create()
                .GetPresignedUrlAsync(asset.AssetURL, DownloadUrlExpiration, asset.AssetName);

            return new ProjectAssetDownloadResponseDto
            {
                DownloadUrl = downloadUrl,
                ExpiresAt = expiresAt
            };
        }

        public async Task DeleteAssetAsync(
            int accountId,
            int assetId)
        {
            var asset = await LoadAssetForAccessAsync(
                accountId,
                assetId,
                AssetPermissionIds.Delete,
                asTracking: true);

            asset.IsDeleted = true;
            asset.DeletedAt = DateTime.UtcNow;
            asset.DeletedBy = accountId;

            await _context.SaveChangesAsync();
        }

        private async Task<ProjectAsset> LoadAssetForAccessAsync(
            int accountId,
            int assetId,
            string requiredPermissionId,
            bool asTracking)
        {
            var query = _context.ProjectAssets
                .Include(asset => asset.Project)
                    .ThenInclude(project => project!.Workspace)
                .Include(asset => asset.Workspace)
                .Where(asset =>
                    asset.AssetID == assetId
                    && asset.Workspace != null
                    && !asset.Workspace.IsDeleted
                    && (asset.Project == null || !asset.Project.IsDeleted));

            if (!asTracking)
            {
                query = query.AsNoTracking();
            }

            var asset = await query.FirstOrDefaultAsync();
            if (asset == null)
            {
                throw new KeyNotFoundException("AssetNotFound");
            }

            await EnsurePermissionAsync(
                accountId,
                asset.WorkspaceID,
                requiredPermissionId);

            return asset;
        }

        private async Task<MembershipAccess> ResolveActiveMembershipAsync(
            int accountId,
            int workspaceId)
        {
            var membership = await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member =>
                    member.WorkspaceID == workspaceId
                    && member.Resource.AccountID == accountId
                    && member.Status == "Active"
                    && !member.Workspace.IsDeleted)
                .Select(member => new MembershipAccess
                {
                    WorkspaceMemberID = member.WorkspaceMemberID,
                    WorkspaceRoleID = member.WorkspaceRoleID,
                    RoleName = member.WorkspaceRole.RoleName,
                    FullName = member.Resource.FullName
                })
                .FirstOrDefaultAsync();

            return membership
                   ?? throw new UnauthorizedAccessException(
                       "Ban khong co quyen truy cap workspace cua Asset.");
        }

        private async Task EnsurePermissionAsync(
            int accountId,
            int workspaceId,
            string requiredPermissionId)
        {
            var membership = await ResolveActiveMembershipAsync(accountId, workspaceId);
            if (string.Equals(membership.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var hasPermission = await _context.RolePermissions
                .AsNoTracking()
                .AnyAsync(rolePermission =>
                    rolePermission.WorkspaceRoleID == membership.WorkspaceRoleID
                    && rolePermission.PermissionID == requiredPermissionId);

            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("UnauthorizedAssetAccess");
            }
        }

        private async Task AddMonthlyUploadUsageAsync(
            int workspaceId,
            DateOnly billingMonth,
            decimal fileSizeMB)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (
    SELECT 1
    FROM WorkspaceMonthlyUsages WITH (UPDLOCK, HOLDLOCK)
    WHERE WorkspaceID = {workspaceId} AND BillingMonth = {billingMonth}
)
BEGIN
    INSERT INTO WorkspaceMonthlyUsages (WorkspaceID, BillingMonth, AIQueryCount, StorageUsedMB, UpdatedAt)
    VALUES ({workspaceId}, {billingMonth}, 0, 0, SYSUTCDATETIME())
END");

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE WorkspaceMonthlyUsages
SET StorageUsedMB = ISNULL(StorageUsedMB, 0) + {fileSizeMB},
    UpdatedAt = SYSUTCDATETIME()
WHERE WorkspaceID = {workspaceId}
  AND BillingMonth = {billingMonth}");
        }

        private static IFormFile ValidateUploadFile(IFormFile? file)
        {
            if (file == null)
            {
                throw new ArgumentException("FileRequired");
            }

            if (file.Length <= 0)
            {
                throw new ArgumentException("FileEmpty");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new ArgumentException("FileTooLarge");
            }

            return file;
        }

        private static string SanitizeFileName(string fileName)
        {
            var sanitizedFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(sanitizedFileName))
            {
                throw new ArgumentException("InvalidFileName");
            }

            // 1. Thay thế khoảng trắng bằng dấu gạch dưới
            sanitizedFileName = sanitizedFileName.Replace(" ", "_");

            // 2. Chuyển tiếng Việt có dấu thành không dấu
            sanitizedFileName = RemoveVietnameseDiacritics(sanitizedFileName);

            // 3. Chỉ cho phép chữ, số, dấu chấm, dấu gạch ngang, gạch dưới. Thay thế các ký tự khác thành '_'
            sanitizedFileName = System.Text.RegularExpressions.Regex.Replace(
                sanitizedFileName, 
                @"[^a-zA-Z0-9\.\-_]", 
                "_"
            );

            // 4. Giới hạn độ dài và loại bỏ ký tự không hợp lệ của OS
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                sanitizedFileName = sanitizedFileName.Replace(invalidChar, '_');
            }

            sanitizedFileName = sanitizedFileName.Trim();
            if (sanitizedFileName.Length > 255)
            {
                throw new ArgumentException("FileNameTooLong");
            }

            return sanitizedFileName;
        }

        private static string RemoveVietnameseDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Xử lý thủ công chữ đ và Đ do không tự tách tổ hợp FormD được
            text = text.Replace('đ', 'd').Replace('Đ', 'D');

            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        private static string ResolveAssetType(string extension)
        {
            if (ImageExtensions.Contains(extension))
            {
                return "Image";
            }

            if (DocumentExtensions.Contains(extension))
            {
                return "Document";
            }

            if (FileExtensions.Contains(extension))
            {
                return "File";
            }

            throw new ArgumentException("UnsupportedFileFormat");
        }

        private static string BuildBlobPath(
            int workspaceId,
            int projectId,
            string fileName)
        {
            return $"workspaces/{workspaceId}/projects/{projectId}/assets/{Guid.NewGuid():N}_{fileName}";
        }

        private static string BuildWorkspaceBlobPath(
            int workspaceId,
            string fileName)
        {
            return $"workspaces/{workspaceId}/assets/{Guid.NewGuid():N}_{fileName}";
        }

        private static int CalculateFileSizeKB(long fileSizeBytes)
        {
            return (int)Math.Ceiling(fileSizeBytes / 1024d);
        }

        private static decimal CalculateFileSizeMB(long fileSizeBytes)
        {
            var sizeInMB = Math.Round(
                fileSizeBytes / 1024m / 1024m,
                2,
                MidpointRounding.AwayFromZero);

            return Math.Max(sizeInMB, 0.01m);
        }

        private static DateOnly GetCurrentBillingMonth(DateTime now)
        {
            var today = DateOnly.FromDateTime(now);
            return new DateOnly(today.Year, today.Month, 1);
        }

        private static string? NormalizeAssetType(string? assetType)
        {
            var normalized = NormalizeOptionalString(assetType);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "FILE" => "File",
                "IMAGE" => "Image",
                "DOCUMENT" => "Document",
                _ => normalized
            };

            return AllowedAssetTypes.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static async Task TryDeleteUploadedBlobAsync(
            IStorageStrategy storageStrategy,
            string blobPath)
        {
            try
            {
                await storageStrategy.DeleteFileAsync(blobPath);
            }
            catch
            {
                // Preserve the original database exception; orphan cleanup can be retried operationally.
            }
        }

        private sealed class MembershipAccess
        {
            public int WorkspaceMemberID { get; set; }
            public int WorkspaceRoleID { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public string? FullName { get; set; }
        }
    }
}
