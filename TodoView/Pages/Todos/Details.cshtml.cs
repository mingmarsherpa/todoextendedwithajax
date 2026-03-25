using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TodoView.Data;
using TodoView.Models;

namespace TodoView.Pages.Todos
{
    public class DetailsModel : PageModel
    {
        private readonly TodoView.Data.TodoDbContext _context;
        private readonly UserManager<User> _userManager;

        public DetailsModel(TodoView.Data.TodoDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Todo Todo { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var todo = await _context.TodoItems.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (todo is not null)
            {
                Todo = todo;

                return Page();
            }

            return NotFound();
        }
    }
}
