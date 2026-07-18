namespace AllocServer.Events.DomainEvents
{
    public class TaskCommentCreatedEvent : IDomainEvent
    {
        public int CommentID { get; }
        public int TaskID { get; }
        public string TaskName { get; }
        public int MemberID { get; }
        public int? ParentCommentID { get; }
        public string Content { get; }

        public TaskCommentCreatedEvent(int commentId, int taskId, string taskName, int memberId, int? parentCommentId, string content)
        {
            CommentID = commentId;
            TaskID = taskId;
            TaskName = taskName;
            MemberID = memberId;
            ParentCommentID = parentCommentId;
            Content = content;
        }
    }
}
