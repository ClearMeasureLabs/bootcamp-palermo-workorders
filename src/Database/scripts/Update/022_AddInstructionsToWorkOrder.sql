-- Add Instructions column to WorkOrder table
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrder]') 
               AND name = 'Instructions')
BEGIN
    ALTER TABLE [dbo].[WorkOrder]
    ADD [Instructions] NVARCHAR(4000) NULL;
    
    PRINT 'Instructions column added to WorkOrder table successfully.';
END
ELSE
BEGIN
    PRINT 'Instructions column already exists in WorkOrder table.';
END
GO
