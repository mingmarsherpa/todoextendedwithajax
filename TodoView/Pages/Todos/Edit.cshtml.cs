using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TodoView.Models;

namespace TodoView.Pages.Todos
{
    public class EditModel : PageModel
    {
        private readonly TodoView.Data.TodoDbContext _context;
        private readonly UserManager<User> _userManager; // 1. Added UserManager

        public EditModel(TodoView.Data.TodoDbContext context, UserManager<User> userManager)
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

            // 2. Security: Only find the Todo if it belongs to the current user
            var todo = await _context.TodoItems
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (todo == null) return NotFound();

            Todo = todo;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 3. Prevent validation errors for fields we set manually
            ModelState.Remove("Todo.UserId");
            ModelState.Remove("Todo.User");

            if (!ModelState.IsValid) return Page();

            var userId = _userManager.GetUserId(User);

            // 4. Double-check ownership before saving
            var existsAndOwned = await _context.TodoItems
                .AnyAsync(t => t.Id == Todo.Id && t.UserId == userId);

            if (!existsAndOwned) return NotFound();

            // 5. Ensure the UserId isn't changed by a malicious form post
            Todo.UserId = userId;

            _context.Attach(Todo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TodoExists(Todo.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private bool TodoExists(int id)
        {
            var userId = _userManager.GetUserId(User);
            return _context.TodoItems.Any(e => e.Id == id && e.UserId == userId);
        }
    }
}