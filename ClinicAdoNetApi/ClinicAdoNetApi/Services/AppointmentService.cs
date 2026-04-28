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

    public async Task<AppointmentDetailsDto?> GetByIdAsync(int idAppointment)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = """
                  SELECT
                      a.IdAppointment,
                      a.AppointmentDate,
                      a.Status,
                      a.Reason,
                      a.InternalNotes,
                      a.CreatedAt,

                      p.IdPatient,
                      p.FirstName + N' ' + p.LastName AS PatientFullName,
                      p.Email,
                      p.PhoneNumber,
                      p.DateOfBirth,

                      d.IdDoctor,
                      d.FirstName + N' ' + d.LastName AS DoctorFullName,
                      d.LicenseNumber,

                      s.Name AS SpecializationName
                  FROM dbo.Appointments a
                  JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                  JOIN dbo.Doctors d ON d.IdDoctor = a.IdDoctor
                  JOIN dbo.Specializations s ON s.IdSpecialization = d.IdSpecialization
                  WHERE a.IdAppointment = @IdAppointment;
                  """;

        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new AppointmentDetailsDto
        {
            IdAppointment = reader.GetInt32(0),
            AppointmentDate = reader.GetDateTime(1),
            Status = reader.GetString(2),
            Reason = reader.GetString(3),
            InternalNotes = reader.IsDBNull(4) ? null : reader.GetString(4),
            CreatedAt = reader.GetDateTime(5),

            IdPatient = reader.GetInt32(6),
            PatientFullName = reader.GetString(7),
            PatientEmail = reader.GetString(8),
            PatientPhoneNumber = reader.GetString(9),
            PatientDateOfBirth = reader.GetDateTime(10),

            IdDoctor = reader.GetInt32(11),
            DoctorFullName = reader.GetString(12),
            DoctorLicenseNumber = reader.GetString(13),
            SpecializationName = reader.GetString(14)
        };
    }

    public async Task<(CreateAppointmentStatus Status, int? IdAppointment)> CreateAsync(
        CreateAppointmentRequestDto request)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        if (!await ActivePatientExistsAsync(connection, request.IdPatient))
            return (CreateAppointmentStatus.PatientNotFoundOrInactive, null);

        if (!await ActiveDoctorExistsAsync(connection, request.IdDoctor))
            return (CreateAppointmentStatus.DoctorNotFoundOrInactive, null);

        if (await DoctorHasConflictAsync(connection, request.IdDoctor, request.AppointmentDate))
            return (CreateAppointmentStatus.DoctorTimeConflict, null);

        var sql = """
                  INSERT INTO dbo.Appointments
                      (IdPatient, IdDoctor, AppointmentDate, Status, Reason)
                  OUTPUT INSERTED.IdAppointment
                  VALUES
                      (@IdPatient, @IdDoctor, @AppointmentDate, N'Scheduled', @Reason);
                  """;

        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = request.IdPatient;
        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = request.IdDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = request.AppointmentDate;
        command.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = request.Reason.Trim();

        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());

        return (CreateAppointmentStatus.Success, newId);
    }

    private static async Task<bool> ActivePatientExistsAsync(SqlConnection connection, int idPatient)
    {
        var sql = """
                  SELECT COUNT(1)
                  FROM dbo.Patients
                  WHERE IdPatient = @IdPatient
                    AND IsActive = 1;
                  """;

        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = idPatient;

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());

        return count > 0;
    }

    private static async Task<bool> ActiveDoctorExistsAsync(SqlConnection connection, int idDoctor)
    {
        var sql = """
                  SELECT COUNT(1)
                  FROM dbo.Doctors
                  WHERE IdDoctor = @IdDoctor
                    AND IsActive = 1;
                  """;

        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());

        return count > 0;
    }

    private static async Task<bool> DoctorHasConflictAsync(
        SqlConnection connection,
        int idDoctor,
        DateTime appointmentDate)
    {
        var sql = """
                  SELECT COUNT(1)
                   FROM dbo.Appointments
                   WHERE IdDoctor = @IdDoctor
                     AND AppointmentDate = @AppointmentDate
                     AND Status = N'Scheduled'
                  """;

        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = appointmentDate;

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());

        return count > 0;
    }

    private static string? Normalize(string? value)
    {
        value = value?.Trim();

        return string.IsNullOrEmpty(value) ? null : value;
    }
}