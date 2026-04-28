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