BEGIN TRANSACTION
GO
PRINT N'Creating [dbo].[Room] table'
GO
CREATE TABLE [dbo].[Room]
(
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	CONSTRAINT [PK_Room] PRIMARY KEY CLUSTERED ([Id])
)
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO

PRINT N'Creating [dbo].[WorkOrderRooms] junction table'
GO
CREATE TABLE [dbo].[WorkOrderRooms]
(
	[WorkOrderId] [uniqueidentifier] NOT NULL,
	[RoomId] [uniqueidentifier] NOT NULL,
	CONSTRAINT [PK_WorkOrderRooms] PRIMARY KEY CLUSTERED ([WorkOrderId], [RoomId]),
	CONSTRAINT [FK_WorkOrderRooms_WorkOrder] FOREIGN KEY ([WorkOrderId]) REFERENCES [dbo].[WorkOrder]([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_WorkOrderRooms_Room] FOREIGN KEY ([RoomId]) REFERENCES [dbo].[Room]([Id]) ON DELETE CASCADE
)
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO

PRINT N'Seeding static rooms'
GO
INSERT INTO [dbo].[Room] ([Id], [Name]) VALUES (NEWID(), 'Choir')
INSERT INTO [dbo].[Room] ([Id], [Name]) VALUES (NEWID(), 'Kitchen')
INSERT INTO [dbo].[Room] ([Id], [Name]) VALUES (NEWID(), 'Chapel')
INSERT INTO [dbo].[Room] ([Id], [Name]) VALUES (NEWID(), 'Nursery')
INSERT INTO [dbo].[Room] ([Id], [Name]) VALUES (NEWID(), 'Foyer')
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO

PRINT N'Dropping RoomNumber column from WorkOrder table'
GO
ALTER TABLE [dbo].[WorkOrder] DROP COLUMN [RoomNumber]
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO

IF @@TRANCOUNT>0 BEGIN
PRINT 'The database update succeeded'
COMMIT TRANSACTION
END
ELSE PRINT 'The database update failed'
GO
