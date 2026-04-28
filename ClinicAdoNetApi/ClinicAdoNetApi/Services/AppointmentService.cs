using System.Data;
using ClinicAdoNetApi.DTOs;
using Microsoft.Data.SqlClient;

namespace ClinicAdoNetApi.Services;

public class AppointmentService(IConfiguration configuration) : IAppointmentService
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
                                                ?? throw new InvalidOperationException(
                                                    "Connection string DefaultConnection not found.");

    public async Task<List<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName, int? idDoctor)
    {
        status = Normalize(status);
        patientLastName = Normalize(patientLastName);

        var appointments = new List<AppointmentListDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = """
                  SELECT
                      a.IdAppointment,
                      a.AppointmentDate,
                      a.Status,
                      a.Reason,
                      p.FirstName + N' ' + p.LastName AS PatientFullName,
                      p.Email AS PatientEmail
                  FROM dbo.Appointments a
                  JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                  WHERE (@Status IS NULL OR a.Status = @Status)
                    AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
                    AND (@IdDoctor IS NULL OR a.IdDoctor = @IdDoctor)
                  ORDER BY a.AppointmentDate;
                  """;

        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value =
            (object?)status ?? DBNull.Value;

        command.Parameters.Add("@PatientLastName", SqlDbType.NVarChar, 80).Value =
            (object?)patientLastName ?? DBNull.Value;

        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value =
            (object?)idDoctor ?? DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            appointments.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5)
            });

        return appointments;
    }

    private static string? Normalize(string? value)
    {
        value = value?.Trim();

        return string.IsNullOrEmpty(value) ? null : value;
    }
}