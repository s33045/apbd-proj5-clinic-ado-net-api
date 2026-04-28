using ClinicAdoNetApi.DTOs;

namespace ClinicAdoNetApi.Services;

public interface IAppointmentService
{
    Task<List<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName, int? idDoctor);
}