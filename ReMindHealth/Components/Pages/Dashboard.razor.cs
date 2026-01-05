using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Domain.Models;
using ReMindHealth.Infrastructure.Data;

namespace ReMindHealth.Components.Pages
{
    public partial class Dashboard
    {
        private string greeting = "";
        private bool isLoading = true;
        private List<Conversation> recentConversations = new();

        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
        [Inject] private IConversationService ConversationService { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var appUser = await UserManager.GetUserAsync(user);

                if (appUser != null && !appUser.HasAcceptedPrivacy)
                {
                    NavigationManager.NavigateTo("/privacy");
                    return;
                }
            }

            await LoadDashboardAsync();
        }

        private async Task LoadDashboardAsync()
        {
            try
            {
                // Set greeting
                var berlinTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
                DateTime berlinTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, berlinTimeZone);
                int hour = berlinTime.Hour;

                if (hour >= 6 && hour < 12)
                {
                    greeting = "Guten Morgen 👋";
                }
                else if (hour >= 12 && hour < 18)
                {
                    greeting = "Guten Mittag ☀️";
                }
                else
                {
                    greeting = "Guten Abend 🌙";
                }
        
                var allConversations = await ConversationService.GetRecentConversationsAsync(10);
                recentConversations = allConversations
                    .Where(c => c.ProcessingStatus == "Completed" && !string.IsNullOrEmpty(c.Summary))
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dashboard: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private void NavigateTo(string url)
        {
            NavigationManager.NavigateTo(url);
        }
        private void NavigateToConversation(Guid conversationId)
        {
            NavigationManager.NavigateTo($"/summaries?id={conversationId}");
        }
    }
}