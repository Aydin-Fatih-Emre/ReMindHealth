using Microsoft.AspNetCore.Components;
using ReMindHealth.Application.Interfaces;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Domain.Models;

namespace ReMindHealth.Components.Pages;

public partial class Kalender
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IAppointmentService AppointmentService { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;

    private bool isLoading = true;
    private List<ExtractedAppointment> termine = new();
    private ExtractedAppointment? selectedTermin;
    private bool showAddModal = false;
    private ExtractedAppointment newTermin = new();
    private string errorMessage = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadAppointments();
    }

    private async Task LoadAppointments()
    {
        try
        {
            isLoading = true;
            termine = await AppointmentService.GetUserAppointmentsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading appointments: {ex.Message}");
            errorMessage = "Fehler beim Laden der Termine";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void SelectTermin(ExtractedAppointment termin)
    {
        selectedTermin = termin;
    }

    private void NavigateTo(string url)
    {
        NavigationManager.NavigateTo(url);
    }

    private void OpenAddModal()
    {
        newTermin = new ExtractedAppointment
        {
            AppointmentId = Guid.NewGuid(),
            AppointmentDateTime = DateTime.Now,
            CreatedAt = DateTime.UtcNow
        };
        errorMessage = string.Empty;
        showAddModal = true;
    }

    private void CloseAddModal()
    {
        showAddModal = false;
        newTermin = new();
        errorMessage = string.Empty;
    }

    private async Task SaveNewTermin()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newTermin.Title))
            {
                errorMessage = "Titel ist erforderlich";
                return;
            }

            if (newTermin.AppointmentDateTime == default)
            {
                errorMessage = "Datum und Uhrzeit sind erforderlich";
                return;
            }

            var userId = await UserService.GetCurrentUserIdAsync();

            newTermin.UserId = userId;
            newTermin.ConversationId = null;
            newTermin.AppointmentDateTime = newTermin.AppointmentDateTime.ToUniversalTime();
            newTermin.CreatedAt = DateTime.UtcNow;

            await AppointmentService.CreateAppointmentAsync(newTermin);

            await LoadAppointments();
            CloseAddModal();
        }
        catch (Exception ex)
        {
            errorMessage = $"Fehler beim Speichern: {ex.Message}";
            Console.WriteLine($"Error saving appointment: {ex.Message}");
        }
    }

    private string GetDueStatus(ExtractedAppointment termin)
    {
        var now = DateTime.UtcNow;

        if (termin.AppointmentDateTime < now)
            return "overdue";

        if (termin.AppointmentDateTime.Date == now.Date)
            return "today";

        if (termin.AppointmentDateTime <= now.AddDays(3))
            return "soon";

        return "future";
    }
}