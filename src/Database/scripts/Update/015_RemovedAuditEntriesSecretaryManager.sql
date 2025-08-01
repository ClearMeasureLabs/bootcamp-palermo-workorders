/*
Deployment script for HeadspringTraining
*/

GO
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;

SET NUMERIC_ROUNDABORT OFF;


GO
PRINT N'Dropping FK_AuditEntry_Employee...';


GO
ALTER TABLE [dbo].[AuditEntry] DROP CONSTRAINT [FK_AuditEntry_Employee];


GO
PRINT N'Dropping FK_AuditEntry_WorkOrder...';


GO
ALTER TABLE [dbo].[AuditEntry] DROP CONSTRAINT [FK_AuditEntry_WorkOrder];


GO
PRINT N'Dropping FK_Employee_Employee_ForManager...';


GO
ALTER TABLE [dbo].[Employee] DROP CONSTRAINT [FK_Employee_Employee_ForManager];


GO
PRINT N'Dropping FK_Employee_Employee_ForSecretary...';


GO
ALTER TABLE [dbo].[Employee] DROP CONSTRAINT [FK_Employee_Employee_ForSecretary];


GO
PRINT N'Dropping [dbo].[AuditEntry]...';


GO
DROP TABLE [dbo].[AuditEntry];


GO
PRINT N'Altering [dbo].[Employee]...';


GO
ALTER TABLE [dbo].[Employee] DROP COLUMN [EmployeeType], COLUMN [ManagerId], COLUMN [Secretary], COLUMN [WeekendEmail];


GO
PRINT N'Altering [dbo].[WorkOrder]...';


GO
ALTER TABLE [dbo].[WorkOrder] DROP COLUMN [CompletedDate], COLUMN [CreatedDate];


GO
