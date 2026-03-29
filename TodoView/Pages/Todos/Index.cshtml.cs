using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TodoView.Data;
using TodoView.Models;

namespace TodoView.Pages.Todos
{
    public class IndexModel : PageModel
    {
        private const string ListViewMode = "list";
        private const string CardViewMode = "card";
        private const string AllStatusFilter = "all";
        private const string ActiveStatusFilter = "active";
        private const string CompletedStatusFilter = "completed";
        private readonly TodoDbContext _context;
        private readonly UserManager<User> _userManager;

        public IndexModel(TodoDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Todo> Todos { get; set; } = new List<Todo>();
        public IReadOnlyList<TodoReminderClientModel> PendingReminders { get; private set; } = [];

        [BindProperty]
        public Todo Todo { get; set; } = new();

        [BindProperty(SupportsGet = true, Name = "view")]
        [ValidateNever]
        public string? ViewMode { get; set; }

        [BindProperty(SupportsGet = true, Name = "status")]
        [ValidateNever]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true, Name = "q")]
        [ValidateNever]
        public string? SearchTerm { get; set; }

        public string ModalTitle { get; private set; } = string.Empty;
        public string ModalDescription { get; private set; } = string.Empty;
        public string SubmitButtonText { get; private set; } = string.Empty;
        public string FormAction { get; private set; } = string.Empty;
        public bool ShowInlineForm { get; private set; }
        public bool IsCardView => ViewMode == CardViewMode;
        public bool IsListView => !IsCardView;
        public bool IsAllStatusFilter => StatusFilter == AllStatusFilter;
        public bool IsActiveStatusFilter => StatusFilter == ActiveStatusFilter;
        public bool IsCompletedStatusFilter => StatusFilter == CompletedStatusFilter;
        public bool HasActiveFilters => !IsAllStatusFilter || !string.IsNullOrWhiteSpace(SearchTerm);
        public int TotalTodoCount { get; private set; }
        public int CompletedTodoCount { get; private set; }
        public int ActiveTodoCount => TotalTodoCount - CompletedTodoCount;

        public sealed class TodoReminderClientModel
        {
            public int Id { get; init; }
            public string Title { get; init; } = string.Empty;
            public string ReminderAt { get; init; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            NormalizeFilters();
            await LoadTodosAsync();
        }

        public async Task<PartialViewResult> OnGetListPartialAsync()
        {
            NormalizeFilters();
            await LoadTodosAsync();
            return Partial("_TodoListPartial", this);
        }

        public async Task<IActionResult> OnGetCreateModalAsync()
        {
            NormalizeFilters();
            Todo = new Todo();
            ConfigureForm(
                "Create New Task",
                "Capture the task details without leaving your dashboard.",
                "Create Task",
                BuildPageUrl("Create")!);

            if (IsAjaxRequest())
            {
                return Partial("_TodoFormModal", this);
            }

            ShowInlineForm = true;
            await LoadTodosAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            NormalizeFilters();
            ModelState.Remove(nameof(ViewMode));
            ModelState.Remove(nameof(StatusFilter));
            ModelState.Remove(nameof(SearchTerm));
            ModelState.Remove("Todo.UserId");
            ModelState.Remove("Todo.User");
            ModelState.Remove("Todo.ReminderTriggeredAt");

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
                    BuildPageUrl("Create")!);

                if (IsAjaxRequest())
                {
                    return Partial("_TodoFormModal", this);
                }

                ShowInlineForm = true;
                await LoadTodosAsync();
                return Page();
            }

            Todo.UserId = user.Id;
            Todo.User = user;
            Todo.Title = Todo.Title.Trim();
            Todo.Description = Todo.Description?.Trim() ?? string.Empty;
            Todo.ReminderTriggeredAt = null;
            _context.TodoItems.Add(Todo);
            await _context.SaveChangesAsync();

            if (IsAjaxRequest())
            {
                return new JsonResult(new
                {
                    success = true,
                    reloadUrl = BuildPageUrl("ListPartial"),
                    notification = new
                    {
                        title = "Task created",
                        message = $"Created \"{Todo.Title}\".",
                        tone = "success"
                    }
                });
            }

            return RedirectToPage("./Index", BuildIndexRouteValues());
        }

        public async Task<IActionResult> OnGetEditModalAsync(int? id)
        {
            NormalizeFilters();
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
                BuildPageUrl("Edit")!);

            if (IsAjaxRequest())
            {
                return Partial("_TodoFormModal", this);
            }

            ShowInlineForm = true;
            await LoadTodosAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            NormalizeFilters();
            ModelState.Remove(nameof(ViewMode));
            ModelState.Remove(nameof(StatusFilter));
            ModelState.Remove(nameof(SearchTerm));
            ModelState.Remove("Todo.UserId");
            ModelState.Remove("Todo.User");
            ModelState.Remove("Todo.ReminderTriggeredAt");

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
                    BuildPageUrl("Edit")!);

                if (IsAjaxRequest())
                {
                    return Partial("_TodoFormModal", this);
                }

                ShowInlineForm = true;
                await LoadTodosAsync();
                return Page();
            }

            var normalizedReminderAt = Todo.ReminderAt;
            var reminderChanged = existingTodo.ReminderAt != normalizedReminderAt;

            existingTodo.Title = Todo.Title.Trim();
            existingTodo.Description = Todo.Description?.Trim() ?? string.Empty;
            existingTodo.IsDone = Todo.IsDone;
            existingTodo.ReminderAt = normalizedReminderAt;

            if (normalizedReminderAt is null || reminderChanged)
            {
                existingTodo.ReminderTriggeredAt = null;
            }

            await _context.SaveChangesAsync();

            if (IsAjaxRequest())
            {
                return new JsonResult(new
                {
                    success = true,
                    reloadUrl = BuildPageUrl("ListPartial"),
                    notification = new
                    {
                        title = "Task updated",
                        message = $"Updated \"{existingTodo.Title}\".",
                        tone = "success"
                    }
                });
            }

            return RedirectToPage("./Index", BuildIndexRouteValues());
        }

        public async Task<IActionResult> OnGetDetailsModalAsync(int? id)
        {
            NormalizeFilters();
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
            NormalizeFilters();
            var todo = await FindOwnedTodoAsync(id);
            if (todo is null)
            {
                return NotFound();
            }

            Todo = todo;
            return Partial("_TodoDeleteModal", this);
        }

        public async Task<IActionResult> OnPostTriggerReminderAsync(int id)
        {
            var todo = await FindOwnedTodoAsync(id);
            if (todo is null)
            {
                return new JsonResult(new { success = false })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (todo.IsDone || todo.ReminderAt is null || todo.ReminderTriggeredAt.HasValue)
            {
                return new JsonResult(new { success = false });
            }

            todo.ReminderTriggeredAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                notification = new
                {
                    title = "Reminder",
                    message = $"\"{todo.Title}\" is due now.",
                    tone = "info",
                    duration = 5000
                }
            });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            NormalizeFilters();
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
                reloadUrl = BuildPageUrl("ListPartial"),
                notification = new
                {
                    title = "Task deleted",
                    message = $"Deleted \"{todo.Title}\".",
                    tone = "success"
                }
            });
        }

        private async Task LoadTodosAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
            {
                Todos = new List<Todo>();
                TotalTodoCount = 0;
                CompletedTodoCount = 0;
                return;
            }

            var ownedTodos = _context.TodoItems
                .Where(t => t.UserId == userId);

            TotalTodoCount = await ownedTodos.CountAsync();
            CompletedTodoCount = await ownedTodos.CountAsync(t => t.IsDone);

            var filteredTodos = ownedTodos;

            var reminderTodos = await ownedTodos
                .AsNoTracking()
                .Where(t => !t.IsDone && t.ReminderAt.HasValue && !t.ReminderTriggeredAt.HasValue)
                .OrderBy(t => t.ReminderAt)
                .Select(t => new { t.Id, t.Title, t.ReminderAt })
                .ToListAsync();

            PendingReminders = reminderTodos
                .Select(t => new TodoReminderClientModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    ReminderAt = t.ReminderAt!.Value.ToString("yyyy-MM-ddTHH:mm:ss")
                })
                .ToList();

            filteredTodos = StatusFilter switch
            {
                ActiveStatusFilter => filteredTodos.Where(t => !t.IsDone),
                CompletedStatusFilter => filteredTodos.Where(t => t.IsDone),
                _ => filteredTodos
            };

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                filteredTodos = filteredTodos.Where(t =>
                    t.Title.Contains(SearchTerm) ||
                    t.Description.Contains(SearchTerm));
            }

            Todos = await filteredTodos
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

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private string? BuildPageUrl(string handler, object? routeValues = null)
        {
            var values = BuildIndexRouteValues(routeValues);
            return Url.Page("./Index", handler, values);
        }

        private RouteValueDictionary BuildIndexRouteValues(object? routeValues = null)
        {
            var values = new RouteValueDictionary(routeValues)
            {
                ["view"] = ViewMode
            };

            if (!IsAllStatusFilter)
            {
                values["status"] = StatusFilter;
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                values["q"] = SearchTerm;
            }

            return values;
        }

        private void NormalizeFilters()
        {
            ViewMode = string.Equals(ViewMode, CardViewMode, StringComparison.OrdinalIgnoreCase)
                ? CardViewMode
                : ListViewMode;

            StatusFilter = StatusFilter?.Trim().ToLowerInvariant() switch
            {
                ActiveStatusFilter => ActiveStatusFilter,
                CompletedStatusFilter => CompletedStatusFilter,
                _ => AllStatusFilter
            };

            SearchTerm = SearchTerm?.Trim() ?? string.Empty;
        }
    }
}
