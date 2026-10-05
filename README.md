# Clinic Management System

An ASP.NET Core MVC clinic management application with SQL Server and Entity Framework Core. The application supports patient registration and booking, doctor consultations, prescriptions, and an administrator management portal.

## Technology stack

- .NET 10 / ASP.NET Core MVC
- C#
- Entity Framework Core 10
- SQL Server
- Razor views
- Cookie authentication and role-based authorization
- Bootstrap

> **Important:** This repository currently targets modern ASP.NET Core (`net10.0`). It is not an ASP.NET MVC 5 / .NET Framework application.

## Main features

### Patient

- Register a patient account
- Sign in and sign out
- View only the patient's own appointments
- Book an appointment with a doctor
- Select date, time, duration, and reason
- Doctor availability and overlapping appointment validation
- View accepted, rejected, and completed appointment statuses
- View consultation diagnosis and notes
- View prescriptions, medication, dosage, frequency, and duration

### Doctor

- Register with a license number and department
- Sign in through the normal login page
- View assigned appointments
- Accept or reject pending appointments
- Open consultations for accepted appointments
- Record diagnosis and consultation notes
- Mark consultations as completed
- Create an optional prescription during consultation
- View patient information related to assigned appointments

### Admin

- Protected admin portal at `/Admin`
- View patient, doctor, and pending appointment counts
- View all appointments
- Accept, reject, or complete appointments
- Manage all domain resources through the protected `/api/admin` API:
  - Users
  - Departments
  - Doctors
  - Patients
  - Appointments
  - Medical records
  - Prescriptions
  - Prescription items
- Create, read, update, and delete operations where supported
- Relationship checks prevent deleting records that still have dependent data
- The last administrator account cannot be deleted

## Workflow

```text
Patient registers
        |
Patient signs in
        |
Patient books appointment
        |
Doctor accepts or rejects
        |
Doctor performs consultation
        |
Doctor records diagnosis and notes
        |
Doctor creates prescription
        |
Patient views the consultation result
```

## Project structure

```text
Clinic_Management_System/
├── Controllers/
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── AdminDataController.cs
│   ├── AppointmentsController.cs
│   ├── ClinicController.cs
│   └── DoctorController.cs
├── Data/
│   └── ClinicDbContext.cs
├── Migrations/
├── Models/
│   └── ClinicEntities.cs
├── Services/
│   └── AppointmentService.cs
├── Views/
│   ├── Account/
│   ├── Admin/
│   ├── Clinic/
│   ├── Doctor/
│   └── Shared/
├── appsettings.Development.json
├── Clinic_Management_System.csproj
└── Program.cs
```

## Requirements

- .NET 10 SDK
- SQL Server or SQL Server LocalDB
- Visual Studio, Visual Studio Code, or the .NET CLI

Check the installed SDK:

```powershell
dotnet --version
```

## Configuration

The application intentionally loads `appsettings.Development.json` as its application settings file. The base `appsettings.json` is not used by the current `Program.cs` configuration setup.

Configure the database in:

```text
appsettings.Development.json
```

Example:

```json
{
  "ConnectionStrings": {
    "ClinicDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=ClinicManagementDb_Development;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "AdminAccount": {
    "FullName": "Clinic Administrator",
    "Email": "admin@clinic.local",
    "Password": "ChangeThisDevelopmentPassword!"
  }
}
```

Do not commit real database credentials or production passwords to configuration files. Use environment variables or a secret store for production.

## Database setup

Restore dependencies:

```powershell
dotnet restore
```

Apply the existing EF Core migrations:

```powershell
dotnet ef database update --context ClinicDbContext
```

If the EF command is unavailable, install the tool:

```powershell
dotnet tool install --global dotnet-ef
```

Create a new migration after changing the entity model:

```powershell
dotnet ef migrations add DescribeYourChange --context ClinicDbContext
dotnet ef database update --context ClinicDbContext
```

The initial migration includes development seed data for departments, a doctor, and a patient.

## Development accounts

The application creates the administrator account on startup if it does not already exist:

```text
Admin email:    admin@clinic.local
Admin password: Admin123!
```

The seeded doctor account is:

```text
Doctor email:   ahmed@clinic.local
Doctor password: Doctor123!
```

The seeded patient account is:

```text
Patient email:  mariam@clinic.local
Patient password: Patient123!
```

These credentials are for local development only. Change them before using the application in any shared or production environment.

## Running the application

```powershell
dotnet run
```

The default development URLs are configured in `Properties/launchSettings.json`:

```text
http://localhost:5062
https://localhost:7009
```

Useful pages:

```text
/Account/Register
/Account/DoctorRegister
/Account/Login
/Clinic
/Clinic/Book
/Doctor
/Admin
```

## API endpoints

### Appointment API

```text
GET    /api/appointments
GET    /api/appointments/{id}
POST   /api/appointments
PATCH  /api/appointments/{id}/status
```

An example request file is available at:

```text
Clinic_Management_System.http
```

### Admin API

All admin API routes require an authenticated user with the `Admin` role.

```text
/api/admin/users
/api/admin/departments
/api/admin/doctors
/api/admin/patients
/api/admin/appointments
/api/admin/medical-records
/api/admin/prescriptions
/api/admin/prescription-items
```

Use the HTTP verbs shown by the controller actions to list, create, update, and delete resources.

## Authorization rules

- `/Clinic` requires authentication.
- Only patients can book appointments.
- Patients can only view their own appointments and consultation results.
- Doctors can only manage appointments assigned to their doctor profile.
- Admin portal pages and admin API operations require the `Admin` role.
- Cookie authentication is configured with an eight-hour expiration and sliding expiration.

## Important implementation notes

### Appointment conflict checking

Booking rejects overlapping appointments for the same doctor unless the existing appointment is `Rejected` or `Cancelled`.

The check compares the requested start/end time with the existing appointment start/end time. The database also has an index on doctor and start time to support appointment lookups.

### SQL Server cascade paths

Several relationships use `DeleteBehavior.NoAction` because SQL Server rejects multiple cascade paths involving users, patients, doctors, and appointments. Deletions of related records must therefore be deliberate and are guarded by controller checks where appropriate.

### Passwords

Passwords are hashed using `PasswordHasher<User>`. Never use the development placeholder values or store plain-text passwords in a production environment.

### API data contracts

The API currently exposes some EF entities for the admin-only CRUD surface. If the API becomes public or is consumed by external clients, replace entity request/response types with dedicated DTOs and add explicit validation.

### Environment settings

The current configuration explicitly loads `appsettings.Development.json`. If production deployment is added, introduce an environment-specific configuration strategy and move secrets outside source control.

### Seed data

Seed data is defined in `ClinicDbContext.OnModelCreating`. The startup code upgrades the seeded development doctor and patient placeholder passwords to valid development password hashes.

## Validation

Build the project:

```powershell
dotnet build
```

The project should build with no errors. If the executable is locked by a running application, stop the running development process and build again.

## Security checklist before production

- Change all development account passwords.
- Remove or replace seeded development accounts.
- Store connection strings in a secure secret provider.
- Do not expose password hashes through APIs.
- Add DTOs instead of binding EF entities directly from public requests.
- Add audit logging for medical record and prescription changes.
- Enable HTTPS and configure a valid production certificate.
- Add stronger account lockout, password reset, and email verification flows.
- Review authorization rules for every new endpoint.
- Back up the SQL Server database and test restore procedures.
