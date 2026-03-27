# TodoView

TodoView is an ASP.NET Core Razor Pages application with ASP.NET Core Identity for authentication and authorization. It manages todo items per user and includes an admin-only user management area.

## Stack

- ASP.NET Core Razor Pages
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity

## Project Structure

- `TodoView/Program.cs`: application startup, service registration, authorization rules, middleware
- `TodoView/Data/TodoDbContext.cs`: EF Core database context and Identity integration
- `TodoView/Models/User.cs`: application user entity
- `TodoView/Authorization/AppRoles.cs`: role name constants
- `TodoView/Data/Seed/IdentitySeeder.cs`: startup seeding for roles and admin account
- `TodoView/Areas/Identity/Pages/Account/Register.cshtml.cs`: self-service registration flow
- `TodoView/Pages/Admin/Users/*`: admin-only user management pages

## How Role Is Implemented

This project uses ASP.NET Core Identity roles. A role is not stored as a custom column on the `User` entity. Instead, the application uses Identity's built-in role system.

### 1. Role support is enabled at startup

In `TodoView/Program.cs`, Identity is configured like this:

- `AddDefaultIdentity<User>()` registers the Identity system for the custom `User` type.
- `AddRoles<IdentityRole>()` enables role support.
- `AddEntityFrameworkStores<TodoDbContext>()` stores Identity users and roles in the database through EF Core.

That means role information is managed by Identity, not by a custom property in `Models/User.cs`.

### 2. The database context uses Identity tables

`TodoView/Data/TodoDbContext.cs` inherits from `IdentityDbContext<User>`.

That is important because `IdentityDbContext<TUser>` automatically includes the schema needed for:

- users
- roles
- user-role joins
- claims
- logins
- tokens

So the role system is backed by Identity tables such as:

- `AspNetRoles`
- `AspNetUserRoles`

The `User` model in `TodoView/Models/User.cs` only contains application-specific profile fields:

- `FirstName`
- `LastName`
- `Address`
- `CreatedAt`
- `TodoItems`

There is no `Role` property on `User`. Role membership is resolved through Identity APIs such as `UserManager` and `RoleManager`.

### 3. Role names are centralized

The project defines role names in `TodoView/Authorization/AppRoles.cs`:

- `Admin`
- `User`

This avoids scattering raw string literals across the codebase and reduces typo risk.

## Role Lifecycle

The role lifecycle in this project has four parts:

1. role definitions
2. role creation
3. role assignment
4. role enforcement

### Role definitions

`AppRoles.cs` is the single source of truth for the two managed roles:

- `Admin`
- `User`

### Role creation

Role creation happens automatically during startup in `TodoView/Data/Seed/IdentitySeeder.cs`.

When the app starts, `Program.cs` creates a service scope and calls:

- `IdentitySeeder.SeedAsync(scope.ServiceProvider)`

Inside the seeder:

- it gets `RoleManager<IdentityRole>`
- it checks whether `Admin` exists
- it checks whether `User` exists
- it creates any missing role with `roleManager.CreateAsync(new IdentityRole(role))`

So roles do not need to be inserted manually before first run.

### Role assignment

Roles are assigned in three different flows.

#### A. Seeded admin account

`IdentitySeeder.cs` loads admin seed settings from configuration and ensures the configured admin account exists.

If the admin user does not exist:

- a new `User` is created
- the password from configuration is applied
- the account is marked `EmailConfirmed = true`

Then the seeder makes sure the admin account belongs to:

- `Admin`
- `User`

The app intentionally gives admins both roles. In this design, admin access is treated as an elevated user who should still retain standard user permissions.

#### B. Public registration flow

`TodoView/Areas/Identity/Pages/Account/Register.cshtml.cs` handles normal sign-up.

After a new account is created successfully:

- the code ensures the `User` role exists
- the new account is added to the `User` role with `AddToRoleAsync(user, AppRoles.User)`

So a regular registered user always gets the `User` role.

#### C. Admin user management flow

Admins can create and edit other users through `TodoView/Pages/Admin/Users`.

In `Create.cshtml.cs`:

- the selected role comes from the form
- if `Admin` is selected, the code adds both `Admin` and `User`
- otherwise it adds only `User`

In `Edit.cshtml.cs`:

- the code loads the current roles for the user
- it removes the managed roles (`Admin` and `User`)
- it reassigns roles based on the selected option
- selecting `Admin` again adds both `Admin` and `User`

This is why the UI effectively models two access levels:

- standard user
- admin user

Even though the underlying Identity system supports multiple roles, this project currently manages only those two.

### Default-role backfill

The seeder also loops through all existing users. If a user has no role at all, it assigns the `User` role.

That protects the app from older accounts or partial data where a user exists but role membership was never created.

## How Authorization Is Enforced

Authorization is enforced in two main ways:

- Razor Pages folder conventions
- `[Authorize]` attributes

### 1. Folder-level authorization

In `TodoView/Program.cs`:

- `AuthorizeFolder("/Todos")` requires an authenticated user for the todo pages
- `AuthorizeFolder("/Admin/Users", AppRoles.Admin)` restricts the admin user-management area to admins

The second call means the `/Admin/Users` folder is protected by an authorization policy named `Admin`.

### 2. Named policies

In `Program.cs`, two policies are registered:

- `"Admin"` requires role `Admin`
- `"User"` requires role `User`

That means the folder rule for `/Admin/Users` resolves to a policy that only succeeds if the signed-in user is in the `Admin` role.

The `"User"` policy exists as well, although in the current codebase it is not the primary mechanism used for the todo pages.

### 3. Page-level role attributes

Each admin page model also declares:

- `[Authorize(Roles = AppRoles.Admin)]`

This appears on:

- `Pages/Admin/Users/Index.cshtml.cs`
- `Pages/Admin/Users/Create.cshtml.cs`
- `Pages/Admin/Users/Edit.cshtml.cs`
- `Pages/Admin/Users/Details.cshtml.cs`
- `Pages/Admin/Users/Delete.cshtml.cs`

So admin authorization is enforced twice:

- by folder convention
- by page attribute

That is redundant, but valid. It makes the restriction explicit on the page models even if the folder convention changes later.

## How Roles Are Read in the UI

The UI does not read a `Role` property from the user table. Instead, it always asks Identity for the current role assignments.

Examples:

- `Pages/Admin/Users/Index.cshtml.cs` uses `_userManager.GetRolesAsync(user)` when building the user list
- `Pages/Admin/Users/Details.cshtml.cs` uses `_userManager.GetRolesAsync(user)` for the details page
- `Pages/Admin/Users/Edit.cshtml.cs` uses `_userManager.GetRolesAsync(user)` to preselect the edit form value

This is the correct approach because roles live in Identity's role tables, not on the `User` entity itself.

## Practical Flow

Here is the end-to-end role flow in plain terms.

### When the app starts

- Identity is initialized with role support
- the seeder runs
- missing `Admin` and `User` roles are created
- the configured admin account is created if needed
- the admin account is assigned `Admin` and `User`
- any role-less account is assigned `User`

### When a normal visitor registers

- a new `User` record is created
- the account is added to the `User` role
- the user is signed in
- the user is redirected to `/Todos/Index`

### When an admin creates a user

- the admin chooses a role in the form
- the new user is created
- `User` is always assigned
- `Admin` is also assigned if the admin role was selected

### When an admin edits a user

- the current managed roles are loaded
- existing `Admin` and `User` assignments are removed
- the selected role configuration is re-applied
- optional password reset can also happen in the same operation

## Todo AJAX Modal CRUD

The todo area now uses a single-page CRUD flow based on AJAX-loaded Bootstrap modals. The app stays on `/Todos/Index` while modal content and the todo list are updated dynamically.

### What changed

Before this change:

- todo create, edit, details, and delete each used their own Razor Page
- clicking an action navigated to a new URL
- submitting a form redirected back to the index page

After this change:

- all todo actions open in a Bootstrap modal
- modal HTML is loaded from Razor Page handlers with `fetch`
- create, edit, and delete forms submit with `fetch`
- only the todo list section reloads after success
- the visible browser URL stays on `/Todos/Index`

### Files involved

- `TodoView/Pages/Todos/Index.cshtml`: main todo page, modal host, and list container
- `TodoView/Pages/Todos/Index.cshtml.cs`: list, modal, and AJAX POST handlers
- `TodoView/Pages/Todos/_TodoListPartial.cshtml`: todo table partial
- `TodoView/Pages/Todos/_TodoFormModal.cshtml`: shared modal form for create and edit
- `TodoView/Pages/Todos/_TodoDetailsModal.cshtml`: read-only details modal
- `TodoView/Pages/Todos/_TodoDeleteModal.cshtml`: delete confirmation modal
- `TodoView/wwwroot/js/site.js`: AJAX modal loading, form submission, and partial refresh logic

The old standalone todo pages were removed because they were no longer used:

- `TodoView/Pages/Todos/Create.cshtml`
- `TodoView/Pages/Todos/Create.cshtml.cs`
- `TodoView/Pages/Todos/Edit.cshtml`
- `TodoView/Pages/Todos/Edit.cshtml.cs`
- `TodoView/Pages/Todos/Delete.cshtml`
- `TodoView/Pages/Todos/Delete.cshtml.cs`
- `TodoView/Pages/Todos/Details.cshtml`
- `TodoView/Pages/Todos/Details.cshtml.cs`

### Frontend flow

The entry point is `TodoView/Pages/Todos/Index.cshtml`.

Key changes:

- the old create link was replaced by a button with `data-url="@Url.Page("./Index", "CreateModal")"`
- the todo table was moved into a partial rendered inside `#todo-list-container`
- the page contains one reusable modal shell: `#todoCrudModal`

The buttons in `_TodoListPartial.cshtml` use `data-url` values that point to Razor Page handlers:

- `?handler=CreateModal`
- `?handler=EditModal&id=...`
- `?handler=DetailsModal&id=...`
- `?handler=DeleteModal&id=...`

JavaScript in `TodoView/wwwroot/js/site.js` listens for clicks on `.js-todo-modal-trigger`, loads the returned HTML with `fetch`, injects it into the modal, reparses unobtrusive validation, and opens the modal.

### Backend flow

All todo AJAX handlers now live in `TodoView/Pages/Todos/Index.cshtml.cs`.

GET handlers:

- `OnGetAsync()`: renders the full page
- `OnGetListPartialAsync()`: returns only the todo list partial
- `OnGetCreateModal()`: returns the create modal HTML
- `OnGetEditModalAsync(int? id)`: returns the edit modal HTML
- `OnGetDetailsModalAsync(int? id)`: returns the details modal HTML
- `OnGetDeleteModalAsync(int? id)`: returns the delete modal HTML

POST handlers:

- `OnPostCreateAsync()`
- `OnPostEditAsync()`
- `OnPostDeleteAsync(int? id)`

Successful POST handlers do not redirect. Instead, they return JSON like:

```json
{
  "success": true,
  "reloadUrl": "/Todos/Index?handler=ListPartial"
}
```

The browser then fetches that partial and replaces only `#todo-list-container`.

If validation fails, the POST handler returns the modal partial HTML again. That lets the modal stay open and display validation messages.

### Partial refresh behavior

This implementation does not patch a single row in place. Instead, after create, edit, or delete:

- the server saves the change
- the server returns JSON success
- the browser fetches the latest list partial
- the browser replaces the full todo list container

That keeps the logic simple and ensures the empty state, ordering, and action buttons remain consistent.

### Antiforgery

Razor Pages AJAX POST still requires antiforgery protection.

`@Html.AntiForgeryToken()` is included in:

- `_TodoFormModal.cshtml`
- `_TodoDeleteModal.cshtml`

The JavaScript reads the token from the form and appends it to `FormData` before making the POST request.

If the token is missing or invalid, the server may return HTTP 400.

### Why the modal is moved to `document.body`

The modal element is appended to `document.body` in `site.js` before it is used.

This prevents layout stacking and focus issues caused by nested containers, z-index contexts, or overflow rules in the page structure. That change was added because the popup could open while the form inside it was difficult or impossible to interact with.

### Security and ownership checks

Todo ownership checks were preserved.

- todos are loaded only for the current authenticated user
- edit, details, and delete queries always filter by both `Id` and `UserId`
- edit updates only an owned row
- delete removes only an owned row

The helper methods responsible for this are:

- `LoadTodosAsync()`
- `FindOwnedTodoAsync(int? id)`

### Debugging checklist

If the AJAX todo flow breaks, check these in order.

#### 1. Browser network tab

Confirm that opening modals sends GET requests such as:

- `/Todos/Index?handler=CreateModal`
- `/Todos/Index?handler=EditModal&id=1`
- `/Todos/Index?handler=DeleteModal&id=1`

Confirm that submitting forms sends POST requests such as:

- `/Todos/Index?handler=Create`
- `/Todos/Index?handler=Edit`
- `/Todos/Index?handler=Delete&id=1`

On success:

- the POST should return JSON
- the browser should then request `/Todos/Index?handler=ListPartial`

On validation failure:

- the POST should return HTML for the modal body

#### 2. Browser console

Check for:

- JavaScript errors from `fetch`
- Bootstrap modal errors
- validation parsing errors

#### 3. Antiforgery token

Inspect the modal form HTML and confirm it includes:

- `__RequestVerificationToken`

If not, POST may fail with HTTP 400.

#### 4. Handler names

Razor Pages handler mapping is important:

- `OnGetCreateModal()` maps to `?handler=CreateModal`
- `OnPostCreateAsync()` maps to `?handler=Create`
- `OnGetEditModalAsync()` maps to `?handler=EditModal`
- `OnPostEditAsync()` maps to `?handler=Edit`
- `OnPostDeleteAsync()` maps to `?handler=Delete`

If a `data-url` or form `action` points to the wrong handler name, the request will fail or return unexpected content.

#### 5. Response type

The JavaScript expects:

- JSON for successful POST
- HTML for invalid POST or modal GET

If a successful POST accidentally returns HTML or a redirect, the client-side flow will not behave correctly.

### Tradeoffs of this approach

Pros:

- better UX than full-page reloads
- URL stays stable
- no frontend framework required
- keeps Razor Pages and server-rendered HTML

Cons:

- more custom JavaScript to debug
- the flow depends on consistent response types
- partial replacement can become brittle if selectors or markup change
- more moving pieces than plain full-page Razor Pages CRUD

### Alternatives

If this custom AJAX approach becomes hard to maintain, the main alternatives are:

- Full-page Razor Pages CRUD: simplest to debug, but changes URLs and reloads the whole page
- HTMX: likely the best alternative for this project because it keeps Razor Pages and reduces custom JavaScript
- Row-level AJAX updates: more granular refreshes, but more complex DOM update logic
- MVC plus API/partials: cleaner separation for larger apps, but more structure and code
- React or Vue: useful only if the UI becomes much more dynamic

For this project, HTMX would be the cleanest alternative if you want to keep modal CRUD without maintaining as much manual `fetch` logic.

### When a protected page is requested

- authentication checks whether the user is signed in
- authorization checks whether the user satisfies the page/folder requirement
- for admin pages, the user must be in the `Admin` role

## Important Design Decisions

### Admin implies User

This project does not treat `Admin` and `User` as mutually exclusive.

If a user is an admin, the code intentionally assigns:

- `Admin`
- `User`

That makes admin effectively a superset role.

### Role management is limited to two known roles

The current UI and code paths only manage:

- `Admin`
- `User`

If more roles are introduced later, the current edit logic would need to be reviewed because it removes only these managed roles and then re-adds based on a two-option form.

### Authorization is implemented both centrally and locally

The project uses:

- central folder authorization in `Program.cs`
- local `[Authorize(Roles = ...)]` attributes on page models

This improves clarity, though it duplicates some access rules.

## Files to Read If You Want to Trace It Yourself

- `TodoView/Program.cs`
- `TodoView/Data/TodoDbContext.cs`
- `TodoView/Models/User.cs`
- `TodoView/Authorization/AppRoles.cs`
- `TodoView/Data/Seed/IdentitySeeder.cs`
- `TodoView/Areas/Identity/Pages/Account/Register.cshtml.cs`
- `TodoView/Pages/Admin/Users/Create.cshtml.cs`
- `TodoView/Pages/Admin/Users/Edit.cshtml.cs`
- `TodoView/Pages/Admin/Users/Index.cshtml.cs`
- `TodoView/Pages/Admin/Users/Details.cshtml.cs`
- `TodoView/Pages/Admin/Users/Delete.cshtml.cs`

## Summary

Role is implemented here through ASP.NET Core Identity's built-in role system.

- roles are enabled in startup
- role names are centralized in `AppRoles`
- roles are persisted in Identity tables, not on `User`
- startup seeding creates missing roles and a default admin
- registration assigns `User`
- admin CRUD pages manage `Admin` and `User`
- authorization checks roles using policies, folder conventions, and `[Authorize]` attributes

If you want, I can also add a second document that explains the entire authentication and authorization pipeline with sequence diagrams and request-by-request examples.
