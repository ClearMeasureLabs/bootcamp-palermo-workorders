alter table work_orders add column if not exists creator_username varchar(100);
alter table work_orders add column if not exists assignee_username varchar(100);

create table if not exists roles (
    id uuid primary key,
    name varchar(100) not null unique,
    can_create_work_order boolean not null,
    can_fulfill_work_order boolean not null
);

create table if not exists employees (
    id uuid primary key,
    username varchar(100) not null unique,
    first_name varchar(100) not null,
    last_name varchar(120) not null,
    email_address varchar(255) not null
);

alter table work_orders add constraint fk_work_order_creator_employee
    foreign key (creator_username) references employees(username);
alter table work_orders add constraint fk_work_order_assignee_employee
    foreign key (assignee_username) references employees(username);

create table if not exists employee_roles (
    employee_id uuid not null references employees(id),
    role_id uuid not null references roles(id),
    primary key (employee_id, role_id)
);

insert into roles (id, name, can_create_work_order, can_fulfill_work_order)
select '10000000-0000-0000-0000-000000000001', 'Facility Lead', true, false
where not exists (select 1 from roles where name = 'Facility Lead');
insert into roles (id, name, can_create_work_order, can_fulfill_work_order)
select '10000000-0000-0000-0000-000000000002', 'Fulfillment', false, true
where not exists (select 1 from roles where name = 'Fulfillment');
insert into roles (id, name, can_create_work_order, can_fulfill_work_order)
select '10000000-0000-0000-0000-000000000003', 'Minister', true, true
where not exists (select 1 from roles where name = 'Minister');
insert into roles (id, name, can_create_work_order, can_fulfill_work_order)
select '10000000-0000-0000-0000-000000000004', 'Deacon', false, true
where not exists (select 1 from roles where name = 'Deacon');
insert into roles (id, name, can_create_work_order, can_fulfill_work_order)
select '10000000-0000-0000-0000-000000000005', 'Parishioner', false, false
where not exists (select 1 from roles where name = 'Parishioner');

insert into employees (id, username, first_name, last_name, email_address)
select '20000000-0000-0000-0000-000000000001', 'hsimpson', 'Homer', 'Simpson', 'homer@simpson.com'
where not exists (select 1 from employees where username = 'hsimpson');
insert into employees (id, username, first_name, last_name, email_address)
select '20000000-0000-0000-0000-000000000002', 'tlovejoy', 'Timothy', 'Lovejoy Jr', 'reverend@firstchurchspringfield.org'
where not exists (select 1 from employees where username = 'tlovejoy');
insert into employees (id, username, first_name, last_name, email_address)
select '20000000-0000-0000-0000-000000000003', 'nflanders', 'Ned', 'Flanders', 'neddy@okily.dokily.com'
where not exists (select 1 from employees where username = 'nflanders');
insert into employees (id, username, first_name, last_name, email_address)
select '20000000-0000-0000-0000-000000000004', 'gwillie', 'Groundskeeper Willie', 'MacDougal', 'willie@springfieldelementary.edu'
where not exists (select 1 from employees where username = 'gwillie');
insert into employees (id, username, first_name, last_name, email_address)
select '20000000-0000-0000-0000-000000000005', 'msimpson', 'Marge', 'Simpson', 'marge@simpson.com'
where not exists (select 1 from employees where username = 'msimpson');

insert into employee_roles (employee_id, role_id)
select e.id, r.id from employees e, roles r
where e.username = 'hsimpson' and r.name in ('Facility Lead', 'Fulfillment')
and not exists (select 1 from employee_roles er where er.employee_id = e.id and er.role_id = r.id);
insert into employee_roles (employee_id, role_id)
select e.id, r.id from employees e, roles r
where e.username = 'tlovejoy' and r.name = 'Minister'
and not exists (select 1 from employee_roles er where er.employee_id = e.id and er.role_id = r.id);
insert into employee_roles (employee_id, role_id)
select e.id, r.id from employees e, roles r
where e.username = 'nflanders' and r.name in ('Deacon', 'Parishioner')
and not exists (select 1 from employee_roles er where er.employee_id = e.id and er.role_id = r.id);
insert into employee_roles (employee_id, role_id)
select e.id, r.id from employees e, roles r
where e.username = 'gwillie' and r.name = 'Fulfillment'
and not exists (select 1 from employee_roles er where er.employee_id = e.id and er.role_id = r.id);
insert into employee_roles (employee_id, role_id)
select e.id, r.id from employees e, roles r
where e.username = 'msimpson' and r.name = 'Parishioner'
and not exists (select 1 from employee_roles er where er.employee_id = e.id and er.role_id = r.id);

-- Backfill existing owner display names when they match a seeded demo employee exactly.
-- Unknown legacy names remain NULL so they cannot be mistaken for an authenticated username.
update work_orders w
set creator_username = (select e.username from employees e
    where lower(trim(e.first_name || ' ' || e.last_name)) = lower(trim(w.creator_name)))
where w.creator_username is null and exists (select 1 from employees e
    where lower(trim(e.first_name || ' ' || e.last_name)) = lower(trim(w.creator_name)));
update work_orders w
set assignee_username = (select e.username from employees e
    where lower(trim(e.first_name || ' ' || e.last_name)) = lower(trim(w.assignee_name)))
where w.assignee_username is null and exists (select 1 from employees e
    where lower(trim(e.first_name || ' ' || e.last_name)) = lower(trim(w.assignee_name)));
