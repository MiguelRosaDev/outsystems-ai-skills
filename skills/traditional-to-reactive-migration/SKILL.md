---
name: traditional-to-reactive-migration
description: Architectural and step-by-step migration guide for converting OutSystems 11 Traditional Web applications into Reactive Web applications. Covers Preparation to asynchronous Aggregates/Data Actions, Session Variables to Client Variables, Screen Actions (server PostBack) to Client Actions + single Orchestrator Server Action, Notify/OnNotify to strongly-typed Block Events, and RichWidgets/Popup_Editor to OutSystems UI Reactive patterns. Use whenever migrating, modernizing, or refactoring Traditional Web modules to Reactive Web.
---

# traditional-to-reactive-migration

Migrating an **OutSystems 11 (O11) Traditional Web** application to **Reactive Web** is **not** a 1-to-1 copy-paste of screens and actions. Traditional Web executes synchronously on the server (ASP.NET PostBack / Ajax Submit), whereas Reactive Web is a client-side Single-Page Application (React SPA) where UI logic runs in the browser and communicates with the server asynchronously over HTTP.

When inspecting a Traditional Web module via MCP tools (`getScreenNamesTraditional`, `getScreenTraditional`, `getWebBlockNamesTraditional`, `getWebBlockTraditional`, `getSessionVariables`, `getEmailTemplateTraditional`), follow these strict translation rules.

---

## 1. Pre-Migration Architecture: Extract Core Logic First

Traditional Web applications frequently suffer from monolithic modules where Entities, Business Logic, and UI Screens live in the same module.
1. **Never migrate database Entities into a Reactive UI module**:
   - Keep or extract Entities and CRUD wrappers in a Core Service (`_CS`) module, and shared Server Actions in a Business Logic (`_BL`) module.
   - This allows the legacy Traditional Web app and the new Reactive Web app (`_CW` + End-User UI) to **run side-by-side** against the exact same database tables during a phased (Strangler Fig) migration.

---

## 2. Paradigm Mapping: Traditional Web vs. Reactive Web

| Traditional Web Concept | Reactive Web Equivalent | Critical Migration Rule |
| :--- | :--- | :--- |
| **`Preparation`** (Synchronous server flow before render) | **Screen Aggregates** (`Fetch = At start`) + **Data Actions** + **`OnInitialize`** | Independent queries become parallel **Screen Aggregates**. Dependent multi-step server queries/logic become a single **Data Action**. Never chain sequential server calls across `OnAfterFetch` events. |
| **`Screen Action`** (`Submit` / `Ajax Submit` running on server) | **`Client Action`** (browser) + **1 Orchestrator `Server Action`** | Traditional Screen Actions ran on the server. In Reactive, UI events trigger **Client Actions** in the browser. Move all server queries/writes from the old Screen Action into **one single Server Action** and call it from the Client Action. |
| **`Session Variable`** (`Session.<Name>` on server) | **`Client Variable`** (`Client.<Name>` in browser `localStorage`) **OR** Server DB state | Only migrate non-sensitive UI preferences (filters, active tab, theme) to `Client Variables`. **Never** put sensitive/authoritative state (roles, NIF, permissions, balances) in `Client Variables`—resolve them on the server via `GetUserId()`. |
| **`Ajax Refresh`** widget node | **Automatic Reactive Data Binding** (`Refresh Data` for Aggregates/Data Actions) | Remove all `Ajax Refresh` nodes. Updating a Local Variable or executing `Refresh Data` automatically re-renders every bound widget on the screen. |
| **Web Block `Notify` + `OnNotify` + `NotifyGetMessage()`** | **Block `Events` (Strongly-Typed)** | Replace untyped string `Notify` messages with named Block **Events** that carry typed Input Parameters (e.g., `OnItemSelected(ItemId)`). |
| **`Popup_Editor`** (RichWidgets iframe + separate Screen + `Popup_Editor_Notify`) | **`Popup` Widget** (inline on Screen or wrapping a Block) | Do **not** create a separate Screen for a modal popup. Use the Reactive `Popup` widget bound to a boolean Local Variable (`ShowPopup = True/False`) and encapsulate reusable modal content inside a **Block**. |
| **`Feedback_Message`** (RichWidgets Server Action) | **`FeedbackMessage`** (Built-in Client Node) | Replace `RichWidgets\Feedback_Message` calls with the native `FeedbackMessage` node in the Client Action flow. |
| **`List_Navigation` / `List_Counter` / `List_SortColumn`** | **Reactive `Pagination` & `Table` `OnSort` Event** | Use a `StartIndex` and `MaxRecords` Local Variable bound to the Screen Aggregate's `Start Index` and `Max Records`, and refresh the Aggregate inside the `OnNavigate` / `OnSort` Client Action. |

---

## 3. Step-by-Step Screen Migration Rules

### Rule 1: Converting `Preparation` Without Creating Waterfall Round-Trips
In Traditional Web, a `Preparation` often looks like:
```text
[Traditional Preparation]
  Start -> GetCustomerById (Aggregate)
        -> GetCustomerOrders (Aggregate using GetCustomerById.List.Current)
        -> CalculateLoyaltyPoints (Server Action)
        -> CheckPermissions (Server Action) -> End
```
* **❌ Wrong Reactive Migration**: Creating 2 Screen Aggregates and calling `CalculateLoyaltyPoints` and `CheckPermissions` inside `OnAfterFetch` or `OnReady` (which causes 3–4 sequential HTTP round-trips after page load).
* **✅ Correct Reactive Migration**:
  - If `CustomerId` is already a Screen Input Parameter, `GetCustomerById` and `GetCustomerOrders` (filtering by `CustomerId`) can run as **parallel Screen Aggregates** (`Fetch = At start`), OR
  - Consolidate the dependent queries and server calculations (`CalculateLoyaltyPoints` + `CheckPermissions`) into a **single `Data Action`** (`GetScreenData`) that returns a composite output structure in **one HTTP round-trip**.

### Rule 2: Converting Traditional `Screen Actions` to Reactive `Client Actions`
In Traditional Web, every button click ran a `Screen Action` on the server, where developers freely placed Aggregates, `CreateOrUpdate` entity actions, and REST calls directly on the canvas.
In Reactive Web:
1. Create a **Client Action** for the UI event (e.g., `SaveOnClick`).
2. Keep client-side form validation (`Form.Valid`) and UI state toggles (`IsSaving = True`) inside the Client Action.
3. Encapsulate **all** server-side Aggregates, CRUD wrappers, and business logic from the old Screen Action into **a single Server Action** (respecting the `client-action-single-server-call` rule).
4. Return the result (`IsSuccess`, `ErrorMessage`, `UpdatedId`) to the Client Action, trigger `FeedbackMessage`, and navigate to the `Destination`.

### Rule 3: Eliminating `Popup_Editor` Screens
In Traditional Web, every modal dialog required its own dedicated Web Screen, `Popup_Editor` RichWidget, `Popup_Editor_Notify`, and `Popup_Editor_Close`.
When migrating to Reactive Web:
1. Convert the old Popup Screen into either:
   - An inline **`Popup` dialog widget** directly on the parent Screen (for simple confirmations/forms), OR
   - A reusable **Block** placed inside a `Popup` widget on the parent Screen (when the modal has its own data fetching and validation logic).
2. Replace `Popup_Editor_Notify` + `Popup_Editor_Close` with a typed **Block Event** (e.g., `OnSaved` / `OnCancelled`) that the parent Screen handles by setting `IsPopupOpen = False` and calling `Refresh Data`.

### Rule 4: Auditing `Session Variables` Before Creating `Client Variables`
Inspect every variable returned by `getSessionVariables` in the Traditional module:
- **Search filters, pagination state, UI toggle states**: Convert to `Client Variables` (if shared across screens) or Screen Input Parameters / Local Variables.
- **Security context, Role flags (`Session.IsAdmin`), Customer NIF/Email, Tenant/Company IDs, Shopping Cart prices**: **NEVER** store in `Client Variables`. Look them up on the server inside Server Actions / Data Actions using the authenticated `GetUserId()`.

---

## 4. Migration Verification Checklist

Before marking a migrated Reactive Screen or Block complete:
- [ ] **Zero Business/DB Logic in UI**: Are all Entities and CRUD actions kept in `_CS`/`_BL` modules rather than copied into the new Reactive module?
- [ ] **No Sequential `OnAfterFetch` Waterfalls**: Were dependent `Preparation` steps consolidated into a single **Data Action** instead of chained across `OnAfterFetch` client handlers?
- [ ] **Single Server Call per Client Action**: Were multi-step server operations from old `Screen Actions` wrapped into **1 Server Action**?
- [ ] **Session Security Check**: Are `Client Variables` completely free of PII (`NIF`, `Email`), security flags (`IsAdmin`), and sensitive tokens?
- [ ] **Popups & Blocks Modernized**: Were `Popup_Editor` screens converted into Reactive `Popup` widgets/Blocks with typed `Events` instead of `Notify` strings?
