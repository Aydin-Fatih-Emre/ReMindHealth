using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using ReMindHealth.Application.DTOs.Responses;
using ReMindHealth.Application.Interfaces.IServices;

namespace ReMindHealth.Components.Account.Pages;

public partial class AccountManager
{
    private UserInfoResponse? currentUser;
    private string? errorMessage;
    private string? successMessage;
    private bool isLoading = true;
    private bool isSubmitting = false;
    private bool showDeleteConfirmation = false;

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILogger<AccountManager> Logger { get; set; } = default!;

    [SupplyParameterFromForm]
    private ProfileInputModel ProfileInput { get; set; } = new();

    [SupplyParameterFromForm]
    private PasswordInputModel PasswordInput { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            currentUser = await UserService.GetCurrentUserInfoAsync();

            if (currentUser == null)
            {
                NavigationManager.NavigateTo("/Account/Login");
                return;
            }

            // Pre-fill form with current user data
            ProfileInput.FirstName = currentUser.FirstName;
            ProfileInput.LastName = currentUser.LastName;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading account settings");
            errorMessage = "Fehler beim Laden der Kontoeinstellungen.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task UpdateProfile()
    {
        if (isSubmitting || currentUser == null) return;

        isSubmitting = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            var success = await UserService.UpdateUserProfileAsync(
                currentUser.UserId,
                ProfileInput.FirstName,
                ProfileInput.LastName);

            if (success)
            {
                successMessage = "✓ Profil erfolgreich aktualisiert!";

                // Refresh current user data
                currentUser = await UserService.GetCurrentUserInfoAsync();
            }
            else
            {
                errorMessage = "Fehler beim Aktualisieren des Profils.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating profile");
            errorMessage = "Fehler beim Aktualisieren des Profils.";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private async Task ChangePassword()
    {
        if (isSubmitting || currentUser == null) return;

        isSubmitting = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            var success = await UserService.ChangePasswordAsync(
                currentUser.UserId,
                PasswordInput.OldPassword,
                PasswordInput.NewPassword);

            if (success)
            {
                successMessage = "✓ Passwort erfolgreich geändert!";
                PasswordInput = new PasswordInputModel();
            }
            else
            {
                errorMessage = "Das aktuelle Passwort ist falsch oder das neue Passwort erfüllt nicht die Anforderungen.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error changing password");
            errorMessage = "Fehler beim Ändern des Passworts.";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private void ShowDeleteConfirmation()
    {
        showDeleteConfirmation = true;
        errorMessage = null;
        successMessage = null;
    }

    private void CancelDelete()
    {
        showDeleteConfirmation = false;
    }

    private async Task DeleteAccount()
    {
        if (isSubmitting || currentUser == null) return;

        isSubmitting = true;
        errorMessage = null;

        try
        {
            var success = await UserService.DeleteAccountAsync(currentUser.UserId); 

            if (success)
            {
                NavigationManager.NavigateTo("/Account/Login", true);
            }
            else
            {
                errorMessage = "Fehler beim Löschen des Kontos.";
                isSubmitting = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting account");
            errorMessage = "Fehler beim Löschen des Kontos.";
            isSubmitting = false;
        }
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/dashboard");
    }

    private sealed class ProfileInputModel
    {
        [StringLength(50, ErrorMessage = "Vorname darf maximal 50 Zeichen lang sein.")]
        public string FirstName { get; set; } = "";

        [StringLength(50, ErrorMessage = "Nachname darf maximal 50 Zeichen lang sein.")]
        public string LastName { get; set; } = "";
    }

    private sealed class PasswordInputModel
    {
        [Required(ErrorMessage = "Aktuelles Passwort ist erforderlich")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; } = "";

        [Required(ErrorMessage = "Neues Passwort ist erforderlich")]
        [StringLength(100, ErrorMessage = "Das Passwort muss mindestens {2} Zeichen lang sein.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        public string ConfirmPassword { get; set; } = "";
    }
}