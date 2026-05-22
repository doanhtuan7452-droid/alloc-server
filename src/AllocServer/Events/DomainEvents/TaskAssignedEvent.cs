using AllocServer.Events;

namespace AllocServer.Events.DomainEvents
{
    public class TaskAssignedEvent : IDomainEvent
    {
        public int TaskID { get; }
        public int AssigneeMemberID { get; }
        public int AssignerMemberID { get; }
        public string TaskName { get; }

        public TaskAssignedEvent(int taskId, int assigneeMemberId, int assignerMemberId, string taskName)
        {
            TaskID = taskId;
            AssigneeMemberID = assigneeMemberId;
            AssignerMemberID = assignerMemberId;
            TaskName = taskName;
        }
    }
}
