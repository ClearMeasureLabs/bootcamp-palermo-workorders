BEGIN TRANSACTION
GO
PRINT N'Adding [PreferredLanguage] to [dbo].[Employee]'
GO
ALTER TABLE [dbo].[Employee] ADD [PreferredLanguage] NVARCHAR(10) NOT NULL CONSTRAINT [DF_Employee_PreferredLanguage] DEFAULT 'en-US'
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
