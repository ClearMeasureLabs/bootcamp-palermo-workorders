-- Add Instructions column to WorkOrder table
-- Instructions is optional (NULL) to allow work order creators to provide execution instructions when needed
-- 4000 character limit matches Description field and accommodates detailed step-by-step instructions

ALTER TABLE [dbo].[WorkOrder]
	ADD [Instructions] NVARCHAR(4000) NULL;
GO
