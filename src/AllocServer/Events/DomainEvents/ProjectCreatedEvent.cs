namespace AllocServer.Events.DomainEvents
{
    public class ProjectCreatedEvent : IDomainEvent
    {
        public int ProjectID { get; }
        public string ProjectName { get; }
        public int CreatorMemberID { get; }
        public int WorkspaceID { get; }

        public ProjectCreatedEvent(int projectId, string projectName, int creatorMemberId, int workspaceId)
        {
            ProjectID = projectId;
            ProjectName = projectName;
            CreatorMemberID = creatorMemberId;
            WorkspaceID = workspaceId;
        }
    }
}
