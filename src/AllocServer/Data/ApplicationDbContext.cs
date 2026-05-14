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
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Revenue> Revenues { get; set; }
        public DbSet<ProjectAsset> ProjectAssets { get; set; }
        public DbSet<Risk> Risks { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Timesheet> Timesheets { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<OTRequest> OTRequests { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskAssignee> TaskAssignees { get; set; }
        public DbSet<TaskDependency> TaskDependencies { get; set; }
        public DbSet<Message> Messages { get; set; }

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
        }
    }
}
