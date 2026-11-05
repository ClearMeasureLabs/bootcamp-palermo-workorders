# Work order schema comparison

This audit compares the focused Java rewrite with the final first-party schema expressed by `src/Database/scripts/Update`. It documents implemented tables and known differences; it does not claim full schema parity.

## Final first-party tables

| Source table | Final source shape | Java Flyway/JPA shape | Remaining difference |
|---|---|---|---|
| `WorkOrder` | GUID primary key; `Number nvarchar(7)`; required `Title nvarchar(300)`; `Description nvarchar(4000)`; `Instructions nvarchar(4000)`; status converter stored in 3 characters; creator GUID FK required and assignee GUID FK nullable; nullable Created/Assigned/Completed datetime fields; nullable `RoomNumber nvarchar(900)`; nullable `DueDate date`. | UUID key; `number varchar(7)`; title varchar(300); nullable description/instructions; `status varchar(3)` with source codes; required `creator_id` and nullable `assignee_id` UUID FKs. Java also retains owner display names and username FKs as compatibility/query fields. Lifecycle timestamps with timezone, room varchar(900), due date, and Java optimistic-lock `version`. | Core key widths, status codes, and Employee GUID references now match. Java retains username/name compatibility columns, has an extra `version`, and uses timezone timestamps. V8 intentionally fails and rolls back if legacy numbers exceed seven characters or an owner cannot be mapped to an Employee; those externally visible or orphan values need operator resolution before migration. |
| `Employee` | GUID key; required username 100, first name 100, last name 120, email 255, and preferred language 10. `PreferredLanguage` defaults to `en-US`. | UUID key; same field widths and non-null constraints; preferred language is added by `V7` with the source default. | Field widths/defaults match. Java uses UUID employee keys but work-order creator/assignee ownership is keyed by username. |
| `Role` | GUID key; required name 100 and two required capability booleans. | UUID key; unique name 100 and two non-null booleans. | Field widths and capability facts match. |
| `EmployeeRoles` | Composite `(EmployeeId, RoleId)` primary key; both columns are non-null GUID FKs to Employee and Role. | `employee_roles` composite UUID key and foreign keys to Java employee and role tables. | Relationship shape matches; Java uses snake-case identifiers. |
| `WorkOrderAttachment` | GUID key; required WorkOrder FK with cascade delete; required FileName 500, ContentType 200, FileSize bigint, UploadedBy employee FK, and UploadedDate. | UUID key; matching required metadata columns, cascade to work orders, uploader FK, timestamp-with-time-zone, plus order/date index. | Metadata and FK delete behavior are represented. Java records metadata only and does not store file bytes; timestamp storage differs. |

The source `AuditEntry` table was introduced and then dropped by migration 020. Migration 022 creates the `nServiceBus` SQL schema only; it does not define application tables. Any runtime worker persistence tables are outside this focused port.

## Migrations added here

- `V6__match_source_work_order_title_length.sql` raises the Java title column from 240 to the final source width of 300. Java validation and the create form now use 300 as well.
- `V7__add_employee_preferred_language.sql` adds the source employee preference column with a 10-character width, non-null constraint, and `en-US` default.
- `V8__match_work_order_number_status_and_employee_keys.sql` narrows WorkOrder.Number to 7, maps enum names to source three-character status codes, and backfills required/optional employee UUID FKs from existing usernames. It fails transactionally rather than truncate >7-character numbers or discard an unmapped owner. Resolve those migration blockers before retrying against a populated prototype database.

Java Flyway is a new schema history, not a conversion runner for the original SQL Server upgrade scripts. Remaining timestamp, username/name compatibility, optimistic-lock, and attachment binary-storage differences are explicit gaps.
