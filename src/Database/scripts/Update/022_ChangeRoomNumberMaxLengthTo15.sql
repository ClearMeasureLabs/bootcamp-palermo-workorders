/*	Friday, January 20, 2026
	User: 
	Server: 
	Database: ChurchBulletin
	Application: 
*/

/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
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
-- Change RoomNumber column max length from nvarchar(50) to nvarchar(15)
ALTER TABLE dbo.WorkOrder
	ALTER COLUMN RoomNumber nvarchar(15) NULL
GO
COMMIT
