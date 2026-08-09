-- Tạo bảng AIToolExecutionLogs
CREATE TABLE AIToolExecutionLogs (
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    WorkspaceID INT NULL,
    AccountID INT NULL,
    ToolName VARCHAR(100) NOT NULL,
    Arguments NVARCHAR(MAX) NOT NULL,
    IsSuccess BIT NOT NULL,
    StatusCode INT NOT NULL,
    ErrorCode VARCHAR(50) NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    ExecutionTimeMs BIGINT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    DeletedBy INT NULL,
    UpdatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    CONSTRAINT FK_AIToolExecutionLogs_Workspaces FOREIGN KEY (WorkspaceID) REFERENCES Workspaces(WorkspaceID),
    CONSTRAINT FK_AIToolExecutionLogs_Accounts FOREIGN KEY (AccountID) REFERENCES Accounts(AccountID)
);

-- Index cho tìm kiếm/lọc nhật ký theo Workspace hoặc theo ToolName
CREATE NONCLUSTERED INDEX IX_AIToolExecutionLogs_WorkspaceID_CreatedAt 
ON AIToolExecutionLogs(WorkspaceID, CreatedAt DESC) 
WHERE IsDeleted = 0;

CREATE NONCLUSTERED INDEX IX_AIToolExecutionLogs_ToolName_CreatedAt 
ON AIToolExecutionLogs(ToolName, CreatedAt DESC) 
WHERE IsDeleted = 0;

-- Thêm UNIQUE FILTERED INDEX để đảm bảo mỗi Workspace chỉ có 1 gói cước Active tại một thời điểm
CREATE UNIQUE NONCLUSTERED INDEX UQ_WorkspaceSubscriptions_Active_Workspace 
ON WorkspaceSubscriptions(WorkspaceID) 
WHERE IsDeleted = 0 AND Status = 'Active';
