-- Add Instructions field to WorkOrder table
-- Purpose: Allows work order creators to provide optional execution instructions
-- for the person assigned to fulfill the work order.
-- This field supports up to 4000 characters of plain text.

BEGIN TRANSACTION
GO
PRINT N'Altering [dbo].[WorkOrder] to add Instructions column'
GO
ALTER TABLE [dbo].[WorkOrder] ADD [Instructions] NVARCHAR(4000) NULL
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
