using ReMindHealth.Domain.Models;

namespace ReMindHealth.Application.Interfaces.IRepositories;

public interface IAppointmentRepository : IRepository<ExtractedAppointment>
{
    Task<List<ExtractedAppointment>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<List<ExtractedAppointment>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<ExtractedAppointment>> GetUpcomingByUserIdAsync(string userId, int days = 30, CancellationToken cancellationToken = default);
}