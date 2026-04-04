using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TodoView.Authorization;
using TodoView.Models;

namespace TodoView.Pages.Admin.Users;

[Authorize(Roles = AppRoles.Admin)]
public class IndexModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public IndexModel(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public IList<UserListItem> Users { get; private set; } = new List<UserListItem>();
    public IList<string> AvailableRoles { get; private set; } = new List<string> { AppRoles.User, AppRoles.Admin };

    [BindProperty]
    public CreateUserInputModel CreateInput { get; set; } = new();

    [BindProperty]
    public EditUserInputModel EditInput { get; set; } = new();

    public UserDeleteViewModel UserToDelete { get; private set; } = new();
    public UserDetailsViewModel UserDetails { get; private set; } = new();

    public string ModalTitle { get; private set; } = string.Empty;
    public string ModalDescription { get; private set; } = string.Empty;
    public string SubmitButtonText { get; private set; } = string.Empty;
    public string FormAction { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadUsersAsync();
    }

    public async Task<PartialViewResult> OnGetListPartialAsync()
    {
        await LoadUsersAsync();
        return Partial("_UserListPartial", this);
    }

    public IActionResult OnGetCreateModal()
    {
        CreateInput = new CreateUserInputModel();
        ConfigureCreateForm();
        return Partial("_CreateUserFormModal", this);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        ConfigureCreateForm();
        ModelState.Clear();
        TryValidateModel(CreateInput, nameof(CreateInput));

        if (!ModelState.IsValid)
        {
            return Partial("_CreateUserFormModal", this);
        }

        var existingUser = await _userManager.FindByEmailAsync(CreateInput.Email);
        if (existingUser is not null)
        {
            ModelState.AddModelError("CreateInput.Email", "A user with this email already exists.");
            return Partial("_CreateUserFormModal", this);
        }

        await EnsureRoleExistsAsync(CreateInput.Role);

        var user = new User
        {
            UserName = CreateInput.Email,
            Email = CreateInput.Email,
            FirstName = CreateInput.FirstName,
            LastName = CreateInput.LastName,
            Address = CreateInput.Address,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, CreateInput.Password);
        if (!createResult.Succeeded)
        {
            AddErrors(createResult);
            return Partial("_CreateUserFormModal", this);
        }

        var rolesToAdd = CreateInput.Role == AppRoles.Admin
            ? new[] { AppRoles.Admin, AppRoles.User }
            : new[] { AppRoles.User };

        var addRoleResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
        if (!addRoleResult.Succeeded)
        {
            AddErrors(addRoleResult);
            return Partial("_CreateUserFormModal", this);
        }

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

    public async Task<IActionResult> OnGetEditModalAsync(string? id)
    {
        var user = await _userManager.FindByIdAsync(id ?? string.Empty);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        EditInput = new EditUserInputModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Address = user.Address,
            Email = user.Email ?? string.Empty,
            Role = roles.Contains(AppRoles.Admin) ? AppRoles.Admin : AppRoles.User
        };

        ConfigureEditForm(user.Id);
        return Partial("_EditUserFormModal", this);
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        ConfigureEditForm(EditInput.Id);
        ModelState.Clear();
        TryValidateModel(EditInput, nameof(EditInput));

        if (!ModelState.IsValid)
        {
            return Partial("_EditUserFormModal", this);
        }

        var user = await _userManager.FindByIdAsync(EditInput.Id);
        if (user is null)
        {
            return NotFound();
        }

        var otherUser = await _userManager.FindByEmailAsync(EditInput.Email);
        if (otherUser is not null && otherUser.Id != user.Id)
        {
            ModelState.AddModelError("EditInput.Email", "A user with this email already exists.");
            return Partial("_EditUserFormModal", this);
        }

        user.FirstName = EditInput.FirstName;
        user.LastName = EditInput.LastName;
        user.Address = EditInput.Address;

        if (!string.Equals(user.Email, EditInput.Email, StringComparison.OrdinalIgnoreCase))
        {
            var setEmailResult = await _userManager.SetEmailAsync(user, EditInput.Email);
            if (!setEmailResult.Succeeded)
            {
                AddErrors(setEmailResult);
                return Partial("_EditUserFormModal", this);
            }

            var setUserNameResult = await _userManager.SetUserNameAsync(user, EditInput.Email);
            if (!setUserNameResult.Succeeded)
            {
                AddErrors(setUserNameResult);
                return Partial("_EditUserFormModal", this);
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
                return Partial("_EditUserFormModal", this);
            }
        }

        var rolesToAdd = EditInput.Role == AppRoles.Admin
            ? new[] { AppRoles.Admin, AppRoles.User }
            : new[] { AppRoles.User };

        var addRolesResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
        if (!addRolesResult.Succeeded)
        {
            AddErrors(addRolesResult);
            return Partial("_EditUserFormModal", this);
        }

        if (!string.IsNullOrWhiteSpace(EditInput.NewPassword))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetPasswordResult = await _userManager.ResetPasswordAsync(user, resetToken, EditInput.NewPassword);
            if (!resetPasswordResult.Succeeded)
            {
                AddErrors(resetPasswordResult);
                return Partial("_EditUserFormModal", this);
            }
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            AddErrors(updateResult);
            return Partial("_EditUserFormModal", this);
        }

        return new JsonResult(new
        {
            success = true,
            reloadUrl = Url.Page("./Index", "ListPartial"),
            notification = new
            {
                title = "User updated",
                message = $"Updated {(GetUserLabel(user.FirstName, user.LastName, user.Email) ?? "the user")}.",
                tone = "success"
            }
        });
    }

    public async Task<IActionResult> OnGetDetailsModalAsync(string? id)
    {
        var user = await _userManager.FindByIdAsync(id ?? string.Empty);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        UserDetails = new UserDetailsViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Address = user.Address,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };

        return Partial("_UserDetailsModal", this);
    }

    public async Task<IActionResult> OnGetDeleteModalAsync(string? id)
    {
        var user = await _userManager.FindByIdAsync(id ?? string.Empty);
        if (user is null)
        {
            return NotFound();
        }

        UserToDelete = new UserDeleteViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}".Trim()
        };

        return Partial("_UserDeleteModal", this);
    }

    public async Task<IActionResult> OnPostDeleteAsync(string? id)
    {
        RemoveModelStatePrefix(nameof(CreateInput));
        RemoveModelStatePrefix(nameof(EditInput));

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
            AddErrors(deleteResult);
            return await ReloadDeleteStateAsync(id);
        }

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

    private async Task<IActionResult> ReloadDeleteStateAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        UserToDelete = new UserDeleteViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}".Trim()
        };

        return Partial("_UserDeleteModal", this);
    }

    private void ConfigureCreateForm()
    {
        ModalTitle = "Create User";
        ModalDescription = "Create a standard account or another admin without leaving the directory.";
        SubmitButtonText = "Create User";
        FormAction = Url.Page("./Index", "Create") ?? "./Index?handler=Create";
    }

    private void ConfigureEditForm(string userId)
    {
        ModalTitle = "Edit User";
        ModalDescription = "Adjust account details, roles, or credentials without leaving the admin list.";
        SubmitButtonText = "Save Changes";
        FormAction = Url.Page("./Index", "Edit", new { id = userId }) ?? $"./Index?handler=Edit&id={userId}";
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

    private void RemoveModelStatePrefix(string prefix)
    {
        var keys = ModelState.Keys
            .Where(key => key.Equals(prefix, StringComparison.Ordinal) || key.StartsWith($"{prefix}.", StringComparison.Ordinal))
            .ToList();

        foreach (var key in keys)
        {
            ModelState.Remove(key);
        }
    }

    private async Task LoadUsersAsync()
    {
        var users = _userManager.Users.OrderBy(user => user.Email).ToList();
        var items = new List<UserListItem>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count == 0)
            {
                roles = new List<string> { AppRoles.User };
            }

            items.Add(new UserListItem
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                CreatedAt = user.CreatedAt,
                Roles = roles.OrderBy(role => role).ToList()
            });
        }

        Users = items;
    }

    private static string? GetUserLabel(string firstName, string lastName, string? email)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? email : fullName;
    }

    public class UserListItem
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }

    public class CreateUserInputModel
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

    public class EditUserInputModel
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

    public class UserDeleteViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class UserDetailsViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
