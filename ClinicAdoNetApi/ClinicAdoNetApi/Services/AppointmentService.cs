using ClinicAdoNetApi.DTOs;

namespace ClinicAdoNetApi.Services;

public class AppointmentService(IConfiguration configuration) : IAppointmentService
{
    private readonly string _connectionString = configuration.GetConnectionString("ClinicAdoNetApi")
                                                ?? throw new InvalidOperationException("Connection string DefaultConnection not found.");

    public Task<List<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName, int? idDoctor)
    {
        throw  new NotImplementedException();
    }

    private static string? Normalize(string? value)
    {
        value = value?.Trim();
        
        return string.IsNullOrEmpty(value) ? null : value;
    }
}