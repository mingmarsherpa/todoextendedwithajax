using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TodoView.Authorization;
using TodoView.Models;

namespace TodoView.Pages.Admin.Users;

[Authorize(Roles = AppRoles.Admin)]
public class EditModel : PageModel
{
    private readonly UserManager<User> _userManager;

    public EditModel(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IList<string> AvailableRoles { get; private set; } = new List<string> { AppRoles.User, AppRoles.Admin };

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        Input = new InputModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Address = user.Address,
            Email = user.Email ?? string.Empty,
            Role = roles.Contains(AppRoles.Admin) ? AppRoles.Admin : AppRoles.User
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByIdAsync(Input.Id);
        if (user is null)
        {
            return NotFound();
        }

        var otherUser = await _userManager.FindByEmailAsync(Input.Email);
        if (otherUser is not null && otherUser.Id != user.Id)
        {
            ModelState.AddModelError("Input.Email", "A user with this email already exists.");
            return Page();
        }

        user.FirstName = Input.FirstName;
        user.LastName = Input.LastName;
        user.Address = Input.Address;

        if (!string.Equals(user.Email, Input.Email, StringComparison.OrdinalIgnoreCase))
        {
            var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
            if (!setEmailResult.Succeeded)
            {
                AddErrors(setEmailResult);
                return Page();
            }

            var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.Email);
            if (!setUserNameResult.Succeeded)
            {
                AddErrors(setUserNameResult);
                return Page();
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var managedRoles = currentRoles.Where(role => role is AppRoles.Admin or AppRoles.User).ToList();
        if (managedRoles.Any())
        {
            var removeRoleResult = await _userManager.RemoveFromRolesAsync(user, managedRoles);
            if (!removeRoleResult.Succeeded)
            {
                AddErrors(removeRoleResult);
                return Page();
            }
        }

        var rolesToAdd = Input.Role == AppRoles.Admin
            ? new[] { AppRoles.Admin, AppRoles.User }
            : new[] { AppRoles.User };

        var addRolesResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
        if (!addRolesResult.Succeeded)
        {
            AddErrors(addRolesResult);
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetPasswordResult = await _userManager.ResetPasswordAsync(user, resetToken, Input.NewPassword);
            if (!resetPasswordResult.Succeeded)
            {
                AddErrors(resetPasswordResult);
                return Page();
            }
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            AddErrors(updateResult);
            return Page();
        }

        TempData["StatusMessage"] = "User updated successfully.";
        return RedirectToPage("./Index");
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    public class InputModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

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
        public string Role { get; set; } = AppRoles.User;

        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword))]
        [Display(Name = "Confirm New Password")]
        public string? ConfirmPassword { get; set; }
    }
}
