-- Add Instructions column to WorkOrder table
-- This field is optional and supports up to 4000 characters of plain text

IF NOT EXISTS (
	SELECT 1
	FROM INFORMATION_SCHEMA.COLUMNS
	WHERE TABLE_SCHEMA = 'dbo'
		AND TABLE_NAME = 'WorkOrder'
		AND COLUMN_NAME = 'Instructions'
)
BEGIN
	ALTER TABLE dbo.WorkOrder
		ADD Instructions NVARCHAR(4000) NULL;

	PRINT 'Instructions column added to WorkOrder table';
END
ELSE
BEGIN
	PRINT 'Instructions column already exists in WorkOrder table';
END
GO
