-- Add Instructions column to WorkOrder table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrder]') AND name = 'Instructions')
BEGIN
	ALTER TABLE [dbo].[WorkOrder]
	ADD [Instructions] nvarchar(4000) NULL
END
GO
