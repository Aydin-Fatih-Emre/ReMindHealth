using Microsoft.AspNetCore.Components;
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
    private bool showEditModal = false;
    private bool showDeleteConfirm = false;
    private ExtractedAppointment editTermin = new();

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


    private void OpenEditModal()
    {
        if (selectedTermin == null) return;

        editTermin = new ExtractedAppointment
        {
            AppointmentId = selectedTermin.AppointmentId,
            UserId = selectedTermin.UserId,
            ConversationId = selectedTermin.ConversationId,
            Title = selectedTermin.Title,
            Description = selectedTermin.Description,
            Location = selectedTermin.Location,
            AppointmentDateTime = selectedTermin.AppointmentDateTime,
            DurationMinutes = selectedTermin.DurationMinutes,
            AttendeeNames = selectedTermin.AttendeeNames,
            ConfidenceScore = selectedTermin.ConfidenceScore,
            CreatedAt = selectedTermin.CreatedAt
        };
        errorMessage = string.Empty;
        showEditModal = true;
    }

    private void CloseEditModal()
    {
        showEditModal = false;
        editTermin = new();
        errorMessage = string.Empty;
    }

    private async Task SaveEditTermin()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(editTermin.Title))
            {
                errorMessage = "Titel ist erforderlich";
                return;
            }

            if (editTermin.AppointmentDateTime == default)
            {
                errorMessage = "Datum und Uhrzeit sind erforderlich";
                return;
            }

            editTermin.AppointmentDateTime = editTermin.AppointmentDateTime.ToUniversalTime();

            await AppointmentService.UpdateAppointmentAsync(editTermin);

            await LoadAppointments();

            // Update selected termin with new data
            selectedTermin = termine.FirstOrDefault(t => t.AppointmentId == editTermin.AppointmentId);

            CloseEditModal();
        }
        catch (Exception ex)
        {
            errorMessage = $"Fehler beim Aktualisieren: {ex.Message}";
            Console.WriteLine($"Error updating appointment: {ex.Message}");
        }
    }

    private void OpenDeleteConfirm()
    {
        if (selectedTermin == null) return;
        showDeleteConfirm = true;
    }

    private void CloseDeleteConfirm()
    {
        showDeleteConfirm = false;
    }

    private async Task ConfirmDelete()
    {
        try
        {
            if (selectedTermin == null) return;

            await AppointmentService.DeleteAppointmentAsync(selectedTermin.AppointmentId);

            var deletedId = selectedTermin.AppointmentId;
            selectedTermin = null;

            await LoadAppointments();

            CloseDeleteConfirm();
        }
        catch (Exception ex)
        {
            errorMessage = $"Fehler beim Löschen: {ex.Message}";
            Console.WriteLine($"Error deleting appointment: {ex.Message}");
        }
    }
}