-- Migration Script: Add HITL Data Verification columns, composite index and update View
-- Purpose: Support Human-In-The-Loop data verification, 4-class risk corrections, and synchronize ML feature pipeline

-- 1. Thêm các cột thẩm định dữ liệu cho bảng AILogs
USE ALLOC;
GO

-- Script sửa lỗi: Thêm cột và ràng buộc Inline an toàn tuyệt đối
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'CorrectedRiskLevel' AND Object_ID = Object_ID(N'AILogs'))
BEGIN
    ALTER TABLE AILogs 
    ADD 
        CorrectedRiskLevel INT NULL 
            CONSTRAINT CHK_AILogs_CorrectedRiskLevel CHECK (CorrectedRiskLevel IS NULL OR CorrectedRiskLevel BETWEEN 0 AND 3),
            
        IsVerified BIT NOT NULL 
            CONSTRAINT DF_AILogs_IsVerified DEFAULT 0,
            
        VerifiedBy INT NULL 
            CONSTRAINT FK_AILogs_VerifiedBy FOREIGN KEY REFERENCES Accounts(AccountID),
            
        VerifiedAt DATETIME NULL;

    PRINT 'Successfully added HITL verification columns and constraints to AILogs table.';
END
ELSE
BEGIN
    PRINT 'HITL verification columns already exist in AILogs table.';
END
GO

-- 2. Thêm Composite Index tối ưu hiệu năng tính chi phí nhân công từ Timesheets
IF NOT EXISTS(SELECT * FROM sys.indexes WHERE Name = N'IX_Timesheets_TaskID_LaborCost' AND Object_ID = Object_ID(N'Timesheets'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Timesheets_TaskID_LaborCost 
    ON Timesheets(TaskID, IsDeleted) 
    INCLUDE (NormalHours, OTHours, LoggedHourlyRate, LoggedOTRate);

    PRINT 'Successfully created index IX_Timesheets_TaskID_LaborCost.';
END
ELSE
BEGIN
    PRINT 'Index IX_Timesheets_TaskID_LaborCost already exists.';
END
GO

-- 3. Cập nhật View vw_ProjectRiskFeatures đồng bộ với ML Feature Pipeline
-- - Complexity_Score: Ánh xạ trung bình trọng số task [2.0, 10.0]
-- - Budget_Utilization_Rate: Tính đầy đủ chi phí nhân công (Timesheets) + chi phí khác (Expenses)
-- - Expected_Budget: Quy đổi về USD thông qua ExchangeRateToUSD
CREATE OR ALTER VIEW vw_ProjectRiskFeatures AS
WITH ProjectTaskStats AS (
    SELECT ProjectID, COUNT(TaskID) AS Total_Tasks
    FROM Tasks
    WHERE IsDeleted = 0
    GROUP BY ProjectID
),
ProjectComplexityStats AS (
    SELECT 
        ProjectID,
        AVG(CASE UPPER(Complexity)
            WHEN 'LOW' THEN 2.0
            WHEN 'MEDIUM' THEN 5.0
            WHEN 'HIGH' THEN 8.0
            WHEN 'CRITICAL' THEN 10.0
            ELSE 5.0
        END) AS Avg_Complexity_Score
    FROM Tasks
    WHERE IsDeleted = 0
    GROUP BY ProjectID
),
ProjectTeamMembers AS (
    -- Lấy danh sách thành viên CỦA dự án kèm ResourceID gốc để ánh xạ kỹ năng chính xác
    SELECT DISTINCT t.ProjectID, ta.WorkspaceMemberID, wm.ResourceID
    FROM Tasks t
    JOIN TaskAssignees ta ON t.TaskID = ta.TaskID
    JOIN WorkspaceMembers wm ON ta.WorkspaceMemberID = wm.WorkspaceMemberID
    WHERE t.IsDeleted = 0
),
ProjectTeamStats AS (
    -- Sử dụng COUNT(DISTINCT) để tránh double counting khi nhân viên có nhiều kỹ năng
    SELECT 
        ptm.ProjectID,
        COUNT(DISTINCT ptm.WorkspaceMemberID) AS Team_Size,
        AVG(CAST(rs.Level AS FLOAT)) AS Avg_Team_Skill_Level
    FROM ProjectTeamMembers ptm
    LEFT JOIN ResourceSkills rs ON ptm.ResourceID = rs.ResourceID
    LEFT JOIN Skills s ON rs.SkillID = s.SkillID AND s.IsDeleted = 0
    GROUP BY ptm.ProjectID
),
ProjectFinancials AS (
    SELECT ProjectID, SUM(Amount) AS Total_Expense
    FROM Expenses
    WHERE IsDeleted = 0
    GROUP BY ProjectID
),
ProjectLaborCosts AS (
    SELECT 
        t.ProjectID,
        SUM(ts.NormalHours * ts.LoggedHourlyRate + ts.OTHours * ts.LoggedOTRate) AS Total_Labor_Cost
    FROM Timesheets ts
    JOIN Tasks t ON ts.TaskID = t.TaskID
    WHERE ts.IsDeleted = 0 AND t.IsDeleted = 0
    GROUP BY t.ProjectID
),
ProjectRiskStats AS (
    SELECT ProjectID, AVG(CAST(RiskScore AS FLOAT)) AS Overall_Risk_Score
    FROM Risks
    WHERE IsDeleted = 0
    GROUP BY ProjectID
)
SELECT 
    p.ProjectID,
    p.ProjectName AS Project_Name,
    DATEDIFF(day, p.StartDate, p.EndDate) AS Project_Duration_Days,
    p.ExpectedBudget * ISNULL(p.ExchangeRateToUSD, 1.0) AS Expected_Budget,
    p.Methodology AS Methodology_Used, 
    
    ISNULL(pts.Total_Tasks, 0) AS Total_Tasks,
    ISNULL(tm.Team_Size, 1) AS Team_Size,
    ISNULL(tm.Avg_Team_Skill_Level, 3.0) AS Avg_Team_Skill_Level,
    
    ISNULL(pcs.Avg_Complexity_Score, 5.0) AS Raw_Complexity_Score,
    
    CASE WHEN p.ExpectedBudget > 0 
         THEN (ISNULL(pf.Total_Expense, 0) + ISNULL(plc.Total_Labor_Cost, 0)) / p.ExpectedBudget 
         ELSE 0 
    END AS Budget_Utilization_Rate,
    
    ISNULL(prs.Overall_Risk_Score, 0) AS Overall_Risk_Score
FROM Projects p
LEFT JOIN ProjectTaskStats pts ON p.ProjectID = pts.ProjectID
LEFT JOIN ProjectComplexityStats pcs ON p.ProjectID = pcs.ProjectID
LEFT JOIN ProjectTeamStats tm ON p.ProjectID = tm.ProjectID
LEFT JOIN ProjectFinancials pf ON p.ProjectID = pf.ProjectID
LEFT JOIN ProjectLaborCosts plc ON p.ProjectID = plc.ProjectID
LEFT JOIN ProjectRiskStats prs ON p.ProjectID = prs.ProjectID
WHERE p.IsDeleted = 0;
GO

PRINT 'Successfully created or altered view vw_ProjectRiskFeatures.';
GO
