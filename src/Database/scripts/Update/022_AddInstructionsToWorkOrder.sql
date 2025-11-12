-- Add Instructions column to WorkOrder table
-- Issue #50: Optional execution instructions field for work orders
-- Allows work order creators to provide detailed instructions (up to 4000 characters)
-- Field is nullable to support optional input
ALTER TABLE [dbo].[WorkOrder]
	ADD [Instructions] NVARCHAR(4000) NULL;
GO
