IF NOT EXISTS (
	SELECT 1
	FROM sys.columns
	WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrder]')
		AND name = 'Instructions'
)
BEGIN
	ALTER TABLE [dbo].[WorkOrder]
	ADD [Instructions] [nvarchar](4000) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF_WorkOrder_Instructions] DEFAULT ('');
END
