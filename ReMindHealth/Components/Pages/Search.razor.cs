using Microsoft.AspNetCore.Components.Web;
using ReMindHealth.Application.Services.Implementation.External;

namespace ReMindHealth.Components.Pages
{
    public partial class Search
    {
        private string searchQuery = "";
        private bool isSearching = false;
        private bool hasSearched = false;
        private DiseaseSearchResult? searchResult = null;

        private async Task PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
                return;

            try
            {
                isSearching = true;
                hasSearched = false;
                searchResult = null;

                searchResult = await DiseaseSearchService.SearchDiseaseAsync(searchQuery.Trim());
                hasSearched = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex.Message}");
                hasSearched = true;
            }
            finally
            {
                isSearching = false;
            }
        }

        private async Task HandleKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await PerformSearch();
            }
        }
    }
}