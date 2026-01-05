using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Interfaces.IServices;

public interface IAppointmentService
{
    Task<ExtractedAppointment?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<List<ExtractedAppointment>> GetUserAppointmentsAsync(CancellationToken cancellationToken = default);
    Task<List<ExtractedAppointment>> GetUpcomingAppointmentsAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<ExtractedAppointment> CreateAppointmentAsync(ExtractedAppointment appointment, CancellationToken cancellationToken = default);
    Task UpdateAppointmentAsync(ExtractedAppointment appointment, CancellationToken cancellationToken = default);
    Task DeleteAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}