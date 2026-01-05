using ReMindHealth.Application.Interfaces.IRepositories;

namespace ReMindHealth.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IConversationRepository ConversationRepository { get; }
    IAppointmentRepository AppointmentRepository { get; }
    ITaskRepository TaskRepository { get; }
    INoteRepository NoteRepository { get; }
    IUserRepository UserRepository { get; }

    Task SaveAsync(CancellationToken cancellationToken = default);
}