namespace ClinicAdoNetApi.Services;

public class AppointmentService(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("ClinicAdoNetApi")
                                                ?? throw new InvalidOperationException("Connection string DefaultConnection not found.");
}