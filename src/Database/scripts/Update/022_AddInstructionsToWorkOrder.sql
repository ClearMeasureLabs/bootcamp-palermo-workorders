-- Add Instructions column to WorkOrder table
-- This column allows work order creators to provide optional execution instructions

ALTER TABLE [dbo].[WorkOrder]
	ADD [Instructions] NVARCHAR(4000) NULL;
GO
