-- QuantumCare Hospital Management System
-- Run this in Supabase SQL Editor. It creates the complete PostgreSQL schema and seed data.

create extension if not exists "pgcrypto";
create extension if not exists "pg_trgm";

create sequence if not exists patient_number_seq start 1001;
create sequence if not exists appointment_number_seq start 5001;
create sequence if not exists invoice_number_seq start 9001;

create or replace function generate_hospital_number()
returns text language plpgsql as $$
begin
    return 'HMS-' || to_char(current_date, 'YYYY') || '-' || lpad(nextval('patient_number_seq')::text, 6, '0');
end;
$$;

create or replace function generate_appointment_number()
returns text language plpgsql as $$
begin
    return 'APT-' || to_char(current_date, 'YYYYMM') || '-' || lpad(nextval('appointment_number_seq')::text, 6, '0');
end;
$$;

create or replace function generate_invoice_number()
returns text language plpgsql as $$
begin
    return 'INV-' || to_char(current_date, 'YYYYMM') || '-' || lpad(nextval('invoice_number_seq')::text, 6, '0');
end;
$$;

create or replace function set_updated_at()
returns trigger language plpgsql as $$
begin
    new.updated_at = now();
    return new;
end;
$$;

create table if not exists roles (
    id uuid primary key default gen_random_uuid(),
    name varchar(50) not null unique,
    description varchar(300),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists users (
    id uuid primary key default gen_random_uuid(),
    role_id uuid not null references roles(id),
    username varchar(80) not null unique,
    email varchar(160) not null unique,
    password_hash text not null,
    first_name varchar(80) not null,
    last_name varchar(80) not null,
    status varchar(30) not null default 'Active',
    last_login_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists departments (
    id uuid primary key default gen_random_uuid(),
    name varchar(120) not null unique,
    description varchar(300),
    location varchar(80),
    status varchar(30) not null default 'Active',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists doctors (
    id uuid primary key default gen_random_uuid(),
    user_id uuid unique references users(id) on delete set null,
    department_id uuid references departments(id) on delete set null,
    first_name varchar(80) not null,
    last_name varchar(80) not null,
    specialization varchar(120) not null,
    phone varchar(40),
    email varchar(160),
    license_number varchar(80) not null unique,
    working_schedule varchar(220),
    status varchar(30) not null default 'Active',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists receptionists (
    id uuid primary key default gen_random_uuid(),
    user_id uuid unique references users(id) on delete set null,
    first_name varchar(80) not null,
    last_name varchar(80) not null,
    phone varchar(40),
    email varchar(160),
    shift varchar(120),
    status varchar(30) not null default 'Active',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists rooms (
    id uuid primary key default gen_random_uuid(),
    department_id uuid references departments(id) on delete set null,
    room_number varchar(30) not null unique,
    floor varchar(30) not null,
    room_type varchar(40) not null check (room_type in ('General', 'Private', 'ICU', 'Emergency')),
    capacity int not null check (capacity > 0),
    current_occupancy int not null default 0 check (current_occupancy >= 0),
    status varchar(30) not null default 'Available' check (status in ('Available', 'Occupied', 'Maintenance')),
    price_per_day numeric(12,2) not null default 0,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint room_capacity_guard check (current_occupancy <= capacity)
);

create table if not exists patients (
    id uuid primary key default gen_random_uuid(),
    hospital_number varchar(30) not null unique default generate_hospital_number(),
    assigned_doctor_id uuid references doctors(id) on delete set null,
    current_room_id uuid references rooms(id) on delete set null,
    first_name varchar(80) not null,
    last_name varchar(80) not null,
    date_of_birth date not null,
    gender varchar(20) not null,
    personal_number varchar(40) not null unique,
    phone varchar(40),
    email varchar(160),
    address varchar(300),
    emergency_contact_name varchar(120),
    emergency_contact_phone varchar(40),
    blood_type varchar(10),
    allergies text,
    chronic_diseases text,
    registration_date date not null default current_date,
    status varchar(30) not null default 'Active' check (status in ('Active', 'Admitted', 'Discharged')),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists patient_room_assignments (
    id uuid primary key default gen_random_uuid(),
    patient_id uuid not null references patients(id) on delete cascade,
    room_id uuid not null references rooms(id) on delete restrict,
    admission_date date not null default current_date,
    expected_discharge_date date,
    actual_discharge_date date,
    notes text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists appointments (
    id uuid primary key default gen_random_uuid(),
    appointment_number varchar(40) not null unique default generate_appointment_number(),
    patient_id uuid not null references patients(id) on delete cascade,
    doctor_id uuid not null references doctors(id) on delete restrict,
    service_id uuid,
    appointment_date date not null,
    appointment_time time not null,
    reason varchar(220) not null,
    status varchar(30) not null default 'Scheduled' check (status in ('Scheduled', 'Waiting', 'In Progress', 'Completed', 'Cancelled')),
    notes text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists visits (
    id uuid primary key default gen_random_uuid(),
    patient_id uuid not null references patients(id) on delete cascade,
    doctor_id uuid not null references doctors(id) on delete restrict,
    appointment_id uuid references appointments(id) on delete set null,
    visit_date timestamptz not null default now(),
    symptoms text not null,
    diagnosis text not null,
    disease varchar(160),
    treatment_plan text not null,
    notes text,
    follow_up_date date,
    visit_status varchar(30) not null default 'Open',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists diseases (
    id uuid primary key default gen_random_uuid(),
    disease_name varchar(160) not null unique,
    category varchar(120) not null,
    description text,
    common_symptoms text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists diagnoses (
    id uuid primary key default gen_random_uuid(),
    patient_id uuid not null references patients(id) on delete cascade,
    doctor_id uuid not null references doctors(id) on delete restrict,
    disease_id uuid references diseases(id) on delete set null,
    disease_name varchar(160) not null,
    icd_code varchar(40),
    severity varchar(30) not null default 'Low' check (severity in ('Low', 'Medium', 'High', 'Critical')),
    description text,
    diagnosis_date date not null default current_date,
    treatment_recommendation text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists prescriptions (
    id uuid primary key default gen_random_uuid(),
    patient_id uuid not null references patients(id) on delete cascade,
    doctor_id uuid not null references doctors(id) on delete restrict,
    medication_name varchar(160) not null,
    dosage varchar(100) not null,
    frequency varchar(100) not null,
    duration varchar(100) not null,
    instructions text,
    prescription_date date not null default current_date,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists lab_tests (
    id uuid primary key default gen_random_uuid(),
    patient_id uuid not null references patients(id) on delete cascade,
    doctor_id uuid not null references doctors(id) on delete restrict,
    test_name varchar(160) not null,
    test_type varchar(120) not null,
    requested_date date not null default current_date,
    status varchar(30) not null default 'Requested' check (status in ('Requested', 'In Progress', 'Completed')),
    result text,
    result_date date,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists password_reset_tokens (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references users(id) on delete cascade,
    token varchar(120) not null unique,
    expires_at timestamptz not null,
    used_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists medication_inventory (
    id uuid primary key default gen_random_uuid(),
    medication_name varchar(160) not null,
    category varchar(80) not null default 'General',
    unit varchar(80) not null default 'pcs',
    quantity_in_stock int not null default 0 check (quantity_in_stock >= 0),
    reorder_level int not null default 10 check (reorder_level >= 0),
    expiry_date date,
    supplier varchar(120),
    status varchar(30) not null default 'Available',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists notifications (
    id uuid primary key default gen_random_uuid(),
    user_id uuid references users(id) on delete cascade,
    title varchar(160) not null,
    message text not null,
    notification_type varchar(40) not null default 'Info',
    link_url varchar(120),
    is_read boolean not null default false,
    read_at timestamptz,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists services (
    id uuid primary key default gen_random_uuid(),
    department_id uuid references departments(id) on delete set null,
    service_name varchar(160) not null unique,
    description text,
    price numeric(12,2) not null default 0,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'appointments_service_id_fkey') then
        alter table appointments
        add constraint appointments_service_id_fkey foreign key (service_id) references services(id) on delete set null;
    end if;
end $$;

create table if not exists appointment_services (
    appointment_id uuid not null references appointments(id) on delete cascade,
    service_id uuid not null references services(id) on delete restrict,
    created_at timestamptz not null default now(),
    primary key (appointment_id, service_id)
);

create index if not exists idx_appointment_services_service on appointment_services(service_id);

create table if not exists payments (
    id uuid primary key default gen_random_uuid(),
    patient_id uuid not null references patients(id) on delete cascade,
    service_id uuid not null references services(id) on delete restrict,
    amount numeric(12,2) not null check (amount >= 0),
    total_amount numeric(12,2) not null default 0 check (total_amount >= 0),
    balance_amount numeric(12,2) not null default 0 check (balance_amount >= 0),
    payment_method varchar(40) not null check (payment_method in ('Cash', 'Card', 'Bank Transfer')),
    payment_date timestamptz not null default now(),
    status varchar(30) not null default 'Paid' check (status in ('Paid', 'Pending', 'Cancelled')),
    notes text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

alter table payments
add column if not exists total_amount numeric(12,2) not null default 0,
add column if not exists balance_amount numeric(12,2) not null default 0;

create table if not exists payment_services (
    payment_id uuid not null references payments(id) on delete cascade,
    service_id uuid not null references services(id) on delete restrict,
    created_at timestamptz not null default now(),
    primary key (payment_id, service_id)
);

create index if not exists idx_payment_services_service on payment_services(service_id);

insert into payment_services (payment_id, service_id)
select id, service_id
from payments
where service_id is not null
on conflict (payment_id, service_id) do nothing;

update payments
set total_amount = case when total_amount <= 0 then amount else total_amount end,
    balance_amount = greatest((case when total_amount <= 0 then amount else total_amount end) - amount, 0),
    status = case
        when status = 'Cancelled' then 'Cancelled'
        when greatest((case when total_amount <= 0 then amount else total_amount end) - amount, 0) > 0 then 'Pending'
        else status
    end,
    updated_at = now()
where total_amount <= 0 or balance_amount <> greatest(total_amount - amount, 0);

create table if not exists invoices (
    id uuid primary key default gen_random_uuid(),
    payment_id uuid not null unique references payments(id) on delete cascade,
    invoice_number varchar(40) not null unique default generate_invoice_number(),
    invoice_date timestamptz not null default now(),
    total_amount numeric(12,2) not null,
    status varchar(30) not null default 'Issued',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

update invoices i
set total_amount = py.total_amount,
    updated_at = now()
from payments py
where py.id = i.payment_id
  and i.total_amount <> py.total_amount;

create table if not exists audit_logs (
    id uuid primary key default gen_random_uuid(),
    user_id uuid references users(id) on delete set null,
    action varchar(120) not null,
    entity_name varchar(80),
    entity_id uuid,
    details text,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

do $$
declare
    table_name text;
begin
    foreach table_name in array array[
        'roles','users','departments','doctors','receptionists','rooms','patients',
        'patient_room_assignments','appointments','visits','diseases','diagnoses',
        'prescriptions','lab_tests','password_reset_tokens','medication_inventory',
        'notifications','services','payments','invoices','audit_logs'
    ]
    loop
        execute format('drop trigger if exists trg_%I_updated_at on %I', table_name, table_name);
        execute format('create trigger trg_%I_updated_at before update on %I for each row execute function set_updated_at()', table_name, table_name);
    end loop;
end $$;

create index if not exists idx_users_role_id on users(role_id);
create index if not exists idx_users_email_lower on users(lower(email));
create index if not exists idx_patients_name_trgm on patients using gin ((first_name || ' ' || last_name) gin_trgm_ops);
create index if not exists idx_patients_personal_number on patients(personal_number);
create index if not exists idx_patients_phone on patients(phone);
create index if not exists idx_patients_status on patients(status);
create index if not exists idx_patients_doctor on patients(assigned_doctor_id);
create index if not exists idx_patients_room on patients(current_room_id);
create index if not exists idx_doctors_department on doctors(department_id);
create index if not exists idx_rooms_status on rooms(status);
create index if not exists idx_appointments_doctor_date on appointments(doctor_id, appointment_date);
create index if not exists idx_appointments_patient on appointments(patient_id);
create index if not exists idx_visits_patient on visits(patient_id);
create index if not exists idx_diagnoses_patient on diagnoses(patient_id);
create index if not exists idx_prescriptions_patient on prescriptions(patient_id);
create index if not exists idx_lab_tests_patient on lab_tests(patient_id);
create index if not exists idx_password_reset_tokens_token on password_reset_tokens(token);
create index if not exists idx_medication_inventory_name on medication_inventory(medication_name);
create index if not exists idx_notifications_user_read on notifications(user_id, is_read, created_at desc);
create index if not exists idx_payments_patient on payments(patient_id);
create index if not exists idx_audit_logs_created_at on audit_logs(created_at desc);
create unique index if not exists idx_appointments_doctor_slot_active on appointments(doctor_id, appointment_date, appointment_time) where status <> 'Cancelled';

-- Seed reference data
insert into roles (id, name, description) values
('10000000-0000-0000-0000-000000000001','Admin','Full hospital administration and reporting access'),
('10000000-0000-0000-0000-000000000002','Doctor','Clinical access for assigned patients, visits, diagnoses, prescriptions, and lab tests'),
('10000000-0000-0000-0000-000000000003','Receptionist','Front desk intake, appointments, room assignment, and billing')
on conflict (id) do nothing;

insert into departments (id, name, description, location) values
('20000000-0000-0000-0000-000000000001','Emergency','24/7 urgent care and triage','Ground floor'),
('20000000-0000-0000-0000-000000000002','Cardiology','Heart and vascular diagnostics','Floor 2'),
('20000000-0000-0000-0000-000000000003','Pediatrics','Child and adolescent care','Floor 1'),
('20000000-0000-0000-0000-000000000004','Internal Medicine','Adult diagnosis and chronic disease care','Floor 3'),
('20000000-0000-0000-0000-000000000005','Radiology','Imaging and diagnostic support','Floor -1')
on conflict (id) do nothing;

insert into users (id, role_id, username, email, password_hash, first_name, last_name) values
('30000000-0000-0000-0000-000000000001','10000000-0000-0000-0000-000000000001','admin@vleradent.com','admin@vleradent.com','PBKDF2-SHA256$100000$Kvicsrb0fxDSC/9wbOFlKw==$sjaE2LQKBRKIHNlk7KDa0TqA/ztn+abCP6n/oUDa9T0=','Admin','Vlera Dent'),
('30000000-0000-0000-0000-000000000002','10000000-0000-0000-0000-000000000002','vlorentina.sahiti@vleradent.com','vlorentina.sahiti@vleradent.com','PBKDF2-SHA256$100000$+7QomWt7oXQCq7CFK3sbAA==$GGXME0KqylmWA7/LSjCzRlTxEH5GtsyJpmz3Tz+XszQ=','Vlorentina','Sahiti'),
('30000000-0000-0000-0000-000000000003','10000000-0000-0000-0000-000000000002','doctor2','doctor2@quantumcare.test','PBKDF2-SHA256$100000$+7QomWt7oXQCq7CFK3sbAA==$GGXME0KqylmWA7/LSjCzRlTxEH5GtsyJpmz3Tz+XszQ=','Blerim','Hoxha'),
('30000000-0000-0000-0000-000000000004','10000000-0000-0000-0000-000000000002','doctor3','doctor3@quantumcare.test','PBKDF2-SHA256$100000$+7QomWt7oXQCq7CFK3sbAA==$GGXME0KqylmWA7/LSjCzRlTxEH5GtsyJpmz3Tz+XszQ=','Diellza','Berisha'),
('30000000-0000-0000-0000-000000000005','10000000-0000-0000-0000-000000000003','reception@vleradent.com','reception@vleradent.com','PBKDF2-SHA256$100000$THzcO2ZpEGQSRy0xhwxxFA==$qc6/idD+2eS+B+5bW5MkrUdY7AVFfHZuWaxeJ72nBbA=','Reception','Vlera Dent'),
('30000000-0000-0000-0000-000000000006','10000000-0000-0000-0000-000000000003','reception2','reception2@quantumcare.test','PBKDF2-SHA256$100000$THzcO2ZpEGQSRy0xhwxxFA==$qc6/idD+2eS+B+5bW5MkrUdY7AVFfHZuWaxeJ72nBbA=','Era','Rugova')
on conflict (id) do nothing;

insert into doctors (id, user_id, department_id, first_name, last_name, specialization, phone, email, license_number, working_schedule) values
('40000000-0000-0000-0000-000000000001','30000000-0000-0000-0000-000000000002','20000000-0000-0000-0000-000000000002','Elira','Gashi','Cardiologist','+38344111222','elira.gashi@quantumcare.test','KS-MD-1001','Mon-Fri 08:00-16:00'),
('40000000-0000-0000-0000-000000000002','30000000-0000-0000-0000-000000000003','20000000-0000-0000-0000-000000000004','Blerim','Hoxha','Internal Medicine','+38344111333','blerim.hoxha@quantumcare.test','KS-MD-1002','Mon-Sat 09:00-17:00'),
('40000000-0000-0000-0000-000000000003','30000000-0000-0000-0000-000000000004','20000000-0000-0000-0000-000000000003','Diellza','Berisha','Pediatrician','+38344111444','diellza.berisha@quantumcare.test','KS-MD-1003','Tue-Sat 08:00-15:00')
on conflict (id) do nothing;

insert into receptionists (id, user_id, first_name, last_name, phone, email, shift) values
('50000000-0000-0000-0000-000000000001','30000000-0000-0000-0000-000000000005','Luan','Shala','+38344111555','luan.shala@quantumcare.test','Morning 07:00-15:00'),
('50000000-0000-0000-0000-000000000002','30000000-0000-0000-0000-000000000006','Era','Rugova','+38344111666','era.rugova@quantumcare.test','Evening 15:00-23:00')
on conflict (id) do nothing;

insert into rooms (id, department_id, room_number, floor, room_type, capacity, current_occupancy, status, price_per_day) values
('60000000-0000-0000-0000-000000000001','20000000-0000-0000-0000-000000000001','E-101','0','Emergency',4,2,'Available',45),
('60000000-0000-0000-0000-000000000002','20000000-0000-0000-0000-000000000002','C-201','2','Private',1,1,'Occupied',85),
('60000000-0000-0000-0000-000000000003','20000000-0000-0000-0000-000000000002','C-202','2','General',3,1,'Available',40),
('60000000-0000-0000-0000-000000000004','20000000-0000-0000-0000-000000000003','P-110','1','General',4,0,'Available',35),
('60000000-0000-0000-0000-000000000005','20000000-0000-0000-0000-000000000003','P-111','1','Private',1,0,'Available',70),
('60000000-0000-0000-0000-000000000006','20000000-0000-0000-0000-000000000004','I-301','3','General',4,1,'Available',38),
('60000000-0000-0000-0000-000000000007','20000000-0000-0000-0000-000000000004','I-302','3','Private',1,0,'Available',75),
('60000000-0000-0000-0000-000000000008','20000000-0000-0000-0000-000000000001','ICU-1','0','ICU',2,1,'Available',150),
('60000000-0000-0000-0000-000000000009','20000000-0000-0000-0000-000000000001','ICU-2','0','ICU',2,0,'Maintenance',150),
('60000000-0000-0000-0000-000000000010','20000000-0000-0000-0000-000000000005','R-010','-1','Private',1,0,'Available',60)
on conflict (id) do nothing;

insert into patients (id, assigned_doctor_id, current_room_id, first_name, last_name, date_of_birth, gender, personal_number, phone, email, address, emergency_contact_name, emergency_contact_phone, blood_type, allergies, chronic_diseases, registration_date, status) values
('70000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000001','60000000-0000-0000-0000-000000000002','Arta','Kelmendi','1984-03-12','Female','KS1000000001','+38345123001','arta.kelmendi@example.com','Rr. UCK 12, Pristina','Besnik Kelmendi','+38349123001','A+','Penicillin','Hypertension',current_date,'Admitted'),
('70000000-0000-0000-0000-000000000002','40000000-0000-0000-0000-000000000002','60000000-0000-0000-0000-000000000001','Fisnik','Morina','1978-07-22','Male','KS1000000002','+38345123002','fisnik.morina@example.com','Peja Center','Liridona Morina','+38349123002','O+','','Diabetes type 2',current_date,'Admitted'),
('70000000-0000-0000-0000-000000000003','40000000-0000-0000-0000-000000000003',null,'Leona','Rama','2016-11-02','Female','KS1000000003','+38345123003','leona.rama@example.com','Prizren 4','Ardian Rama','+38349123003','B+','Dust','Asthma',current_date,'Active'),
('70000000-0000-0000-0000-000000000004','40000000-0000-0000-0000-000000000001','60000000-0000-0000-0000-000000000003','Valon','Syla','1990-01-18','Male','KS1000000004','+38345123004','valon.syla@example.com','Mitrovica','Nora Syla','+38349123004','AB+','','',current_date - interval '1 day','Admitted'),
('70000000-0000-0000-0000-000000000005','40000000-0000-0000-0000-000000000002','60000000-0000-0000-0000-000000000006','Drita','Haziri','1965-05-09','Female','KS1000000005','+38345123005','drita.haziri@example.com','Gjilan','Agron Haziri','+38349123005','O-','Ibuprofen','COPD',current_date - interval '2 days','Admitted'),
('70000000-0000-0000-0000-000000000006','40000000-0000-0000-0000-000000000003',null,'Albin','Rexha','2012-02-14','Male','KS1000000006','+38345123006','albin.rexha@example.com','Ferizaj','Mimoza Rexha','+38349123006','A-','','',current_date - interval '2 days','Active'),
('70000000-0000-0000-0000-000000000007','40000000-0000-0000-0000-000000000001','60000000-0000-0000-0000-000000000008','Teuta','Mehmeti','1972-09-27','Female','KS1000000007','+38345123007','teuta.mehmeti@example.com','Gjakova','Ilir Mehmeti','+38349123007','B-','Seafood','Heart failure',current_date - interval '3 days','Admitted'),
('70000000-0000-0000-0000-000000000008','40000000-0000-0000-0000-000000000002',null,'Krenar','Bytyqi','1988-12-01','Male','KS1000000008','+38345123008','krenar.bytyqi@example.com','Vushtrri','Blerta Bytyqi','+38349123008','A+','','Migraine',current_date - interval '4 days','Active'),
('70000000-0000-0000-0000-000000000009','40000000-0000-0000-0000-000000000003',null,'Rina','Krasniqi','2019-08-16','Female','KS1000000009','+38345123009','rina.krasniqi@example.com','Pristina','Flamur Krasniqi','+38349123009','O+','Pollen','',current_date - interval '5 days','Active'),
('70000000-0000-0000-0000-000000000010','40000000-0000-0000-0000-000000000001',null,'Nderim','Shabani','1959-06-30','Male','KS1000000010','+38345123010','nderim.shabani@example.com','Suhareka','Vjosa Shabani','+38349123010','AB-','','Arrhythmia',current_date - interval '6 days','Active'),
('70000000-0000-0000-0000-000000000011','40000000-0000-0000-0000-000000000002',null,'Elona','Dervishi','1995-04-21','Female','KS1000000011','+38345123011','elona.dervishi@example.com','Lipjan','Ardit Dervishi','+38349123011','A+','','',current_date - interval '7 days','Active'),
('70000000-0000-0000-0000-000000000012','40000000-0000-0000-0000-000000000003',null,'Dion','Osmani','2014-10-10','Male','KS1000000012','+38345123012','dion.osmani@example.com','Podujeva','Albana Osmani','+38349123012','O+','Nuts','',current_date - interval '8 days','Active'),
('70000000-0000-0000-0000-000000000013','40000000-0000-0000-0000-000000000001',null,'Vlera','Zeqiri','1981-02-03','Female','KS1000000013','+38345123013','vlera.zeqiri@example.com','Malisheva','Mentor Zeqiri','+38349123013','B+','','Hypertension',current_date - interval '10 days','Active'),
('70000000-0000-0000-0000-000000000014','40000000-0000-0000-0000-000000000002',null,'Besar','Leka','1970-12-25','Male','KS1000000014','+38345123014','besar.leka@example.com','Rahovec','Vlora Leka','+38349123014','A-','','Kidney stones',current_date - interval '12 days','Discharged'),
('70000000-0000-0000-0000-000000000015','40000000-0000-0000-0000-000000000003',null,'Jona','Ahmeti','2018-07-07','Female','KS1000000015','+38345123015','jona.ahmeti@example.com','Decan','Rrezarta Ahmeti','+38349123015','O-','','',current_date - interval '13 days','Active'),
('70000000-0000-0000-0000-000000000016','40000000-0000-0000-0000-000000000001',null,'Mirlind','Tahiri','1999-03-19','Male','KS1000000016','+38345123016','mirlind.tahiri@example.com','Skenderaj','Genta Tahiri','+38349123016','A+','','',current_date - interval '14 days','Active'),
('70000000-0000-0000-0000-000000000017','40000000-0000-0000-0000-000000000002',null,'Shpresa','Selimi','1968-01-11','Female','KS1000000017','+38345123017','shpresa.selimi@example.com','Istog','Dardan Selimi','+38349123017','B-','Aspirin','Diabetes type 2',current_date - interval '15 days','Active'),
('70000000-0000-0000-0000-000000000018','40000000-0000-0000-0000-000000000003',null,'Trim','Koci','2010-09-05','Male','KS1000000018','+38345123018','trim.koci@example.com','Kamenica','Mirela Koci','+38349123018','AB+','','',current_date - interval '16 days','Active'),
('70000000-0000-0000-0000-000000000019','40000000-0000-0000-0000-000000000001',null,'Flutura','Gashi','1986-06-06','Female','KS1000000019','+38345123019','flutura.gashi@example.com','Pristina','Kushtrim Gashi','+38349123019','O+','','High cholesterol',current_date - interval '20 days','Active'),
('70000000-0000-0000-0000-000000000020','40000000-0000-0000-0000-000000000002',null,'Ardian','Musa','1976-11-29','Male','KS1000000020','+38345123020','ardian.musa@example.com','Peja','Lume Musa','+38349123020','A+','','',current_date - interval '24 days','Discharged')
on conflict (id) do nothing;

insert into patient_room_assignments (id, patient_id, room_id, admission_date, expected_discharge_date, notes) values
('d0000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000001','60000000-0000-0000-0000-000000000002',current_date,current_date + interval '4 days','Cardiology observation'),
('d0000000-0000-0000-0000-000000000002','70000000-0000-0000-0000-000000000002','60000000-0000-0000-0000-000000000001',current_date,current_date + interval '2 days','Emergency stabilization'),
('d0000000-0000-0000-0000-000000000003','70000000-0000-0000-0000-000000000004','60000000-0000-0000-0000-000000000003',current_date - interval '1 day',current_date + interval '5 days','Chest pain monitoring'),
('d0000000-0000-0000-0000-000000000004','70000000-0000-0000-0000-000000000005','60000000-0000-0000-0000-000000000006',current_date - interval '2 days',current_date + interval '6 days','Respiratory support'),
('d0000000-0000-0000-0000-000000000005','70000000-0000-0000-0000-000000000007','60000000-0000-0000-0000-000000000008',current_date - interval '3 days',current_date + interval '3 days','ICU observation')
on conflict (id) do nothing;

insert into diseases (id, disease_name, category, description, common_symptoms) values
('80000000-0000-0000-0000-000000000001','Hypertension','Cardiology','Elevated blood pressure requiring monitoring','Headache, dizziness, fatigue'),
('80000000-0000-0000-0000-000000000002','Type 2 Diabetes','Endocrinology','Insulin resistance and high blood sugar','Thirst, fatigue, frequent urination'),
('80000000-0000-0000-0000-000000000003','Asthma','Pulmonology','Chronic airway inflammation','Wheezing, cough, shortness of breath'),
('80000000-0000-0000-0000-000000000004','Bronchitis','Pulmonology','Inflammation of bronchial tubes','Cough, mucus, chest discomfort'),
('80000000-0000-0000-0000-000000000005','Arrhythmia','Cardiology','Irregular heartbeat rhythm','Palpitations, weakness, dizziness'),
('80000000-0000-0000-0000-000000000006','Migraine','Neurology','Recurring headache disorder','Headache, nausea, light sensitivity'),
('80000000-0000-0000-0000-000000000007','Pneumonia','Infectious Disease','Infection of lung tissue','Fever, cough, chest pain'),
('80000000-0000-0000-0000-000000000008','Otitis Media','Pediatrics','Middle ear infection','Ear pain, fever, irritability')
on conflict (id) do nothing;

insert into services (id, department_id, service_name, description, price) values
('90000000-0000-0000-0000-000000000001','20000000-0000-0000-0000-000000000001','Kontrolle stomatologjike','Kontroll fillestar, vleresim oral dhe plan trajtimi.',15),
('90000000-0000-0000-0000-000000000002','20000000-0000-0000-0000-000000000002','Pastrimi i gureve dentare','Pastrim profesional i gureve, pllakave dhe polishim i dhembeve.',25),
('90000000-0000-0000-0000-000000000003','20000000-0000-0000-0000-000000000002','Mbushje e dhembit','Mbushje estetike kompozite per karies dhe demtime te vogla.',30),
('90000000-0000-0000-0000-000000000004','20000000-0000-0000-0000-000000000003','Zbardhim i dhembeve','Zbardhim profesional per buzeqeshje me te ndritshme.',80),
('90000000-0000-0000-0000-000000000005','20000000-0000-0000-0000-000000000004','Trajtim kanali','Endodonci per dhembe me infeksion ose dhimbje te thelle.',70),
('90000000-0000-0000-0000-000000000006','20000000-0000-0000-0000-000000000005','Heqje e dhembit','Ekstraksion i sigurt i dhembit me kujdes pas trajtimit.',35),
('90000000-0000-0000-0000-000000000007','20000000-0000-0000-0000-000000000005','Kurora dentare','Kurora porcelani/zirconi per rikthim funksioni dhe estetike.',180),
('90000000-0000-0000-0000-000000000008','20000000-0000-0000-0000-000000000004','Ura dentare','Zevendesim i dhembeve te munguar me ure dentare fikse.',250),
('90000000-0000-0000-0000-000000000009','20000000-0000-0000-0000-000000000001','Implant dentar','Vendosje implanti per zevendesim te dhembit te munguar.',550),
('90000000-0000-0000-0000-000000000010','20000000-0000-0000-0000-000000000001','Proteze dentare','Proteze parciale ose totale sipas nevojes se pacientit.',220),
('90000000-0000-0000-0000-000000000011','20000000-0000-0000-0000-000000000002','Rregullim ortodontik','Konsulte dhe planifikim per drejtim te dhembeve.',60),
('90000000-0000-0000-0000-000000000012','20000000-0000-0000-0000-000000000003','Faseta dentare','Faseta estetike per forme, ngjyre dhe simetri te buzeqeshjes.',200),
('90000000-0000-0000-0000-000000000013','20000000-0000-0000-0000-000000000004','Radiografi dentare','Imazheri dentare per diagnoze dhe plan trajtimi.',15),
('90000000-0000-0000-0000-000000000014','20000000-0000-0000-0000-000000000005','Trajtim i mishrave te dhembeve','Trajtim periodontal per inflamacion dhe gjakderdhje.',45),
('90000000-0000-0000-0000-000000000015','20000000-0000-0000-0000-000000000001','Dhembe femijesh','Kontroll dhe trajtim stomatologjik per femije.',20),
('90000000-0000-0000-0000-000000000016','20000000-0000-0000-0000-000000000002','Fluorizim','Mbrojtje preventive kunder kariesit me fluor.',18),
('90000000-0000-0000-0000-000000000017','20000000-0000-0000-0000-000000000003','Sealant per femije','Mbrojtje e dhemballeve me sealant kunder kariesit.',20),
('90000000-0000-0000-0000-000000000018','20000000-0000-0000-0000-000000000004','Urgjence dentare','Trajtim i dhimbjeve akute, infeksioneve ose thyerjeve dentare.',40)
on conflict (id) do update set department_id = excluded.department_id, service_name = excluded.service_name, description = excluded.description, price = excluded.price, updated_at = now();

insert into medication_inventory (id, medication_name, category, unit, quantity_in_stock, reorder_level, expiry_date, supplier, status) values
('ac000000-0000-0000-0000-000000000001','Anestezion dental','Anestezion','ampula',80,20,(current_date + interval '10 months')::date,'Dental Pharma KS','Available'),
('ac000000-0000-0000-0000-000000000002','Gjilpera dentare','Material shpenzues','cope',450,100,(current_date + interval '18 months')::date,'Dental Supply Prishtina','Available'),
('ac000000-0000-0000-0000-000000000003','Doreza nitrile','Higjiene','pako',35,12,(current_date + interval '24 months')::date,'MediDent','Available'),
('ac000000-0000-0000-0000-000000000004','Maska kirurgjikale','Higjiene','pako',28,10,(current_date + interval '24 months')::date,'MediDent','Available'),
('ac000000-0000-0000-0000-000000000005','Kompozit per mbushje','Material restaurues','shiringa',18,8,(current_date + interval '12 months')::date,'Dental Line','Available'),
('ac000000-0000-0000-0000-000000000006','Bonding dental','Material restaurues','shishe',9,5,(current_date + interval '8 months')::date,'Dental Line','Available'),
('ac000000-0000-0000-0000-000000000007','Guta percha','Endodonci','pako',14,6,(current_date + interval '20 months')::date,'EndoCare','Available'),
('ac000000-0000-0000-0000-000000000008','Freza dentare','Instrumente','sete',5,4,(current_date + interval '18 months')::date,'Dental Instruments KS','Low Stock')
on conflict (id) do update set medication_name = excluded.medication_name, category = excluded.category, unit = excluded.unit, quantity_in_stock = excluded.quantity_in_stock, reorder_level = excluded.reorder_level, expiry_date = excluded.expiry_date, supplier = excluded.supplier, status = excluded.status, updated_at = now();

insert into appointments (id, patient_id, doctor_id, appointment_date, appointment_time, reason, status, notes) values
('a0000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000001',current_date,'09:00','Blood pressure follow-up','Waiting','Bring last ECG'),
('a0000000-0000-0000-0000-000000000002','70000000-0000-0000-0000-000000000003','40000000-0000-0000-0000-000000000003',current_date,'10:30','Pediatric cough assessment','Scheduled',''),
('a0000000-0000-0000-0000-000000000003','70000000-0000-0000-0000-000000000008','40000000-0000-0000-0000-000000000002',current_date,'11:00','Migraine consult','Scheduled',''),
('a0000000-0000-0000-0000-000000000004','70000000-0000-0000-0000-000000000010','40000000-0000-0000-0000-000000000001',current_date,'13:30','Arrhythmia review','In Progress',''),
('a0000000-0000-0000-0000-000000000005','70000000-0000-0000-0000-000000000011','40000000-0000-0000-0000-000000000002',current_date + interval '1 day','09:15','General exam','Scheduled','')
on conflict (id) do nothing;

insert into appointment_services (appointment_id, service_id) values
('a0000000-0000-0000-0000-000000000001','90000000-0000-0000-0000-000000000001'),
('a0000000-0000-0000-0000-000000000001','90000000-0000-0000-0000-000000000002'),
('a0000000-0000-0000-0000-000000000002','90000000-0000-0000-0000-000000000015'),
('a0000000-0000-0000-0000-000000000003','90000000-0000-0000-0000-000000000013'),
('a0000000-0000-0000-0000-000000000004','90000000-0000-0000-0000-000000000003'),
('a0000000-0000-0000-0000-000000000005','90000000-0000-0000-0000-000000000001')
on conflict (appointment_id, service_id) do nothing;

insert into visits (id, patient_id, doctor_id, appointment_id, visit_date, symptoms, diagnosis, disease, treatment_plan, notes, follow_up_date, visit_status) values
('b0000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000001','a0000000-0000-0000-0000-000000000001',now() - interval '2 hours','Headache and high BP','Stage 1 hypertension flare','Hypertension','Adjust medication and observe for 24 hours','Stable',current_date + interval '7 days','Completed'),
('b0000000-0000-0000-0000-000000000002','70000000-0000-0000-0000-000000000003','40000000-0000-0000-0000-000000000003',null,now() - interval '1 day','Cough and mild wheezing','Asthma exacerbation','Asthma','Nebulizer and inhaler education','Parent informed',current_date + interval '14 days','Completed')
on conflict (id) do nothing;

insert into diagnoses (id, patient_id, doctor_id, disease_id, disease_name, icd_code, severity, description, diagnosis_date, treatment_recommendation) values
('e0000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000001','80000000-0000-0000-0000-000000000001','Hypertension','I10','Medium','Elevated BP under observation',current_date,'Daily blood pressure log and medication adjustment'),
('e0000000-0000-0000-0000-000000000002','70000000-0000-0000-0000-000000000002','40000000-0000-0000-0000-000000000002','80000000-0000-0000-0000-000000000002','Type 2 Diabetes','E11','High','Glucose out of range during emergency intake',current_date,'Endocrine review and diet plan'),
('e0000000-0000-0000-0000-000000000003','70000000-0000-0000-0000-000000000003','40000000-0000-0000-0000-000000000003','80000000-0000-0000-0000-000000000003','Asthma','J45','Medium','Mild wheezing episode',current_date - interval '1 day','Continue inhaler and monitor triggers')
on conflict (id) do nothing;

insert into prescriptions (id, patient_id, doctor_id, medication_name, dosage, frequency, duration, instructions, prescription_date) values
('f0000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000001','Amlodipine','5 mg','Once daily','30 days','Take every morning after breakfast',current_date),
('f0000000-0000-0000-0000-000000000002','70000000-0000-0000-0000-000000000003','40000000-0000-0000-0000-000000000003','Salbutamol inhaler','2 puffs','As needed','14 days','Use for wheezing, max every 4 hours',current_date - interval '1 day')
on conflict (id) do nothing;

insert into lab_tests (id, patient_id, doctor_id, test_name, test_type, requested_date, status, result, result_date) values
('aa000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000001','Lipid panel','Blood',current_date,'Requested',null,null),
('aa000000-0000-0000-0000-000000000002','70000000-0000-0000-0000-000000000002','40000000-0000-0000-0000-000000000002','HbA1c','Blood',current_date,'In Progress',null,null),
('aa000000-0000-0000-0000-000000000003','70000000-0000-0000-0000-000000000003','40000000-0000-0000-0000-000000000003','Chest X-Ray','Imaging',current_date - interval '1 day','Completed','No acute infiltrate',current_date)
on conflict (id) do nothing;

insert into payments (id, patient_id, service_id, amount, total_amount, balance_amount, payment_method, payment_date, status, notes) values
('c0000000-0000-0000-0000-000000000001','70000000-0000-0000-0000-000000000001','90000000-0000-0000-0000-000000000002',45,45,0,'Card',now(),'Paid','Card authorization accepted'),
('c0000000-0000-0000-0000-000000000002','70000000-0000-0000-0000-000000000002','90000000-0000-0000-0000-000000000001',25,25,0,'Cash',now(),'Paid',''),
('c0000000-0000-0000-0000-000000000003','70000000-0000-0000-0000-000000000003','90000000-0000-0000-0000-000000000004',30,80,50,'Bank Transfer',now() - interval '1 day','Pending','Awaiting transfer confirmation')
on conflict (id) do nothing;

insert into invoices (payment_id, total_amount, status)
select id, total_amount, 'Issued' from payments
where id in ('c0000000-0000-0000-0000-000000000001','c0000000-0000-0000-0000-000000000002','c0000000-0000-0000-0000-000000000003')
on conflict (payment_id) do nothing;

insert into audit_logs (id, user_id, action, entity_name, entity_id, details) values
('ab000000-0000-0000-0000-000000000001','30000000-0000-0000-0000-000000000001','patient created','patients','70000000-0000-0000-0000-000000000001','Seed patient Arta Kelmendi'),
('ab000000-0000-0000-0000-000000000002','30000000-0000-0000-0000-000000000002','diagnosis added','diagnoses','e0000000-0000-0000-0000-000000000001','Hypertension added for Arta Kelmendi'),
('ab000000-0000-0000-0000-000000000003','30000000-0000-0000-0000-000000000002','prescription created','prescriptions','f0000000-0000-0000-0000-000000000001','Amlodipine prescription created'),
('ab000000-0000-0000-0000-000000000004','30000000-0000-0000-0000-000000000005','payment registered','payments','c0000000-0000-0000-0000-000000000001','Card payment registered'),
('ab000000-0000-0000-0000-000000000005','30000000-0000-0000-0000-000000000005','room assigned','patient_room_assignments','d0000000-0000-0000-0000-000000000001','Patient assigned to C-201')
on conflict (id) do nothing;
