BEGIN TRANSACTION
GO
PRINT N'Creating [nServiceBus] schema'
GO
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'nServiceBus')
    EXEC('CREATE SCHEMA [nServiceBus]')
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
