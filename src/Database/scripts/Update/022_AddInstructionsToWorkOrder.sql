-- Add Instructions column to WorkOrder table
-- This field is optional and allows work order creators to provide execution instructions
-- UI Positioning: Displayed between Description and RoomNumber fields for optimal workflow
-- Field appears after Description to provide detailed execution guidance
-- Field appears before RoomNumber to maintain logical form flow
-- Supports up to 4000 characters (plain text only)

IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrder]') 
               AND name = 'Instructions')
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
