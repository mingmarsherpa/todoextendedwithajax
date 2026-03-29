using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TodoView.Authorization;
using TodoView.Models;

namespace TodoView.Pages.Admin.Users;

[Authorize(Roles = AppRoles.Admin)]
public class DeleteModel : PageModel
{
    private readonly UserManager<User> _userManager;

    public DeleteModel(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public UserDeleteViewModel UserDetails { get; private set; } = new();

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

        UserDetails = new UserDeleteViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}".Trim()
        };

        return IsAjaxRequest() ? Partial("_UserDeleteModal", this) : Page();
    }

    public async Task<IActionResult> OnPostAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var currentAdminId = _userManager.GetUserId(User);
        if (currentAdminId == id)
        {
            ModelState.AddModelError(string.Empty, "You cannot delete the currently signed-in admin account.");
            return await ReloadDeleteStateAsync(id);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var userLabel = GetUserLabel(user.FirstName, user.LastName, user.Email) ?? "the user";
        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            foreach (var error in deleteResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return await ReloadDeleteStateAsync(id);
        }

        if (IsAjaxRequest())
        {
            return new JsonResult(new
            {
                success = true,
                reloadUrl = Url.Page("./Index", "ListPartial"),
                notification = new
                {
                    title = "User deleted",
                    message = $"Deleted {userLabel}.",
                    tone = "success"
                }
            });
        }

        SetPopup("User deleted", $"Deleted {userLabel}.");
        return RedirectToPage("./Index");
    }

    private async Task<IActionResult> ReloadDeleteStateAsync(string id)
    {
        await OnGetAsync(id);
        return IsAjaxRequest() ? Partial("_UserDeleteModal", this) : Page();
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
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

    public class UserDeleteViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
