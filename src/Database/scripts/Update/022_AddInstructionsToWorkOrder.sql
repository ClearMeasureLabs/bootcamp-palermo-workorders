-- Add Instructions column to WorkOrder table
-- This allows work order creators to optionally provide execution instructions
-- Instructions field supports up to 4000 characters, plain text only

ALTER TABLE [dbo].[WorkOrder]
	ADD [Instructions] NVARCHAR(4000) NULL;
GO
