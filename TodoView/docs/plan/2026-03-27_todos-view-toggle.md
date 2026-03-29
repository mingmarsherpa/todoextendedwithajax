# Todo View Toggle Plan

## Metadata
- Date: 2026-03-27
- Feature: todos-view-toggle
- Status: implemented
- Area: `Pages/Todos`

## Goal
Add a user-facing toggle on the todos page that switches between list view and card view without breaking the current create, edit, details, and delete workflows.

## Current State
- The todos page currently renders a single table-based list via `_TodoListPartial.cshtml`.
- The page uses partial reloads and modal-driven CRUD interactions.
- There is no persisted or request-driven display mode for todo presentation.

## Desired Outcome
- Users can switch between:
  - list view
  - card view
- The selected view remains active after partial refreshes triggered by todo create, edit, or delete actions.
- Both views show the same todo data and action buttons.
- Empty-state handling remains available in both views.

## Implementation Plan
1. Add a display-mode concept to the todos page model.
   - Introduce a small, explicit mode value such as `list` or `card`.
   - Accept the mode through query string or request input.
   - Default to `list` when the mode is missing or invalid.

2. Update the main todos page to expose the toggle control.
   - Add a compact toggle UI near the page header.
   - Make the active state visually obvious.
   - Ensure the control works with the existing partial refresh approach.

3. Refactor todo rendering to support two layouts.
   - Keep the current table layout as list view.
   - Add a card layout partial or conditional rendering block for card view.
   - Reuse the same modal trigger actions in both layouts.

4. Preserve the chosen mode during async updates.
   - Include the active mode when requesting the list partial.
   - Return the correct partial state after create, edit, and delete operations.
   - Ensure the frontend refresh target does not silently reset to list view.

5. Add styling for the card presentation and toggle control.
   - Match the current visual language of the todo dashboard.
   - Keep layout responsive for smaller screens.
   - Avoid duplicating core action styles where existing classes can be reused.

6. Validate behavior manually.
   - Verify list view renders correctly.
   - Verify card view renders correctly.
   - Verify modal actions still work from both layouts.
   - Verify the selected mode survives partial reloads.
   - Verify empty state looks correct in both modes.

## Expected File Touchpoints
- `TodoView/Pages/Todos/Index.cshtml`
- `TodoView/Pages/Todos/Index.cshtml.cs`
- `TodoView/Pages/Todos/_TodoListPartial.cshtml`
- `TodoView/wwwroot/css/site.css`
- Optional new partial if card view is split out from the existing partial

## Open Decisions
- Whether to persist the chosen mode only for the current request cycle or across sessions.
- Whether the card view should include full descriptions or a shorter summary.
- Whether the toggle should be buttons, tabs, or segmented controls.

## Risks
- Partial refresh behavior may reset the chosen mode unless the mode is carried through every reload URL.
- Duplicating markup between list and card layouts could make future todo UI changes harder to maintain.
- Card layout density may degrade on smaller screens if descriptions are too long.

## Acceptance Criteria
- A toggle is visible on the todos page.
- Switching the toggle updates the todo presentation between list and card layouts.
- CRUD modal actions work from both layouts.
- Partial reloads preserve the selected layout.
- Invalid or missing mode input falls back safely to list view.

## Implementation Notes
- Implemented with a query-backed `view` mode on `Pages/Todos/Index`.
- Supported values are `list` and `card`.
- Invalid or missing values normalize to `list`.
- CRUD modal flows carry the active mode forward so partial reloads keep the selected layout.
