-- Increase RoomNumber column max length from 50 to 500 characters
ALTER TABLE dbo.WorkOrder
    ALTER COLUMN RoomNumber nvarchar(500) NULL;
