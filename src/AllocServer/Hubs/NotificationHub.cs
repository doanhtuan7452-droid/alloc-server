using AllocServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AllocServer.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ApplicationDbContext _dbContext;

        public NotificationHub(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public override async Task OnConnectedAsync()
        {
            var accountIdString = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                                  ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(accountIdString, out int accountId))
            {
                var activeMemberIds = await _dbContext.WorkspaceMembers
                    .AsNoTracking()
                    .Include(m => m.Resource)
                    .Include(m => m.Workspace)
                    .Where(m => m.Resource.AccountID == accountId && m.Status == "Active" && !m.Resource.IsDeleted && !m.Workspace.IsDeleted)
                    .Select(m => m.WorkspaceMemberID)
                    .ToListAsync();

                foreach (var memberId in activeMemberIds)
                {
                    var groupName = ConversationHub.BuildUserGroup(memberId);
                    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                }
            }

            await base.OnConnectedAsync();
        }
    }
}
