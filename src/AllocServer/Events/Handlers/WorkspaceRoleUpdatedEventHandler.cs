using AllocServer.Data;
using AllocServer.Events.DomainEvents;
using AllocServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Events.Handlers
{
    public class WorkspaceRoleUpdatedEventHandler : IEventHandler<WorkspaceRoleUpdatedEvent>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IDistributedCache _cache;
        private readonly IHubContext<NotificationHub> _hubContext;

        public WorkspaceRoleUpdatedEventHandler(
            ApplicationDbContext dbContext,
            IDistributedCache cache,
            IHubContext<NotificationHub> hubContext)
        {
            _dbContext = dbContext;
            _cache = cache;
            _hubContext = hubContext;
        }

        public async Task HandleAsync(WorkspaceRoleUpdatedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var members = await _dbContext.WorkspaceMembers
                .Where(m => m.WorkspaceRoleID == domainEvent.WorkspaceRoleID && m.Status != "Deactivated")
                .Include(m => m.Resource)
                .Select(m => new { m.WorkspaceMemberID, m.Resource.AccountID })
                .ToListAsync(cancellationToken);

            foreach (var m in members)
            {
                // 1. Evict Auth Cache
                var cacheKey = $"workspace_auth_{m.AccountID}_{domainEvent.WorkspaceID}";
                await _cache.RemoveAsync(cacheKey, cancellationToken);

                // 2. Notify client to reload permissions via SignalR
                var groupName = ConversationHub.BuildUserGroup(m.WorkspaceMemberID);
                await _hubContext.Clients.Group(groupName).SendAsync("PermissionsChanged", new { workspaceId = domainEvent.WorkspaceID }, cancellationToken);
            }
        }
    }
}
