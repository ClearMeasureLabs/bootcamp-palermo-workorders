-- Refuse to narrow persisted prototype numbers that cannot fit in the source's
-- seven-character key. Flyway rolls back this migration; operators must resolve
-- those externally visible identifiers before retrying rather than losing them.
alter table work_orders alter column number type varchar(7);

-- Keep lifecycle state while storing the source's three-character status codes.
update work_orders set status = case upper(status)
    when 'DRAFT' then 'DRT'
    when 'ASSIGNED' then 'ASD'
    when 'IN_PROGRESS' then 'IPG'
    when 'COMPLETE' then 'CMP'
    when 'CANCELLED' then 'CNL'
    else upper(status)
end;
alter table work_orders alter column status type varchar(3);

-- Retain the Java username columns for filtering and API compatibility, and add
-- source-shaped employee GUID foreign keys. Missing owners cause NOT NULL/check
-- validation to fail, rolling back the migration without discarding old values.
alter table work_orders add column creator_id uuid;
alter table work_orders add column assignee_id uuid;
update work_orders w set creator_id = (select e.id from employees e where e.username = w.creator_username)
where exists (select 1 from employees e where e.username = w.creator_username);
update work_orders w set assignee_id = (select e.id from employees e where e.username = w.assignee_username)
where exists (select 1 from employees e where e.username = w.assignee_username);
alter table work_orders alter column creator_id set not null;
alter table work_orders add constraint fk_work_order_creator_employee_id
    foreign key (creator_id) references employees(id);
alter table work_orders add constraint fk_work_order_assignee_employee_id
    foreign key (assignee_id) references employees(id);
alter table work_orders add constraint ck_work_order_assignee_backfill
    check (assignee_username is null or assignee_id is not null);
