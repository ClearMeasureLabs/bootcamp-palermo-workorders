-- Migration: Add Instructions column to WorkOrder table
-- Issue: #48 - Add WorkOrder Instructions field

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.WorkOrder') 
    AND name = 'Instructions'
)
BEGIN
    ALTER TABLE dbo.WorkOrder
    ADD Instructions NVARCHAR(4000) NULL;
    
    PRINT 'Added Instructions column to dbo.WorkOrder table';
END
ELSE
BEGIN
    PRINT 'Instructions column already exists in dbo.WorkOrder table';
END
GO
