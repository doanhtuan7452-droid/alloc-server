USE ALLOC;
GO

-- Messages API migration support:
-- ProjectAssets becomes a workspace-scoped asset table while keeping the
-- existing table name for backward compatibility with current C# services.

IF COL_LENGTH('ProjectAssets', 'WorkspaceID') IS NULL
BEGIN
    ALTER TABLE ProjectAssets ADD WorkspaceID INT NULL;
END
GO

UPDATE asset
SET WorkspaceID = project.WorkspaceID
FROM ProjectAssets asset
JOIN Projects project ON project.ProjectID = asset.ProjectID
WHERE asset.WorkspaceID IS NULL;
GO

IF EXISTS (
    SELECT 1
    FROM ProjectAssets
    WHERE WorkspaceID IS NULL
)
BEGIN
    THROW 50001, 'Cannot migrate ProjectAssets: WorkspaceID still contains NULL values.', 1;
END
GO

ALTER TABLE ProjectAssets ALTER COLUMN WorkspaceID INT NOT NULL;
GO

DECLARE @ProjectAssetProjectFkName SYSNAME;

SELECT TOP 1 @ProjectAssetProjectFkName = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.tables parentTable ON parentTable.object_id = fk.parent_object_id
JOIN sys.columns parentColumn
    ON parentColumn.object_id = parentTable.object_id
    AND parentColumn.column_id = fkc.parent_column_id
JOIN sys.tables referencedTable ON referencedTable.object_id = fk.referenced_object_id
WHERE parentTable.name = 'ProjectAssets'
  AND parentColumn.name = 'ProjectID'
  AND referencedTable.name = 'Projects';

IF @ProjectAssetProjectFkName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE ProjectAssets DROP CONSTRAINT ' + QUOTENAME(@ProjectAssetProjectFkName));
END
GO

ALTER TABLE ProjectAssets ALTER COLUMN ProjectID INT NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_ProjectAssets_Projects_ProjectID'
)
BEGIN
    ALTER TABLE ProjectAssets
    ADD CONSTRAINT FK_ProjectAssets_Projects_ProjectID
    FOREIGN KEY (ProjectID) REFERENCES Projects(ProjectID);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_ProjectAssets_Workspaces_WorkspaceID'
)
BEGIN
    ALTER TABLE ProjectAssets
    ADD CONSTRAINT FK_ProjectAssets_Workspaces_WorkspaceID
    FOREIGN KEY (WorkspaceID) REFERENCES Workspaces(WorkspaceID);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ProjectAssets_WorkspaceID_ProjectID'
      AND object_id = OBJECT_ID('ProjectAssets')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProjectAssets_WorkspaceID_ProjectID
    ON ProjectAssets(WorkspaceID, ProjectID)
    WHERE IsDeleted = 0;
END
GO
