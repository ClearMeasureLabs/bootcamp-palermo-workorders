create table work_orders (
    id uuid primary key,
    number varchar(24) not null unique,
    title varchar(240) not null,
    description varchar(4000),
    instructions varchar(4000),
    room_number varchar(900),
    creator_name varchar(160),
    assignee_name varchar(160),
    status varchar(24) not null,
    created_at timestamp with time zone not null,
    assigned_at timestamp with time zone,
    completed_at timestamp with time zone,
    due_date date
);
