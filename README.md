# Vlera Dent Management System

Vlera Dent is a modern ASP.NET Core MVC dental clinic management system for admin, dentist, and receptionist workflows. The persistent database backend is now Cloudflare D1 through a Cloudflare Worker API.

## Technologies

- ASP.NET Core MVC, C#, .NET 10
- Flutter cross-platform app for Android, iOS, and Windows
- Bootstrap 5, Bootstrap Icons, Chart.js, SweetAlert2
- Cloudflare Workers API
- Cloudflare D1 SQLite database
- Cookie authentication in the MVC app
- D1 prepared statements, validation, and audit logs

## Cloudflare D1 Setup

1. In Cloudflare Dashboard, open **Storage & databases**.
2. Open **D1 SQL Database**.
3. Create a database named:

```text
hospital_management_db
```

4. Copy the D1 database ID.
5. Put it in [`wrangler.toml`](wrangler.toml):

```toml
[[d1_databases]]
binding = "DB"
database_name = "hospital_management_db"
database_id = "1bc36565-9270-4115-b32e-8512be704177"
```

## Run D1 Migrations

Install Wrangler if needed:

```powershell
npm.cmd install -g wrangler
```

Login to Cloudflare CLI before remote commands:

```powershell
npx.cmd wrangler login
```

Create tables locally:

```powershell
wrangler d1 execute hospital_management_db --local --file=./migrations/0001_init.sql
wrangler d1 execute hospital_management_db --local --file=./seed.sql
```

Create tables in Cloudflare production:

```powershell
wrangler d1 execute hospital_management_db --remote --file=./migrations/0001_init.sql
wrangler d1 execute hospital_management_db --remote --file=./seed.sql
```

## Run Cloudflare Worker API Locally

```powershell
wrangler dev
```

Local API URL:

```text
http://127.0.0.1:8787
```

## Deploy Cloudflare Worker API

```powershell
wrangler deploy
```

The D1 API is deployed as Cloudflare Pages Functions here:

```text
https://vlera-dent-d1-api.pages.dev
```

Set this URL in the ASP.NET app:

```powershell
$env:D1_API_BASE_URL="https://vlera-dent-d1-api.pages.dev"
```

Optional, protect non-login API endpoints with a token:

```powershell
wrangler secret put D1_API_TOKEN
$env:D1_API_TOKEN="THE-SAME-TOKEN"
```

On Render, add this environment variable:

```text
D1_API_BASE_URL=https://vlera-dent-d1-api.pages.dev
D1_API_TOKEN=THE-SAME-TOKEN
```

No legacy database URL, database keys, or database password is required in the ASP.NET app.

## Run ASP.NET App Locally

Start the Worker first, then run:

```powershell
dotnet restore
dotnet build
dotnet run
```

Open the URL printed by `dotnet run`, usually `http://localhost:5117`.

## Flutter App

The native Flutter app lives in:

```text
vlera_dent_app/
```

It is not a PWA. It is a real Flutter application for Android, iOS, and Windows. It preserves the existing Vlera Dent workflows by loading the Render-hosted backend inside a native WebView shell. The data path remains:

```text
Flutter App -> Render Backend -> Cloudflare D1
```

The app does not connect directly to Cloudflare D1.

## Cloudflare Frontend

The public frontend can be hosted through Cloudflare while the ASP.NET backend stays on Render:

```text
Browser / Flutter App -> Cloudflare Frontend Worker -> Render ASP.NET Backend -> Cloudflare D1 API -> Cloudflare D1
```

Cloudflare frontend Worker config:

```text
wrangler.frontend.toml
```

Deploy the frontend Worker:

```powershell
npx.cmd wrangler deploy -c wrangler.frontend.toml
```

The Worker proxies all web traffic to the Render backend and rewrites redirects back to the Cloudflare frontend URL. This keeps the current MVC design and workflows without rebuilding the UI as a separate static frontend.

Default backend URL:

```text
https://vlera-dent-frontend.drin80614.workers.dev/Auth/Login
```

Run locally on an available device:

```powershell
cd vlera_dent_app
..\tools\flutter\bin\flutter.bat pub get
..\tools\flutter\bin\flutter.bat run --dart-define BACKEND_URL=https://vlera-dent-frontend.drin80614.workers.dev/Auth/Login
```

Build Android APK:

```powershell
cd vlera_dent_app
..\tools\flutter\bin\flutter.bat build apk --release --dart-define BACKEND_URL=https://vlera-dent-frontend.drin80614.workers.dev/Auth/Login
```

Or use the helper script:

```powershell
cd vlera_dent_app
.\installers\Build-AndroidApk.ps1
```

Output:

```text
vlera_dent_app/build/app/outputs/flutter-apk/app-release.apk
```

Build Android App Bundle:

```powershell
cd vlera_dent_app
..\tools\flutter\bin\flutter.bat build appbundle --release --dart-define BACKEND_URL=https://vlera-dent-frontend.drin80614.workers.dev/Auth/Login
```

Build Windows desktop package:

```powershell
cd vlera_dent_app
.\installers\Build-WindowsPackage.ps1
```

Output:

```text
vlera_dent_app/build/installer/VleraDent-Windows.zip
```

Build iOS on macOS:

```bash
cd vlera_dent_app
flutter build ios --release --dart-define BACKEND_URL=https://vlera-dent-frontend.drin80614.workers.dev/Auth/Login
```

Notes:

- Android minimum version is Android 8.0.
- iOS minimum deployment target is iOS 13.
- Windows supports Windows 10 and Windows 11.
- On Windows, enable Windows Developer Mode if Flutter asks for symlink support.

## Free Direct Installation

You do not need to pay Google Play, Apple App Store, or Microsoft Store fees if the app is only distributed directly.

Android free install:

1. Build `app-release.apk`.
2. Send the APK to the phone.
3. On the phone, allow **Install unknown apps** for the app used to open the APK.
4. Open the APK and install **Vlera Dent**.

Windows free install:

1. Build the Windows package with `.\installers\Build-WindowsPackage.ps1`.
2. Send `VleraDent-Windows.zip` to the laptop.
3. Extract the zip.
4. Run `VleraDent.exe`.

iPhone/iPad note:

Apple does not allow normal permanent direct installation like Android without an Apple Developer/App Store/TestFlight flow. A free Apple ID can be used only for local development installs from Xcode and usually expires after a short period, so it is not recommended for a clinic production app.

## Default Login Users

After running `seed.sql`:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@vleradent.com` | `Admin123!` |
| Dentist | `vlorentina.sahiti@vleradent.com` | `Doctor123!` |
| Receptionist | `reception@vleradent.com` | `Reception123!` |

## D1 API Endpoints

- `POST /api/auth/login`
- `GET /api/patients/search?q=`
- `POST /api/patients`
- `GET /api/patients/:id`
- `GET /api/patients/:id/history`
- `POST /api/appointments`
- `GET /api/appointments`
- `PATCH /api/appointments/:id`
- `POST /api/visits`
- `POST /api/prescriptions`
- `GET /api/prescriptions/pending-print`
- `PATCH /api/prescriptions/:id/printed`
- `POST /api/payments`
- `GET /api/payments/pending-print`
- `PATCH /api/payments/:id/printed`
- `GET /api/rooms`
- `POST /api/hospitalizations`
- `GET /api/reports/daily`

## Project Structure

```text
Controllers/           MVC controllers and role portals
Data/                  Cloudflare D1 API client for ASP.NET
Models/                Clinic domain entities
Services/              Password hashing service for MVC account tools
ViewModels/            Dashboard, workflow, auth, search, and report models
Views/                 Bootstrap MVC views and shared layout
wwwroot/               CSS, JavaScript, Bootstrap assets, PWA files
vlera_dent_app/        Flutter Android, iOS, and Windows application
workers/d1-api/        Cloudflare Worker API using D1 prepared statements
cloudflare-pages-api/  Deployed Pages Functions API bundle
migrations/            D1 SQLite-compatible migrations
schema.sql             Full D1 schema
seed.sql               Starter Vlera Dent data
wrangler.toml          Cloudflare Worker and D1 binding config
```

## Main Features

- Admin, dentist, and receptionist role workflows
- Patient registration, search, details, and history
- Appointment creation with one or more services
- Appointment services automatically create visit history and pending payment rows
- Prescriptions and payments pending print queue
- Dental services, invoices, partial payments, balances, and status tracking
- Rooms, beds, and hospitalization support in D1
- Daily report endpoint
- Audit logs for patient creation, visits, prescriptions, payments, print actions, and appointments

## Security Notes

- Do not put Cloudflare API tokens in frontend code.
- D1 queries are executed only inside the Cloudflare Worker with prepared statements.
- The frontend/MVC app calls API endpoints; it does not connect directly to D1.
- Keep `D1_API_BASE_URL` in environment variables for production.
