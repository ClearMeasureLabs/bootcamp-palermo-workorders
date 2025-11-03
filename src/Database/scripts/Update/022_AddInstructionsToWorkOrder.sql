IF COL_LENGTH('dbo.WorkOrder', 'Instructions') IS NULL
BEGIN
    ALTER TABLE [dbo].[WorkOrder]
    ADD [Instructions] NVARCHAR(4000) NULL;
END

