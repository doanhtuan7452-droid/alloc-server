using AllocServer.Data;
using AllocServer.DTOs.Conversations;
using AllocServer.DTOs.Messages;
using AllocServer.Hubs;
using AllocServer.Interfaces.Conversations;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace AllocServer.Services.Conversations
{
    public class ConversationService : IConversationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ConversationHub> _hubContext;

        public ConversationService(
            ApplicationDbContext context,
            IHubContext<ConversationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private async Task<WorkspaceMember> GetCurrentWorkspaceMemberAsync(int accountId, int workspaceId)
        {
            var member = await _context.WorkspaceMembers
                .Include(m => m.Resource)
                .FirstOrDefaultAsync(m => m.Resource.AccountID == accountId 
                                       && m.WorkspaceID == workspaceId 
                                       && m.Status == "Active");
            if (member == null)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập Workspace này.");
            }
            return member;
        }

        public async Task<ConversationDetailResponse> CreateConversationAsync(int accountId, int workspaceId, CreateConversationRequest request)
        {
            var currentMember = await GetCurrentWorkspaceMemberAsync(accountId, workspaceId);

            var allMemberIds = new HashSet<int>(request.WorkspaceMemberIds);
            allMemberIds.Add(currentMember.WorkspaceMemberID); // Auto-add creator

            string? conversationKey = null;

            // Validations based on Type
            if (request.Type != "Project_Channel")
            {
                request.ProjectId = null;
            }

            if (request.Type == "Direct")
            {
                if (allMemberIds.Count != 2)
                    throw new ArgumentException("Direct conversation must have exactly 2 members.");

                var otherMemberId = allMemberIds.First(id => id != currentMember.WorkspaceMemberID);
                
                int minId = Math.Min(currentMember.WorkspaceMemberID, otherMemberId);
                int maxId = Math.Max(currentMember.WorkspaceMemberID, otherMemberId);
                conversationKey = $"Direct_{workspaceId}_{minId}_{maxId}";

                // Check if a Direct conversation already exists between these two members in the workspace
                var existingDirectConversationId = await _context.ConversationMembers
                    .Where(cm => cm.Conversation.Type == "Direct" && cm.Conversation.WorkspaceID == workspaceId)
                    .GroupBy(cm => cm.ConversationID)
                    .Where(g => g.Count() == 2 && g.Any(x => x.MemberID == currentMember.WorkspaceMemberID) && g.Any(x => x.MemberID == otherMemberId))
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync();

                if (existingDirectConversationId != 0)
                {
                    return await GetConversationDetailsAsync(accountId, existingDirectConversationId);
                }
            }
            else if (request.Type == "Group")
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    throw new ArgumentException("Group conversation must have a Name.");
                if (allMemberIds.Count < 2)
                    throw new ArgumentException("Group conversation must have at least 2 members.");
            }
            else if (request.Type == "Project_Channel")
            {
                if (!request.ProjectId.HasValue)
                    throw new ArgumentException("Project_Channel conversation must be associated with a Project.");

                var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectID == request.ProjectId && p.WorkspaceID == workspaceId);
                if (project == null)
                    throw new KeyNotFoundException("Không tìm thấy Project hoặc Project không thuộc Workspace.");
            }

            // Verify all requested members belong to the workspace
            var validMembersCount = await _context.WorkspaceMembers
                .CountAsync(m => allMemberIds.Contains(m.WorkspaceMemberID) && m.WorkspaceID == workspaceId && m.Status == "Active");

            if (validMembersCount != allMemberIds.Count)
            {
                throw new ArgumentException("Một số thành viên không hợp lệ hoặc không thuộc Workspace này.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                // Re-check after acquiring lock (to prevent race conditions)
                if (request.Type == "Direct")
                {
                    var otherMemberId = allMemberIds.First(id => id != currentMember.WorkspaceMemberID);
                    var existingDirectConversationId = await _context.ConversationMembers
                        .Where(cm => cm.Conversation.Type == "Direct" && cm.Conversation.WorkspaceID == workspaceId)
                        .GroupBy(cm => cm.ConversationID)
                        .Where(g => g.Count() == 2 && g.Any(x => x.MemberID == currentMember.WorkspaceMemberID) && g.Any(x => x.MemberID == otherMemberId))
                        .Select(g => g.Key)
                        .FirstOrDefaultAsync();

                    if (existingDirectConversationId != 0)
                    {
                        await transaction.RollbackAsync();
                        return await GetConversationDetailsAsync(accountId, existingDirectConversationId);
                    }
                }

                var conversation = new Conversation
                {
                    WorkspaceID = workspaceId,
                    ProjectID = request.ProjectId,
                    Name = request.Name,
                    Type = request.Type,
                    ConversationKey = conversationKey,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();

                var conversationMembers = allMemberIds.Select(memberId => new ConversationMember
                {
                    ConversationID = conversation.ConversationID,
                    MemberID = memberId,
                    JoinedAt = DateTime.UtcNow,
                    LastReadAt = DateTime.UtcNow
                }).ToList();

                _context.ConversationMembers.AddRange(conversationMembers);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return await GetConversationDetailsAsync(accountId, conversation.ConversationID);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ConversationListItemResponse>> GetWorkspaceConversationsAsync(int accountId, int workspaceId)
        {
            var currentMember = await GetCurrentWorkspaceMemberAsync(accountId, workspaceId);

            // Optimal query avoiding N+1
            var conversationsQuery = await _context.ConversationMembers
                .Where(cm => cm.MemberID == currentMember.WorkspaceMemberID && cm.Conversation.WorkspaceID == workspaceId)
                .Select(cm => new
                {
                    ConversationId = cm.ConversationID,
                    WorkspaceId = cm.Conversation.WorkspaceID,
                    ProjectId = cm.Conversation.ProjectID,
                    Name = cm.Conversation.Name,
                    Type = cm.Conversation.Type,
                    CreatedAt = cm.Conversation.CreatedAt,
                    LastReadAt = cm.LastReadAt,
                    
                    // Retrieve the last message
                    LastMessage = _context.Messages
                        .IgnoreQueryFilters()
                        .Where(m => m.ConversationID == cm.ConversationID)
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => new { m.Content, m.CreatedAt, m.IsDeleted })
                        .FirstOrDefault(),

                    // Calculate Unread Count
                    UnreadCount = _context.Messages
                        .Count(m => m.ConversationID == cm.ConversationID 
                                 && m.CreatedAt > cm.LastReadAt 
                                 && m.SenderID != currentMember.WorkspaceMemberID),

                    // Pre-fetch other member name for Direct conversations
                    OtherMemberName = cm.Conversation.Type == "Direct" 
                        ? _context.ConversationMembers
                            .Where(other => other.ConversationID == cm.ConversationID && other.MemberID != currentMember.WorkspaceMemberID)
                            .Select(other => other.WorkspaceMember.Resource.FullName)
                            .FirstOrDefault() 
                        : null,

                    // Sort column
                    SortAt = _context.Messages
                        .IgnoreQueryFilters()
                        .Where(m => m.ConversationID == cm.ConversationID)
                        .Max(m => (DateTime?)m.CreatedAt) ?? cm.Conversation.CreatedAt
                })
                .ToListAsync();

            var responseList = conversationsQuery.Select(item => new
            {
                Response = new ConversationListItemResponse
                {
                    ConversationId = item.ConversationId,
                    WorkspaceId = item.WorkspaceId,
                    ProjectId = item.ProjectId,
                    Name = item.Type == "Direct" ? (item.OtherMemberName ?? "Unknown User") : item.Name,
                    Type = item.Type,
                    LastMessageContent = item.LastMessage == null
                        ? null
                        : item.LastMessage.IsDeleted
                            ? "[Tin nhan da thu hoi]"
                            : item.LastMessage.Content,
                    LastMessageAt = item.LastMessage?.CreatedAt,
                    UnreadCount = item.UnreadCount
                },
                item.SortAt
            })
            .OrderByDescending(x => x.SortAt)
            .Select(x => x.Response)
            .ToList();

            return responseList;
        }

        public async Task<ConversationDetailResponse> GetConversationDetailsAsync(int accountId, int conversationId)
        {
            var member = await _context.ConversationMembers
                .Include(cm => cm.WorkspaceMember.Resource)
                .Include(cm => cm.Conversation)
                .Include(cm => cm.Conversation.Workspace)
                .FirstOrDefaultAsync(cm => cm.ConversationID == conversationId 
                                        && cm.WorkspaceMember.Resource.AccountID == accountId
                                        && cm.WorkspaceMember.Status == "Active"
                                        && !cm.Conversation.Workspace.IsDeleted);

            if (member == null)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập hội thoại này hoặc hội thoại không tồn tại.");

            var conversation = member.Conversation;

            var membersList = await _context.ConversationMembers
                .Include(cm => cm.WorkspaceMember.Resource)
                .Where(cm => cm.ConversationID == conversationId)
                .ToListAsync();

            // Compute dynamic name for Direct
            string? displayValue = conversation.Name;
            if (conversation.Type == "Direct")
            {
                var otherMember = membersList.FirstOrDefault(m => m.MemberID != member.MemberID);
                displayValue = otherMember?.WorkspaceMember?.Resource?.FullName ?? "Unknown User";
            }

            return new ConversationDetailResponse
            {
                ConversationId = conversation.ConversationID,
                WorkspaceId = conversation.WorkspaceID,
                ProjectId = conversation.ProjectID,
                Name = displayValue,
                Type = conversation.Type,
                CreatedAt = conversation.CreatedAt,
                Members = membersList.Select(m => new ConversationMemberDto
                {
                    WorkspaceMemberId = m.MemberID,
                    ResourceId = m.WorkspaceMember.ResourceID,
                    FullName = m.WorkspaceMember.Resource.FullName,
                    AvatarUrl = m.WorkspaceMember.Resource.AvatarURL,
                    JoinedAt = m.JoinedAt,
                    LastReadAt = m.LastReadAt
                }).ToList()
            };
        }

        public async Task<List<MessageResponse>> GetConversationMessagesAsync(
            int accountId,
            int conversationId,
            GetConversationMessagesQuery query)
        {
            await LoadConversationAccessAsync(accountId, conversationId);

            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var keyword = NormalizeOptionalString(query.Keyword);

            var messagesQuery = _context.Messages
                .IgnoreQueryFilters()
                .Include(message => message.Sender)
                    .ThenInclude(sender => sender!.Resource)
                .Include(message => message.MessageAssets)
                    .ThenInclude(messageAsset => messageAsset.Asset)
                .AsNoTracking()
                .Where(message => message.ConversationID == conversationId);

            if (query.BeforeMessageId.HasValue)
            {
                messagesQuery = messagesQuery.Where(message => message.MessageID < query.BeforeMessageId.Value);
            }

            if (keyword != null)
            {
                messagesQuery = messagesQuery.Where(message =>
                    !message.IsDeleted
                    && message.Content != null
                    && message.Content.Contains(keyword));
            }

            var messages = await messagesQuery
                .OrderByDescending(message => message.MessageID)
                .Take(pageSize)
                .ToListAsync();

            return messages
                .OrderBy(message => message.MessageID)
                .Select(MapMessageResponse)
                .ToList();
        }

        public async Task<MessageResponse> SendMessageAsync(
            int accountId,
            int conversationId,
            CreateMessageRequest request)
        {
            var (conversation, currentMember, _, _) = await LoadConversationAccessAsync(
                accountId,
                conversationId,
                asTracking: false);

            var content = NormalizeOptionalString(request.Content);
            var assetIds = request.AssetIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (content == null && assetIds.Count == 0)
            {
                throw new ArgumentException("Tin nhan phai co noi dung hoac tai lieu dinh kem.");
            }

            if (assetIds.Count > 0)
            {
                await ValidateMessageAssetsAsync(conversation, assetIds);
            }

            Message message;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                message = new Message
                {
                    ConversationID = conversationId,
                    SenderID = currentMember.WorkspaceMemberID,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                if (assetIds.Count > 0)
                {
                    var messageAssets = assetIds.Select(assetId => new MessageAsset
                    {
                        MessageID = message.MessageID,
                        AssetID = assetId
                    });

                    _context.MessageAssets.AddRange(messageAssets);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            var response = await LoadMessageResponseAsync(message.MessageID, includeDeleted: false);
            await _hubContext.Clients
                .Group(ConversationHub.BuildConversationGroup(conversationId))
                .SendAsync("MessageCreated", response);

            return response;
        }

        public async Task MarkConversationAsReadAsync(int accountId, int conversationId)
        {
            var member = await _context.ConversationMembers
                .Include(cm => cm.WorkspaceMember.Resource)
                .Include(cm => cm.Conversation.Workspace)
                .FirstOrDefaultAsync(cm => cm.ConversationID == conversationId 
                                        && cm.WorkspaceMember.Resource.AccountID == accountId
                                        && cm.WorkspaceMember.Status == "Active"
                                        && !cm.Conversation.Workspace.IsDeleted);

            if (member == null)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập hội thoại này.");

            member.LastReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var payload = new
            {
                ConversationId = conversationId,
                MemberId = member.MemberID,
                LastReadAt = member.LastReadAt
            };

            await _hubContext.Clients
                .Group(ConversationHub.BuildConversationGroup(conversationId))
                .SendAsync("ConversationRead", payload);

            await _hubContext.Clients
                .Group(ConversationHub.BuildUserGroup(member.MemberID))
                .SendAsync("ConversationRead", payload);
        }
        private async Task<(Conversation Conversation, WorkspaceMember CurrentMember, string RoleName, int WorkspaceRoleId)> LoadConversationAccessAsync(int accountId, int conversationId, bool asTracking = false)
        {
            var query = _context.ConversationMembers
                .Include(cm => cm.Conversation)
                .Include(cm => cm.WorkspaceMember)
                    .ThenInclude(wm => wm.Resource)
                .Include(cm => cm.WorkspaceMember.WorkspaceRole)
                .Include(cm => cm.Conversation.Workspace)
                .Where(cm => cm.ConversationID == conversationId 
                          && cm.WorkspaceMember.Resource.AccountID == accountId
                          && cm.WorkspaceMember.Status == "Active"
                          && !cm.Conversation.IsDeleted
                          && !cm.Conversation.Workspace.IsDeleted);

            if (!asTracking)
            {
                query = query.AsNoTracking();
            }

            var cm = await query.FirstOrDefaultAsync();

            if (cm == null)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập hội thoại này hoặc hội thoại không tồn tại.");
            }

            var workspaceIsDeleted = cm.Conversation != null
                && cm.Conversation.Workspace is { IsDeleted: true };
            if (workspaceIsDeleted)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập hội thoại này hoặc hội thoại không tồn tại.");
            }

            return (cm.Conversation, cm.WorkspaceMember, cm.WorkspaceMember.WorkspaceRole!.RoleName, cm.WorkspaceMember.WorkspaceRoleID);
        }

        private async Task<bool> HasManagePermissionAsync(int workspaceRoleId, string roleName)
        {
            if (roleName == "Owner") return true;

            return await _context.RolePermissions
                .AnyAsync(rp => rp.WorkspaceRoleID == workspaceRoleId && rp.PermissionID == Filters.ConversationPermissionIds.Manage);
        }

        private async Task ValidateMessageAssetsAsync(Conversation conversation, List<int> assetIds)
        {
            var validAssetCount = await _context.ProjectAssets
                .AsNoTracking()
                .CountAsync(asset =>
                    assetIds.Contains(asset.AssetID)
                    && asset.WorkspaceID == conversation.WorkspaceID
                    && (conversation.Type == "Project_Channel"
                        ? asset.ProjectID == conversation.ProjectID
                        : asset.ProjectID == null));

            if (validAssetCount != assetIds.Count)
            {
                throw new ArgumentException("Mot hoac nhieu tai lieu dinh kem khong hop le cho hoi thoai nay.");
            }
        }

        private async Task<MessageResponse> LoadMessageResponseAsync(int messageId, bool includeDeleted)
        {
            var query = includeDeleted
                ? _context.Messages.IgnoreQueryFilters()
                : _context.Messages.AsQueryable();

            var message = await query
                .Include(item => item.Sender)
                    .ThenInclude(sender => sender!.Resource)
                .Include(item => item.MessageAssets)
                    .ThenInclude(messageAsset => messageAsset.Asset)
                .AsNoTracking()
                .FirstAsync(item => item.MessageID == messageId);

            return MapMessageResponse(message);
        }

        private static MessageResponse MapMessageResponse(Message message)
        {
            return new MessageResponse
            {
                MessageId = message.MessageID,
                ConversationId = message.ConversationID,
                SenderId = message.SenderID,
                SenderName = message.Sender?.Resource?.FullName,
                SenderAvatarUrl = message.Sender?.Resource?.AvatarURL,
                Content = message.IsDeleted ? "[Tin nhan da thu hoi]" : message.Content,
                CreatedAt = message.CreatedAt,
                IsEdited = message.IsEdited,
                IsDeleted = message.IsDeleted,
                Assets = message.IsDeleted
                    ? null
                    : message.MessageAssets
                        .Where(messageAsset => messageAsset.Asset != null)
                        .Select(messageAsset => new AssetResponse
                        {
                            AssetId = messageAsset.AssetID,
                            AssetName = messageAsset.Asset!.AssetName,
                            AssetType = messageAsset.Asset.AssetType,
                            FileSizeKB = messageAsset.Asset.FileSizeKB,
                            CreatedAt = messageAsset.Asset.CreatedAt
                        })
                        .ToList()
            };
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        public async Task<ConversationDetailResponse> RenameConversationAsync(int accountId, int conversationId, UpdateConversationNameRequest request)
        {
            var (conversation, currentMember, roleName, workspaceRoleId) = await LoadConversationAccessAsync(accountId, conversationId, asTracking: true);

            if (conversation.Type == "Direct")
                throw new ArgumentException("Không thể đổi tên hội thoại 1-1.");

            var hasManagePerm = await HasManagePermissionAsync(workspaceRoleId, roleName);
            if (!hasManagePerm)
                throw new UnauthorizedAccessException("Bạn không có quyền đổi tên hội thoại này.");

            var newName = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Tên hội thoại không được để trống.");

            conversation.Name = newName;
            await _context.SaveChangesAsync();

            return await GetConversationDetailsAsync(accountId, conversationId);
        }

        public async Task DeleteConversationAsync(int accountId, int conversationId)
        {
            var (conversation, currentMember, roleName, workspaceRoleId) = await LoadConversationAccessAsync(accountId, conversationId, asTracking: true);

            if (conversation.Type != "Direct")
            {
                var hasManagePerm = await HasManagePermissionAsync(workspaceRoleId, roleName);
                if (!hasManagePerm)
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa/giải tán hội thoại này.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                conversation.IsDeleted = true;
                conversation.DeletedAt = DateTime.UtcNow;
                conversation.DeletedBy = accountId;
                conversation.ConversationKey = null;

                await _context.Messages
                    .Where(m => m.ConversationID == conversationId && !m.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.IsDeleted, true)
                        .SetProperty(m => m.DeletedAt, DateTime.UtcNow)
                        .SetProperty(m => m.DeletedBy, accountId));

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _hubContext.Clients
                    .Group(ConversationHub.BuildConversationGroup(conversationId))
                    .SendAsync("ConversationCleared", new { ConversationId = conversationId, DeletedBy = accountId });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ConversationDetailResponse> AddMembersAsync(int accountId, int conversationId, AddConversationMembersRequest request)
        {
            var (conversation, currentMember, roleName, workspaceRoleId) = await LoadConversationAccessAsync(accountId, conversationId, asTracking: false);

            if (conversation.Type == "Direct")
                throw new ArgumentException("Không thể thêm thành viên vào hội thoại 1-1.");

            var hasManagePerm = await HasManagePermissionAsync(workspaceRoleId, roleName);
            if (!hasManagePerm)
                throw new UnauthorizedAccessException("Bạn không có quyền thêm thành viên vào hội thoại này.");

            if (request.WorkspaceMemberIds.Any(id => id <= 0))
                throw new ArgumentException("ID thành viên không hợp lệ.");

            var memberIdsToAdd = request.WorkspaceMemberIds
                .Distinct()
                .ToList();

            if (!memberIdsToAdd.Any())
                return await GetConversationDetailsAsync(accountId, conversationId);

            var validMembersCount = await _context.WorkspaceMembers
                .CountAsync(m => memberIdsToAdd.Contains(m.WorkspaceMemberID) && m.WorkspaceID == conversation.WorkspaceID && m.Status == "Active");

            if (validMembersCount != memberIdsToAdd.Count)
                throw new ArgumentException("Một số thành viên không hợp lệ hoặc không thuộc Workspace này.");

            var existingMemberIds = await _context.ConversationMembers
                .Where(cm => cm.ConversationID == conversationId && memberIdsToAdd.Contains(cm.MemberID))
                .Select(cm => cm.MemberID)
                .ToListAsync();

            var newMemberIds = memberIdsToAdd.Except(existingMemberIds).ToList();

            if (!newMemberIds.Any())
                return await GetConversationDetailsAsync(accountId, conversationId);

            var newMembers = newMemberIds.Select(id => new ConversationMember
            {
                ConversationID = conversationId,
                MemberID = id,
                JoinedAt = DateTime.UtcNow,
                LastReadAt = DateTime.UtcNow
            }).ToList();

            try
            {
                _context.ConversationMembers.AddRange(newMembers);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Có lỗi xảy ra hoặc dữ liệu bị trùng lặp, vui lòng thử lại.");
            }

            return await GetConversationDetailsAsync(accountId, conversationId);
        }

        public async Task RemoveMemberAsync(int accountId, int conversationId, int targetMemberId)
        {
            var (conversation, currentMember, roleName, workspaceRoleId) = await LoadConversationAccessAsync(accountId, conversationId, asTracking: false);

            if (conversation.Type == "Direct")
                throw new ArgumentException("Không thể xóa thành viên khỏi hội thoại 1-1.");

            if (currentMember.WorkspaceMemberID != targetMemberId)
            {
                var hasManagePerm = await HasManagePermissionAsync(workspaceRoleId, roleName);
                if (!hasManagePerm)
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa thành viên khác khỏi hội thoại này.");
            }

            var targetCm = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationID == conversationId && cm.MemberID == targetMemberId);

            if (targetCm == null)
                throw new KeyNotFoundException("Không tìm thấy thành viên trong hội thoại."); 

            var currentMemberCount = await _context.ConversationMembers.CountAsync(cm => cm.ConversationID == conversationId);
            if (currentMemberCount <= 1)
            {
                throw new InvalidOperationException("Không thể xóa thành viên cuối cùng của hội thoại. Vui lòng giải tán hội thoại.");
            }

            _context.ConversationMembers.Remove(targetCm);
            await _context.SaveChangesAsync();
        }

    }
}
