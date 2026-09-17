-- #9525: Room selector on the work order create/edit form
-- Creates the Room table and adds a nullable RoomId FK to WorkOrder

CREATE TABLE dbo.Room
(
    Id     UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Room_Id DEFAULT (NEWID()),
    Number NVARCHAR(50)     NOT NULL,
    Name   NVARCHAR(200)    NOT NULL,
    CONSTRAINT PK_Room PRIMARY KEY (Id)
);

-- Seed a few rooms so the dropdown is usable out of the box
INSERT INTO dbo.Room (Id, Number, Name)
VALUES (NEWID(), '101', 'Conference Room A'),
       (NEWID(), '102', 'Conference Room B'),
       (NEWID(), '201', 'Sanctuary'),
       (NEWID(), '202', 'Fellowship Hall'),
       (NEWID(), '301', 'Basement Storage');

-- Add nullable RoomId FK to WorkOrder; existing rows default to NULL
ALTER TABLE dbo.WorkOrder
    ADD RoomId UNIQUEIDENTIFIER NULL
        CONSTRAINT FK_WorkOrder_Room FOREIGN KEY REFERENCES dbo.Room (Id)
            ON DELETE RESTRICT
            ON UPDATE NO ACTION;
