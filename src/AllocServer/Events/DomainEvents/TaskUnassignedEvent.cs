namespace AllocServer.Events.DomainEvents
{
    public class TaskUnassignedEvent : IDomainEvent
    {
        public int TaskID { get; }
        public int MemberID { get; }
        public int AssignerMemberID { get; }
        public string TaskName { get; }

        public TaskUnassignedEvent(int taskId, int memberId, int assignerMemberId, string taskName)
        {
            TaskID = taskId;
            MemberID = memberId;
            AssignerMemberID = assignerMemberId;
            TaskName = taskName;
        }
    }
}
