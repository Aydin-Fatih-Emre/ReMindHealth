using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using ReMindHealth.Application.Interfaces.IServices;

namespace ReMindHealth.Components.Account.Pages;

public partial class ForgotPassword
{
    private string? errorMessage;
    private string? successMessage;
    private bool isSubmitting = false;
    private bool resetSuccessful = false;

    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private ILogger<ForgotPassword> Logger { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    public async Task ResetPasswordDirect()
    {
        if (isSubmitting) return;
        isSubmitting = true;
        errorMessage = null;
        successMessage = null;
        resetSuccessful = false;

        try
        {
            var success = await UserService.ResetPasswordAsync(Input.Email, Input.NewPassword);

            if (!success)
            {
                errorMessage = "Es wurde kein Konto mit dieser E-Mail-Adresse gefunden oder das Zurücksetzen ist fehlgeschlagen.";
                isSubmitting = false;
                return;
            }

            successMessage = "✓ Passwort erfolgreich zurückgesetzt! Sie sind jetzt angemeldet.";
            resetSuccessful = true;
            isSubmitting = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error resetting password");
            errorMessage = "Ein Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.";
            isSubmitting = false;
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
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        public string ConfirmPassword { get; set; } = "";
    }
}