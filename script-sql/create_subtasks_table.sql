-- Script to create the SubTasks table
-- Features: Nested task checklists, assignment constraints, standard audit trails (FIST), and ordering.

IF OBJECT_ID('dbo.SubTasks', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SubTasks (
        SubTaskID INT IDENTITY(1,1) PRIMARY KEY,
        TaskID INT NOT NULL,
        SubTaskName NVARCHAR(255) NOT NULL,
        Status VARCHAR(50) NOT NULL DEFAULT 'To-do',
        WorkspaceMemberID INT NULL,
        OrderIndex INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAt DATETIME NULL,
        DeletedBy INT NULL,
        CONSTRAINT FK_SubTasks_Tasks FOREIGN KEY (TaskID) REFERENCES dbo.Tasks(TaskID),
        CONSTRAINT FK_SubTasks_WorkspaceMembers FOREIGN KEY (WorkspaceMemberID) REFERENCES dbo.WorkspaceMembers(WorkspaceMemberID),
        CONSTRAINT CHK_SubTasks_Status CHECK (Status IN ('To-do', 'Done'))
    );

    -- Index for optimizing task-based queries
    CREATE NONCLUSTERED INDEX IX_SubTasks_TaskID ON dbo.SubTasks(TaskID) WHERE IsDeleted = 0;

    -- Index for optimizing member-based queries
    CREATE NONCLUSTERED INDEX IX_SubTasks_WorkspaceMemberID ON dbo.SubTasks(WorkspaceMemberID) WHERE IsDeleted = 0 AND WorkspaceMemberID IS NOT NULL;
END
GO
