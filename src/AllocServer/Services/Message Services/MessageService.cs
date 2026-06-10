using AllocServer.Data;
using AllocServer.DTOs.Messages;
using AllocServer.Hubs;
using AllocServer.Interfaces.Messages;
using AllocServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Message_Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ConversationHub> _hubContext;

        public MessageService(
            ApplicationDbContext context,
            IHubContext<ConversationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<MessageResponse> EditMessageAsync(
            int accountId,
            int messageId,
            UpdateMessageRequest request)
        {
            var content = NormalizeRequiredContent(request.Content);
            var (message, currentMemberId) = await LoadMessageForSenderActionAsync(accountId, messageId);

            if (message.SenderID != currentMemberId)
            {
                throw new UnauthorizedAccessException("UnauthorizedMessageEdit");
            }

            message.Content = content;
            message.IsEdited = true;
            await _context.SaveChangesAsync();

            var response = await LoadMessageResponseAsync(messageId, includeDeleted: false);
            await _hubContext.Clients
                .Group(ConversationHub.BuildConversationGroup(message.ConversationID))
                .SendAsync("MessageEdited", response);

            return response;
        }

        public async Task DeleteMessageAsync(int accountId, int messageId)
        {
            var (message, currentMemberId) = await LoadMessageForSenderActionAsync(accountId, messageId);

            if (message.SenderID != currentMemberId)
            {
                throw new UnauthorizedAccessException("UnauthorizedMessageRecall");
            }

            message.IsDeleted = true;
            message.DeletedAt = DateTime.UtcNow;
            message.DeletedBy = accountId;

            await _context.SaveChangesAsync();

            var response = await LoadMessageResponseAsync(messageId, includeDeleted: true);
            await _hubContext.Clients
                .Group(ConversationHub.BuildConversationGroup(message.ConversationID))
                .SendAsync("MessageDeleted", response);
        }

        private async Task<(Message Message, int CurrentMemberId)> LoadMessageForSenderActionAsync(
            int accountId,
            int messageId)
        {
            var message = await _context.Messages
                .Include(item => item.Conversation)
                    .ThenInclude(conversation => conversation!.Workspace)
                .FirstOrDefaultAsync(item =>
                    item.MessageID == messageId
                    && item.Conversation != null
                    && !item.Conversation.IsDeleted
                    && item.Conversation.Workspace != null
                    && !item.Conversation.Workspace.IsDeleted);

            if (message == null)
            {
                throw new KeyNotFoundException("MessageNotFound");
            }

            var currentMemberId = await _context.ConversationMembers
                .AsNoTracking()
                .Where(member =>
                    member.ConversationID == message.ConversationID
                    && member.WorkspaceMember.Resource.AccountID == accountId
                    && member.WorkspaceMember.Status == "Active")
                .Select(member => (int?)member.MemberID)
                .FirstOrDefaultAsync();

            if (!currentMemberId.HasValue)
            {
                throw new UnauthorizedAccessException("UnauthorizedMessageAccess");
            }

            return (message, currentMemberId.Value);
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

        private static string NormalizeRequiredContent(string? content)
        {
            var normalized = string.IsNullOrWhiteSpace(content)
                ? null
                : content.Trim();

            if (normalized == null)
            {
                throw new ArgumentException("MessageContentRequired");
            }

            return normalized;
        }
    }
}
