-- Shrink WorkOrder Description column from 4000 to 2000 characters
ALTER TABLE [dbo].[WorkOrder]
ALTER COLUMN [Description] NVARCHAR(2000) NULL;
