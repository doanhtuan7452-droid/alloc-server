USE ALLOC;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Tasks_TaskName_ProjectID' AND object_id = OBJECT_ID('Tasks'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tasks_TaskName_ProjectID 
    ON Tasks(TaskName) 
    INCLUDE (ProjectID) 
    WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Resources_FullName' AND object_id = OBJECT_ID('Resources'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Resources_FullName 
    ON Resources(FullName) 
    WHERE IsDeleted = 0;
END
GO
