/*
   Monday, November 3, 2025
   User: 
   Server: 
   Database: ChurchBulletin
   Application: Issue #45 - Add WorkOrder Instructions
*/

/* Add Instructions field to WorkOrder table */
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.WorkOrder ADD
	Instructions nvarchar(4000) NULL
GO
ALTER TABLE dbo.WorkOrder SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
