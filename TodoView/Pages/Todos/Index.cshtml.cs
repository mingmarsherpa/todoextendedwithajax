using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TodoView.Models;
using TodoView.Data;

namespace TodoView.Pages.Todos
{
    public class IndexModel : PageModel
    {
        private readonly TodoDbContext _context;
        private readonly UserManager<User> _userManager; // Add UserManager

        public IndexModel(TodoDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Todo> Todos { get; set; } = new List<Todo>();

        public async Task OnGetAsync()
        {
            // 1. Get the ID of the person currently logged in
            var userId = _userManager.GetUserId(User);

            if (userId != null)
            {
                // 2. Query the Users table, but "Include" their TodoItems
                // This is called Eager Loading
                var userWithItems = await _context.Users
                    .Include(u => u.TodoItems) 
                    .FirstOrDefaultAsync(u => u.Id == userId);

                // 3. Set the list to the user's specific items
                Todos = userWithItems?.TodoItems ?? new List<Todo>();
            }
        }
    }
}