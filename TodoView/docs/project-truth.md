# Project Truth

This document is the current source of truth for the `TodoView` project. It should describe the codebase as it exists now, not as planned.

## Document Rules
- Update this file whenever behavior, architecture, setup, dependencies, data shape, or delivery status changes.
- Prefer facts that are verifiable in code over roadmap language.
- When something is incomplete, mark it as incomplete instead of implying it exists.
- Keep dates explicit.

## Snapshot
- Last updated: 2026-03-28
- Project type: ASP.NET Core Razor Pages web application
- Target framework: `.NET 9.0`
- Primary purpose: authenticated todo management with an admin area for user management
- Solution layout:
  - Solution root: `TodoView.sln`
  - App project: `TodoView/`
  - Project docs: `TodoView/docs/`

## Current Stack
- UI framework: Razor Pages
- Authentication: ASP.NET Core Identity
- Authorization model: role-based authorization with `Admin` and `User`
- Database access: Entity Framework Core
- Database provider: SQL Server
- Static assets: Bootstrap, jQuery, jQuery Validation

## Current Runtime Configuration
- Connection string source: `appsettings.json`
- Default SQL Server database name: `tododb`
- Seeded admin configuration source: `SeedAdmin` section in `appsettings.json`
- Seeded admin behavior:
  - Roles `Admin` and `User` are created on startup if missing.
  - A configured admin user is created on startup if missing.
  - The seeded admin is assigned both `Admin` and `User` roles.
  - Existing users with no role are assigned the `User` role on startup.

## Current Domain Model

### User
- Extends `IdentityUser`
- Additional fields:
  - `FirstName`
  - `LastName`
  - `Address`
  - `CreatedAt`
- Relationship:
  - one user to many todos through `TodoItems`

### Todo
- Fields:
  - `Id`
  - `Title`
  - `Description`
  - `IsDone`
  - `UserId`
- Relationship:
  - each todo belongs to one user
- Delete behavior:
  - todos cascade-delete when their owning user is deleted

## Current Application Behavior

### Authentication and Authorization
- Identity registration and login pages are present under `Areas/Identity/Pages/Account/`.
- Confirmed account is not required for sign-in.
- `/Todos` is protected and requires an authenticated user.
- `/Admin/Users` is restricted to the `Admin` role.

### Todo Management
- The current todo UI is centered on `Pages/Todos/Index`.
- Authenticated users can:
  - list their own todos
  - search todos by title or description
  - filter todos by all, active, or completed status
  - switch between list view and card view
  - create a todo
  - edit a todo they own
  - view todo details
  - delete a todo they own
- Todo queries are scoped to the current authenticated user.
- Todo summary metrics show total, active, and completed counts for the signed-in user.
- Todo list ordering:
  - incomplete items first
  - then alphabetical by title
- Todo create, edit, details, and delete flows use partial views and modal-driven interactions.
- The selected todo view mode is request-driven through a `view` query parameter and currently supports `list` and `card`.
- Todo search is request-driven through a `q` query parameter.
- Todo status filtering is request-driven through a `status` query parameter and currently supports `all`, `active`, and `completed`.

### Admin User Management
- The admin user management UI is centered on `Pages/Admin/Users/`.
- Admins can access:
  - user index
  - user details
  - create user
  - edit user
  - delete user
- The admin area uses partial views and modal-driven interactions for list and detail workflows.

## Current Data and Infrastructure Notes
- `TodoDbContext` inherits from `IdentityDbContext<User>`.
- Application startup creates a service scope and runs identity seeding before request handling.
- HTTPS redirection is enabled.
- HSTS is enabled outside development.

## Current Dependencies
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` `9.0.14`
- `Microsoft.AspNetCore.Identity.UI` `9.0.14`
- `Microsoft.EntityFrameworkCore` `9.0.14`
- `Microsoft.EntityFrameworkCore.SqlServer` `9.0.14`
- `Microsoft.EntityFrameworkCore.Tools` `9.0.14`
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` `9.0.12`
- `Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore` `9.0.12`

## Known Issues and Risks
- Sensitive local development credentials are currently committed in `appsettings.json` for the SQL connection and seeded admin account.
- Validation messages in some model attributes contain wording that appears incorrect or misleading.
- No test project is present in the current solution snapshot.

## Current Gaps
- No implemented API layer is present; the application is server-rendered Razor Pages.
- No background jobs, queues, or external integrations are present in the current snapshot.
- No dedicated docs index exists yet beyond agent guidance and this truth document.

## Update Checklist
When the project changes, review these sections:
- Snapshot
- Current Stack
- Current Runtime Configuration
- Current Domain Model
- Current Application Behavior
- Current Dependencies
- Known Issues and Risks
- Current Gaps
