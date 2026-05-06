using Dapper;
using HospitalManagamentSystem.Models;
using HospitalManagamentSystem.ViewModels;

namespace HospitalManagamentSystem.Data;

public class HospitalRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HospitalRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string AppointmentServicesSelectSql = """
        left join lateral (
            select string_agg(s.service_name, ', ' order by s.service_name) as service_names,
                   sum(s.price) as services_total,
                   min(s.service_name) as service_name,
                   sum(s.price) as service_price
            from (
                select aps.service_id
                from appointment_services aps
                where aps.appointment_id = a.id
                union
                select a.service_id
                where a.service_id is not null
            ) selected_services
            join services s on s.id = selected_services.service_id
        ) svc on true
        """;

    private const string PaymentServicesSelectSql = """
        left join lateral (
            select string_agg(s.service_name, ', ' order by s.service_name) as service_names,
                   min(s.service_name) as service_name
            from (
                select ps.service_id
                from payment_services ps
                where ps.payment_id = py.id
                union
                select py.service_id
                where py.service_id is not null
            ) selected_services
            join services s on s.id = selected_services.service_id
        ) svc on true
        """;

    public async Task EnsureFeatureSchemaAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
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

            create index if not exists idx_password_reset_tokens_token on password_reset_tokens(token);
            create index if not exists idx_medication_inventory_name on medication_inventory(medication_name);
            create index if not exists idx_notifications_user_read on notifications(user_id, is_read, created_at desc);
            create unique index if not exists idx_appointments_doctor_slot_active
                on appointments(doctor_id, appointment_date, appointment_time)
                where status <> 'Cancelled';

            alter table appointments
            add column if not exists service_id uuid references services(id) on delete set null;

            create table if not exists appointment_services (
                appointment_id uuid not null references appointments(id) on delete cascade,
                service_id uuid not null references services(id) on delete restrict,
                created_at timestamptz not null default now(),
                primary key (appointment_id, service_id)
            );

            create index if not exists idx_appointment_services_service on appointment_services(service_id);

            insert into appointment_services (appointment_id, service_id)
            select id, service_id
            from appointments
            where service_id is not null
            on conflict (appointment_id, service_id) do nothing;

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

            create table if not exists invoices (
                id uuid primary key default gen_random_uuid(),
                payment_id uuid not null unique references payments(id) on delete cascade,
                invoice_number varchar(40) not null unique default ('INV-' || to_char(now(), 'YYYYMMDD') || '-' || upper(substr(gen_random_uuid()::text, 1, 8))),
                invoice_date timestamptz not null default now(),
                total_amount numeric(12,2) not null default 0,
                status varchar(30) not null default 'Issued',
                created_at timestamptz not null default now(),
                updated_at timestamptz not null default now()
            );

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

            update invoices i
            set total_amount = py.total_amount,
                updated_at = now()
            from payments py
            where py.id = i.payment_id
              and i.total_amount <> py.total_amount;

            delete from medication_inventory
            where medication_name in (
                'Amlodipine 5 mg',
                'Salbutamol inhaler',
                'Ceftriaxone 1 g',
                'Insulin rapid acting'
            );

            insert into medication_inventory (medication_name, category, unit, quantity_in_stock, reorder_level, expiry_date, supplier, status)
            select *
            from (values
                ('Anestezion dental', 'Anestezion', 'ampula', 80, 20, (current_date + interval '10 months')::date, 'Dental Pharma KS', 'Available'),
                ('Gjilpera dentare', 'Material shpenzues', 'cope', 450, 100, (current_date + interval '18 months')::date, 'Dental Supply Prishtina', 'Available'),
                ('Doreza nitrile', 'Higjiene', 'pako', 35, 12, (current_date + interval '24 months')::date, 'MediDent', 'Available'),
                ('Maska kirurgjikale', 'Higjiene', 'pako', 28, 10, (current_date + interval '24 months')::date, 'MediDent', 'Available'),
                ('Kompozit per mbushje', 'Material restaurues', 'shiringa', 18, 8, (current_date + interval '12 months')::date, 'Dental Line', 'Available'),
                ('Bonding dental', 'Material restaurues', 'shishe', 9, 5, (current_date + interval '8 months')::date, 'Dental Line', 'Available'),
                ('Acid etch gel', 'Material restaurues', 'shiringa', 12, 6, (current_date + interval '9 months')::date, 'Dental Line', 'Available'),
                ('Guta percha', 'Endodonci', 'pako', 14, 6, (current_date + interval '20 months')::date, 'EndoCare', 'Available'),
                ('Paper points', 'Endodonci', 'pako', 10, 5, (current_date + interval '20 months')::date, 'EndoCare', 'Available'),
                ('Material per devitalizim', 'Endodonci', 'cope', 7, 5, (current_date + interval '7 months')::date, 'EndoCare', 'Low Stock'),
                ('Cement glass ionomer', 'Material restaurues', 'pako', 6, 4, (current_date + interval '11 months')::date, 'Dental Supply Prishtina', 'Available'),
                ('Alginate per mase', 'Protetike', 'pako', 11, 5, (current_date + interval '14 months')::date, 'ProDent', 'Available'),
                ('Gips dental', 'Protetike', 'kg', 25, 8, (current_date + interval '24 months')::date, 'ProDent', 'Available'),
                ('Disqe polishimi', 'Instrumente', 'pako', 16, 6, (current_date + interval '16 months')::date, 'Dental Line', 'Available'),
                ('Freza dentare', 'Instrumente', 'sete', 5, 4, (current_date + interval '18 months')::date, 'Dental Instruments KS', 'Low Stock'),
                ('Material zbardhimi', 'Estetike dentare', 'sete', 8, 3, (current_date + interval '6 months')::date, 'SmilePro', 'Available')
            ) as seed(medication_name, category, unit, quantity_in_stock, reorder_level, expiry_date, supplier, status)
            where not exists (
                select 1
                from medication_inventory existing
                where lower(existing.medication_name) = lower(seed.medication_name)
            );

            update users
            set username = 'admin@vleradent.com',
                email = 'admin@vleradent.com',
                first_name = 'Admin',
                last_name = 'Vlera Dent',
                updated_at = now()
            where id = '30000000-0000-0000-0000-000000000001';

            update users
            set username = 'vlorentina.sahiti@vleradent.com',
                email = 'vlorentina.sahiti@vleradent.com',
                first_name = 'Vlorentina',
                last_name = 'Sahiti',
                updated_at = now()
            where id = '30000000-0000-0000-0000-000000000002';

            update users
            set username = 'reception@vleradent.com',
                email = 'reception@vleradent.com',
                first_name = 'Reception',
                last_name = 'Vlera Dent',
                updated_at = now()
            where id = '30000000-0000-0000-0000-000000000005';

            update doctors
            set first_name = 'Vlorentina',
                last_name = 'Sahiti',
                specialization = 'Stomatologe',
                email = 'vlorentina.sahiti@vleradent.com',
                status = 'Active',
                updated_at = now()
            where id = '40000000-0000-0000-0000-000000000001';

            update receptionists
            set first_name = 'Reception',
                last_name = 'Vlera Dent',
                email = 'reception@vleradent.com',
                status = 'Active',
                updated_at = now()
            where id = '50000000-0000-0000-0000-000000000001';

            update doctors
            set status = 'Inactive', updated_at = now()
            where id <> '40000000-0000-0000-0000-000000000001';

            update users
            set status = 'Inactive', updated_at = now()
            where id in ('30000000-0000-0000-0000-000000000003','30000000-0000-0000-0000-000000000004');

            update patients
            set assigned_doctor_id = '40000000-0000-0000-0000-000000000001', updated_at = now()
            where assigned_doctor_id is not null;

            update appointments
            set doctor_id = '40000000-0000-0000-0000-000000000001', updated_at = now()
            where doctor_id <> '40000000-0000-0000-0000-000000000001';

            update visits
            set doctor_id = '40000000-0000-0000-0000-000000000001', updated_at = now()
            where doctor_id <> '40000000-0000-0000-0000-000000000001';

            update diagnoses
            set doctor_id = '40000000-0000-0000-0000-000000000001', updated_at = now()
            where doctor_id <> '40000000-0000-0000-0000-000000000001';

            update prescriptions
            set doctor_id = '40000000-0000-0000-0000-000000000001', updated_at = now()
            where doctor_id <> '40000000-0000-0000-0000-000000000001';

            update lab_tests
            set doctor_id = '40000000-0000-0000-0000-000000000001', updated_at = now()
            where doctor_id <> '40000000-0000-0000-0000-000000000001';

            insert into services (id, department_id, service_name, description, price)
            values
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
            on conflict (id) do update set
                department_id = excluded.department_id,
                service_name = excluded.service_name,
                description = excluded.description,
                price = excluded.price,
                updated_at = now();
            """);
    }

    public async Task<AppUser?> GetUserByLoginAsync(string login)
    {
        const string sql = """
            select u.*, r.name as role_name, d.id as doctor_id, rec.id as receptionist_id
            from users u
            join roles r on r.id = u.role_id
            left join doctors d on d.user_id = u.id
            left join receptionists rec on rec.user_id = u.id
            where (lower(u.username) = lower(@Login) or lower(u.email) = lower(@Login))
              and u.status = 'Active'
            limit 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<AppUser>(sql, new { Login = login.Trim() });
    }

    public async Task<long> GetLiveSyncVersionAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>("""
            select coalesce(floor(extract(epoch from max(changed_at)) * 1000)::bigint, 0)
            from (
                select greatest(created_at, updated_at) as changed_at from users
                union all select greatest(created_at, updated_at) from patients
                union all select greatest(created_at, updated_at) from doctors
                union all select greatest(created_at, updated_at) from receptionists
                union all select greatest(created_at, updated_at) from departments
                union all select greatest(created_at, updated_at) from services
                union all select greatest(created_at, updated_at) from appointments
                union all select created_at from appointment_services
                union all select greatest(created_at, updated_at) from visits
                union all select greatest(created_at, updated_at) from prescriptions
                union all select greatest(created_at, updated_at) from lab_tests
                union all select greatest(created_at, updated_at) from payments
                union all select created_at from payment_services
                union all select greatest(created_at, updated_at) from invoices
                union all select greatest(created_at, updated_at) from medication_inventory
                union all select created_at from audit_logs
            ) live_changes;
            """);
    }

    public async Task<AppUser?> GetUserByIdAsync(Guid id)
    {
        const string sql = """
            select u.*, r.name as role_name, d.id as doctor_id, rec.id as receptionist_id
            from users u
            join roles r on r.id = u.role_id
            left join doctors d on d.user_id = u.id
            left join receptionists rec on rec.user_id = u.id
            where u.id = @Id
            limit 1;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<AppUser>(sql, new { Id = id });
    }

    public async Task<string?> CreatePasswordResetTokenAsync(string email)
    {
        var user = await GetUserByLoginAsync(email);
        if (user is null)
        {
            return null;
        }

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", string.Empty)
            .Replace("/", string.Empty)
            .Replace("=", string.Empty);

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            insert into password_reset_tokens (user_id, token, expires_at)
            values (@UserId, @Token, now() + interval '30 minutes');
            """, new { UserId = user.Id, Token = token });

        await AddNotificationAsync(user.Id, "Password reset requested", "A password reset link was generated for your account.", "Security", "/Account/Profile");
        return token;
    }

    public async Task<bool> ResetPasswordAsync(string token, string passwordHash)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var userId = await connection.QueryFirstOrDefaultAsync<Guid?>("""
            select user_id
            from password_reset_tokens
            where token = @Token and used_at is null and expires_at > now()
            limit 1;
            """, new { Token = token }, transaction);

        if (!userId.HasValue)
        {
            return false;
        }

        await connection.ExecuteAsync("update users set password_hash = @PasswordHash, updated_at = now() where id = @UserId;", new { UserId = userId.Value, PasswordHash = passwordHash }, transaction);
        await connection.ExecuteAsync("update password_reset_tokens set used_at = now(), updated_at = now() where token = @Token;", new { Token = token }, transaction);
        transaction.Commit();

        await AddAuditLogAsync(userId.Value, "password reset completed", "users", userId.Value, "Password changed with reset token");
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPasswordHash, Func<string, string, bool> verifyPassword)
    {
        var user = await GetUserByIdAsync(userId);
        if (user is null || !verifyPassword(currentPassword, user.PasswordHash))
        {
            return false;
        }

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("update users set password_hash = @PasswordHash, updated_at = now() where id = @UserId;", new { UserId = userId, PasswordHash = newPasswordHash });
        await AddAuditLogAsync(userId, "password changed", "users", userId, "User changed their password");
        return true;
    }

    public async Task TouchLastLoginAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("update users set last_login_at = now(), updated_at = now() where id = @UserId;", new { UserId = userId });
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var roles = await connection.QueryAsync<Role>("select * from roles order by name;");
        return roles.AsList();
    }

    public async Task<IReadOnlyList<AppUser>> GetUsersAsync()
    {
        const string sql = """
            select u.*, r.name as role_name, d.id as doctor_id, rec.id as receptionist_id
            from users u
            join roles r on r.id = u.role_id
            left join doctors d on d.user_id = u.id
            left join receptionists rec on rec.user_id = u.id
            order by u.created_at desc;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var users = await connection.QueryAsync<AppUser>(sql);
        return users.AsList();
    }

    public async Task<Guid> CreateUserAsync(UserCreateViewModel model, string passwordHash, Guid actorUserId)
    {
        const string sql = """
            insert into users (role_id, username, email, password_hash, first_name, last_name, status)
            values (@RoleId, @Username, @Email, @PasswordHash, @FirstName, @LastName, @Status)
            returning id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var id = await connection.QuerySingleAsync<Guid>(sql, new
        {
            model.RoleId,
            model.Username,
            model.Email,
            PasswordHash = passwordHash,
            model.FirstName,
            model.LastName,
            model.Status
        });

        await AddAuditLogAsync(actorUserId, "user created", "users", id, $"{model.Username} ({model.Email})");
        return id;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(string role, string userName, Guid? doctorId = null)
    {
        using var connection = _connectionFactory.CreateConnection();

        async Task<long> SafeCountAsync(string sql, object? parameters = null)
        {
            try
            {
                return await connection.ExecuteScalarAsync<long>(sql, parameters);
            }
            catch
            {
                return 0;
            }
        }

        async Task<IReadOnlyList<T>> SafeQueryAsync<T>(string sql, object? parameters = null)
        {
            try
            {
                var rows = await connection.QueryAsync<T>(sql, parameters);
                return rows.AsList();
            }
            catch
            {
                return Array.Empty<T>();
            }
        }

        var totalPatients = await SafeCountAsync("select count(*) from patients;");
        var patientsToday = await SafeCountAsync("select count(*) from patients where registration_date = current_date;");
        var totalDoctors = await SafeCountAsync("select count(*) from doctors where status = 'Active';");
        var totalServices = await SafeCountAsync("select count(*) from services;");
        var lowStockItems = await SafeCountAsync("select count(*) from medication_inventory where quantity_in_stock <= reorder_level or status = 'Low Stock';");
        var todayAppointments = await SafeCountAsync("select count(*) from appointments where appointment_date = current_date;");
        var tomorrowAppointments = await SafeCountAsync("select count(*) from appointments where appointment_date = current_date + 1;");
        var pendingPayments = await SafeCountAsync("select count(*) from payments where status = 'Pending' or coalesce(balance_amount, 0) > 0;");
        var completedVisits = await SafeCountAsync("select count(*) from visits where visit_status = 'Completed';");

        var assignedPatientsSql = """
            select p.*, concat(d.first_name, ' ', d.last_name) as assigned_doctor_name, r.room_number as current_room_number
            from patients p
            left join doctors d on d.id = p.assigned_doctor_id
            left join rooms r on r.id = p.current_room_id
            where (@DoctorId::uuid is null or p.assigned_doctor_id = @DoctorId)
            order by p.created_at desc
            limit 6;
            """;

        var appointmentSql = """
            select a.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name, d.specialization as doctor_specialization,
                   svc.service_name, svc.service_price, svc.service_names, svc.services_total
            from appointments a
            join patients p on p.id = a.patient_id
            join doctors d on d.id = a.doctor_id
            """ + AppointmentServicesSelectSql + """
            where a.appointment_date = current_date
              and (@DoctorId::uuid is null or a.doctor_id = @DoctorId)
            order by a.appointment_time
            limit 8;
            """;

        var remindersSql = """
            select a.id, a.appointment_number, a.appointment_date, a.appointment_time,
                   concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name,
                   a.status, a.reason, svc.service_names
            from appointments a
            join patients p on p.id = a.patient_id
            join doctors d on d.id = a.doctor_id
            """ + AppointmentServicesSelectSql + """
            where a.appointment_date between current_date and current_date + 1
              and a.status not in ('Completed', 'Cancelled')
              and (@DoctorId::uuid is null or a.doctor_id = @DoctorId)
            order by a.appointment_date, a.appointment_time
            limit 10;
            """;

        var diagnosesSql = """
            select dg.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name
            from diagnoses dg
            join patients p on p.id = dg.patient_id
            join doctors d on d.id = dg.doctor_id
            where (@DoctorId::uuid is null or dg.doctor_id = @DoctorId)
            order by dg.diagnosis_date desc, dg.created_at desc
            limit 6;
            """;

        var pendingPaymentsSql = """
            select py.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   p.hospital_number, coalesce(svc.service_names, s.service_name) as service_name,
                   svc.service_names
            from payments py
            join patients p on p.id = py.patient_id
            left join services s on s.id = py.service_id
            """ + PaymentServicesSelectSql + """
            where py.status = 'Pending' or py.balance_amount > 0
            order by py.payment_date desc
            limit 6;
            """;

        var activitySql = """
            select al.*, concat(u.first_name, ' ', u.last_name) as user_name
            from audit_logs al
            left join users u on u.id = al.user_id
            order by al.created_at desc
            limit 8;
            """;

        var recentPatients = await SafeQueryAsync<Patient>(assignedPatientsSql, new { DoctorId = doctorId });
        var appointments = await SafeQueryAsync<Appointment>(appointmentSql, new { DoctorId = role == AppRoles.Doctor ? doctorId : null });
        var reminders = await SafeQueryAsync<AppointmentReminderItem>(remindersSql, new { DoctorId = role == AppRoles.Doctor ? doctorId : null });
        var doctors = await SafeQueryAsync<Doctor>(DoctorSelectSql + " where d.status = 'Active' order by d.first_name limit 8;");
        var diagnoses = await SafeQueryAsync<Diagnosis>(diagnosesSql, new { DoctorId = role == AppRoles.Doctor ? doctorId : null });
        var payments = await SafeQueryAsync<Payment>(pendingPaymentsSql);
        var activity = await SafeQueryAsync<AuditLog>(activitySql);

        var metrics = new List<DashboardMetric>
        {
            new() { Label = "Total patients", Value = totalPatients.ToString("N0"), Icon = "bi-people", Accent = "primary", Caption = "All registered records" },
            new() { Label = "Registered today", Value = patientsToday.ToString("N0"), Icon = "bi-person-plus", Accent = "success", Caption = "New intake" },
            new() { Label = "Total dentists", Value = totalDoctors.ToString("N0"), Icon = "bi-heart-pulse", Accent = "info", Caption = "Active dental staff" },
            new() { Label = "Dental services", Value = totalServices.ToString("N0"), Icon = "bi-stars", Accent = "success", Caption = "Treatment catalog" },
            new() { Label = "Low stock items", Value = lowStockItems.ToString("N0"), Icon = "bi-box-seam", Accent = "warning", Caption = "Inventory attention" },
            new() { Label = "Today appointments", Value = todayAppointments.ToString("N0"), Icon = "bi-calendar2-check", Accent = "primary", Caption = "Scheduled for today" },
            new() { Label = "Tomorrow reminders", Value = tomorrowAppointments.ToString("N0"), Icon = "bi-bell", Accent = "warning", Caption = "Upcoming appointments" },
            new() { Label = "Pending payments", Value = pendingPayments.ToString("N0"), Icon = "bi-credit-card", Accent = "danger", Caption = "Needs billing follow-up" },
            new() { Label = "Completed visits", Value = completedVisits.ToString("N0"), Icon = "bi-check2-circle", Accent = "success", Caption = "Closed clinical work" }
        };

        if (role == AppRoles.Doctor)
        {
            var assignedCount = doctorId.HasValue
                ? await SafeCountAsync("select count(*) from patients where assigned_doctor_id = @DoctorId;", new { DoctorId = doctorId })
                : 0;
            var pendingVisits = doctorId.HasValue
                ? await SafeCountAsync("select count(*) from appointments where doctor_id = @DoctorId and status in ('Waiting', 'In Progress');", new { DoctorId = doctorId })
                : 0;
            metrics[0] = new DashboardMetric { Label = "Assigned patients", Value = assignedCount.ToString("N0"), Icon = "bi-person-lines-fill", Accent = "primary", Caption = "Your active panel" };
            metrics[7] = new DashboardMetric { Label = "Pending visits", Value = pendingVisits.ToString("N0"), Icon = "bi-hourglass-split", Accent = "warning", Caption = "Waiting or in progress" };
        }

        return new DashboardViewModel
        {
            Role = role,
            UserName = userName,
            Metrics = metrics,
            RecentPatients = recentPatients,
            TodayAppointments = appointments,
            AvailableDoctors = doctors,
            AvailableRooms = Array.Empty<Room>(),
            RecentDiagnoses = diagnoses,
            PendingPayments = payments,
            RecentActivity = activity,
            AppointmentReminders = reminders
        };
    }

    public async Task<(IReadOnlyList<Patient> Patients, int TotalCount)> GetPatientsAsync(
        string? search,
        string? status,
        Guid? doctorId,
        Guid? roomId,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var offset = (page - 1) * pageSize;

        var where = """
            where (@Search is null or
                   p.first_name ilike '%' || @Search || '%' or
                   p.last_name ilike '%' || @Search || '%' or
                   concat_ws(' ', p.first_name, p.last_name) ilike '%' || @Search || '%' or
                   concat_ws(' ', p.last_name, p.first_name) ilike '%' || @Search || '%' or
                   p.hospital_number ilike '%' || @Search || '%' or
                   p.personal_number ilike '%' || @Search || '%' or
                   p.phone ilike '%' || @Search || '%' or
                   p.email ilike '%' || @Search || '%' or
                   (@SearchDigits is not null and regexp_replace(coalesce(p.phone, ''), '[^0-9]', '', 'g') ilike '%' || @SearchDigits || '%'))
              and (@Status is null or p.status = @Status)
              and (@DoctorId::uuid is null or p.assigned_doctor_id = @DoctorId)
              and (@RoomId::uuid is null or p.current_room_id = @RoomId)
            """;

        var listSql = $"""
            select p.*, concat(d.first_name, ' ', d.last_name) as assigned_doctor_name, r.room_number as current_room_number
            from patients p
            left join doctors d on d.id = p.assigned_doctor_id
            left join rooms r on r.id = p.current_room_id
            {where}
            order by p.created_at desc
            limit @PageSize offset @Offset;
            """;

        var countSql = $"""
            select count(*)
            from patients p
            left join doctors d on d.id = p.assigned_doctor_id
            left join rooms r on r.id = p.current_room_id
            {where};
            """;

        var parameters = new
        {
            Search = Clean(search),
            SearchDigits = DigitsOnly(search),
            Status = Clean(status),
            DoctorId = doctorId,
            RoomId = roomId,
            PageSize = pageSize,
            Offset = offset
        };

        using var connection = _connectionFactory.CreateConnection();
        var patients = await connection.QueryAsync<Patient>(listSql, parameters);
        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        return (patients.AsList(), total);
    }

    public async Task<IReadOnlyList<Patient>> GetPatientOptionsAsync(Guid? doctorId = null)
    {
        const string sql = """
            select p.*, concat(d.first_name, ' ', d.last_name) as assigned_doctor_name, r.room_number as current_room_number
            from patients p
            left join doctors d on d.id = p.assigned_doctor_id
            left join rooms r on r.id = p.current_room_id
            where (@DoctorId::uuid is null or p.assigned_doctor_id = @DoctorId)
            order by p.first_name, p.last_name
            limit 500;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var patients = await connection.QueryAsync<Patient>(sql, new { DoctorId = doctorId });
        return patients.AsList();
    }

    public async Task<Patient?> GetPatientAsync(Guid id)
    {
        const string sql = """
            select p.*, concat(d.first_name, ' ', d.last_name) as assigned_doctor_name, r.room_number as current_room_number
            from patients p
            left join doctors d on d.id = p.assigned_doctor_id
            left join rooms r on r.id = p.current_room_id
            where p.id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Patient>(sql, new { Id = id });
    }

    public async Task<PatientDetailsViewModel?> GetPatientDetailsAsync(Guid id)
    {
        var patient = await GetPatientAsync(id);
        if (patient is null)
        {
            return null;
        }

        using var connection = _connectionFactory.CreateConnection();

        async Task<IReadOnlyList<T>> SafeQueryAsync<T>(string sql, object? parameters = null)
        {
            try
            {
                var rows = await connection.QueryAsync<T>(sql, parameters);
                return rows.AsList();
            }
            catch
            {
                return Array.Empty<T>();
            }
        }

        async Task<IReadOnlyList<Payment>> SafePaymentHistoryAsync()
        {
            try
            {
                var rows = await connection.QueryAsync<Payment>("""
                    select py.*, concat(p.first_name, ' ', p.last_name) as patient_name, p.hospital_number,
                           coalesce(svc.service_names, s.service_name) as service_name,
                           svc.service_names
                    from payments py
                    join patients p on p.id = py.patient_id
                    left join services s on s.id = py.service_id
                    """ + PaymentServicesSelectSql + """
                    where py.patient_id = @Id order by py.payment_date desc;
                    """, new { Id = id });
                return rows.AsList();
            }
            catch
            {
                var rows = await connection.QueryAsync<Payment>("""
                    select py.*, concat(p.first_name, ' ', p.last_name) as patient_name, p.hospital_number, s.service_name
                    from payments py
                    join patients p on p.id = py.patient_id
                    join services s on s.id = py.service_id
                    where py.patient_id = @Id order by py.payment_date desc;
                    """, new { Id = id });
                return rows.AsList();
            }
        }

        var visits = await SafeQueryAsync<Visit>("""
            select v.*, concat(p.first_name, ' ', p.last_name) as patient_name, concat(d.first_name, ' ', d.last_name) as doctor_name
            from visits v join patients p on p.id = v.patient_id join doctors d on d.id = v.doctor_id
            where v.patient_id = @Id order by v.visit_date desc;
            """, new { Id = id });
        var diagnoses = await SafeQueryAsync<Diagnosis>("""
            select dg.*, concat(p.first_name, ' ', p.last_name) as patient_name, concat(d.first_name, ' ', d.last_name) as doctor_name
            from diagnoses dg join patients p on p.id = dg.patient_id join doctors d on d.id = dg.doctor_id
            where dg.patient_id = @Id order by dg.diagnosis_date desc, dg.created_at desc;
            """, new { Id = id });
        var prescriptions = await SafeQueryAsync<Prescription>("""
            select pr.*, concat(p.first_name, ' ', p.last_name) as patient_name, concat(d.first_name, ' ', d.last_name) as doctor_name, d.license_number as doctor_license_number
            from prescriptions pr join patients p on p.id = pr.patient_id join doctors d on d.id = pr.doctor_id
            where pr.patient_id = @Id order by pr.prescription_date desc, pr.created_at desc;
            """, new { Id = id });
        var payments = await SafePaymentHistoryAsync();
        var roomAssignments = await SafeQueryAsync<PatientRoomAssignment>("""
            select pra.*, concat(p.first_name, ' ', p.last_name) as patient_name, r.room_number, r.room_type
            from patient_room_assignments pra join patients p on p.id = pra.patient_id join rooms r on r.id = pra.room_id
            where pra.patient_id = @Id order by pra.admission_date desc;
            """, new { Id = id });
        var labTests = await SafeQueryAsync<LabTest>("""
            select lt.*, concat(p.first_name, ' ', p.last_name) as patient_name, concat(d.first_name, ' ', d.last_name) as doctor_name
            from lab_tests lt join patients p on p.id = lt.patient_id join doctors d on d.id = lt.doctor_id
            where lt.patient_id = @Id order by lt.requested_date desc, lt.created_at desc;
            """, new { Id = id });

        return new PatientDetailsViewModel
        {
            Patient = patient,
            Visits = visits.AsList(),
            Diagnoses = diagnoses.AsList(),
            Prescriptions = prescriptions.AsList(),
            Payments = payments.AsList(),
            RoomAssignments = roomAssignments.AsList(),
            LabTests = labTests.AsList()
        };
    }

    public async Task<Guid> CreatePatientAsync(Patient patient, Guid actorUserId)
    {
        const string sql = """
            insert into patients
            (assigned_doctor_id, first_name, last_name, date_of_birth, gender, personal_number, phone, email, address,
             emergency_contact_name, emergency_contact_phone, blood_type, allergies, chronic_diseases, registration_date, status)
            values
            (@AssignedDoctorId, @FirstName, @LastName, @DateOfBirth, @Gender, @PersonalNumber, @Phone, @Email, @Address,
             @EmergencyContactName, @EmergencyContactPhone, @BloodType, @Allergies, @ChronicDiseases, @RegistrationDate, @Status)
            returning id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var id = await connection.QuerySingleAsync<Guid>(sql, patient);
        await AddAuditLogAsync(actorUserId, "patient created", "patients", id, patient.FullName);
        return id;
    }

    public async Task UpdatePatientAsync(Patient patient, Guid actorUserId)
    {
        const string sql = """
            update patients set
                assigned_doctor_id = @AssignedDoctorId,
                first_name = @FirstName,
                last_name = @LastName,
                date_of_birth = @DateOfBirth,
                gender = @Gender,
                personal_number = @PersonalNumber,
                phone = @Phone,
                email = @Email,
                address = @Address,
                emergency_contact_name = @EmergencyContactName,
                emergency_contact_phone = @EmergencyContactPhone,
                blood_type = @BloodType,
                allergies = @Allergies,
                chronic_diseases = @ChronicDiseases,
                registration_date = @RegistrationDate,
                status = @Status,
                updated_at = now()
            where id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, patient);
        await AddAuditLogAsync(actorUserId, "patient edited", "patients", patient.Id, patient.FullName);
    }

    public async Task DeletePatientAsync(Guid id, Guid actorUserId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("delete from patients where id = @Id;", new { Id = id });
        await AddAuditLogAsync(actorUserId, "patient deleted", "patients", id, "Deleted patient record");
    }

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(bool activeOnly = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<Department>(
            "select * from departments where (@ActiveOnly = false or status = 'Active') order by name;",
            new { ActiveOnly = activeOnly });
        return rows.AsList();
    }

    public async Task<Department?> GetDepartmentAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Department>("select * from departments where id = @Id;", new { Id = id });
    }

    public async Task<Guid> CreateDepartmentAsync(Department department)
    {
        const string sql = """
            insert into departments (name, description, location, status)
            values (@Name, @Description, @Location, @Status)
            returning id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Guid>(sql, department);
    }

    public async Task UpdateDepartmentAsync(Department department)
    {
        const string sql = """
            update departments set name = @Name, description = @Description, location = @Location,
                status = @Status, updated_at = now()
            where id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, department);
    }

    public async Task<IReadOnlyList<Doctor>> GetDoctorsAsync(bool activeOnly = false)
    {
        var sql = DoctorSelectSql + " where (@ActiveOnly = false or d.status = 'Active') order by d.first_name, d.last_name;";
        using var connection = _connectionFactory.CreateConnection();
        var doctors = await connection.QueryAsync<Doctor>(sql, new { ActiveOnly = activeOnly });
        return doctors.AsList();
    }

    public async Task<Doctor?> GetDoctorAsync(Guid id)
    {
        var sql = DoctorSelectSql + " where d.id = @Id;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Doctor>(sql, new { Id = id });
    }

    public async Task<Guid> CreateDoctorAsync(Doctor doctor, Guid actorUserId)
    {
        const string sql = """
            insert into doctors (user_id, department_id, first_name, last_name, specialization, phone, email, license_number, working_schedule, status)
            values (@UserId, @DepartmentId, @FirstName, @LastName, @Specialization, @Phone, @Email, @LicenseNumber, @WorkingSchedule, @Status)
            returning id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var id = await connection.QuerySingleAsync<Guid>(sql, doctor);
        await AddAuditLogAsync(actorUserId, "doctor created", "doctors", id, doctor.FullName);
        return id;
    }

    public async Task UpdateDoctorAsync(Doctor doctor, Guid actorUserId)
    {
        const string sql = """
            update doctors set user_id = @UserId, department_id = @DepartmentId, first_name = @FirstName, last_name = @LastName,
                specialization = @Specialization, phone = @Phone, email = @Email, license_number = @LicenseNumber,
                working_schedule = @WorkingSchedule, status = @Status, updated_at = now()
            where id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, doctor);
        await AddAuditLogAsync(actorUserId, "doctor edited", "doctors", doctor.Id, doctor.FullName);
    }

    public async Task DeleteDoctorAsync(Guid id, Guid actorUserId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("delete from doctors where id = @Id;", new { Id = id });
        await AddAuditLogAsync(actorUserId, "doctor deleted", "doctors", id, "Deleted doctor profile");
    }

    public async Task<IReadOnlyList<Receptionist>> GetReceptionistsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<Receptionist>("select * from receptionists order by first_name, last_name;");
        return rows.AsList();
    }

    public async Task<Receptionist?> GetReceptionistAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Receptionist>("select * from receptionists where id = @Id;", new { Id = id });
    }

    public async Task<Guid> CreateReceptionistAsync(Receptionist receptionist, Guid actorUserId)
    {
        const string sql = """
            insert into receptionists (user_id, first_name, last_name, phone, email, shift, status)
            values (@UserId, @FirstName, @LastName, @Phone, @Email, @Shift, @Status)
            returning id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var id = await connection.QuerySingleAsync<Guid>(sql, receptionist);
        await AddAuditLogAsync(actorUserId, "receptionist created", "receptionists", id, receptionist.FullName);
        return id;
    }

    public async Task UpdateReceptionistAsync(Receptionist receptionist, Guid actorUserId)
    {
        const string sql = """
            update receptionists set user_id = @UserId, first_name = @FirstName, last_name = @LastName,
                phone = @Phone, email = @Email, shift = @Shift, status = @Status, updated_at = now()
            where id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, receptionist);
        await AddAuditLogAsync(actorUserId, "receptionist edited", "receptionists", receptionist.Id, receptionist.FullName);
    }

    public async Task<IReadOnlyList<Room>> GetRoomsAsync(bool onlyAvailable = false)
    {
        var sql = RoomSelectSql + """
             where (@OnlyAvailable = false or (r.status = 'Available' and r.current_occupancy < r.capacity))
             order by r.floor, r.room_number;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var rooms = await connection.QueryAsync<Room>(sql, new { OnlyAvailable = onlyAvailable });
        return rooms.AsList();
    }

    public async Task<Room?> GetRoomAsync(Guid id)
    {
        var sql = RoomSelectSql + " where r.id = @Id;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Room>(sql, new { Id = id });
    }

    public async Task<Guid> CreateRoomAsync(Room room)
    {
        const string sql = """
            insert into rooms (department_id, room_number, floor, room_type, capacity, current_occupancy, status, price_per_day)
            values (@DepartmentId, @RoomNumber, @Floor, @RoomType, @Capacity, @CurrentOccupancy, @Status, @PricePerDay)
            returning id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Guid>(sql, room);
    }

    public async Task UpdateRoomAsync(Room room)
    {
        const string sql = """
            update rooms set department_id = @DepartmentId, room_number = @RoomNumber, floor = @Floor, room_type = @RoomType,
                capacity = @Capacity, current_occupancy = @CurrentOccupancy, status = @Status, price_per_day = @PricePerDay,
                updated_at = now()
            where id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, room);
    }

    public async Task<Guid> AssignRoomAsync(PatientRoomAssignment assignment, Guid actorUserId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var room = await connection.QuerySingleOrDefaultAsync<Room>(
            "select * from rooms where id = @RoomId for update;",
            new { assignment.RoomId },
            transaction);

        if (room is null)
        {
            throw new InvalidOperationException("Selected room was not found.");
        }

        if (room.Status == "Maintenance" || room.CurrentOccupancy >= room.Capacity)
        {
            throw new InvalidOperationException("This room has no free capacity.");
        }

        var id = await connection.QuerySingleAsync<Guid>("""
            insert into patient_room_assignments (patient_id, room_id, admission_date, expected_discharge_date, actual_discharge_date, notes)
            values (@PatientId, @RoomId, @AdmissionDate, @ExpectedDischargeDate, @ActualDischargeDate, @Notes)
            returning id;
            """, assignment, transaction);

        await connection.ExecuteAsync("""
            update rooms
            set current_occupancy = current_occupancy + 1,
                status = case when current_occupancy + 1 >= capacity then 'Occupied' else 'Available' end,
                updated_at = now()
            where id = @RoomId;
            update patients set current_room_id = @RoomId, status = 'Admitted', updated_at = now() where id = @PatientId;
            """, assignment, transaction);

        transaction.Commit();
        await AddAuditLogAsync(actorUserId, "room assigned", "patient_room_assignments", id, $"Patient {assignment.PatientId} assigned to room {assignment.RoomId}");
        return id;
    }

    public async Task TransferPatientRoomAsync(RoomTransferViewModel model, Guid actorUserId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var patient = await connection.QuerySingleOrDefaultAsync<Patient>("select * from patients where id = @PatientId for update;", model, transaction)
            ?? throw new InvalidOperationException("Patient was not found.");

        if (!patient.CurrentRoomId.HasValue)
        {
            throw new InvalidOperationException("Patient is not currently admitted to a room.");
        }

        var newRoom = await connection.QuerySingleOrDefaultAsync<Room>("select * from rooms where id = @NewRoomId for update;", model, transaction)
            ?? throw new InvalidOperationException("Selected room was not found.");

        if (newRoom.Status == "Maintenance" || newRoom.CurrentOccupancy >= newRoom.Capacity)
        {
            throw new InvalidOperationException("Selected room has no free capacity.");
        }

        await connection.ExecuteAsync("""
            update patient_room_assignments
            set actual_discharge_date = @TransferDate, notes = concat(coalesce(notes, ''), E'\nTransfer: ', coalesce(@Notes, '')), updated_at = now()
            where patient_id = @PatientId and actual_discharge_date is null;

            update rooms
            set current_occupancy = greatest(current_occupancy - 1, 0),
                status = case when greatest(current_occupancy - 1, 0) = 0 then 'Available' else status end,
                updated_at = now()
            where id = @OldRoomId;

            insert into patient_room_assignments (patient_id, room_id, admission_date, notes)
            values (@PatientId, @NewRoomId, @TransferDate, @Notes);

            update rooms
            set current_occupancy = current_occupancy + 1,
                status = case when current_occupancy + 1 >= capacity then 'Occupied' else 'Available' end,
                updated_at = now()
            where id = @NewRoomId;

            update patients
            set current_room_id = @NewRoomId, status = 'Admitted', updated_at = now()
            where id = @PatientId;
            """, new { model.PatientId, model.NewRoomId, model.TransferDate, model.Notes, OldRoomId = patient.CurrentRoomId.Value }, transaction);

        transaction.Commit();
        await AddAuditLogAsync(actorUserId, "room transfer", "patients", model.PatientId, $"Transferred to room {model.NewRoomId}");
    }

    public async Task DischargePatientAsync(PatientDischargeViewModel model, Guid actorUserId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var patient = await connection.QuerySingleOrDefaultAsync<Patient>("select * from patients where id = @PatientId for update;", model, transaction)
            ?? throw new InvalidOperationException("Patient was not found.");

        if (!patient.CurrentRoomId.HasValue)
        {
            throw new InvalidOperationException("Patient is not assigned to a room.");
        }

        await connection.ExecuteAsync("""
            update patient_room_assignments
            set actual_discharge_date = @DischargeDate, notes = concat(coalesce(notes, ''), E'\nDischarge: ', coalesce(@Notes, '')), updated_at = now()
            where patient_id = @PatientId and actual_discharge_date is null;

            update rooms
            set current_occupancy = greatest(current_occupancy - 1, 0),
                status = case when greatest(current_occupancy - 1, 0) = 0 then 'Available' else status end,
                updated_at = now()
            where id = @RoomId;

            update patients
            set current_room_id = null, status = 'Discharged', updated_at = now()
            where id = @PatientId;
            """, new { model.PatientId, model.DischargeDate, model.Notes, RoomId = patient.CurrentRoomId.Value }, transaction);

        transaction.Commit();
        await AddAuditLogAsync(actorUserId, "patient discharged", "patients", model.PatientId, model.Notes ?? "Discharged from room");
    }

    public async Task<AppointmentListViewModel> GetAppointmentsAsync(Guid? doctorId, DateTime? date, string? status)
    {
        const string multiServiceSql = """
            select a.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name, d.specialization as doctor_specialization,
                   svc.service_name, svc.service_price, svc.service_names, svc.services_total
            from appointments a
            join patients p on p.id = a.patient_id
            join doctors d on d.id = a.doctor_id
            """ + AppointmentServicesSelectSql + """
            where (@DoctorId::uuid is null or a.doctor_id = @DoctorId)
              and (@Date::date is null or a.appointment_date = @Date)
              and (@Status is null or a.status = @Status)
            order by a.appointment_date desc, a.appointment_time desc;
            """;

        const string fallbackSql = """
            select a.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name, d.specialization as doctor_specialization,
                   s.service_name, s.price as service_price, s.service_name as service_names, s.price as services_total
            from appointments a
            join patients p on p.id = a.patient_id
            join doctors d on d.id = a.doctor_id
            left join services s on s.id = a.service_id
            where (@DoctorId::uuid is null or a.doctor_id = @DoctorId)
              and (@Date::date is null or a.appointment_date = @Date)
              and (@Status is null or a.status = @Status)
            order by a.appointment_date desc, a.appointment_time desc;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new
        {
            DoctorId = doctorId,
            Date = date?.Date,
            Status = Clean(status)
        };
        var appointments = await QueryAppointmentsWithFallbackAsync(connection, multiServiceSql, fallbackSql, parameters);

        return new AppointmentListViewModel
        {
            Appointments = appointments,
            Doctors = await GetDoctorsAsync(true),
            DoctorId = doctorId,
            Date = date,
            Status = status
        };
    }

    public async Task<Appointment?> GetAppointmentAsync(Guid id)
    {
        const string multiServiceSql = """
            select a.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name, d.specialization as doctor_specialization,
                   svc.service_name, svc.service_price, svc.service_names, svc.services_total
            from appointments a
            join patients p on p.id = a.patient_id
            join doctors d on d.id = a.doctor_id
            """ + AppointmentServicesSelectSql + """
            where a.id = @Id;
            """;

        const string fallbackSql = """
            select a.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name, d.specialization as doctor_specialization,
                   s.service_name, s.price as service_price, s.service_name as service_names, s.price as services_total
            from appointments a
            join patients p on p.id = a.patient_id
            join doctors d on d.id = a.doctor_id
            left join services s on s.id = a.service_id
            where a.id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var appointments = await QueryAppointmentsWithFallbackAsync(connection, multiServiceSql, fallbackSql, new { Id = id });
        return appointments.FirstOrDefault();
    }

    public async Task<IReadOnlyList<Guid>> GetAppointmentServiceIdsAsync(Guid appointmentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var hasAppointmentServices = await connection.ExecuteScalarAsync<bool>("select to_regclass('public.appointment_services') is not null;");
        if (hasAppointmentServices)
        {
            var serviceIds = await connection.QueryAsync<Guid>("""
                select service_id
                from appointment_services
                where appointment_id = @AppointmentId
                order by created_at;
                """, new { AppointmentId = appointmentId });

            var selected = serviceIds.AsList();
            if (selected.Count > 0)
            {
                return selected;
            }
        }

        var fallbackServiceId = await connection.ExecuteScalarAsync<Guid?>("select service_id from appointments where id = @AppointmentId;", new { AppointmentId = appointmentId });
        return fallbackServiceId.HasValue ? [fallbackServiceId.Value] : [];
    }

    public async Task<Guid> CreateAppointmentAsync(Appointment appointment, IReadOnlyCollection<Guid> serviceIds, Guid actorUserId)
    {
        var selectedServiceIds = serviceIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (selectedServiceIds.Length == 0)
        {
            throw new InvalidOperationException("Select at least one service for the appointment.");
        }

        appointment.ServiceId = selectedServiceIds[0];

        using var connection = _connectionFactory.CreateConnection();
        var conflict = await connection.ExecuteScalarAsync<int>("""
            select count(*)
            from appointments
            where doctor_id = @DoctorId
              and appointment_date = @AppointmentDate
              and appointment_time = @AppointmentTime
              and status <> 'Cancelled';
            """, appointment);

        if (conflict > 0)
        {
            throw new InvalidOperationException("This doctor already has an active appointment at that time.");
        }

        var existingServices = await connection.ExecuteScalarAsync<int>("""
            select count(*)
            from services
            where id = any(@ServiceIds);
            """, new { ServiceIds = selectedServiceIds });

        if (existingServices != selectedServiceIds.Length)
        {
            throw new InvalidOperationException("One or more selected services were not found.");
        }

        const string sql = """
            insert into appointments (patient_id, doctor_id, service_id, appointment_date, appointment_time, reason, status, notes)
            values (@PatientId, @DoctorId, @ServiceId, @AppointmentDate, @AppointmentTime, @Reason, @Status, @Notes)
            returning id;
            """;

        connection.Open();
        using var transaction = connection.BeginTransaction();

        var id = await connection.QuerySingleAsync<Guid>(sql, appointment, transaction);
        var hasAppointmentServices = await connection.ExecuteScalarAsync<bool>("select to_regclass('public.appointment_services') is not null;", transaction: transaction);
        if (hasAppointmentServices)
        {
            await connection.ExecuteAsync("""
                insert into appointment_services (appointment_id, service_id)
                select @AppointmentId, unnest(@ServiceIds::uuid[])
                on conflict (appointment_id, service_id) do nothing;
                """, new { AppointmentId = id, ServiceIds = selectedServiceIds }, transaction);
        }

        await UpsertPlannedVisitForAppointmentAsync(connection, transaction, id, appointment.Status);

        transaction.Commit();
        await AddAuditLogAsync(actorUserId, "appointment created", "appointments", id, appointment.Reason);
        return id;
    }

    public async Task UpdateAppointmentAsync(Appointment appointment, IReadOnlyCollection<Guid> serviceIds, Guid actorUserId)
    {
        var selectedServiceIds = serviceIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (selectedServiceIds.Length == 0)
        {
            throw new InvalidOperationException("Select at least one service for the appointment.");
        }

        appointment.ServiceId = selectedServiceIds[0];

        using var connection = _connectionFactory.CreateConnection();
        var conflict = await connection.ExecuteScalarAsync<int>("""
            select count(*)
            from appointments
            where id <> @Id
              and doctor_id = @DoctorId
              and appointment_date = @AppointmentDate
              and appointment_time = @AppointmentTime
              and status <> 'Cancelled';
            """, appointment);

        if (conflict > 0)
        {
            throw new InvalidOperationException("This doctor already has an active appointment at that time.");
        }

        var existingServices = await connection.ExecuteScalarAsync<int>("""
            select count(*)
            from services
            where id = any(@ServiceIds);
            """, new { ServiceIds = selectedServiceIds });

        if (existingServices != selectedServiceIds.Length)
        {
            throw new InvalidOperationException("One or more selected services were not found.");
        }

        connection.Open();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync("""
            update appointments
            set patient_id = @PatientId,
                doctor_id = @DoctorId,
                service_id = @ServiceId,
                appointment_date = @AppointmentDate,
                appointment_time = @AppointmentTime,
                reason = @Reason,
                status = @Status,
                notes = @Notes,
                updated_at = now()
            where id = @Id;
            """, appointment, transaction);

        var hasAppointmentServices = await connection.ExecuteScalarAsync<bool>("select to_regclass('public.appointment_services') is not null;", transaction: transaction);
        if (hasAppointmentServices)
        {
            await connection.ExecuteAsync("delete from appointment_services where appointment_id = @AppointmentId;", new { AppointmentId = appointment.Id }, transaction);
            await connection.ExecuteAsync("""
                insert into appointment_services (appointment_id, service_id)
                select @AppointmentId, unnest(@ServiceIds::uuid[])
                on conflict (appointment_id, service_id) do nothing;
                """, new { AppointmentId = appointment.Id, ServiceIds = selectedServiceIds }, transaction);
        }

        await UpsertPlannedVisitForAppointmentAsync(connection, transaction, appointment.Id, appointment.Status);

        transaction.Commit();
        await AddAuditLogAsync(actorUserId, "appointment edited", "appointments", appointment.Id, $"{appointment.AppointmentDate:yyyy-MM-dd} {appointment.AppointmentTime:hh\\:mm}");
    }

    public async Task<IReadOnlyList<Appointment>> GetAppointmentCalendarAsync(Guid? doctorId, DateTime start, DateTime end)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string multiServiceSql = """
            select a.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name, d.specialization as doctor_specialization,
                   svc.service_name, svc.service_price, svc.service_names, svc.services_total
            from appointments a
            join patients p on p.id = a.patient_id
            join doctors d on d.id = a.doctor_id
            """ + AppointmentServicesSelectSql + """
            where a.appointment_date between @Start and @End
              and (@DoctorId::uuid is null or a.doctor_id = @DoctorId)
            order by a.appointment_date, a.appointment_time;
            """;

        const string fallbackSql = """
            select a.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name, d.specialization as doctor_specialization,
                   s.service_name, s.price as service_price, s.service_name as service_names, s.price as services_total
            from appointments a
            join patients p on p.id = a.patient_id
            join doctors d on d.id = a.doctor_id
            left join services s on s.id = a.service_id
            where a.appointment_date between @Start and @End
              and (@DoctorId::uuid is null or a.doctor_id = @DoctorId)
            order by a.appointment_date, a.appointment_time;
            """;

        return await QueryAppointmentsWithFallbackAsync(connection, multiServiceSql, fallbackSql, new { DoctorId = doctorId, Start = start.Date, End = end.Date });
    }

    public async Task UpdateAppointmentStatusAsync(Guid id, string status, Guid actorUserId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync("update appointments set status = @Status, updated_at = now() where id = @Id;", new { Id = id, Status = status }, transaction);
        await UpsertPlannedVisitForAppointmentAsync(connection, transaction, id, status);
        transaction.Commit();
        await AddAuditLogAsync(actorUserId, "appointment status updated", "appointments", id, status);
    }

    public async Task<Guid> CreateVisitAsync(Visit visit, Guid actorUserId)
    {
        const string sql = """
            insert into visits (patient_id, doctor_id, appointment_id, visit_date, symptoms, diagnosis, disease, treatment_plan, notes, follow_up_date, visit_status)
            values (@PatientId, @DoctorId, @AppointmentId, @VisitDate, @Symptoms, @Diagnosis, @Disease, @TreatmentPlan, @Notes, @FollowUpDate, @VisitStatus)
            returning id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var id = await connection.QuerySingleAsync<Guid>(sql, visit);

        if (visit.AppointmentId.HasValue && visit.VisitStatus == "Completed")
        {
            await connection.ExecuteAsync("update appointments set status = 'Completed', updated_at = now() where id = @AppointmentId;", visit);
        }

        await AddAuditLogAsync(actorUserId, "visit created", "visits", id, visit.Diagnosis);
        return id;
    }

    private static async Task UpsertPlannedVisitForAppointmentAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        Guid appointmentId,
        string? status)
    {
        await connection.ExecuteAsync("""
            with appointment_context as (
                select a.id,
                       a.patient_id,
                       a.doctor_id,
                       (a.appointment_date + a.appointment_time) as visit_date,
                       coalesce(nullif(a.reason, ''), nullif(svc.service_names, ''), 'Termin i planifikuar') as symptoms,
                       coalesce(nullif(svc.service_names, ''), nullif(a.reason, ''), 'Trajtim dentar') as diagnosis,
                       coalesce(nullif(svc.service_names, ''), nullif(a.reason, ''), 'Trajtim dentar') as disease,
                       coalesce(nullif(svc.service_names, ''), nullif(a.reason, ''), 'Trajtim dentar') as treatment_plan,
                       a.notes,
                       coalesce(@Status, a.status, 'Scheduled') as visit_status
                from appointments a
                left join lateral (
                    select string_agg(s.service_name, ', ' order by s.service_name) as service_names
                    from (
                        select aps.service_id
                        from appointment_services aps
                        where aps.appointment_id = a.id
                        union
                        select a.service_id
                        where a.service_id is not null
                    ) selected_services
                    join services s on s.id = selected_services.service_id
                ) svc on true
                where a.id = @AppointmentId
            ),
            updated as (
                update visits v
                set patient_id = ac.patient_id,
                    doctor_id = ac.doctor_id,
                    visit_date = ac.visit_date,
                    symptoms = ac.symptoms,
                    diagnosis = ac.diagnosis,
                    disease = ac.disease,
                    treatment_plan = ac.treatment_plan,
                    notes = ac.notes,
                    visit_status = ac.visit_status,
                    updated_at = now()
                from appointment_context ac
                where v.appointment_id = ac.id
                returning v.id
            )
            insert into visits (patient_id, doctor_id, appointment_id, visit_date, symptoms, diagnosis, disease, treatment_plan, notes, visit_status)
            select ac.patient_id, ac.doctor_id, ac.id, ac.visit_date, ac.symptoms, ac.diagnosis, ac.disease, ac.treatment_plan, ac.notes, ac.visit_status
            from appointment_context ac
            where not exists (select 1 from updated);
            """, new { AppointmentId = appointmentId, Status = status }, transaction);
    }

    public async Task<IReadOnlyList<Disease>> GetDiseasesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var diseases = await connection.QueryAsync<Disease>("select * from diseases order by disease_name;");
        return diseases.AsList();
    }

    public async Task<Guid> CreateDiseaseAsync(Disease disease)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Guid>("""
            insert into diseases (disease_name, category, description, common_symptoms)
            values (@DiseaseName, @Category, @Description, @CommonSymptoms)
            returning id;
            """, disease);
    }

    public async Task<Guid> CreateDiagnosisAsync(Diagnosis diagnosis, Guid actorUserId)
    {
        const string sql = """
            insert into diagnoses (patient_id, doctor_id, disease_id, disease_name, icd_code, severity, description, diagnosis_date, treatment_recommendation)
            values (@PatientId, @DoctorId, @DiseaseId, @DiseaseName, @IcdCode, @Severity, @Description, @DiagnosisDate, @TreatmentRecommendation)
            returning id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var id = await connection.QuerySingleAsync<Guid>(sql, diagnosis);
        await AddAuditLogAsync(actorUserId, "diagnosis added", "diagnoses", id, diagnosis.DiseaseName);
        return id;
    }

    public async Task<Guid> CreatePrescriptionAsync(Prescription prescription, Guid actorUserId)
    {
        const string sql = """
            insert into prescriptions (patient_id, doctor_id, medication_name, dosage, frequency, duration, instructions, prescription_date)
            values (@PatientId, @DoctorId, @MedicationName, @Dosage, @Frequency, @Duration, @Instructions, @PrescriptionDate)
            returning id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var id = await connection.QuerySingleAsync<Guid>(sql, prescription);
        await AddAuditLogAsync(actorUserId, "prescription created", "prescriptions", id, prescription.MedicationName);
        return id;
    }

    public async Task<Prescription?> GetPrescriptionAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Prescription>("""
            select pr.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name, d.license_number as doctor_license_number
            from prescriptions pr
            join patients p on p.id = pr.patient_id
            join doctors d on d.id = pr.doctor_id
            where pr.id = @Id;
            """, new { Id = id });
    }

    public async Task<Guid> CreateLabTestAsync(LabTest labTest, Guid actorUserId)
    {
        const string sql = """
            insert into lab_tests (patient_id, doctor_id, test_name, test_type, requested_date, status, result, result_date)
            values (@PatientId, @DoctorId, @TestName, @TestType, @RequestedDate, @Status, @Result, @ResultDate)
            returning id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var id = await connection.QuerySingleAsync<Guid>(sql, labTest);
        await AddAuditLogAsync(actorUserId, "lab test requested", "lab_tests", id, labTest.TestName);
        return id;
    }

    public async Task<IReadOnlyList<LabTest>> GetLabTestsAsync(Guid? doctorId = null, string? status = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var tests = await connection.QueryAsync<LabTest>("""
            select lt.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name
            from lab_tests lt
            join patients p on p.id = lt.patient_id
            join doctors d on d.id = lt.doctor_id
            where (@DoctorId::uuid is null or lt.doctor_id = @DoctorId)
              and (@Status is null or lt.status = @Status)
            order by lt.requested_date desc, lt.created_at desc;
            """, new { DoctorId = doctorId, Status = Clean(status) });
        return tests.AsList();
    }

    public async Task<LabTest?> GetLabTestAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<LabTest>("""
            select lt.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name
            from lab_tests lt
            join patients p on p.id = lt.patient_id
            join doctors d on d.id = lt.doctor_id
            where lt.id = @Id;
            """, new { Id = id });
    }

    public async Task UpdateLabResultAsync(Guid id, string status, string? result, DateTime? resultDate, Guid actorUserId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            update lab_tests
            set status = @Status,
                result = @Result,
                result_date = case when @Status = 'Completed' then coalesce(@ResultDate, current_date) else @ResultDate end,
                updated_at = now()
            where id = @Id;
            """, new { Id = id, Status = status, Result = result, ResultDate = resultDate });

        var test = await GetLabTestAsync(id);
        if (test is not null)
        {
            await AddAuditLogAsync(actorUserId, "lab result updated", "lab_tests", id, $"{test.TestName}: {status}");
            await AddNotificationAsync(null, "Lab result updated", $"{test.TestName} for {test.PatientName} is {status}.", "Lab", $"/LabTests/Result/{id}");
        }
    }

    public async Task<IReadOnlyList<ServiceItem>> GetServicesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var services = await connection.QueryAsync<ServiceItem>("""
            select s.*, d.name as department_name
            from services s
            left join departments d on d.id = s.department_id
            order by s.service_name;
            """);
        return services.AsList();
    }

    public async Task<ServiceItem?> GetServiceAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ServiceItem>("select * from services where id = @Id;", new { Id = id });
    }

    public async Task<Guid> CreateServiceAsync(ServiceItem service)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<Guid>("""
            insert into services (department_id, service_name, description, price)
            values (@DepartmentId, @ServiceName, @Description, @Price)
            returning id;
            """, service);
    }

    public async Task UpdateServiceAsync(ServiceItem service)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            update services set department_id = @DepartmentId, service_name = @ServiceName,
                description = @Description, price = @Price, updated_at = now()
            where id = @Id;
            """, service);
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(Guid? patientId = null)
    {
        const string multiServiceSql = """
            select py.*, concat(p.first_name, ' ', p.last_name) as patient_name, p.hospital_number,
                   coalesce(svc.service_names, s.service_name) as service_name,
                   svc.service_names
            from payments py
            join patients p on p.id = py.patient_id
            left join services s on s.id = py.service_id
            """ + PaymentServicesSelectSql + """
            where (@PatientId::uuid is null or py.patient_id = @PatientId)
            order by py.payment_date desc;
            """;

        using var connection = _connectionFactory.CreateConnection();
        try
        {
            var payments = await connection.QueryAsync<Payment>(multiServiceSql, new { PatientId = patientId });
            return payments.AsList();
        }
        catch
        {
            var payments = await connection.QueryAsync<Payment>("""
                select py.*, concat(p.first_name, ' ', p.last_name) as patient_name, p.hospital_number, s.service_name
                from payments py
                join patients p on p.id = py.patient_id
                join services s on s.id = py.service_id
                where (@PatientId::uuid is null or py.patient_id = @PatientId)
                order by py.payment_date desc;
                """, new { PatientId = patientId });
            return payments.AsList();
        }
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid id)
    {
        const string multiServiceSql = """
            select py.*, concat(p.first_name, ' ', p.last_name) as patient_name, p.hospital_number,
                   coalesce(svc.service_names, s.service_name) as service_name,
                   svc.service_names
            from payments py
            join patients p on p.id = py.patient_id
            left join services s on s.id = py.service_id
            """ + PaymentServicesSelectSql + """
            where py.id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        try
        {
            return await connection.QueryFirstOrDefaultAsync<Payment>(multiServiceSql, new { Id = id });
        }
        catch
        {
            return await connection.QueryFirstOrDefaultAsync<Payment>("""
                select py.*, concat(p.first_name, ' ', p.last_name) as patient_name, p.hospital_number, s.service_name
                from payments py
                join patients p on p.id = py.patient_id
                join services s on s.id = py.service_id
                where py.id = @Id;
                """, new { Id = id });
        }
    }

    public async Task<Guid> CreatePaymentAsync(Payment payment, Guid actorUserId, IReadOnlyCollection<Guid>? serviceIds = null)
    {
        var selectedServiceIds = serviceIds is null || serviceIds.Count == 0
            ? new[] { payment.ServiceId }.Where(id => id != Guid.Empty).ToArray()
            : serviceIds.Where(id => id != Guid.Empty).Distinct().ToArray();

        if (selectedServiceIds.Length == 0)
        {
            throw new InvalidOperationException("Select at least one service.");
        }

        payment.ServiceId = selectedServiceIds[0];

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var selectedServices = (await connection.QueryAsync<ServiceItem>("""
            select *
            from services
            where id = any(@ServiceIds);
            """, new { ServiceIds = selectedServiceIds }, transaction)).AsList();

        if (selectedServices.Count != selectedServiceIds.Length)
        {
            throw new InvalidOperationException("One or more selected services were not found.");
        }

        var servicesTotal = selectedServices.Sum(service => service.Price);

        if (payment.TotalAmount <= 0)
        {
            payment.TotalAmount = servicesTotal;
        }

        if (payment.Amount < 0 || payment.TotalAmount < 0)
        {
            throw new InvalidOperationException("Payment amounts cannot be negative.");
        }

        if (payment.Amount > payment.TotalAmount)
        {
            throw new InvalidOperationException("Paid amount cannot be higher than the service total.");
        }

        payment.BalanceAmount = Math.Max(payment.TotalAmount - payment.Amount, 0);
        payment.Status = payment.Status == "Cancelled"
            ? "Cancelled"
            : payment.BalanceAmount > 0 ? "Pending" : "Paid";

        var paymentId = await connection.QuerySingleAsync<Guid>("""
            insert into payments (patient_id, service_id, amount, total_amount, balance_amount, payment_method, payment_date, status, notes)
            values (@PatientId, @ServiceId, @Amount, @TotalAmount, @BalanceAmount, @PaymentMethod, @PaymentDate, @Status, @Notes)
            returning id;
            """, payment, transaction);

        var hasPaymentServices = await connection.ExecuteScalarAsync<bool>("select to_regclass('public.payment_services') is not null;", transaction: transaction);
        if (hasPaymentServices)
        {
            await connection.ExecuteAsync("""
                insert into payment_services (payment_id, service_id)
                select @PaymentId, unnest(@ServiceIds::uuid[])
                on conflict (payment_id, service_id) do nothing;
                """, new { PaymentId = paymentId, ServiceIds = selectedServiceIds }, transaction);
        }

        await connection.ExecuteAsync("""
            insert into invoices (payment_id, total_amount, status)
            values (@PaymentId, @TotalAmount, 'Issued');
            """, new { PaymentId = paymentId, payment.TotalAmount }, transaction);

        transaction.Commit();
        await AddAuditLogAsync(actorUserId, "payment registered", "payments", paymentId, $"Paid {payment.Amount:N2}, remaining {payment.BalanceAmount:N2} ({payment.PaymentMethod})");
        return paymentId;
    }

    public async Task UpdatePaymentBalanceAsync(Guid id, decimal balanceAmount, string status, string? notes, Guid actorUserId)
    {
        if (status is not ("Paid" or "Pending" or "Cancelled"))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Payment status is not supported.");
        }

        if (balanceAmount < 0)
        {
            throw new InvalidOperationException("Remaining balance cannot be negative.");
        }

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var payment = await connection.QuerySingleOrDefaultAsync<Payment>("""
            select *
            from payments
            where id = @Id
            for update;
            """, new { Id = id }, transaction)
            ?? throw new InvalidOperationException("Payment was not found.");

        if (balanceAmount > payment.TotalAmount)
        {
            throw new InvalidOperationException("Remaining balance cannot be higher than the payment total.");
        }

        var nextPaidAmount = payment.TotalAmount - balanceAmount;
        var nextStatus = status == "Cancelled" ? "Cancelled" : balanceAmount > 0 ? "Pending" : "Paid";

        await connection.ExecuteAsync("""
            update payments
            set amount = @Amount,
                balance_amount = @BalanceAmount,
                status = @Status,
                notes = @Notes,
                updated_at = now()
            where id = @Id;
            """, new { Id = id, Amount = nextPaidAmount, BalanceAmount = balanceAmount, Status = nextStatus, Notes = notes }, transaction);

        await connection.ExecuteAsync("""
            update invoices
            set total_amount = @TotalAmount,
                status = case when @Status = 'Cancelled' then 'Cancelled' else 'Issued' end,
                updated_at = now()
            where payment_id = @Id;
            """, new { Id = id, payment.TotalAmount, Status = nextStatus }, transaction);

        transaction.Commit();
        await AddAuditLogAsync(actorUserId, "payment balance updated", "payments", id, $"Paid {nextPaidAmount:N2}, remaining {balanceAmount:N2}, status {nextStatus}");
    }

    public async Task<Invoice?> GetInvoiceByPaymentAsync(Guid paymentId)
    {
        const string multiServiceSql = """
            select i.*, concat(p.first_name, ' ', p.last_name) as patient_name, p.hospital_number,
                   coalesce(svc.service_names, s.service_name) as service_name,
                   py.payment_method, py.amount as paid_amount, py.balance_amount
            from invoices i
            join payments py on py.id = i.payment_id
            join patients p on p.id = py.patient_id
            left join services s on s.id = py.service_id
            """ + PaymentServicesSelectSql + """
            where i.payment_id = @PaymentId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        try
        {
            return await connection.QueryFirstOrDefaultAsync<Invoice>(multiServiceSql, new { PaymentId = paymentId });
        }
        catch
        {
            return await connection.QueryFirstOrDefaultAsync<Invoice>("""
                select i.*, concat(p.first_name, ' ', p.last_name) as patient_name, p.hospital_number,
                       s.service_name, py.payment_method, py.amount as paid_amount, py.balance_amount
                from invoices i
                join payments py on py.id = i.payment_id
                join patients p on p.id = py.patient_id
                join services s on s.id = py.service_id
                where i.payment_id = @PaymentId;
                """, new { PaymentId = paymentId });
        }
    }

    public async Task<InventoryViewModel> GetInventoryAsync(string? search, bool lowStockOnly)
    {
        using var connection = _connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<MedicationInventoryItem>("""
            select *
            from medication_inventory
            where (@Search is null or medication_name ilike '%' || @Search || '%' or category ilike '%' || @Search || '%' or supplier ilike '%' || @Search || '%')
              and (@LowStockOnly = false or quantity_in_stock <= reorder_level)
            order by
              case when quantity_in_stock <= reorder_level then 0 else 1 end,
              medication_name;
            """, new { Search = Clean(search), LowStockOnly = lowStockOnly });

        return new InventoryViewModel { Items = items.AsList(), Search = search, LowStockOnly = lowStockOnly };
    }

    public async Task<MedicationInventoryItem?> GetInventoryItemAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<MedicationInventoryItem>("select * from medication_inventory where id = @Id;", new { Id = id });
    }

    public async Task<Guid> CreateInventoryItemAsync(MedicationInventoryItem item, Guid actorUserId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var id = await connection.QuerySingleAsync<Guid>("""
            insert into medication_inventory (medication_name, category, unit, quantity_in_stock, reorder_level, expiry_date, supplier, status)
            values (@MedicationName, @Category, @Unit, @QuantityInStock, @ReorderLevel, @ExpiryDate, @Supplier, @Status)
            returning id;
            """, item);
        await AddAuditLogAsync(actorUserId, "inventory item created", "medication_inventory", id, item.MedicationName);
        return id;
    }

    public async Task UpdateInventoryItemAsync(MedicationInventoryItem item, Guid actorUserId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            update medication_inventory
            set medication_name = @MedicationName, category = @Category, unit = @Unit,
                quantity_in_stock = @QuantityInStock, reorder_level = @ReorderLevel,
                expiry_date = @ExpiryDate, supplier = @Supplier, status = @Status,
                updated_at = now()
            where id = @Id;
            """, item);
        await AddAuditLogAsync(actorUserId, "inventory item updated", "medication_inventory", item.Id, item.MedicationName);
    }

    public async Task<NotificationsViewModel> GetNotificationsAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var notifications = await connection.QueryAsync<Notification>("""
            select *
            from notifications
            where user_id is null or user_id = @UserId
            order by is_read, created_at desc
            limit 100;
            """, new { UserId = userId });
        var unread = await connection.ExecuteScalarAsync<int>("select count(*) from notifications where (user_id is null or user_id = @UserId) and is_read = false;", new { UserId = userId });
        return new NotificationsViewModel { Notifications = notifications.AsList(), UnreadCount = unread };
    }

    public async Task MarkNotificationReadAsync(Guid id, Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            update notifications
            set is_read = true, read_at = now(), updated_at = now()
            where id = @Id and (user_id is null or user_id = @UserId);
            """, new { Id = id, UserId = userId });
    }

    public async Task AddNotificationAsync(Guid? userId, string title, string message, string type, string? linkUrl = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            insert into notifications (user_id, title, message, notification_type, link_url)
            values (@UserId, @Title, @Message, @Type, @LinkUrl);
            """, new { UserId = userId, Title = title, Message = message, Type = type, LinkUrl = linkUrl });
    }

    public async Task<GlobalSearchViewModel> SearchAsync(string query)
    {
        var clean = Clean(query) ?? string.Empty;
        using var connection = _connectionFactory.CreateConnection();

        var patients = await connection.QueryAsync<Patient>("""
            select p.*, concat(d.first_name, ' ', d.last_name) as assigned_doctor_name, r.room_number as current_room_number
            from patients p
            left join doctors d on d.id = p.assigned_doctor_id
            left join rooms r on r.id = p.current_room_id
            where p.first_name ilike '%' || @Query || '%' or p.last_name ilike '%' || @Query || '%'
               or concat_ws(' ', p.first_name, p.last_name) ilike '%' || @Query || '%'
               or concat_ws(' ', p.last_name, p.first_name) ilike '%' || @Query || '%'
               or p.personal_number ilike '%' || @Query || '%' or p.phone ilike '%' || @Query || '%'
               or p.hospital_number ilike '%' || @Query || '%'
               or p.email ilike '%' || @Query || '%'
               or (@QueryDigits is not null and regexp_replace(coalesce(p.phone, ''), '[^0-9]', '', 'g') ilike '%' || @QueryDigits || '%')
            order by p.created_at desc limit 10;
            """, new { Query = clean, QueryDigits = DigitsOnly(clean) });

        var doctors = await connection.QueryAsync<Doctor>(DoctorSelectSql + """
            where d.first_name ilike '%' || @Query || '%' or d.last_name ilike '%' || @Query || '%'
               or d.specialization ilike '%' || @Query || '%' or d.phone ilike '%' || @Query || '%'
            order by d.first_name limit 10;
            """, new { Query = clean });

        var rooms = await connection.QueryAsync<Room>(RoomSelectSql + """
            where r.room_number ilike '%' || @Query || '%' or r.room_type ilike '%' || @Query || '%'
            order by r.room_number limit 10;
            """, new { Query = clean });

        var appointments = await connection.QueryAsync<Appointment>("""
            select a.*, concat(p.first_name, ' ', p.last_name) as patient_name,
                   concat(d.first_name, ' ', d.last_name) as doctor_name, d.specialization as doctor_specialization,
                   svc.service_name, svc.service_price, svc.service_names, svc.services_total
            from appointments a
            join patients p on p.id = a.patient_id
            join doctors d on d.id = a.doctor_id
            """ + AppointmentServicesSelectSql + """
            where a.appointment_number ilike '%' || @Query || '%'
               or p.first_name ilike '%' || @Query || '%' or p.last_name ilike '%' || @Query || '%'
               or concat_ws(' ', p.first_name, p.last_name) ilike '%' || @Query || '%'
               or d.first_name ilike '%' || @Query || '%' or d.last_name ilike '%' || @Query || '%'
               or svc.service_names ilike '%' || @Query || '%'
            order by a.appointment_date desc, a.appointment_time desc limit 10;
            """, new { Query = clean });

        return new GlobalSearchViewModel
        {
            Query = clean,
            Patients = patients.AsList(),
            Doctors = doctors.AsList(),
            Rooms = rooms.AsList(),
            Appointments = appointments.AsList()
        };
    }

    public async Task<ReportsViewModel> GetReportsAsync(DateTime from, DateTime to)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new { From = from.Date, To = to.Date };

        var dailyPatients = await connection.QueryAsync<DailyPatientReportRow>("""
            select registration_date as report_date,
                   count(*)::int as registered_patients,
                   count(*) filter (where status = 'Admitted')::int as admitted_patients,
                   count(*) filter (where status = 'Discharged')::int as discharged_patients
            from patients
            where registration_date between @From and @To
            group by registration_date
            order by registration_date;
            """, parameters);

        var payments = await connection.QueryAsync<PaymentReportRow>("""
            select payment_date::date as payment_date, payment_method, status,
                   coalesce(sum(total_amount),0) as total_amount,
                   coalesce(sum(amount),0) as paid_amount,
                   coalesce(sum(balance_amount),0) as balance_amount,
                   count(*)::int as payment_count
            from payments
            where payment_date::date between @From and @To
            group by payment_date::date, payment_method, status
            order by payment_date::date desc;
            """, parameters);

        var doctors = await connection.QueryAsync<DoctorPerformanceRow>("""
            select doctor_name,
                   sum(visits_completed)::int as visits_completed,
                   sum(diagnoses_created)::int as diagnoses_created,
                   sum(prescriptions_created)::int as prescriptions_created
            from (
                select concat(d.first_name, ' ', d.last_name) as doctor_name,
                       count(v.id)::int as visits_completed,
                       0::int as diagnoses_created,
                       0::int as prescriptions_created
                from visits v
                join doctors d on d.id = v.doctor_id
                where v.visit_date::date between @From and @To
                group by d.id, d.first_name, d.last_name
                union all
                select concat(d.first_name, ' ', d.last_name) as doctor_name,
                       0::int as visits_completed,
                       count(dg.id)::int as diagnoses_created,
                       0::int as prescriptions_created
                from diagnoses dg
                join doctors d on d.id = dg.doctor_id
                where dg.diagnosis_date between @From and @To
                group by d.id, d.first_name, d.last_name
                union all
                select concat(d.first_name, ' ', d.last_name) as doctor_name,
                       0::int as visits_completed,
                       0::int as diagnoses_created,
                       count(pr.id)::int as prescriptions_created
                from prescriptions pr
                join doctors d on d.id = pr.doctor_id
                where pr.prescription_date between @From and @To
                group by d.id, d.first_name, d.last_name
            ) activity
            group by doctor_name
            order by visits_completed desc, diagnoses_created desc;
            """, parameters);

        var rooms = await connection.QueryAsync<RoomOccupancyRow>("""
            select room_number, room_type, capacity, current_occupancy, status
            from rooms
            order by floor, room_number;
            """);

        var diagnoses = await connection.QueryAsync<DiagnosisReportRow>("""
            select disease_name, severity, count(*)::int as diagnosis_count
            from diagnoses
            where diagnosis_date between @From and @To
            group by disease_name, severity
            order by diagnosis_count desc, disease_name;
            """, parameters);

        var appointments = await connection.QueryAsync<AppointmentReportRow>("""
            select appointment_date, status, count(*)::int as appointment_count
            from appointments
            where appointment_date between @From and @To
            group by appointment_date, status
            order by appointment_date desc;
            """, parameters);

        var monthlyClinic = await connection.QueryAsync<MonthlyClinicReportRow>("""
            with months as (
                select generate_series(date_trunc('month', @From::date), date_trunc('month', @To::date), interval '1 month')::date as month
            ),
            patient_counts as (
                select date_trunc('month', registration_date)::date as month,
                       count(*)::int as new_patients
                from patients
                where registration_date between @From and @To
                group by date_trunc('month', registration_date)
            ),
            appointment_counts as (
                select date_trunc('month', appointment_date)::date as month,
                       count(*)::int as appointment_count,
                       count(*) filter (where status = 'Completed')::int as completed_appointments
                from appointments
                where appointment_date between @From and @To
                group by date_trunc('month', appointment_date)
            ),
            payment_totals as (
                select date_trunc('month', payment_date)::date as month,
                       coalesce(sum(total_amount), 0) as total_billed,
                       coalesce(sum(amount), 0) as total_paid,
                       coalesce(sum(balance_amount), 0) as remaining_balance
                from payments
                where payment_date::date between @From and @To
                group by date_trunc('month', payment_date)
            )
            select m.month,
                   coalesce(pc.new_patients, 0) as new_patients,
                   coalesce(ac.appointment_count, 0) as appointment_count,
                   coalesce(ac.completed_appointments, 0) as completed_appointments,
                   coalesce(pt.total_billed, 0) as total_billed,
                   coalesce(pt.total_paid, 0) as total_paid,
                   coalesce(pt.remaining_balance, 0) as remaining_balance
            from months m
            left join patient_counts pc on pc.month = m.month
            left join appointment_counts ac on ac.month = m.month
            left join payment_totals pt on pt.month = m.month
            order by m.month desc;
            """, parameters);

        var servicePerformance = await connection.QueryAsync<ServicePerformanceReportRow>("""
            select s.service_name,
                   count(py.id)::int as payment_count,
                   coalesce(sum(py.total_amount), 0) as total_billed,
                   coalesce(sum(py.amount), 0) as total_paid,
                   coalesce(sum(py.balance_amount), 0) as remaining_balance
            from payments py
            join services s on s.id = py.service_id
            where py.payment_date::date between @From and @To
            group by s.id, s.service_name
            order by total_billed desc, payment_count desc, s.service_name
            limit 12;
            """, parameters);

        return new ReportsViewModel
        {
            From = from,
            To = to,
            DailyPatients = dailyPatients.AsList(),
            Payments = payments.AsList(),
            DoctorPerformance = doctors.AsList(),
            RoomOccupancy = rooms.AsList(),
            Diagnoses = diagnoses.AsList(),
            Appointments = appointments.AsList(),
            MonthlyClinic = monthlyClinic.AsList(),
            ServicePerformance = servicePerformance.AsList()
        };
    }

    private async Task AddAuditLogAsync(Guid userId, string action, string entityName, Guid entityId, string details)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("""
            insert into audit_logs (user_id, action, entity_name, entity_id, details)
            values (@UserId, @Action, @EntityName, @EntityId, @Details);
            """, new { UserId = userId, Action = action, EntityName = entityName, EntityId = entityId, Details = details });
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? DigitsOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static async Task<IReadOnlyList<Appointment>> QueryAppointmentsWithFallbackAsync(
        System.Data.IDbConnection connection,
        string multiServiceSql,
        string fallbackSql,
        object parameters)
    {
        try
        {
            var appointments = await connection.QueryAsync<Appointment>(multiServiceSql, parameters);
            return appointments.AsList();
        }
        catch
        {
            try
            {
                var appointments = await connection.QueryAsync<Appointment>(fallbackSql, parameters);
                return appointments.AsList();
            }
            catch
            {
                return Array.Empty<Appointment>();
            }
        }
    }

    private const string DoctorSelectSql = """
        select d.*, dep.name as department_name
        from doctors d
        left join departments dep on dep.id = d.department_id
        """;

    private const string RoomSelectSql = """
        select r.*, dep.name as department_name
        from rooms r
        left join departments dep on dep.id = r.department_id
        """;
}
