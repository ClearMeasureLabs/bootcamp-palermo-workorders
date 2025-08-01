PRINT N'Dropping FK_AuditEntry_WorkOrder...';


GO
ALTER TABLE [dbo].[AuditEntry] DROP CONSTRAINT [FK_AuditEntry_WorkOrder];


GO
PRINT N'Dropping FK20E4895F44667591...';


GO
ALTER TABLE [dbo].[Employee] DROP CONSTRAINT [FK20E4895F44667591];


GO
PRINT N'Dropping FKF4B0E86CB8ED815...';


GO
ALTER TABLE [dbo].[Secretary] DROP CONSTRAINT [FKF4B0E86CB8ED815];


GO
PRINT N'Dropping FK72A50C88B8ED815...';


GO
ALTER TABLE [dbo].[Manager] DROP CONSTRAINT [FK72A50C88B8ED815];


GO
PRINT N'Dropping FK_Manager_Secretary...';


GO
ALTER TABLE [dbo].[Manager] DROP CONSTRAINT [FK_Manager_Secretary];


GO
PRINT N'Dropping FK_EmployeeRole_Role...';


GO
ALTER TABLE [dbo].[EmployeeRoles] DROP CONSTRAINT [FK_EmployeeRole_Role];


GO
PRINT N'Dropping PK_EmployeeRole...';


GO
ALTER TABLE [dbo].[EmployeeRoles] DROP CONSTRAINT [PK_EmployeeRole];


GO
PRINT N'Dropping PK__Role__1B0907CE...';


GO
ALTER TABLE [dbo].[Role] DROP CONSTRAINT [PK__Role__1B0907CE];


GO
PRINT N'Dropping PK__WorkOrder_Id...';


GO
ALTER TABLE [dbo].[WorkOrder] DROP CONSTRAINT [PK__WorkOrder_Id];


GO
PRINT N'Dropping [dbo].[Manager]...';


GO
TRUNCATE TABLE [dbo].[Manager];
GO
DROP TABLE [dbo].[Manager];


GO
PRINT N'Dropping [dbo].[Secretary]...';


GO
TRUNCATE TABLE [dbo].[Secretary];
GO
DROP TABLE [dbo].[Secretary];


GO
PRINT N'Starting rebuilding table [dbo].[AuditEntry]...';


GO
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

BEGIN TRANSACTION;

CREATE TABLE [dbo].[tmp_ms_xx_AuditEntry] (
    [WorkOrderId]          UNIQUEIDENTIFIER NOT NULL,
    [EmployeeId]           UNIQUEIDENTIFIER NULL,
    [ArchivedEmployeeName] NVARCHAR (255)   NULL,
    [Date]                 DATETIME         NULL,
    [BeginStatus]          NCHAR (3)        NULL,
    [EndStatus]            NCHAR (3)        NULL,
    [Sequence]             INT              NOT NULL,
    PRIMARY KEY CLUSTERED ([WorkOrderId] ASC, [Sequence] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);

IF EXISTS (SELECT TOP 1 1
           FROM   [dbo].[AuditEntry])
    BEGIN
        INSERT INTO [dbo].[tmp_ms_xx_AuditEntry] ([WorkOrderId], [Sequence], [EmployeeId], [ArchivedEmployeeName], [Date], [BeginStatus], [EndStatus])
        SELECT   [WorkOrderId],
                 [Sequence],
                 [EmployeeId],
                 [ArchivedEmployeeName],
                 [Date],
                 [BeginStatus],
                 [EndStatus]
        FROM     [dbo].[AuditEntry]
        ORDER BY [WorkOrderId] ASC, [Sequence] ASC;
    END

DROP TABLE [dbo].[AuditEntry];

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_AuditEntry]', N'AuditEntry';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
PRINT N'Starting rebuilding table [dbo].[Employee]...';


GO
/*
The column [dbo].[Employee].[EmployeeType] on table [dbo].[Employee] must be added, but the column has no default value and does not allow NULL values. If the table contains data, the ALTER script will not work. To avoid this issue, you must add a default value to the column or mark it as allowing NULL values.
*/
GO
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

BEGIN TRANSACTION;

CREATE TABLE [dbo].[tmp_ms_xx_Employee] (
    [Id]           UNIQUEIDENTIFIER NOT NULL,
    [EmployeeType] NVARCHAR (255)   NOT NULL,
    [UserName]     NVARCHAR (50)    NOT NULL,
    [FirstName]    NVARCHAR (25)    NOT NULL,
    [LastName]     NVARCHAR (25)    NOT NULL,
    [EmailAddress] NVARCHAR (100)   NOT NULL,
    [WeekendEmail] NVARCHAR (255)   NULL,
    [Secretary]    UNIQUEIDENTIFIER NULL,
    [ManagerId]    UNIQUEIDENTIFIER NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);

IF EXISTS (SELECT TOP 1 1
           FROM   [dbo].[Employee])
    BEGIN
        INSERT INTO [dbo].[tmp_ms_xx_Employee] ([Id], [UserName], [FirstName], [LastName], [EmailAddress], [ManagerId], [EmployeeType])
        SELECT   [Id],
                 [UserName],
                 [FirstName],
                 [LastName],
                 [EmailAddress],
                 [ManagerId],
				 'EMP'
        FROM     [dbo].[Employee]
        ORDER BY [Id] ASC;
    END

DROP TABLE [dbo].[Employee];

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_Employee]', N'Employee';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
PRINT N'Creating PK__Employee__C27FE3F00519C6AF...';


GO
ALTER TABLE [dbo].[EmployeeRoles]
    ADD PRIMARY KEY CLUSTERED ([EmployeeId] ASC, [RoleId] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating PK__Role__3214EC0708EA5793...';


GO
ALTER TABLE [dbo].[Role]
    ADD PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating PK__WorkOrde__3214EC070CBAE877...';


GO
ALTER TABLE [dbo].[WorkOrder]
    ADD PRIMARY KEY CLUSTERED ([Id] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF);


GO
PRINT N'Creating FK_AuditEntry_Employee';


GO
ALTER TABLE [dbo].[AuditEntry] WITH NOCHECK
    ADD CONSTRAINT [FK_AuditEntry_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employee] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_AuditEntry_WorkOrder';


GO
ALTER TABLE [dbo].[AuditEntry] WITH NOCHECK
    ADD CONSTRAINT [FK_AuditEntry_WorkOrder] FOREIGN KEY ([WorkOrderId]) REFERENCES [dbo].[WorkOrder] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_Employee_Employee_ForSecretary';


GO
ALTER TABLE [dbo].[Employee] WITH NOCHECK
    ADD CONSTRAINT [FK_Employee_Employee_ForSecretary] FOREIGN KEY ([Secretary]) REFERENCES [dbo].[Employee] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_Employee_Employee_ForManager...';


GO
ALTER TABLE [dbo].[Employee] WITH NOCHECK
    ADD CONSTRAINT [FK_Employee_Employee_ForManager] FOREIGN KEY ([ManagerId]) REFERENCES [dbo].[Employee] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_EmployeeRoles_Employee...';


GO
ALTER TABLE [dbo].[EmployeeRoles] WITH NOCHECK
    ADD CONSTRAINT [FK_EmployeeRoles_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employee] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FKEmployeeRoles_Role...';


GO
ALTER TABLE [dbo].[EmployeeRoles] WITH NOCHECK
    ADD CONSTRAINT [FKEmployeeRoles_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Role] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FK_WorkOrder_EmployeeForCreator...';


GO
ALTER TABLE [dbo].[WorkOrder] WITH NOCHECK
    ADD CONSTRAINT [FK_WorkOrder_EmployeeForCreator] FOREIGN KEY ([CreatorId]) REFERENCES [dbo].[Employee] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Creating FKWorkOrder_EmployeeForAssignee...';


GO
ALTER TABLE [dbo].[WorkOrder] WITH NOCHECK
    ADD CONSTRAINT [FKWorkOrder_EmployeeForAssignee] FOREIGN KEY ([AssigneeId]) REFERENCES [dbo].[Employee] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;


GO
PRINT N'Checking existing data against newly created constraints';


GO
ALTER TABLE [dbo].[AuditEntry] WITH CHECK CHECK CONSTRAINT [FK_AuditEntry_Employee];

ALTER TABLE [dbo].[AuditEntry] WITH CHECK CHECK CONSTRAINT [FK_AuditEntry_WorkOrder];

ALTER TABLE [dbo].[Employee] WITH CHECK CHECK CONSTRAINT [FK_Employee_Employee_ForSecretary];

ALTER TABLE [dbo].[Employee] WITH CHECK CHECK CONSTRAINT [FK_Employee_Employee_ForManager];

ALTER TABLE [dbo].[EmployeeRoles] WITH CHECK CHECK CONSTRAINT [FK_EmployeeRoles_Employee];

ALTER TABLE [dbo].[EmployeeRoles] WITH CHECK CHECK CONSTRAINT [FKEmployeeRoles_Role];

ALTER TABLE [dbo].[WorkOrder] WITH CHECK CHECK CONSTRAINT [FK_WorkOrder_EmployeeForCreator];

ALTER TABLE [dbo].[WorkOrder] WITH CHECK CHECK CONSTRAINT [FKWorkOrder_EmployeeForAssignee];


GO
