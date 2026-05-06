using HospitalManagamentSystem.Models;

namespace HospitalManagamentSystem.Services;

public interface IWhatsAppReminderService
{
    Task<(bool Success, string? Error)> SendAppointmentReminderAsync(Appointment appointment, CancellationToken cancellationToken);
}
