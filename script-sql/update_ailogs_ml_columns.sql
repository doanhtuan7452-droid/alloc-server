-- Migration Script: Add ModelInputJson and ModelOutputJson columns to AILogs table
-- Purpose: Support structured logging of ML input features and predictions for model retraining

IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'ModelInputJson' AND Object_ID = Object_ID(N'AILogs'))
BEGIN
    ALTER TABLE AILogs 
    ADD ModelInputJson NVARCHAR(MAX) NULL, 
        ModelOutputJson NVARCHAR(MAX) NULL;
        
    PRINT 'Successfully added ModelInputJson and ModelOutputJson columns to AILogs table.';
END
ELSE
BEGIN
    PRINT 'ModelInputJson and ModelOutputJson columns already exist in AILogs table.';
END
