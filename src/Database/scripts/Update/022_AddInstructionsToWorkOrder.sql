/*
   Adds optional Instructions column to WorkOrder (nvarchar(4000) NULL)
   Idempotent: only adds column if it does not exist
*/
IF COL_LENGTH('dbo.WorkOrder', 'Instructions') IS NULL
BEGIN
    ALTER TABLE dbo.WorkOrder
        ADD Instructions NVARCHAR(4000) NULL;
END
