# Work order schema comparison

This audit compares the focused Java rewrite with the final first-party schema expressed by `src/Database/scripts/Update`. It documents implemented tables and known differences; it does not claim full schema parity.

## Final first-party tables

| Source table | Final source shape | Java Flyway/JPA shape | Remaining difference |
|---|---|---|---|
| `WorkOrder` | GUID primary key; `Number nvarchar(7)`; required `Title nvarchar(300)`; `Description nvarchar(4000)`; `Instructions nvarchar(4000)`; status converter stored in 3 characters; creator GUID FK required and assignee GUID FK nullable; nullable Created/Assigned/Completed datetime fields; nullable `RoomNumber nvarchar(900)`; nullable `DueDate date`. | UUID key; `number varchar(24)` currently sized for legacy prototype records; new Java numbers now use the source 7-character GUID prefix format. Title varchar(300); nullable description/instructions; `status varchar(24)` enum; owner display names and nullable username FKs; lifecycle timestamps with timezone; room varchar(900), due date, and Java optimistic-lock `version`. | New number generation matches the source format, though the Java column remains wider to preserve existing prototype records. Title width, instructions, room, date-only due date and nullable assignment are represented. Status encoding, timestamp semantics, and owner FK representation differ. `version` is Java-only. |
| `Employee` | GUID key; required username 100, first name 100, last name 120, email 255, and preferred language 10. `PreferredLanguage` defaults to `en-US`. | UUID key; same field widths and non-null constraints; preferred language is added by `V7` with the source default. | Field widths/defaults match. Java uses UUID employee keys but work-order creator/assignee ownership is keyed by username. |
| `Role` | GUID key; required name 100 and two required capability booleans. | UUID key; unique name 100 and two non-null booleans. | Field widths and capability facts match. |
| `EmployeeRoles` | Composite `(EmployeeId, RoleId)` primary key; both columns are non-null GUID FKs to Employee and Role. | `employee_roles` composite UUID key and foreign keys to Java employee and role tables. | Relationship shape matches; Java uses snake-case identifiers. |
| `WorkOrderAttachment` | GUID key; required WorkOrder FK with cascade delete; required FileName 500, ContentType 200, FileSize bigint, UploadedBy employee FK, and UploadedDate. | UUID key; matching required metadata columns, cascade to work orders, uploader FK, timestamp-with-time-zone, plus order/date index. | Metadata and FK delete behavior are represented. Java records metadata only and does not store file bytes; timestamp storage differs. |

The source `AuditEntry` table was introduced and then dropped by migration 020. Migration 022 creates the `nServiceBus` SQL schema only; it does not define application tables. Any runtime worker persistence tables are outside this focused port.

## Migrations added here

- `V6__match_source_work_order_title_length.sql` raises the Java title column from 240 to the final source width of 300. Java validation and the create form now use 300 as well.
- `V7__add_employee_preferred_language.sql` adds the source employee preference column with a 10-character width, non-null constraint, and `en-US` default.

Java Flyway is a new schema history, not a conversion runner for the original SQL Server upgrade scripts. Other differences above remain explicit gaps.
