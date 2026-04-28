namespace ClinicAdoNetApi.Services;

public enum CreateAppointmentStatus
{
    Success,
    PatientNotFoundOrInactive,
    DoctorNotFoundOrInactive,
    DoctorTimeConflict
}