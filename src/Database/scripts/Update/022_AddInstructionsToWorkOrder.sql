-- Add Instructions column to WorkOrder table
-- This column is optional and supports up to 4000 characters

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrder]') AND name = 'Instructions')
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
