-- Add Instructions column to WorkOrder table
-- Instructions field is optional and supports up to 4000 characters
BEGIN TRANSACTION
GO
ALTER TABLE dbo.WorkOrder 
ADD Instructions nvarchar(4000) NULL;
GO

-- Add comment for documentation
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Optional instructions for work order execution, max 4000 characters', 
    @level0type = N'Schema', @level0name = 'dbo', 
    @level1type = N'Table', @level1name = 'WorkOrder', 
    @level2type = N'Column', @level2name = 'Instructions';
GO

COMMIT TRANSACTION
GO