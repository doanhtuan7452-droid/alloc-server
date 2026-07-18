using System;

namespace AllocServer.Events.DomainEvents
{
    public class TaskDeadlineChangedEvent : IDomainEvent
    {
        public int TaskID { get; }
        public string TaskName { get; }
        public DateOnly? OldDeadline { get; }
        public DateOnly? NewDeadline { get; }
        public int ChangerMemberID { get; }

        public TaskDeadlineChangedEvent(int taskId, string taskName, DateOnly? oldDeadline, DateOnly? newDeadline, int changerMemberId)
        {
            TaskID = taskId;
            TaskName = taskName;
            OldDeadline = oldDeadline;
            NewDeadline = newDeadline;
            ChangerMemberID = changerMemberId;
        }
    }
}
