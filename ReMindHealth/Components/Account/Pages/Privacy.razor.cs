using Microsoft.AspNetCore.Components;
using ReMindHealth.Application.Interfaces.IServices;

namespace ReMindHealth.Components.Account.Pages;

public partial class Privacy
{
    private bool check1 = false;
    private bool check2 = false;
    private bool check3 = false;
    private bool isLoading = false;
    private bool hasCheckedPrivacy = false;

    private bool allChecked => check1 && check2 && check3;

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !hasCheckedPrivacy)
        {
            hasCheckedPrivacy = true;

            var userInfo = await UserService.GetCurrentUserInfoAsync();

            if (userInfo?.HasAcceptedPrivacy == true)
            {
                NavigationManager.NavigateTo("/dashboard");
            }
        }
    }

    private async Task AgreeClicked()
    {
        if (!allChecked) return;

        isLoading = true;

        try
        {
            var success = await UserService.AcceptPrivacyPolicyAsync();

            if (success)
            {
                NavigationManager.NavigateTo("/dashboard");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting privacy: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }
}