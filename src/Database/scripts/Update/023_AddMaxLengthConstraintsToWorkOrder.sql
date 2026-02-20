-- Update Title column max length from 200 to 250 characters
ALTER TABLE [dbo].[WorkOrder]
	ALTER COLUMN [Title] NVARCHAR(250) NOT NULL;

-- Update Description column max length from 4000 to 500 characters
ALTER TABLE [dbo].[WorkOrder]
	ALTER COLUMN [Description] NVARCHAR(500) NULL;
