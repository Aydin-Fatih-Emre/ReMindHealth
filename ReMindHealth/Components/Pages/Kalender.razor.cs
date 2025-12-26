using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ReMindHealth.Data;
using ReMindHealth.Models;
using ReMindHealth.Services.Interfaces;
using System.Globalization;

namespace ReMindHealth.Components.Pages
{
    public partial class Kalender
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private ApplicationDbContext Context { get; set; } = default!;
        [Inject] private ICurrentUserService CurrentUserService { get; set; } = default!;

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
                var userId = await CurrentUserService.GetUserIdAsync();

                // Load all appointments for this user, ordered by date
                termine = await Context.ExtractedAppointments
                    .Include(a => a.Conversation)
                    .Where(a => a.UserId == userId &&
                               (a.ConversationId == null || !a.Conversation.IsDeleted))
                    .OrderBy(a => a.AppointmentDateTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading appointments: {ex.Message}");
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

                var userId = await CurrentUserService.GetUserIdAsync();

                newTermin.UserId = userId;
                newTermin.ConversationId = null;
                newTermin.AppointmentDateTime = newTermin.AppointmentDateTime.ToUniversalTime(); // Convert to UTC
                newTermin.CreatedAt = DateTime.UtcNow;

                Context.ExtractedAppointments.Add(newTermin);
                await Context.SaveChangesAsync();

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
        return "overdue"; // überfällig

    if (termin.AppointmentDateTime.Date == now.Date)
        return "today"; // heute

    if (termin.AppointmentDateTime <= now.AddDays(3))
        return "soon"; // bald fällig

    return "future"; // später
    }

    }
}