using ReMindHealth.Application.Interfaces;
using ReMindHealth.Application.Interfaces.IRepositories;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Application.Services.Implementation.Domain;
using ReMindHealth.Application.Services.Implementation.External;
using ReMindHealth.Infrastructure.Data;
using ReMindHealth.Infrastructure.Repositories.Implementation;

namespace ReMindHealth;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Domain Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<INoteService, NoteService>();

        // External Services
        services.AddScoped<ITranscriptionService, AssemblyAITranscriptionService>();
        services.AddScoped<IExtractionService, GeminiExtractionService>();
        services.AddScoped<IDiseaseSearchService, GeminiDiseaseSearchService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();

        return services;
    }
}