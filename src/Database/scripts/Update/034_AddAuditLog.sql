CREATE TABLE [dbo].[AuditLog]
(
	[Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AuditLog] PRIMARY KEY,
	[WorkOrderId] uniqueidentifier NULL,
	[Action] nvarchar(255) NOT NULL,
	[EntityType] nvarchar(255) NOT NULL,
	[EntityId] uniqueidentifier NULL,
	[ActorUserName] nvarchar(255) NOT NULL,
	[ActorDisplayName] nvarchar(500) NULL,
	[PreviousValue] nvarchar(max) NULL,
	[NewValue] nvarchar(max) NULL,
	[Timestamp] datetime2 NOT NULL CONSTRAINT [DF_AuditLog_Timestamp] DEFAULT SYSUTCDATETIME(),
	[IpAddress] nvarchar(45) NULL,
	[UserAgent] nvarchar(500) NULL,
	[CorrelationId] nvarchar(64) NULL
);
CREATE INDEX [IX_AuditLog_Action] ON [dbo].[AuditLog] ([Action]);
CREATE INDEX [IX_AuditLog_EntityType_EntityId_Id] ON [dbo].[AuditLog] ([EntityType], [EntityId], [Id]);
CREATE INDEX [IX_AuditLog_WorkOrderId_Id] ON [dbo].[AuditLog] ([WorkOrderId], [Id]);
CREATE INDEX [IX_AuditLog_Timestamp_Id] ON [dbo].[AuditLog] ([Timestamp] DESC, [Id] DESC);
GO
CREATE TRIGGER [dbo].[AuditLog_BlockUpdate] ON [dbo].[AuditLog] INSTEAD OF UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	THROW 51000, 'Audit records are append-only.', 1;
END;
-- DELETE intentionally remains available to explicit retention and privileged database/test resets.
-- Application tracked updates/deletes are rejected; IAuditLog exposes only cutoff-based retention.
