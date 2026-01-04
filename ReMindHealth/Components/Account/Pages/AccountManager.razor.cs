using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ReMindHealth.Data;

namespace ReMindHealth.Components.Account.Pages
{
    public partial class AccountManager
    {
        private ApplicationUser? currentUser;
        private string? errorMessage;
        private string? successMessage;
        private bool isLoading = true;
        private bool isSubmitting = false;
        private bool showDeleteConfirmation = false;

        [SupplyParameterFromForm]
        private ProfileInputModel ProfileInput { get; set; } = new();

        [SupplyParameterFromForm]
        private PasswordInputModel PasswordInput { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated != true)
                {
                    NavigationManager.NavigateTo("/Account/Login");
                    return;
                }

                currentUser = await UserManager.GetUserAsync(user);

                if (currentUser != null)
                {
                    // Pre-fill form with current user data
                    ProfileInput.FirstName = currentUser.FirstName ?? "";
                    ProfileInput.LastName = currentUser.LastName ?? "";
                    ProfileInput.DateOfBirth = currentUser.DateOfBirth;
                }
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
                currentUser.FirstName = ProfileInput.FirstName;
                currentUser.LastName = ProfileInput.LastName;
                currentUser.DateOfBirth = ProfileInput.DateOfBirth;

                var result = await UserManager.UpdateAsync(currentUser);

                if (result.Succeeded)
                {
                    successMessage = " Profil erfolgreich aktualisiert!";
                }
                else
                {
                    errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
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
                var result = await UserManager.ChangePasswordAsync(
                    currentUser,
                    PasswordInput.OldPassword,
                    PasswordInput.NewPassword
                );

                if (result.Succeeded)
                {
                    await SignInManager.RefreshSignInAsync(currentUser);
                    successMessage = " Passwort erfolgreich geändert!";
                    PasswordInput = new PasswordInputModel();
                }
                else
                {
                    errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
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
                currentUser.IsActive = false;
                var result = await UserManager.UpdateAsync(currentUser);

                if (result.Succeeded)
                {
                    await SignInManager.SignOutAsync();

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

            public DateTime? DateOfBirth { get; set; }
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
}