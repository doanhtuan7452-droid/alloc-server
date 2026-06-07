namespace AllocServer.Events.DomainEvents
{
    public class ReviewCycleCompletedEvent : IDomainEvent
    {
        public int CycleID { get; }
        public int WorkspaceID { get; }

        public ReviewCycleCompletedEvent(int cycleId, int workspaceId)
        {
            CycleID = cycleId;
            WorkspaceID = workspaceId;
        }
    }
}
