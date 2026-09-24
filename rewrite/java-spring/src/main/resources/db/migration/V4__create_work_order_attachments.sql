create table work_order_attachments (
    id uuid primary key,
    work_order_id uuid not null references work_orders(id) on delete cascade,
    file_name varchar(500) not null,
    content_type varchar(200) not null,
    file_size bigint not null,
    uploaded_by_id uuid not null references employees(id) on delete restrict,
    uploaded_date timestamp with time zone not null
);

create index idx_work_order_attachments_order_date
    on work_order_attachments(work_order_id, uploaded_date);
