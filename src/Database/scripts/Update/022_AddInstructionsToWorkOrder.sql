-- Add Instructions column to WorkOrder table

ALTER TABLE [dbo].[WorkOrder]
	ADD [Instructions] NVARCHAR(4000) NULL;
GO
