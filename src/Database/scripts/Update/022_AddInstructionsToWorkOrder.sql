IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrder]') AND name = 'Instructions')
BEGIN
	PRINT N'Adding Instructions column to [dbo].[WorkOrder]'
	ALTER TABLE [dbo].[WorkOrder]
	ADD Instructions NVARCHAR(4000) NULL
END
ELSE
BEGIN
	PRINT N'Instructions column already exists in [dbo].[WorkOrder]'
END


