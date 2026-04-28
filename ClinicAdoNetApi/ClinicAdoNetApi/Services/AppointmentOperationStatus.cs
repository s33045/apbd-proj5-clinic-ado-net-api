namespace ClinicAdoNetApi.Services;

public enum CreateAppointmentStatus
{
    Success,
    PatientNotFoundOrInactive,
    DoctorNotFoundOrInactive,
    DoctorTimeConflict
}

public enum UpdateAppointmentStatus
{
    Success,
    NotFound,
    PatientNotFoundOrInactive,
    DoctorNotFoundOrInactive,
    DoctorTimeConflict,
    CompletedDateCannotBeChanged
}

public enum DeleteAppointmentStatus
{
    Success,
    NotFound,
    CompletedAppointment
}