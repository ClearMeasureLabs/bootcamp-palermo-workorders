-- Add Instructions column to WorkOrder table
-- Issue #45: Add WorkOrder Instructions field

IF NOT EXISTS (
	SELECT 1 
	FROM INFORMATION_SCHEMA.COLUMNS 
	WHERE TABLE_SCHEMA = 'dbo' 
		AND TABLE_NAME = 'WorkOrder' 
		AND COLUMN_NAME = 'Instructions'
)
BEGIN
	ALTER TABLE dbo.WorkOrder
		ADD Instructions NVARCHAR(4000) NULL
END
GO
