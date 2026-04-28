using Microsoft.AspNetCore.Mvc;

namespace ClinicAdoNetApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AppointmentsController : ControllerBase
{
    private const string Scheduled = "Scheduled";
    private const string Completed = "Completed";
    private const string Cancelled = "Cancelled";

    private static bool IsValidStatus(string status)
    {
        return status.Trim() is Scheduled or Completed or Cancelled;
    }
}