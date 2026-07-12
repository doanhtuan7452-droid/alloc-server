using AllocServer.Events;

namespace AllocServer.Events.DomainEvents
{
    public class WorkspaceRoleUpdatedEvent : IDomainEvent
    {
        public int WorkspaceID { get; }
        public int WorkspaceRoleID { get; }
        public string Action { get; } // "RoleNameUpdated", "RoleDeleted", "PermissionsUpdated"

        public WorkspaceRoleUpdatedEvent(int workspaceId, int workspaceRoleId, string action)
        {
            WorkspaceID = workspaceId;
            WorkspaceRoleID = workspaceRoleId;
            Action = action;
        }
    }
}
