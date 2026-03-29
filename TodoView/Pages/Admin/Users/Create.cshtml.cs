using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TodoView.Authorization;
using TodoView.Models;

namespace TodoView.Pages.Admin.Users;

[Authorize(Roles = AppRoles.Admin)]
public class CreateModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public CreateModel(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IList<string> AvailableRoles { get; private set; } = new List<string>();
    public string ModalTitle { get; private set; } = string.Empty;
    public string ModalDescription { get; private set; } = string.Empty;
    public string SubmitButtonText { get; private set; } = string.Empty;
    public string FormAction { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadRolesAsync();
        ConfigureForm();
        return IsAjaxRequest() ? Partial("_CreateUserFormModal", this) : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadRolesAsync();
        ConfigureForm();

        if (!ModelState.IsValid)
        {
            return IsAjaxRequest() ? Partial("_CreateUserFormModal", this) : Page();
        }

        var existingUser = await _userManager.FindByEmailAsync(Input.Email);
        if (existingUser is not null)
        {
            ModelState.AddModelError("Input.Email", "A user with this email already exists.");
            return IsAjaxRequest() ? Partial("_CreateUserFormModal", this) : Page();
        }

        await EnsureRoleExistsAsync(Input.Role);

        var user = new User
        {
            UserName = Input.Email,
            Email = Input.Email,
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            Address = Input.Address,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, Input.Password);
        if (!createResult.Succeeded)
        {
            AddErrors(createResult);
            return IsAjaxRequest() ? Partial("_CreateUserFormModal", this) : Page();
        }

        var rolesToAdd = Input.Role == AppRoles.Admin
            ? new[] { AppRoles.Admin, AppRoles.User }
            : new[] { AppRoles.User };

        var addRoleResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
        if (!addRoleResult.Succeeded)
        {
            AddErrors(addRoleResult);
            return IsAjaxRequest() ? Partial("_CreateUserFormModal", this) : Page();
        }

        if (IsAjaxRequest())
        {
            return new JsonResult(new
            {
                success = true,
                reloadUrl = Url.Page("./Index", "ListPartial"),
                notification = new
                {
                    title = "User created",
                    message = $"Created {(GetUserLabel(user.FirstName, user.LastName, user.Email) ?? "the user")}.",
                    tone = "success"
                }
            });
        }

        SetPopup("User created", $"Created {(GetUserLabel(user.FirstName, user.LastName, user.Email) ?? "the user")}.");
        return RedirectToPage("./Index");
    }

    private Task LoadRolesAsync()
    {
        AvailableRoles = new List<string> { AppRoles.User, AppRoles.Admin };
        return Task.CompletedTask;
    }

    private void ConfigureForm()
    {
        ModalTitle = "Create User";
        ModalDescription = "Create a standard account or another admin without leaving the directory.";
        SubmitButtonText = "Create User";
        FormAction = Url.Page("./Create") ?? "./Create";
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unable to create role '{roleName}'.");
            }
        }

        if (!await _roleManager.RoleExistsAsync(AppRoles.User))
        {
            var result = await _roleManager.CreateAsync(new IdentityRole(AppRoles.User));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unable to create role '{AppRoles.User}'.");
            }
        }
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private void SetPopup(string title, string message, string tone = "success")
    {
        TempData["PopTitle"] = title;
        TempData["PopMessage"] = message;
        TempData["PopTone"] = tone;
    }

    private static string? GetUserLabel(string firstName, string lastName, string? email)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? email : fullName;
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = AppRoles.User;
    }
}
