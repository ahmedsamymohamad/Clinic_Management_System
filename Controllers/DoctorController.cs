using System.Security.Claims;
using Clinic_Management_System.Data;
using Clinic_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Management_System.Controllers;

[Authorize(Roles = nameof(UserRole.Doctor))]
public sealed class DoctorController(ClinicDbContext db) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var doctor = await db.Doctors.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == UserId, cancellationToken);
        if (doctor is null) return NotFound();
        doctor = await db.Doctors.AsNoTracking().Include(x => x.User)
            .SingleAsync(x => x.Id == doctor.Id, cancellationToken);
        ViewBag.Doctor = doctor;
        ViewBag.Appointments = await db.Appointments.AsNoTracking()
            .Include(x => x.Patient).ThenInclude(x => x.User)
            .Include(x => x.MedicalRecord)
            .Where(x => x.DoctorId == doctor.Id)
            .OrderBy(x => x.StartsAt).ToListAsync(cancellationToken);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAppointmentStatus(int id, AppointmentStatus status, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments.Include(x => x.Doctor)
            .SingleOrDefaultAsync(x => x.Id == id && x.Doctor.UserId == UserId, cancellationToken);
        if (appointment is null) return NotFound();
        appointment.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Consultation(int id, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments.Include(x => x.Doctor)
            .Include(x => x.Patient).ThenInclude(x => x.User)
            .Include(x => x.MedicalRecord)
            .SingleOrDefaultAsync(x => x.Id == id && x.Doctor.UserId == UserId, cancellationToken);
        return appointment is null ? NotFound() : View(appointment);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Consultation(int id, string diagnosis, string notes, string? medicationName,
        string? dosage, string? frequency, int? durationDays, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments.Include(x => x.Doctor)
            .Include(x => x.Patient).ThenInclude(x => x.User)
            .Include(x => x.MedicalRecord)
            .SingleOrDefaultAsync(x => x.Id == id && x.Doctor.UserId == UserId, cancellationToken);
        if (appointment is null) return NotFound();
        if (string.IsNullOrWhiteSpace(diagnosis) || string.IsNullOrWhiteSpace(notes))
        {
            ModelState.AddModelError(string.Empty, "Diagnosis and notes are required.");
            return View(appointment);
        }
        appointment.MedicalRecord ??= new MedicalRecord { AppointmentId = appointment.Id };
        appointment.MedicalRecord.Diagnosis = diagnosis.Trim();
        appointment.MedicalRecord.Notes = notes.Trim();
        appointment.MedicalRecord.RecordedAt = DateTimeOffset.UtcNow;
        appointment.Status = AppointmentStatus.Completed;
        if (!string.IsNullOrWhiteSpace(medicationName))
        {
            var prescription = new Prescription { MedicalRecord = appointment.MedicalRecord };
            prescription.Items.Add(new PrescriptionItem
            {
                MedicationName = medicationName.Trim(),
                Dosage = dosage?.Trim() ?? string.Empty,
                Frequency = frequency?.Trim() ?? string.Empty,
                DurationDays = durationDays.GetValueOrDefault()
            });
            db.Prescriptions.Add(prescription);
        }
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
