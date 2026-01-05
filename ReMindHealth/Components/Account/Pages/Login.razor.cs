using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace ReMindHealth.Components.Account.Pages;

public partial class Login
{
    private string? errorMessage;
    private bool isSubmitting = false;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [Inject] private SignInManager<Domain.Models.ApplicationUser> SignInManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILogger<Login> Logger { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    public async Task LoginUser()
    {
        if (isSubmitting) return;
        isSubmitting = true;
        errorMessage = null;

        var result = await SignInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            Logger.LogInformation("User {Email} logged in", Input.Email);
            NavigationManager.NavigateTo(ReturnUrl ?? "/dashboard");
        }
        else
        {
            errorMessage = "Ungültige Anmeldedaten. Bitte überprüfen Sie Ihre E-Mail und Ihr Passwort.";
            isSubmitting = false;
        }
    }

    private sealed class InputModel
    {
        [Required(ErrorMessage = "E-Mail ist erforderlich")]
        [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Passwort ist erforderlich")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Angemeldet bleiben?")]
        public bool RememberMe { get; set; }
    }
}