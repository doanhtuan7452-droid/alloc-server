namespace AllocServer.Events.DomainEvents
{
    public class RiskNotificationEvent : IDomainEvent
    {
        public int RiskID { get; }
        public string RiskName { get; }
        public int ProjectID { get; }
        public string ProjectName { get; }
        public int? RecipientMemberID { get; } // Null if sending to workspace owners/PMs
        public int ActorMemberID { get; }
        public string ActionType { get; } // "Created", "Assigned", "StatusChanged"
        public string Message { get; }

        public RiskNotificationEvent(int riskId, string riskName, int projectId, string projectName, int? recipientMemberId, int actorMemberId, string actionType, string message)
        {
            RiskID = riskId;
            RiskName = riskName;
            ProjectID = projectId;
            ProjectName = projectName;
            RecipientMemberID = recipientMemberId;
            ActorMemberID = actorMemberId;
            ActionType = actionType;
            Message = message;
        }
    }
}
