/*
   Database Migration Script - Issue #45: Add WorkOrder Instructions
   Created: November 5, 2025
   Author: Claude GitHub Agent Sonnet 4.5
   
   Purpose: Add optional Instructions field to support execution instructions for work orders.
   The Instructions field is a nullable nvarchar(4000) column that allows work order creators
   to provide detailed execution instructions. This field appears between Description and 
   Room Number in the UI.
*/

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