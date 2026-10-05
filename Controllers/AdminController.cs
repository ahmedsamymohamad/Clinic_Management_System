using Clinic_Management_System.Data;
using Clinic_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Management_System.Controllers;

[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminController(ClinicDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.PatientCount = await db.Patients.CountAsync(cancellationToken);
        ViewBag.DoctorCount = await db.Doctors.CountAsync(cancellationToken);
        ViewBag.PendingCount = await db.Appointments.CountAsync(x => x.Status == AppointmentStatus.Pending, cancellationToken);
        ViewBag.Appointments = await db.Appointments.AsNoTracking()
            .Include(x => x.Patient).ThenInclude(x => x.User)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .OrderBy(x => x.StartsAt).ToListAsync(cancellationToken);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeAppointmentStatus(int id, AppointmentStatus status, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments.FindAsync([id], cancellationToken);
        if (appointment is null) return NotFound();
        appointment.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
