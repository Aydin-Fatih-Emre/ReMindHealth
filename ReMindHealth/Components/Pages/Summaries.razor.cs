using Microsoft.AspNetCore.Components;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Domain.Models;

namespace ReMindHealth.Components.Pages
{
    public partial class Summaries
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private IConversationService ConversationService { get; set; } = default!;

        [SupplyParameterFromQuery(Name = "id")]
        public Guid? ConversationId { get; set; }

        private bool isLoading = true;
        private List<Conversation> conversations = new();
        private Conversation? selectedConversation;
        private Conversation? conversationDetails;
        private bool showOnlyFavorites = false;

        private List<Conversation> FilteredConversations =>
            showOnlyFavorites
                ? conversations.Where(c => c.IsFavorite).ToList()
                : conversations;

        protected override async Task OnInitializedAsync()
        {
            await LoadConversations();

            // Auto-select conversation if ID is provided in query string
            if (ConversationId.HasValue)
            {
                var conversation = conversations.FirstOrDefault(c => c.ConversationId == ConversationId.Value);
                if (conversation != null)
                {
                    await SelectConversation(conversation);
                }
            }
        }

        private async Task LoadConversations()
        {
            try
            {
                isLoading = true;
                conversations = await ConversationService.GetUserConversationsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading conversations: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task SelectConversation(Conversation conversation)
        {
            if (selectedConversation?.ConversationId == conversation.ConversationId)
            {
                selectedConversation = null;
                conversationDetails = null;
            }
            else
            {
                selectedConversation = conversation;
                conversationDetails = await ConversationService.GetConversationWithDetailsAsync(conversation.ConversationId);
            }
        }

        private async Task ToggleFavorite(Conversation conversation)
        {
            try
            {
                conversation.IsFavorite = !conversation.IsFavorite;
                await ConversationService.UpdateConversationAsync(conversation);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling favorite: {ex.Message}");
                // Revert on error
                conversation.IsFavorite = !conversation.IsFavorite;
            }
        }

        private string GetStatusText(string status)
        {
            return status switch
            {
                "Pending" => "Ausstehend",
                "Converting" => "Konvertiere...",
                "Transcribing" => "Transkribiere...",
                "Transcribed" => "Transkribiert",
                "Analyzing" => "Analysiere...",
                "Completed" => "Abgeschlossen",
                "Failed" => "Fehler",
                _ => status
            };
        }

        private void Zurueck()
        {
            NavigationManager.NavigateTo("/dashboard");
        }
    }
}