using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ReMindHealth.Repositories.Interfaces;
using ReMindHealth.Repositories.Implementations;

namespace ReMindHealth.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, object> _repositories = new();

    public UnitOfWork(ApplicationDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public IConversationRepository ConversationRepository =>
        GetRepositorySingleton<IConversationRepository>();

    public IAppointmentRepository AppointmentRepository =>
        GetRepositorySingleton<IAppointmentRepository>();

    public ITaskRepository TaskRepository =>
        GetRepositorySingleton<ITaskRepository>();

    public INoteRepository NoteRepository =>
        GetRepositorySingleton<INoteRepository>();


    private T GetRepositorySingleton<T>([CallerMemberName] string? repositoryName = null)
        where T : class
    {
        if (repositoryName is null)
        {
            throw new ArgumentNullException(nameof(repositoryName));
        }

        if (_repositories.ContainsKey(repositoryName))
        {
            return (T)_repositories[repositoryName];
        }

        var repository = _serviceProvider.GetRequiredService<T>();
        _repositories.AddOrUpdate(repositoryName, repository, (_, y) => y);
        return repository;
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}