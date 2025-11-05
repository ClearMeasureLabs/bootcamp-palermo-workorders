BEGIN TRANSACTION
GO
PRINT N'Adding [Instructions] column to [dbo].[WorkOrder] (NVARCHAR(4000) NULL)'
GO
IF COL_LENGTH('dbo.WorkOrder', 'Instructions') IS NULL
BEGIN
    ALTER TABLE [dbo].[WorkOrder]
        ADD [Instructions] NVARCHAR(4000) NULL;
END
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO


