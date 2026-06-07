namespace AllocServer.Events.DomainEvents
{
    public class TaskStatusChangedEvent : IDomainEvent
    {
        public int TaskID { get; }
        public string OldStatus { get; }
        public string NewStatus { get; }

        public TaskStatusChangedEvent(int taskId, string oldStatus, string newStatus)
        {
            TaskID = taskId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}
