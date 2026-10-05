using Clinic_Management_System.Data;
using Clinic_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Clinic_Management_System.Services;

/// <summary>Payload used to request a clinic appointment.</summary>
public sealed record BookAppointmentRequest(int PatientId, int DoctorId, DateTimeOffset StartsAt, int DurationMinutes, string? Reason);
/// <summary>Appointment data returned by the clinic API.</summary>
public sealed record AppointmentResponse(int Id, int PatientId, string PatientName, int DoctorId, string DoctorName,
    DateTimeOffset StartsAt, int DurationMinutes, AppointmentStatus Status, string? Reason);

public interface IAppointmentService
{
    Task<IReadOnlyList<AppointmentResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<AppointmentResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<(AppointmentResponse? Appointment, string? Error)> BookAsync(BookAppointmentRequest request, CancellationToken cancellationToken);
    Task<(AppointmentResponse? Appointment, string? Error)> ChangeStatusAsync(int id, AppointmentStatus status, CancellationToken cancellationToken);
}

public sealed class AppointmentService(ClinicDbContext db) : IAppointmentService
{
    public async Task<IReadOnlyList<AppointmentResponse>> GetAllAsync(CancellationToken cancellationToken)
        => await db.Appointments.AsNoTracking()
            .Include(x => x.Patient).ThenInclude(x => x.User)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .OrderBy(x => x.StartsAt)
            .Select(x => ToResponse(x))
            .ToListAsync(cancellationToken);

    public async Task<AppointmentResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => await db.Appointments.AsNoTracking()
            .Include(x => x.Patient).ThenInclude(x => x.User)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Where(x => x.Id == id)
            .Select(x => ToResponse(x))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<(AppointmentResponse? Appointment, string? Error)> BookAsync(
        BookAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (request.DurationMinutes is < 15 or > 120)
            return (null, "Appointment duration must be between 15 and 120 minutes.");

        if (!await db.Doctors.AnyAsync(x => x.Id == request.DoctorId, cancellationToken))
            return (null, "The selected doctor does not exist.");
        if (!await db.Patients.AnyAsync(x => x.Id == request.PatientId, cancellationToken))
            return (null, "The selected patient does not exist.");

        var end = request.StartsAt.AddMinutes(request.DurationMinutes);
        var hasConflict = await db.Appointments.AnyAsync(x =>
            x.DoctorId == request.DoctorId &&
            x.Status != AppointmentStatus.Rejected &&
            x.Status != AppointmentStatus.Cancelled &&
            request.StartsAt < x.StartsAt.AddMinutes(x.DurationMinutes) &&
            end > x.StartsAt, cancellationToken);
        if (hasConflict)
            return (null, "The doctor already has a conflicting appointment.");

        var appointment = new Appointment
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            StartsAt = request.StartsAt,
            DurationMinutes = request.DurationMinutes,
            Reason = request.Reason,
            Status = AppointmentStatus.Pending
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(appointment).Reference(x => x.Patient).LoadAsync(cancellationToken);
        await db.Entry(appointment.Patient).Reference(x => x.User).LoadAsync(cancellationToken);
        await db.Entry(appointment).Reference(x => x.Doctor).LoadAsync(cancellationToken);
        await db.Entry(appointment.Doctor).Reference(x => x.User).LoadAsync(cancellationToken);
        return (ToResponse(appointment), null);
    }

    public async Task<(AppointmentResponse? Appointment, string? Error)> ChangeStatusAsync(
        int id, AppointmentStatus status, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments
            .Include(x => x.Patient).ThenInclude(x => x.User)
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (appointment is null)
            return (null, "Appointment not found.");
        if (status == AppointmentStatus.Accepted)
        {
            var end = appointment.StartsAt.AddMinutes(appointment.DurationMinutes);
            var conflict = await db.Appointments.AnyAsync(x => x.Id != id &&
                x.DoctorId == appointment.DoctorId && x.Status == AppointmentStatus.Accepted &&
                appointment.StartsAt < x.StartsAt.AddMinutes(x.DurationMinutes) && end > x.StartsAt, cancellationToken);
            if (conflict)
                return (null, "The doctor has another accepted appointment at this time.");
        }
        appointment.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return (ToResponse(appointment), null);
    }

    private static AppointmentResponse ToResponse(Appointment x) => new(
        x.Id, x.PatientId, x.Patient.User.FullName, x.DoctorId, x.Doctor.User.FullName,
        x.StartsAt, x.DurationMinutes, x.Status, x.Reason);
}
