using ClinicAdoNetApi.DTOs;
using ClinicAdoNetApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAdoNetApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    private const string Scheduled = "Scheduled";
    private const string Completed = "Completed";
    private const string Cancelled = "Cancelled";

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppointmentListDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? patientLastName,
        [FromQuery] int? idDoctor)
    {
        if (!string.IsNullOrWhiteSpace(status) && !IsValidStatus(status))
            return BadRequest(Error($"Invalid status: {status}"));

        if (idDoctor <= 0)
            return BadRequest(Error($"Invalid doctor id: {idDoctor}"));

        var appointments = await appointmentService.GetAllAsync(status, patientLastName, idDoctor);

        return Ok(appointments);
    }

    [HttpGet("{idAppointment:int}")]
    public async Task<ActionResult<AppointmentDetailsDto>> GetById(int idAppointment)
    {
        if (idAppointment <= 0)
            return BadRequest(Error($"Invalid appointment id: {idAppointment}"));

        var appointment = await appointmentService.GetByIdAsync(idAppointment);

        if (appointment is null)
            return NotFound(Error("Appointment not found"));

        return Ok(appointment);
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDetailsDto>> Create(CreateAppointmentRequestDto request)
    {
        string? message = null;

        if (request.IdPatient <= 0)
            message = "IdPatient must be greater than 0.";

        if (request.IdDoctor <= 0)
            message = "IdDoctor must be greater than 0.";

        if (request.AppointmentDate <= DateTime.Now)
            message = "Appointment date cannot be in the past.";

        if (string.IsNullOrWhiteSpace(request.Reason))
            message = "Reason cannot be empty.";

        if (request.Reason.Trim().Length > 250)
            message = "Reason cannot be longer than 250 characters.";

        if (message is not null)
            return BadRequest(Error(message));

        var result = await appointmentService.CreateAsync(request);

        switch (result.Status)
        {
            case CreateAppointmentStatus.PatientNotFoundOrInactive:
                return BadRequest(Error("Patient does not exist or is inactive."));

            case CreateAppointmentStatus.DoctorNotFoundOrInactive:
                return BadRequest(Error("Doctor does not exist or is inactive."));

            case CreateAppointmentStatus.DoctorTimeConflict:
                return Conflict(Error("Doctor already has a scheduled appointment at this date."));
        }

        var createdAppointment = await appointmentService.GetByIdAsync(result.IdAppointment!.Value);

        return CreatedAtAction(
            nameof(GetById),
            new { idAppointment = result.IdAppointment.Value },
            createdAppointment);
    }

    private static bool IsValidStatus(string status)
    {
        return status.Trim() is Scheduled or Completed or Cancelled;
    }

    private static ErrorResponseDto Error(string message)
    {
        return new ErrorResponseDto
        {
            Message = message
        };
    }
}