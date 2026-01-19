-- Expand WorkOrder Title field from nvarchar(200) to nvarchar(500)
ALTER TABLE [dbo].[WorkOrder]
ALTER COLUMN [Title] nvarchar(500) NOT NULL;
