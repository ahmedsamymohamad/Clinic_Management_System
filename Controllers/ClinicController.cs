using Clinic_Management_System.Data;
using Clinic_Management_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Globalization;

namespace Clinic_Management_System.Controllers;

[Authorize]
public sealed class ClinicController(ClinicDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var query = db.Appointments.AsNoTracking()
            .Include(x => x.Patient).ThenInclude(x => x.User)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Include(x => x.MedicalRecord).ThenInclude(x => x!.Prescriptions).ThenInclude(x => x.Items)
            .AsQueryable();
        if (User.IsInRole(nameof(UserRole.Patient)))
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            query = query.Where(x => x.Patient.UserId == userId);
        }
        var appointments = await query.OrderBy(x => x.StartsAt).ToListAsync(cancellationToken);
        return View(appointments);
    }

    [HttpGet]
    public async Task<IActionResult> Book(CancellationToken cancellationToken)
    {
        if (!User.IsInRole(nameof(UserRole.Patient)))
        {
            TempData["BookingError"] = "Only patient accounts can book appointments. Please register or sign in as a patient.";
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Book), "Clinic") });
        }
        ViewBag.Doctors = await db.Doctors.AsNoTracking().Include(x => x.User).Include(x => x.Department).ToListAsync(cancellationToken);
        return View(new Appointment { StartsAt = DateTimeOffset.Now.AddDays(1).Date.AddHours(9) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(Appointment appointment, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(nameof(UserRole.Patient)))
        {
            TempData["BookingError"] = "Only patient accounts can book appointments. Please sign in as a patient.";
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Book), "Clinic") });
        }
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var patient = await db.Patients.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (patient is null)
        {
            TempData["BookingError"] = "Your patient profile could not be found. Please register a patient account.";
            return RedirectToAction("Login", "Account");
        }
        ModelState.Remove(nameof(appointment.PatientId));
        ModelState.Remove(nameof(appointment.DoctorId));
        ModelState.Remove(nameof(appointment.Patient));
        ModelState.Remove(nameof(appointment.Doctor));
        ModelState.Remove(nameof(appointment.StartsAt));
        ModelState.Remove(nameof(appointment.DurationMinutes));
        if (!int.TryParse(Request.Form["DoctorId"], out var doctorId))
            ModelState.AddModelError(nameof(appointment.DoctorId), "Please select a doctor.");
        else
            appointment.DoctorId = doctorId;
        if (!DateTime.TryParse(Request.Form["StartsAt"], CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var localStart))
            ModelState.AddModelError(nameof(appointment.StartsAt), "Please enter a valid appointment date and time.");
        else
            appointment.StartsAt = new DateTimeOffset(DateTime.SpecifyKind(localStart, DateTimeKind.Local));
        if (!int.TryParse(Request.Form["DurationMinutes"], out var duration))
            ModelState.AddModelError(nameof(appointment.DurationMinutes), "Please enter a duration.");
        else
            appointment.DurationMinutes = duration;
        if (!ModelState.IsValid)
            return await Book(cancellationToken);
        appointment.PatientId = patient.Id;
        var conflict = await db.Appointments.AnyAsync(x => x.DoctorId == appointment.DoctorId &&
            x.Status != AppointmentStatus.Rejected && x.Status != AppointmentStatus.Cancelled &&
            appointment.StartsAt < x.StartsAt.AddMinutes(x.DurationMinutes) &&
            appointment.StartsAt.AddMinutes(appointment.DurationMinutes) > x.StartsAt, cancellationToken);
        if (conflict)
        {
            ModelState.AddModelError(nameof(appointment.StartsAt), "The doctor already has a conflicting appointment.");
            return await Book(cancellationToken);
        }
        if (!await db.Doctors.AnyAsync(x => x.Id == appointment.DoctorId, cancellationToken))
        {
            ModelState.AddModelError(nameof(appointment.DoctorId), "Please select a valid doctor.");
            return await Book(cancellationToken);
        }
        if (appointment.DurationMinutes is < 15 or > 120)
        {
            ModelState.AddModelError(nameof(appointment.DurationMinutes), "Duration must be between 15 and 120 minutes.");
            return await Book(cancellationToken);
        }
        appointment.Status = AppointmentStatus.Pending;
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
