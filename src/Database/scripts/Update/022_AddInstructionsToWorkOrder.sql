-- Add Instructions column to WorkOrder table
-- Instructions is an optional field that allows work order creators to provide execution instructions
-- Maximum length: 4000 characters

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrder]') AND name = 'Instructions')
BEGIN
	ALTER TABLE [dbo].[WorkOrder]
	ADD [Instructions] NVARCHAR(4000) NULL;
	
	PRINT 'Instructions column added to WorkOrder table';
END
ELSE
BEGIN
	PRINT 'Instructions column already exists in WorkOrder table';
END
GO
