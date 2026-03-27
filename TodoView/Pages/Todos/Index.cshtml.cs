using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TodoView.Data;
using TodoView.Models;

namespace TodoView.Pages.Todos
{
    public class IndexModel : PageModel
    {
        private readonly TodoDbContext _context;
        private readonly UserManager<User> _userManager;

        public IndexModel(TodoDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Todo> Todos { get; set; } = new List<Todo>();

        [BindProperty]
        public Todo Todo { get; set; } = new();

        public string ModalTitle { get; private set; } = string.Empty;
        public string ModalDescription { get; private set; } = string.Empty;
        public string SubmitButtonText { get; private set; } = string.Empty;
        public string FormAction { get; private set; } = string.Empty;

        public async Task OnGetAsync()
        {
            await LoadTodosAsync();
        }

        public async Task<PartialViewResult> OnGetListPartialAsync()
        {
            await LoadTodosAsync();
            return Partial("_TodoListPartial", this);
        }

        public PartialViewResult OnGetCreateModal()
        {
            Todo = new Todo();
            ConfigureForm(
                "Create New Task",
                "Capture the task details without leaving your dashboard.",
                "Create Task",
                Url.Page("./Index", "Create")!);

            return Partial("_TodoFormModal", this);
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            ModelState.Remove("Todo.UserId");
            ModelState.Remove("Todo.User");

            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                ConfigureForm(
                    "Create New Task",
                    "Capture the task details without leaving your dashboard.",
                    "Create Task",
                    Url.Page("./Index", "Create")!);

                return Partial("_TodoFormModal", this);
            }

            Todo.UserId = user.Id;
            Todo.User = user;
            _context.TodoItems.Add(Todo);
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                reloadUrl = Url.Page("./Index", "ListPartial")
            });
        }

        public async Task<IActionResult> OnGetEditModalAsync(int? id)
        {
            var todo = await FindOwnedTodoAsync(id);
            if (todo is null)
            {
                return NotFound();
            }

            Todo = todo;
            ConfigureForm(
                "Edit Task",
                "Update the task and keep the list in place.",
                "Save Changes",
                Url.Page("./Index", "Edit")!);

            return Partial("_TodoFormModal", this);
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            ModelState.Remove("Todo.UserId");
            ModelState.Remove("Todo.User");

            var userId = _userManager.GetUserId(User);
            if (userId is null)
            {
                return Challenge();
            }

            var existingTodo = await _context.TodoItems.FirstOrDefaultAsync(t => t.Id == Todo.Id && t.UserId == userId);
            if (existingTodo is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ConfigureForm(
                    "Edit Task",
                    "Update the task and keep the list in place.",
                    "Save Changes",
                    Url.Page("./Index", "Edit")!);

                return Partial("_TodoFormModal", this);
            }

            existingTodo.Title = Todo.Title;
            existingTodo.Description = Todo.Description;
            existingTodo.IsDone = Todo.IsDone;

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                reloadUrl = Url.Page("./Index", "ListPartial")
            });
        }

        public async Task<IActionResult> OnGetDetailsModalAsync(int? id)
        {
            var todo = await FindOwnedTodoAsync(id);
            if (todo is null)
            {
                return NotFound();
            }

            Todo = todo;
            return Partial("_TodoDetailsModal", this);
        }

        public async Task<IActionResult> OnGetDeleteModalAsync(int? id)
        {
            var todo = await FindOwnedTodoAsync(id);
            if (todo is null)
            {
                return NotFound();
            }

            Todo = todo;
            return Partial("_TodoDeleteModal", this);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            var todo = await FindOwnedTodoAsync(id ?? Todo.Id);
            if (todo is null)
            {
                return NotFound();
            }

            _context.TodoItems.Remove(todo);
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                reloadUrl = Url.Page("./Index", "ListPartial")
            });
        }

        private async Task LoadTodosAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
            {
                Todos = new List<Todo>();
                return;
            }

            Todos = await _context.TodoItems
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.IsDone)
                .ThenBy(t => t.Title)
                .ToListAsync();
        }

        private async Task<Todo?> FindOwnedTodoAsync(int? id)
        {
            if (id is null)
            {
                return null;
            }

            var userId = _userManager.GetUserId(User);
            if (userId is null)
            {
                return null;
            }

            return await _context.TodoItems.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        }

        private void ConfigureForm(string title, string description, string submitText, string formAction)
        {
            ModalTitle = title;
            ModalDescription = description;
            SubmitButtonText = submitText;
            FormAction = formAction;
        }
    }
}
