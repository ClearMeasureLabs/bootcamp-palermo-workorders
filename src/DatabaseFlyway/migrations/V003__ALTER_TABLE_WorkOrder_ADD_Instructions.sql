-- Add optional Instructions column to WorkOrder
ALTER TABLE [dbo].[WorkOrder]
ADD [Instructions] NVARCHAR(4000) NULL;