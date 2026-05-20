using AllocServer.Data;
using AllocServer.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AllocServer.Hubs
{
    [Authorize]
    public class ConversationHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ConversationHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var accountId = GetCurrentAccountId();
            if (accountId.HasValue)
            {
                var memberships = await _context.WorkspaceMembers
                    .AsNoTracking()
                    .Where(member =>
                        member.Resource.AccountID == accountId.Value
                        && member.Status == "Active"
                        && !member.Workspace.IsDeleted)
                    .Select(member => new
                    {
                        member.WorkspaceMemberID,
                        member.WorkspaceID
                    })
                    .ToListAsync();

                foreach (var membership in memberships)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, BuildUserGroup(membership.WorkspaceMemberID));
                    await Groups.AddToGroupAsync(Context.ConnectionId, BuildWorkspaceGroup(membership.WorkspaceID));
                }
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinConversation(int conversationId)
        {
            var accountId = GetCurrentAccountId()
                ?? throw new HubException("Token khong hop le.");

            var hasAccess = await _context.ConversationMembers
                .AsNoTracking()
                .AnyAsync(member =>
                    member.ConversationID == conversationId
                    && member.WorkspaceMember.Resource.AccountID == accountId
                    && member.WorkspaceMember.Status == "Active"
                    && !member.Conversation.IsDeleted
                    && !member.Conversation.Workspace.IsDeleted);

            if (!hasAccess)
            {
                throw new HubException("Ban khong co quyen truy cap hoi thoai nay.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, BuildConversationGroup(conversationId));
        }

        public Task LeaveConversation(int conversationId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildConversationGroup(conversationId));
        }

        public static string BuildConversationGroup(int conversationId) => $"conversation:{conversationId}";
        public static string BuildWorkspaceGroup(int workspaceId) => $"workspace:{workspaceId}";
        public static string BuildUserGroup(int workspaceMemberId) => $"user:{workspaceMemberId}";

        private int? GetCurrentAccountId()
        {
            var accountIdClaim = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                                 ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(accountIdClaim, out var accountId)
                ? accountId
                : null;
        }
    }
}
