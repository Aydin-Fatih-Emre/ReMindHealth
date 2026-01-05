using ReMindHealth.Application.Interfaces;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Domain.Models;


namespace ReMindHealth.Application.Services.Implementation.Domain;

public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;

    public AppointmentService(
        IUnitOfWork unitOfWork,
        IUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _userService = currentUserService;
    }

    public Task<ExtractedAppointment?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return _unitOfWork.AppointmentRepository.GetByIdAsync(appointmentId, cancellationToken);
    }

    public async Task<List<ExtractedAppointment>> GetUserAppointmentsAsync(CancellationToken cancellationToken = default)
    {
        var userId = await _userService.GetCurrentUserIdAsync();
        return await _unitOfWork.AppointmentRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<List<ExtractedAppointment>> GetUpcomingAppointmentsAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var userId = await _userService.GetCurrentUserIdAsync();
        return await _unitOfWork.AppointmentRepository.GetUpcomingByUserIdAsync(userId, days, cancellationToken);
    }

    public async Task<ExtractedAppointment> CreateAppointmentAsync(ExtractedAppointment appointment, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.AppointmentRepository.AddAsync(appointment, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        return appointment;
    }

    public async Task UpdateAppointmentAsync(ExtractedAppointment appointment, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.AppointmentRepository.UpdateAsync(appointment, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task DeleteAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.AppointmentRepository.DeleteAsync(appointmentId, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}