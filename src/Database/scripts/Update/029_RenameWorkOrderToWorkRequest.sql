/*
	Rename the WorkOrder domain to WorkRequest.

	Renames the tables, the attachment foreign-key column, and every
	constraint/index whose name still contains "WorkOrder". Historical
	scripts (003-028) are intentionally left untouched; this migration
	brings an existing database in line with the renamed EF mappings
	(dbo.WorkRequest, dbo.WorkRequestAttachment, WorkRequestId).
*/
BEGIN TRANSACTION
GO

-- Tables ---------------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[WorkOrder]', N'U') IS NOT NULL
	EXEC sp_rename N'[dbo].[WorkOrder]', N'WorkRequest';
GO

IF OBJECT_ID(N'[dbo].[WorkOrderAttachment]', N'U') IS NOT NULL
	EXEC sp_rename N'[dbo].[WorkOrderAttachment]', N'WorkRequestAttachment';
GO

-- Columns --------------------------------------------------------------------
IF COL_LENGTH(N'[dbo].[WorkRequestAttachment]', N'WorkOrderId') IS NOT NULL
	EXEC sp_rename N'[dbo].[WorkRequestAttachment].[WorkOrderId]', N'WorkRequestId', N'COLUMN';
GO

IF COL_LENGTH(N'[dbo].[AuditEntry]', N'WorkOrderId') IS NOT NULL
	EXEC sp_rename N'[dbo].[AuditEntry].[WorkOrderId]', N'WorkRequestId', N'COLUMN';
GO

IF COL_LENGTH(N'[dbo].[Role]', N'CanCreateWorkOrder') IS NOT NULL
	EXEC sp_rename N'[dbo].[Role].[CanCreateWorkOrder]', N'CanCreateWorkRequest', N'COLUMN';
GO

IF COL_LENGTH(N'[dbo].[Role]', N'CanFulfillWorkOrder') IS NOT NULL
	EXEC sp_rename N'[dbo].[Role].[CanFulfillWorkOrder]', N'CanFulfillWorkRequest', N'COLUMN';
GO

-- Rename constraints/indexes whose name still contains "WorkOrder", scoped to
-- the domain tables only. NServiceBus infrastructure tables (WorkOrderProcessing*)
-- are deliberately excluded; they are managed by the messaging endpoint.
DECLARE @sql NVARCHAR(MAX) = N'';

DECLARE @domain TABLE (id INT PRIMARY KEY);
INSERT INTO @domain (id)
SELECT object_id
FROM sys.tables
WHERE schema_id = SCHEMA_ID(N'dbo')
	AND name IN (N'WorkRequest', N'WorkRequestAttachment', N'AuditEntry', N'Role');

-- Key/foreign/default/check constraints on the domain tables (schema-qualified
-- to avoid sp_rename ambiguity).
SELECT @sql = @sql + N'EXEC sp_rename N''[dbo].[' + o.name + N']'', N'''
		+ REPLACE(o.name, 'WorkOrder', 'WorkRequest') + N''', N''OBJECT'';' + CHAR(10)
FROM sys.objects o
WHERE o.name LIKE N'%WorkOrder%'
	AND o.type IN ('PK', 'F', 'D', 'C', 'UQ')
	AND o.parent_object_id IN (SELECT id FROM @domain);

-- Standalone indexes only (PK/UQ-backed indexes are renamed with their
-- constraint above, so exclude them here to avoid a double rename).
SELECT @sql = @sql + N'EXEC sp_rename N''[dbo].[' + t.name + N'].[' + i.name
		+ N']'', N''' + REPLACE(i.name, 'WorkOrder', 'WorkRequest') + N''', N''INDEX'';' + CHAR(10)
FROM sys.indexes i
	INNER JOIN sys.tables t ON t.object_id = i.object_id
WHERE i.name LIKE N'%WorkOrder%'
	AND i.is_primary_key = 0
	AND i.is_unique_constraint = 0
	AND i.object_id IN (SELECT id FROM @domain);

IF LEN(@sql) > 0
	EXEC sp_executesql @sql;
GO

IF @@ERROR <> 0 AND @@TRANCOUNT > 0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
