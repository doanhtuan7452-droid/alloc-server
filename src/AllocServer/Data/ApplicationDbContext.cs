using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountSession> AccountSessions { get; set; }
        public DbSet<Resource> Resources { get; set; }
        
        // Workspace Entities
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<WorkspaceRole> WorkspaceRoles { get; set; }
        public DbSet<WorkspacePermission> WorkspacePermissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }
        public DbSet<WorkspaceCurrentLimit> WorkspaceCurrentLimits { get; set; }
        public DbSet<WorkspaceMonthlyUsage> WorkspaceMonthlyUsages { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Revenue> Revenues { get; set; }
        public DbSet<ProjectAsset> ProjectAssets { get; set; }
        public DbSet<AILog> AILogs { get; set; }
        public DbSet<Risk> Risks { get; set; }
        public DbSet<RiskMitigation> RiskMitigations { get; set; }
        public DbSet<RiskLifecycle> RiskLifecycles { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationMember> ConversationMembers { get; set; }
        public DbSet<Timesheet> Timesheets { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<OTRequest> OTRequests { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskAsset> TaskAssets { get; set; }
        public DbSet<TaskAssignee> TaskAssignees { get; set; }
        public DbSet<TaskDependency> TaskDependencies { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageAsset> MessageAssets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>()
                .HasQueryFilter(a => !a.IsDeleted);

            modelBuilder.Entity<Resource>()
                .HasQueryFilter(r => !r.IsDeleted);

            modelBuilder.Entity<Workspace>()
                .HasQueryFilter(w => !w.IsDeleted);

            modelBuilder.Entity<WorkspaceRole>()
                .HasQueryFilter(wr => !wr.IsDeleted);

            modelBuilder.Entity<Project>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<ProjectTask>()
                .HasQueryFilter(t => !t.IsDeleted);

            modelBuilder.Entity<Expense>()
                .HasQueryFilter(e => !e.IsDeleted);

            modelBuilder.Entity<Revenue>()
                .HasQueryFilter(r => !r.IsDeleted);

            modelBuilder.Entity<ProjectAsset>()
                .HasQueryFilter(a => !a.IsDeleted);

            modelBuilder.Entity<Risk>()
                .HasQueryFilter(r => !r.IsDeleted);

            modelBuilder.Entity<Conversation>()
                .HasQueryFilter(c => !c.IsDeleted);

            modelBuilder.Entity<Conversation>()
                .HasIndex(c => c.ConversationKey)
                .IsUnique()
                .HasFilter("[ConversationKey] IS NOT NULL AND [IsDeleted] = 0");

            modelBuilder.Entity<Timesheet>()
                .HasQueryFilter(t => !t.IsDeleted);

            modelBuilder.Entity<LeaveRequest>()
                .HasQueryFilter(r => !r.IsDeleted);

            modelBuilder.Entity<OTRequest>()
                .HasQueryFilter(r => !r.IsDeleted);

            modelBuilder.Entity<TaskComment>()
                .HasQueryFilter(c => !c.IsDeleted);

            modelBuilder.Entity<Message>()
                .HasQueryFilter(m => !m.IsDeleted);

            // Explicit FK constraints
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Workspace)
                .WithMany()
                .HasForeignKey(c => c.WorkspaceID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Project)
                .WithMany()
                .HasForeignKey(c => c.ProjectID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany()
                .HasForeignKey(m => m.ConversationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderID)
                .OnDelete(DeleteBehavior.Restrict);

            // WorkspaceCurrentLimit maps to a View
            modelBuilder.Entity<WorkspaceCurrentLimit>()
                .ToView("vw_WorkspaceCurrentLimits")
                .HasNoKey();

            modelBuilder.Entity<RolePermission>()
                .HasKey(rolePermission => new
                {
                    rolePermission.WorkspaceRoleID,
                    rolePermission.PermissionID
                });

            modelBuilder.Entity<AILog>()
                .HasOne(log => log.Project)
                .WithMany()
                .HasForeignKey(log => log.ProjectID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AILog>()
                .HasIndex(log => new
                {
                    log.ProjectID,
                    log.CreatedAt
                });

            modelBuilder.Entity<WorkspaceMonthlyUsage>()
                .HasOne(usage => usage.Workspace)
                .WithMany()
                .HasForeignKey(usage => usage.WorkspaceID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkspaceMonthlyUsage>()
                .HasIndex(usage => new
                {
                    usage.WorkspaceID,
                    usage.BillingMonth
                })
                .IsUnique();

            // UQ_Accounts_Email — Unique Email nhưng bỏ qua bản ghi đã xóa mềm
            modelBuilder.Entity<Account>()
                .HasIndex(a => a.Email)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<AccountSession>()
                .HasIndex(s => s.RefreshToken)
                .IsUnique();

            // UQ_Resources_AccountID — Mỗi Account chỉ có 1 Resource (1-1), bỏ qua xóa mềm
            modelBuilder.Entity<Resource>()
                .HasIndex(r => r.AccountID)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // UQ_Projects_Workspace_Name — Không cho phép trùng tên dự án trong cùng 1 Workspace
            modelBuilder.Entity<Project>()
                .HasIndex(p => new { p.WorkspaceID, p.ProjectName })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<TaskAssignee>()
                .HasKey(taskAssignee => new
                {
                    taskAssignee.TaskID,
                    taskAssignee.WorkspaceMemberID,
                    taskAssignee.AssigneeType
                });

            modelBuilder.Entity<TaskAssignee>()
                .HasOne(taskAssignee => taskAssignee.Task)
                .WithMany()
                .HasForeignKey(taskAssignee => taskAssignee.TaskID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskAssignee>()
                .HasOne(taskAssignee => taskAssignee.WorkspaceMember)
                .WithMany()
                .HasForeignKey(taskAssignee => taskAssignee.WorkspaceMemberID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Timesheet>()
                .HasOne(timesheet => timesheet.Task)
                .WithMany()
                .HasForeignKey(timesheet => timesheet.TaskID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Timesheet>()
                .HasOne(timesheet => timesheet.WorkspaceMember)
                .WithMany()
                .HasForeignKey(timesheet => timesheet.WorkspaceMemberID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Timesheet>()
                .HasIndex(timesheet => new
                {
                    timesheet.TaskID,
                    timesheet.WorkspaceMemberID,
                    timesheet.WorkDate
                })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(request => request.WorkspaceMember)
                .WithMany()
                .HasForeignKey(request => request.WorkspaceMemberID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(request => request.Approver)
                .WithMany()
                .HasForeignKey(request => request.ApproverID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OTRequest>()
                .HasOne(request => request.WorkspaceMember)
                .WithMany()
                .HasForeignKey(request => request.WorkspaceMemberID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OTRequest>()
                .HasOne(request => request.Approver)
                .WithMany()
                .HasForeignKey(request => request.ApproverID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OTRequest>()
                .HasOne(request => request.Task)
                .WithMany()
                .HasForeignKey(request => request.TaskID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Expense>()
                .HasOne(expense => expense.Project)
                .WithMany()
                .HasForeignKey(expense => expense.ProjectID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Revenue>()
                .HasOne(revenue => revenue.Project)
                .WithMany()
                .HasForeignKey(revenue => revenue.ProjectID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectAsset>()
                .HasOne(asset => asset.Workspace)
                .WithMany()
                .HasForeignKey(asset => asset.WorkspaceID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectAsset>()
                .HasOne(asset => asset.Project)
                .WithMany()
                .HasForeignKey(asset => asset.ProjectID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectAsset>()
                .HasOne(asset => asset.UploadedByMember)
                .WithMany()
                .HasForeignKey(asset => asset.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectAsset>()
                .HasIndex(asset => new
                {
                    asset.WorkspaceID,
                    asset.ProjectID
                })
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<ProjectAsset>()
                .Property(asset => asset.AssetType)
                .HasMaxLength(20);

            modelBuilder.Entity<ProjectAsset>()
                .Property(asset => asset.AssetName)
                .HasMaxLength(255);

            // Risk — Computed column + FK relationships
            modelBuilder.Entity<Risk>()
                .Property(risk => risk.RiskScore)
                .HasComputedColumnSql("([Probability]*[Impact])", stored: true);

            modelBuilder.Entity<Risk>()
                .HasOne(risk => risk.Project)
                .WithMany()
                .HasForeignKey(risk => risk.ProjectID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Risk>()
                .HasOne(risk => risk.Task)
                .WithMany()
                .HasForeignKey(risk => risk.TaskID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Risk>()
                .HasOne(risk => risk.Owner)
                .WithMany()
                .HasForeignKey(risk => risk.OwnerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Risk>()
                .HasOne(risk => risk.AILog)
                .WithMany()
                .HasForeignKey(risk => risk.AILogID)
                .OnDelete(DeleteBehavior.SetNull);

            // RiskMitigation — FK relationships
            modelBuilder.Entity<RiskMitigation>()
                .HasOne(mitigation => mitigation.Risk)
                .WithMany()
                .HasForeignKey(mitigation => mitigation.RiskID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RiskMitigation>()
                .HasOne(mitigation => mitigation.AssignedMember)
                .WithMany()
                .HasForeignKey(mitigation => mitigation.AssignedMemberID)
                .OnDelete(DeleteBehavior.Restrict);

            // RiskLifecycle — FK relationships
            modelBuilder.Entity<RiskLifecycle>()
                .HasOne(lifecycle => lifecycle.Risk)
                .WithMany()
                .HasForeignKey(lifecycle => lifecycle.RiskID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RiskLifecycle>()
                .HasOne(lifecycle => lifecycle.ChangedByMember)
                .WithMany()
                .HasForeignKey(lifecycle => lifecycle.ChangedByMemberID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskDependency>()
                .HasOne(taskDependency => taskDependency.PredecessorTask)
                .WithMany()
                .HasForeignKey(taskDependency => taskDependency.PredecessorTaskID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskDependency>()
                .HasOne(taskDependency => taskDependency.SuccessorTask)
                .WithMany()
                .HasForeignKey(taskDependency => taskDependency.SuccessorTaskID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskDependency>()
                .HasIndex(taskDependency => new
                {
                    taskDependency.PredecessorTaskID,
                    taskDependency.SuccessorTaskID,
                    taskDependency.DependencyType
                })
                .IsUnique();

            // TaskComment - FK relationships
            modelBuilder.Entity<TaskComment>()
                .HasOne(c => c.Task)
                .WithMany()
                .HasForeignKey(c => c.TaskID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskComment>()
                .HasOne(c => c.WorkspaceMember)
                .WithMany()
                .HasForeignKey(c => c.MemberID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskComment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentID)
                .OnDelete(DeleteBehavior.Restrict);

            // TaskAsset - Composite Key and FK relationships
            modelBuilder.Entity<TaskAsset>()
                .HasKey(ta => new { ta.TaskID, ta.AssetID });

            modelBuilder.Entity<TaskAsset>()
                .HasOne(ta => ta.Task)
                .WithMany()
                .HasForeignKey(ta => ta.TaskID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskAsset>()
                .HasOne(ta => ta.Asset)
                .WithMany()
                .HasForeignKey(ta => ta.AssetID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskAsset>()
                .HasOne(ta => ta.AttachedByMember)
                .WithMany()
                .HasForeignKey(ta => ta.AttachedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // ConversationMember - Composite Key, FKs, and Index
            modelBuilder.Entity<ConversationMember>()
                .HasKey(cm => new { cm.ConversationID, cm.MemberID });

            modelBuilder.Entity<ConversationMember>()
                .HasOne(cm => cm.Conversation)
                .WithMany()
                .HasForeignKey(cm => cm.ConversationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConversationMember>()
                .HasOne(cm => cm.WorkspaceMember)
                .WithMany()
                .HasForeignKey(cm => cm.MemberID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConversationMember>()
                .HasIndex(cm => new { cm.MemberID, cm.ConversationID })
                .HasDatabaseName("IX_ConversationMembers_MemberID_ConversationID");

            // MessageAsset - Composite Key and FKs
            modelBuilder.Entity<MessageAsset>()
                .HasKey(ma => new { ma.MessageID, ma.AssetID });

            modelBuilder.Entity<MessageAsset>()
                .HasOne(ma => ma.Message)
                .WithMany(message => message.MessageAssets)
                .HasForeignKey(ma => ma.MessageID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessageAsset>()
                .HasOne(ma => ma.Asset)
                .WithMany()
                .HasForeignKey(ma => ma.AssetID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

