IF NOT EXISTS (
	SELECT 1
	FROM sys.columns
	WHERE object_id = OBJECT_ID(N'dbo.WorkOrder', N'U')
		AND name = N'Instructions'
)
BEGIN
	ALTER TABLE dbo.WorkOrder
	ADD Instructions nvarchar(4000) NULL;
END
GO
