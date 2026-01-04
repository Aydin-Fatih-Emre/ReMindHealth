using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Radzen;
using ReMindHealth.Data;

namespace ReMindHealth.Components.Account.Pages
{
    public partial class Register
    {
        private string? errorMessage;
        private string? successMessage;
        private bool isSubmitting = false;

        [SupplyParameterFromForm]
        private InputModel Input { get; set; } = new();

        public async Task RegisterUser()
        {
            isSubmitting = true;
            errorMessage = null;
            successMessage = null;
            bool shouldNavigate = false;

            try
            {
                var user = CreateUser();
                await UserManager.SetUserNameAsync(user, Input.Email);
                await UserManager.SetEmailAsync(user, Input.Email);

                var result = await UserManager.CreateAsync(user, Input.Password);

                if (!result.Succeeded)
                {
                    errorMessage = string.Join(" ", result.Errors.Select(error => error.Description));
                    isSubmitting = false;
                    return;
                }

                // Auto-confirm and sign-in
                var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
                await UserManager.ConfirmEmailAsync(user, code);
                await SignInManager.SignInAsync(user, isPersistent: false);

                for (int i = 3; i > 0; i--)
                {
                    successMessage = $" Konto erfolgreich erstellt! Weiterleitung in {i} Sekunden...";
                    StateHasChanged();
                    await Task.Delay(1000);
                }
                shouldNavigate = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during registration");
                errorMessage = "Ein Fehler ist aufgetreten.";
                isSubmitting = false;
            }

            if (shouldNavigate)
            {
                NavigationManager.NavigateTo("/privacy");
            }
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'.");
            }
        }

        private sealed class InputModel
        {
            [Required(ErrorMessage = "E-Mail ist erforderlich")]
            [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Passwort ist erforderlich")]
            [StringLength(100, ErrorMessage = "Das Passwort muss mindestens {2} Zeichen lang sein.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Die Passwörter stimmen nicht überein.")]
            public string ConfirmPassword { get; set; } = "";
        }
    }
}