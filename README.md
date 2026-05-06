# Vlera Dent Management System

Vlera Dent is a modern ASP.NET Core MVC dental clinic management web application for admin, dentist, and reception staff. It includes role-based authentication, dashboards, patient intake, clinical history, visits, diagnoses, prescriptions, lab requests, rooms, appointments, billing, invoices, global search, audit logs, and admin reports.

## Technologies

- ASP.NET Core MVC, C#, .NET 10
- Bootstrap 5, Bootstrap Icons, Chart.js, SweetAlert2
- Supabase PostgreSQL via Npgsql and Dapper
- Cookie authentication with PBKDF2 password hashing
- Server-side validation and role-based authorization

## Supabase Configuration

Project:

- URL: `https://qlrwhcypmpegbbakqqxd.supabase.co`
- Project ID: `qlrwhcypmpegbbakqqxd`

Do not commit real keys or database passwords. Use environment variables or user secrets. This app is configured to save data in Supabase PostgreSQL, not in a local database.

Required application configuration:

```json
"Supabase": {
  "Url": "https://qlrwhcypmpegbbakqqxd.supabase.co",
  "ProjectId": "qlrwhcypmpegbbakqqxd",
  "AnonKey": "",
  "ServiceRoleKey": "",
  "DbPassword": "",
  "PostgresConnectionString": ""
}
```

The MVC app connects to Supabase PostgreSQL with a server-side connection. URL and Project ID identify the Supabase project, but PostgreSQL still requires the database password or a full connection string.

Option A, easiest with the provided Project ID:

```powershell
$env:SUPABASE_DB_PASSWORD="YOUR-SUPABASE-DATABASE-PASSWORD"
```

The app will build this hosted Supabase connection automatically:

```text
Host=db.qlrwhcypmpegbbakqqxd.supabase.co;Port=5432;Database=postgres;Username=postgres;SSL Mode=Require
```

Option B, provide the full Supabase connection string:

```powershell
$env:SUPABASE_DB_CONNECTION="Host=db.qlrwhcypmpegbbakqqxd.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR-PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
```

or:

```powershell
dotnet user-secrets set "Supabase:DbPassword" "YOUR-SUPABASE-DATABASE-PASSWORD"
```

## WhatsApp Appointment Reminders

Automatic WhatsApp reminders are disabled by default. To enable them on Render, add these environment variables:

```text
WhatsApp__Enabled=true
WhatsApp__AccessToken=YOUR_META_WHATSAPP_ACCESS_TOKEN
WhatsApp__PhoneNumberId=YOUR_WHATSAPP_PHONE_NUMBER_ID
WhatsApp__ReminderLeadHours=24
WhatsApp__PollMinutes=5
WhatsApp__ClinicName=Vlera Dent
```

The background worker checks upcoming scheduled appointments, sends one reminder, and stores the reminder status in Supabase. The appointment list also includes a manual WhatsApp button when the patient has a phone number.

## Create Database Tables

1. Open Supabase Dashboard.
2. Go to SQL Editor.
3. Run [`scripts/supabase_schema.sql`](scripts/supabase_schema.sql).
4. Confirm tables were created in the `public` schema.

The script creates all required tables, generated numbers, triggers, indexes, foreign keys, and realistic seed data.

## Run Locally

```powershell
dotnet restore
dotnet build
dotnet run
```

Open the URL printed by `dotnet run`, usually `https://localhost:7xxx` or `http://localhost:5xxx`.

## Default Login Users

After running the SQL seed:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@vleradent.com` | `Admin123!` |
| Dentist | `vlorentina.sahiti@vleradent.com` | `Doctor123!` |
| Receptionist | `reception@vleradent.com` | `Reception123!` |

## Project Structure

```text
Controllers/     MVC controllers and role portals
Data/            Supabase options, Npgsql connection factory, Dapper repository
Models/          Hospital domain entities
Services/        Password hashing and WhatsApp reminder services
ViewModels/      Dashboard, workflow, auth, search, and report models
Views/           Bootstrap MVC views and shared layout
wwwroot/         CSS, JavaScript, Bootstrap assets
scripts/         Supabase PostgreSQL schema and seed SQL
```

## Main Features

- Admin, Dentist, and Receptionist role permissions
- Login, logout, access denied, secure cookie auth
- Role-aware dashboard with metrics, charts, recent activity, and quick actions
- Patient CRUD, search, filters, pagination, profile tabs, and print profile
- Dentist workflows: assigned patients, visits, prescriptions, lab tests
- Reception workflows: patient registration, appointment creation, payments, receipts
- Services, billing, automatic invoice numbers, print-friendly receipts
- Reports with date filters, print support, and CSV export
- Global search across patients, dentists, and appointments
- Audit logs for important patient, prescription, and payment actions
- Forgot-password reset links for local/dev recovery and user profile password changes
- Lab test work queue with result/status updates and completion tracking
- Dental inventory with low-stock indicators, expiry dates, suppliers, and admin editing
- Notifications center for operational alerts
- Weekly appointment calendar with doctor filtering and active-slot conflict prevention

## Security Notes

- Passwords are hashed with PBKDF2-SHA256.
- Sensitive patient operations are server-side and role-checked.
- No Supabase secrets are hardcoded.
- Use HTTPS and rotate any exposed development credentials before production.
- Consider adding Supabase Row Level Security policies if the database is accessed by any client other than this server application.
