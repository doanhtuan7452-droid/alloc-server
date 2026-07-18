namespace AllocServer.Events.DomainEvents
{
    public class TaskCreatedEvent : IDomainEvent
    {
        public int TaskID { get; }
        public string TaskName { get; }
        public int CreatorMemberID { get; }
        public int ProjectID { get; }
        public int WorkspaceID { get; }

        public TaskCreatedEvent(int taskId, string taskName, int creatorMemberId, int projectId, int workspaceId)
        {
            TaskID = taskId;
            TaskName = taskName;
            CreatorMemberID = creatorMemberId;
            ProjectID = projectId;
            WorkspaceID = workspaceId;
        }
    }
}
