using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TodoView.Models;

namespace TodoView.Pages.Todos
{
    public class DeleteModel : PageModel
    {
        private readonly TodoView.Data.TodoDbContext _context;
        private readonly UserManager<User> _userManager; // 1. Inject UserManager

        public DeleteModel(TodoView.Data.TodoDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Todo Todo { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            // 2. Only show the confirmation page if the user owns this item
            var todo = await _context.TodoItems
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (todo == null)
            {
                return NotFound();
            }

            Todo = todo;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            // 3. Security: Find the item specifically belonging to this user
            var todo = await _context.TodoItems
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (todo != null)
            {
                _context.TodoItems.Remove(todo);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}