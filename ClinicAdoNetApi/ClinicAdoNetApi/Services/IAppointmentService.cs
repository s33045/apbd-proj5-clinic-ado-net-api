using ClinicAdoNetApi.DTOs;

namespace ClinicAdoNetApi.Services;

public interface IAppointmentService
{
    Task<List<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName, int? idDoctor);
    Task<AppointmentDetailsDto?> GetByIdAsync(int idAppointment);
    Task<(CreateAppointmentStatus Status, int? IdAppointment)> CreateAsync(CreateAppointmentRequestDto request);
    Task<UpdateAppointmentStatus> UpdateAsync(int idAppointment, UpdateAppointmentRequestDto request);
    Task<DeleteAppointmentStatus> DeleteAsync(int idAppointment);
}