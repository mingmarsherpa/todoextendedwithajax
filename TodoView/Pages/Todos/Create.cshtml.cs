using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TodoView.Models;

namespace TodoView.Pages.Todos
{
    public class CreateModel : PageModel
    {
        private readonly TodoView.Data.TodoDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(TodoView.Data.TodoDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Todo Todo { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Todo.UserId");
            ModelState.Remove("Todo.User");
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return Challenge(); // Redirects to login page
            }
            
            Todo.UserId = user.Id;
            Todo.User = user;
            _context.TodoItems.Add(Todo);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
