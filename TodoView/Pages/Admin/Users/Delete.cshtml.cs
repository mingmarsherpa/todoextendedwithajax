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

        return Page();
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
            return await OnGetAsync(id);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            foreach (var error in deleteResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return await OnGetAsync(id);
        }

        TempData["StatusMessage"] = "User deleted successfully.";
        return RedirectToPage("./Index");
    }

    public class UserDeleteViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
