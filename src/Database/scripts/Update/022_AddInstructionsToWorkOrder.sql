-- Adding Instructions field to WorkOrder table for Issue #46
ALTER TABLE [dbo].[WorkOrder]
ADD [Instructions] NVARCHAR(4000) NULL;
GO
