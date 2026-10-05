using Clinic_Management_System.Models;
using Clinic_Management_System.Services;
using Microsoft.AspNetCore.Mvc;

namespace Clinic_Management_System.Controllers;

[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController(IAppointmentService appointments) : ControllerBase
{
    /// <summary>Lists appointments ordered by their scheduled start time.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await appointments.GetAllAsync(cancellationToken));

    /// <summary>Gets one appointment by identifier.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var appointment = await appointments.GetByIdAsync(id, cancellationToken);
        return appointment is null ? NotFound(new ProblemDetails { Title = "Appointment not found" }) : Ok(appointment);
    }

    /// <summary>Books an appointment after validating the patient, doctor, and time conflict.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentResponse>> Book(
        [FromBody] BookAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await appointments.BookAsync(request, cancellationToken);
        if (result.Appointment is null)
            return Conflict(new ProblemDetails { Title = "Appointment could not be booked", Detail = result.Error });
        return CreatedAtAction(nameof(GetAll), new { id = result.Appointment.Id }, result.Appointment);
    }

    /// <summary>Accepts or rejects an appointment request.</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentResponse>> ChangeStatus(
        int id, [FromBody] ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await appointments.ChangeStatusAsync(id, request.Status, cancellationToken);
        if (result.Appointment is null)
            return result.Error == "Appointment not found."
                ? NotFound(new ProblemDetails { Title = "Appointment not found" })
                : Conflict(new ProblemDetails { Title = "Appointment status could not be changed", Detail = result.Error });
        return Ok(result.Appointment);
    }
}

/// <summary>Appointment status update payload.</summary>
public sealed record ChangeStatusRequest(AppointmentStatus Status);
