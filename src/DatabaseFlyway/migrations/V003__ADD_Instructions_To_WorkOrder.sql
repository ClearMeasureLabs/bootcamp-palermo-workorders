-- Add Instructions column to WorkOrder table
-- GitHub Issue #45: Add WorkOrder Instructions Field

ALTER TABLE dbo.WorkOrder
ADD Instructions nvarchar(4000) NULL;
