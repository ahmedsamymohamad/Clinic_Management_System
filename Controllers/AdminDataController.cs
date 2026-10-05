using Clinic_Management_System.Data;
using Clinic_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Management_System.Controllers;

public sealed record AdminUserResponse(int Id, string FullName, string Email, UserRole Role);

[ApiController, Authorize(Roles = nameof(UserRole.Admin)), Route("api/admin")]
public sealed class AdminDataController(ClinicDbContext db) : ControllerBase
{
    [HttpGet("users")] public async Task<ActionResult<List<AdminUserResponse>>> Users(CancellationToken ct) =>
        await db.Users.AsNoTracking().Select(x => new AdminUserResponse(x.Id, x.FullName, x.Email, x.Role)).ToListAsync(ct);
    [HttpPost("users")] public async Task<ActionResult<User>> CreateUser(User model, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(x => x.Email == model.Email, ct)) return Conflict("Email already exists.");
        model.PasswordHash = "managed-by-account-registration";
        db.Users.Add(model); await db.SaveChangesAsync(ct); return Created($"/api/admin/users/{model.Id}", model);
    }
    [HttpGet("departments")] public async Task<ActionResult<List<Department>>> Departments(CancellationToken ct) =>
        await db.Departments.AsNoTracking().ToListAsync(ct);
    [HttpPut("users/{id:int}")] public async Task<IActionResult> UpdateUser(int id, User model, CancellationToken ct)
    {
        var entity = await db.Users.FindAsync([id], ct); if (entity is null) return NotFound();
        entity.FullName = model.FullName; entity.Email = model.Email; entity.Role = model.Role;
        await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpDelete("users/{id:int}")] public async Task<IActionResult> DeleteUser(int id, CancellationToken ct)
    {
        var entity = await db.Users.FindAsync([id], ct); if (entity is null) return NotFound();
        if (entity.Role == UserRole.Admin && await db.Users.CountAsync(x => x.Role == UserRole.Admin, ct) == 1) return Conflict("At least one admin is required.");
        db.Users.Remove(entity); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpPost("departments")] public async Task<ActionResult<Department>> CreateDepartment(Department model, CancellationToken ct)
    {
        db.Departments.Add(model); await db.SaveChangesAsync(ct); return Created($"/api/admin/departments/{model.Id}", model);
    }
    [HttpPut("departments/{id:int}")] public async Task<IActionResult> UpdateDepartment(int id, Department model, CancellationToken ct)
    {
        var entity = await db.Departments.FindAsync([id], ct); if (entity is null) return NotFound();
        entity.Name = model.Name; await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpDelete("departments/{id:int}")] public async Task<IActionResult> DeleteDepartment(int id, CancellationToken ct)
    {
        var entity = await db.Departments.FindAsync([id], ct); if (entity is null) return NotFound();
        if (await db.Doctors.AnyAsync(x => x.DepartmentId == id, ct)) return Conflict("Department has doctors.");
        db.Departments.Remove(entity); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpGet("patients")] public async Task<ActionResult<List<Patient>>> Patients(CancellationToken ct) =>
        await db.Patients.Include(x => x.User).AsNoTracking().ToListAsync(ct);
    [HttpGet("doctors")] public async Task<ActionResult<List<Doctor>>> Doctors(CancellationToken ct) =>
        await db.Doctors.Include(x => x.User).Include(x => x.Department).AsNoTracking().ToListAsync(ct);
    [HttpPost("doctors")] public async Task<ActionResult<Doctor>> CreateDoctor(Doctor model, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.Id == model.UserId && x.Role == UserRole.Doctor, ct)) return BadRequest("User must be a doctor.");
        db.Doctors.Add(model); await db.SaveChangesAsync(ct); return Created($"/api/admin/doctors/{model.Id}", model);
    }
    [HttpPut("doctors/{id:int}")] public async Task<IActionResult> UpdateDoctor(int id, Doctor model, CancellationToken ct)
    {
        var entity = await db.Doctors.FindAsync([id], ct); if (entity is null) return NotFound();
        entity.DepartmentId = model.DepartmentId; entity.LicenseNumber = model.LicenseNumber;
        await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpDelete("doctors/{id:int}")] public async Task<IActionResult> DeleteDoctor(int id, CancellationToken ct)
    {
        var entity = await db.Doctors.FindAsync([id], ct); if (entity is null) return NotFound();
        if (await db.Appointments.AnyAsync(x => x.DoctorId == id, ct)) return Conflict("Doctor has appointments.");
        db.Doctors.Remove(entity); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpGet("appointments")] public async Task<ActionResult<List<Appointment>>> Appointments(CancellationToken ct) =>
        await db.Appointments.Include(x => x.Patient).ThenInclude(x => x.User).Include(x => x.Doctor).ThenInclude(x => x.User).AsNoTracking().ToListAsync(ct);
    [HttpPut("appointments/{id:int}")] public async Task<IActionResult> UpdateAppointment(int id, Appointment model, CancellationToken ct)
    {
        var entity = await db.Appointments.FindAsync([id], ct); if (entity is null) return NotFound();
        entity.PatientId = model.PatientId; entity.DoctorId = model.DoctorId; entity.StartsAt = model.StartsAt;
        entity.DurationMinutes = model.DurationMinutes; entity.Status = model.Status; entity.Reason = model.Reason;
        await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpDelete("appointments/{id:int}")] public async Task<IActionResult> DeleteAppointment(int id, CancellationToken ct)
    {
        var entity = await db.Appointments.FindAsync([id], ct); if (entity is null) return NotFound();
        db.Appointments.Remove(entity); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpPut("patients/{id:int}")] public async Task<IActionResult> UpdatePatient(int id, Patient model, CancellationToken ct)
    {
        var entity = await db.Patients.FindAsync([id], ct); if (entity is null) return NotFound();
        entity.PhoneNumber = model.PhoneNumber; entity.DateOfBirth = model.DateOfBirth;
        await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpPost("patients")] public async Task<ActionResult<Patient>> CreatePatient(Patient model, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.Id == model.UserId && x.Role == UserRole.Patient, ct)) return BadRequest("User must be a patient.");
        db.Patients.Add(model); await db.SaveChangesAsync(ct); return Created($"/api/admin/patients/{model.Id}", model);
    }
    [HttpDelete("patients/{id:int}")] public async Task<IActionResult> DeletePatient(int id, CancellationToken ct)
    {
        var entity = await db.Patients.FindAsync([id], ct); if (entity is null) return NotFound();
        if (await db.Appointments.AnyAsync(x => x.PatientId == id, ct)) return Conflict("Patient has appointments.");
        db.Patients.Remove(entity); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpGet("medical-records")] public async Task<ActionResult<List<MedicalRecord>>> Records(CancellationToken ct) =>
        await db.MedicalRecords.Include(x => x.Appointment).AsNoTracking().ToListAsync(ct);
    [HttpPost("medical-records")] public async Task<ActionResult<MedicalRecord>> CreateRecord(MedicalRecord model, CancellationToken ct)
    {
        db.MedicalRecords.Add(model); await db.SaveChangesAsync(ct); return Created($"/api/admin/medical-records/{model.Id}", model);
    }
    [HttpPut("medical-records/{id:int}")] public async Task<IActionResult> UpdateRecord(int id, MedicalRecord model, CancellationToken ct)
    {
        var entity = await db.MedicalRecords.FindAsync([id], ct); if (entity is null) return NotFound();
        entity.Diagnosis = model.Diagnosis; entity.Notes = model.Notes; await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpDelete("medical-records/{id:int}")] public async Task<IActionResult> DeleteRecord(int id, CancellationToken ct)
    {
        var entity = await db.MedicalRecords.FindAsync([id], ct); if (entity is null) return NotFound();
        db.MedicalRecords.Remove(entity); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpGet("prescriptions")] public async Task<ActionResult<List<Prescription>>> Prescriptions(CancellationToken ct) =>
        await db.Prescriptions.Include(x => x.Items).AsNoTracking().ToListAsync(ct);
    [HttpPost("prescriptions")] public async Task<ActionResult<Prescription>> CreatePrescription(Prescription model, CancellationToken ct)
    {
        db.Prescriptions.Add(model); await db.SaveChangesAsync(ct); return Created($"/api/admin/prescriptions/{model.Id}", model);
    }
    [HttpPut("prescriptions/{id:int}")] public async Task<IActionResult> UpdatePrescription(int id, Prescription model, CancellationToken ct)
    {
        var entity = await db.Prescriptions.FindAsync([id], ct); if (entity is null) return NotFound();
        entity.MedicalRecordId = model.MedicalRecordId; await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpDelete("prescriptions/{id:int}")] public async Task<IActionResult> DeletePrescription(int id, CancellationToken ct)
    {
        var entity = await db.Prescriptions.FindAsync([id], ct); if (entity is null) return NotFound();
        db.Prescriptions.Remove(entity); await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpPut("prescription-items/{id:int}")] public async Task<IActionResult> UpdatePrescriptionItem(int id, PrescriptionItem model, CancellationToken ct)
    {
        var entity = await db.PrescriptionItems.FindAsync([id], ct); if (entity is null) return NotFound();
        entity.MedicationName = model.MedicationName; entity.Dosage = model.Dosage;
        entity.Frequency = model.Frequency; entity.DurationDays = model.DurationDays;
        await db.SaveChangesAsync(ct); return NoContent();
    }
    [HttpPost("prescription-items")] public async Task<ActionResult<PrescriptionItem>> CreatePrescriptionItem(PrescriptionItem model, CancellationToken ct)
    {
        db.PrescriptionItems.Add(model); await db.SaveChangesAsync(ct); return Created($"/api/admin/prescription-items/{model.Id}", model);
    }
    [HttpDelete("prescription-items/{id:int}")] public async Task<IActionResult> DeletePrescriptionItem(int id, CancellationToken ct)
    {
        var entity = await db.PrescriptionItems.FindAsync([id], ct); if (entity is null) return NotFound();
        db.PrescriptionItems.Remove(entity); await db.SaveChangesAsync(ct); return NoContent();
    }
}
