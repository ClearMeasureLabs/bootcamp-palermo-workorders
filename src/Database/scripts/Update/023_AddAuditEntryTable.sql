-- Create AuditEntry table for tracking work order changes
CREATE TABLE dbo.AuditEntry
(
	WorkOrderId uniqueidentifier NOT NULL,
	Sequence int NOT NULL,
	EmployeeId uniqueidentifier NULL,
	ArchivedEmployeeName nvarchar(100) NULL,
	Date datetime NOT NULL,
	BeginStatus char(3) NULL,
	EndStatus char(3) NULL,
	CONSTRAINT PK_AuditEntry PRIMARY KEY CLUSTERED (WorkOrderId, Sequence)
);
GO

ALTER TABLE dbo.AuditEntry 
	ADD CONSTRAINT FK_AuditEntry_WorkOrder 
	FOREIGN KEY (WorkOrderId) 
	REFERENCES dbo.WorkOrder(Id);
GO

ALTER TABLE dbo.AuditEntry 
	ADD CONSTRAINT FK_AuditEntry_Employee 
	FOREIGN KEY (EmployeeId) 
	REFERENCES dbo.Employee(Id);
GO
