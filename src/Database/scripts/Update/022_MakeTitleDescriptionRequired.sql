BEGIN TRANSACTION
GO
PRINT N'Making Title and Description columns required in [dbo].[WorkOrder]'
GO

-- Update any null values to empty string before making NOT NULL
UPDATE [dbo].[WorkOrder] SET [Title] = '' WHERE [Title] IS NULL
GO
UPDATE [dbo].[WorkOrder] SET [Description] = '' WHERE [Description] IS NULL
GO

-- Alter Title column to NOT NULL (it's already NOT NULL, but this ensures consistency)
ALTER TABLE [dbo].[WorkOrder] ALTER COLUMN [Title] NVARCHAR(200) NOT NULL
GO

-- Alter Description column to NOT NULL
ALTER TABLE [dbo].[WorkOrder] ALTER COLUMN [Description] NVARCHAR(4000) NOT NULL
GO

IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
