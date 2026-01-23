-- Add Instructions column to WorkOrder table
ALTER TABLE dbo.WorkOrder
	ADD Instructions nvarchar(4000) NULL;
GO
