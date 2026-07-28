-- ============================================================================
-- SQL MIGRATION SCRIPT: UPDATE EXCHANGERATEToUSD PRECISION TO DECIMAL(18,12)
-- ============================================================================
BEGIN TRANSACTION;
BEGIN TRY
    -- 1. Tìm và Drop Default Constraint của cột ExchangeRateToUSD trong bảng Projects
    DECLARE @DefaultConstraintName NVARCHAR(200);
    SELECT @DefaultConstraintName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('Projects') AND c.name = 'ExchangeRateToUSD';

    IF @DefaultConstraintName IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE Projects DROP CONSTRAINT ' + @DefaultConstraintName);
    END;

    -- 2. Tìm và Drop Check Constraint của cột ExchangeRateToUSD trong bảng Projects
    DECLARE @CheckConstraintName NVARCHAR(200);
    SELECT @CheckConstraintName = cc.name
    FROM sys.check_constraints cc
    JOIN sys.columns c ON cc.parent_object_id = c.object_id AND cc.parent_column_id = c.column_id
    WHERE cc.parent_object_id = OBJECT_ID('Projects') AND c.name = 'ExchangeRateToUSD';

    IF @CheckConstraintName IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE Projects DROP CONSTRAINT ' + @CheckConstraintName);
    END;

    -- 3. Thay đổi kiểu dữ liệu cột thành DECIMAL(18,12)
    ALTER TABLE Projects ALTER COLUMN ExchangeRateToUSD DECIMAL(18,12) NOT NULL;

    -- 4. Tạo lại Default Constraint mới
    ALTER TABLE Projects ADD CONSTRAINT DF_Projects_ExchangeRateToUSD DEFAULT 1.0 FOR ExchangeRateToUSD;

    -- 5. Tạo lại Check Constraint mới
    ALTER TABLE Projects ADD CONSTRAINT CHK_Projects_ExchangeRateToUSD CHECK (ExchangeRateToUSD > 0);

    COMMIT TRANSACTION;
    PRINT 'Migration completed successfully.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error occurred. Transaction rolled back.';
    THROW;
END CATCH;
GO
