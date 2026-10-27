---
name: traditional-to-reactive-migration
description: Comprehensive architectural, frontend, and backend migration guide for converting OutSystems 11 Traditional Web applications into Reactive Web applications. Covers SSR vs React SPA, ViewState elimination, Preparation to asynchronous Aggregates/Data Actions, Screen Actions to Client Actions + single Server Action, HTTPRequestHandler replacements, Download/Upload, Emails, Session vs Client Variables, and RichWidgets/Silk UI to OutSystems UI. Use whenever migrating, modernizing, or refactoring Traditional Web modules to Reactive Web.
---

# traditional-to-reactive-migration

Migrating an **OutSystems 11 (O11) Traditional Web** application to **Reactive Web** is a fundamental architectural shift from **Server-Side Rendering (SSR with ASP.NET PostBack & `ViewState`)** to a **Client-Side Single-Page Application (ReactJS SPA communicating with stateless REST/JSON endpoints)**.

When inspecting a Traditional Web module via MCP tools (`getScreenNamesTraditional`, `getScreenTraditional`, `getWebBlockNamesTraditional`, `getWebBlockTraditional`, `getSessionVariables`, `getEmailTemplateTraditional`, `getThemeTraditional`), follow these strict Frontend and Backend conversion rules based on official OutSystems 11 documentation.

---

## 1. Factory Migration Strategy & Hybrid Coexistence

1. **Bottom-Up Producer-to-Consumer Order**:
   - Traditional Web Blocks and Themes **cannot** be referenced by Reactive Web modules.
   - Always migrate **from bottom to top**: extract/decouple **Core (`_CS`, `_BL`)** modules first, then **Core Widgets (`_CW`)**, and finally **End-User Reactive UI** modules.
2. **Decouple Entities from Traditional UI Modules**:
   - Never copy Entities into the new Reactive UI module. Keep Entities and CRUD wrappers in a Core Service (`_CS`) module so both the legacy Traditional Web app and the new Reactive Web app can run side-by-side over the same database tables (*Strangler Fig pattern*).
3. **Single Sign-On (SSO) Between App Types**:
   - Traditional Web and Reactive Web use different session cookies by default. When running a hybrid factory where users navigate between Traditional and Reactive screens, ensure **Single Sign-On between App Types** is enabled in Service Center (`Administration > Security`).

---

## 2. Backend & Execution Architecture Differences

| Backend Dimension | Traditional Web (SSR) | Reactive Web (SPA) | Migration Action Required |
| :--- | :--- | :--- | :--- |
| **State Serialization (`ViewState`)** | Every screen Local Variable and `Preparation` query list is serialized into hidden `__VIEWSTATE` HTML and sent back and forth on every PostBack/Ajax Submit. | **No `ViewState` exists.** Local Variables live in browser memory (React state). Only explicit inputs/outputs of a called Server Action travel over the network as JSON. | Stop using workarounds built to avoid `ViewState` bloat; keep UI state in Screen Local Variables and pass only required inputs to Server Actions. |
| **Screen Data Fetching (`Preparation`)** | Runs **synchronously** top-to-bottom on the server *before* HTML is sent to the browser. | **No `Preparation` exists.** Screen shell renders immediately; **Screen Aggregates** and **Data Actions** run **asynchronously and in parallel**. | Convert independent queries into Screen Aggregates (`Fetch = At start`). Consolidate dependent multi-step server queries/logic into a single **Data Action**. |
| **UI Event Logic (`Screen Actions`)** | `Submit` and `Ajax Submit` Screen Actions execute **on the server** with direct access to DB Aggregates, Entities, and `Session`. | UI events trigger **Client Actions** executing **in the browser (JavaScript)**. Client Actions cannot access DB Entities or Server Variables directly. | Split every Traditional Screen Action into: **(1) a Client Action** (UI validation, state, navigation) calling **(2) at most ONE Orchestrator Server Action** (DB & business logic). |
| **`Session Variables` vs `Client Variables`** | `Session.<Var>` is stored in server memory / Session DB. | `Client.<Var>` is stored in the browser's **`localStorage`** (inspectable and editable by the user in DevTools). | Convert **only** non-sensitive UI preferences (filters, active tab, theme) to `Client Variables`. **Never** migrate roles (`IsAdmin`), `NIF`, `Email`, tokens, or prices to `Client Variables`—resolve them on the server via `GetUserId()`. |
| **Global Request Hooks** | Server-side System Event **`OnBeginWebRequest`** runs before every HTTP page request. | Client-side System Events **`OnApplicationReady`** (SPA boot) and **`OnApplicationResume`**, plus Layout Block `OnInitialize`. | Move global client initialization to `OnApplicationReady`, and move security/authorization checks inside the Server Actions / Data Actions that serve data. |

---

## 3. Critical Backend-to-Frontend Traps (`HTTPRequestHandler`, `Download`, `Upload`, `Email`)

### Trap 1: `HTTPRequestHandler` Actions Fail in Reactive Server Actions
In Traditional Web, developers call `HTTPRequestHandler` extension actions (`RunJavaScript`, `AddJavaScriptTag`, `AddStyleSheetTag`, `SetPageTitle`) inside `Preparation` or `Screen Actions` to manipulate the HTML response.
In Reactive Web, **Server Actions return JSON, not HTML, and have no connection to the browser DOM**:
* **`HTTPRequestHandler\RunJavaScript`** -> **Remove from Server Actions.** Move the JavaScript execution into a **Client Action** using the native **`JavaScript` node** (passing inputs/outputs via `$parameters`).
* **`HTTPRequestHandler\AddJavaScriptTag`** -> Replace with the **`RequireScript`** Client Action or add the script to the Screen/Block `Required Scripts` property.
* **`HTTPRequestHandler\SetPageTitle`** -> Bind the Screen's **`Title`** expression property directly or set `document.title` in a Client Action `JavaScript` node.

### Trap 2: The `Download` Node Moved from Server to Client
* **Traditional Web**: The `Download` node sits at the end of a **Screen Action** (`Method = Submit`) on the server and streams the file in the HTTP response.
* **Reactive Web**: The **`Download` node is ONLY available inside Client Actions** (as an End node).
  - **Migration Pattern**: Create a Server Action that generates/fetches the file and returns `FileContent` (`Binary Data`) and `FileName` (`Text`) as Output Parameters. Call that Server Action from your **Client Action**, and end the Client Action with the **`Download`** node.

### Trap 3: The `Upload` Widget No Longer Uses Page `Submit`
* **Traditional Web**: Required a button with `Method = Submit` to post multipart form data to a Screen Action.
* **Reactive Web**: The **`Upload` widget** binds directly to two Screen Local Variables (`FileContent` of type `Binary Data` and `FileName` of type `Text`) on the client. A standard Client Action reads those variables and passes them to a Server Action asynchronously without reloading the screen.

### Trap 4: `Emails` & `Send Email`
* **Traditional Web**: Emails have a `Preparation` and can be sent from any Screen Action or Server Action.
* **Reactive Web**:
  - Reactive Email templates do **not** have a `Preparation` (use **Aggregates** or **Data Actions** inside the Email definition to load data).
  - The **`Send Email`** node can **only be executed inside a Server Action** (never directly on a Client Action canvas).

---

## 4. Frontend, Widgets & UI Pattern Mapping (`RichWidgets` / `Silk UI` -> `OutSystems UI`)

| Traditional Web Widget / Pattern | Reactive Web Equivalent | Key Implementation Difference |
| :--- | :--- | :--- |
| **`Ajax Refresh`** node | **None (Automatic Reactive Binding)** | Delete all `Ajax Refresh` nodes. Mutating a Local Variable or executing `Refresh Data` on an Aggregate/Data Action automatically re-renders bound widgets. |
| **`TableRecords`** + `List_Navigation` + `List_SortColumn` | **`Table`** widget + **`Pagination`** (`OutSystemsUI`) | Bind `StartIndex` and `MaxRecords` Local Variables to the Aggregate. Handle sorting via the Table's **`OnSort`** event (`SortAttribute` local variable) and call `Refresh Data`. |
| **`ListRecords`** | **`List`** widget (or `Gallery`) | Note: In Reactive Web, avoid mutating `.List.Current` outside a list context; pass the current item or `Id` explicitly to the triggered Client Action. |
| **`ComboBox`** (`Source Record List` / `Special List`) | **`Dropdown`** (or `DropdownSearch` / `DropdownTags`) | `Special List` does not exist on the standard `Dropdown`; use `Empty Text` for the default prompt option, or combine options in a Data Action / Local List if custom special entries are needed. |
| **Web Block `Notify` + `OnNotify` + `NotifyGetMessage()`** | **Block `Events` (Strongly-Typed)** | Replace untyped string `Notify` calls with named Block **Events** carrying typed Input Parameters (e.g., `OnCustomerSelected(CustomerId)`). |
| **`Popup_Editor`** (RichWidgets iframe + separate Screen + `Popup_Editor_Notify`) | **`Popup` Widget** (inline on Screen or wrapping a Block) | Eliminate the separate Popup Web Screen. Place a **`Popup`** widget controlled by a boolean Local Variable (`IsPopupOpen`) on the parent Screen, and put complex modal content inside a reusable **Block** that emits an Event (`OnSaved` / `OnClosed`). |
| **Button/Link `Confirmation Message` property** | **Confirmation `Popup` Dialog** (or JS `confirm`) | Reactive Buttons/Links do **not** have a built-in `Confirmation Message` property. Open a confirmation `Popup` widget before executing the destructive Client Action. |
| **`Input_Calendar` (RichWidgets)** | **`DatePicker` (`OutSystemsUI`)** | Use the OutSystems UI `DatePicker` block or native `<input type="date">` (`Input` widget with `Input Type = Date`). |
| **`Feedback_Message` (RichWidgets)** | **`FeedbackMessage` (Built-in Node)** | Use the native `FeedbackMessage` tool directly inside the Client Action flow. |

---

## 5. Pre-Commit Migration Checklist

Before marking a migrated Reactive Screen or Block complete:
- [ ] **No `Preparation` Waterfall**: Were dependent server queries/actions from `Preparation` consolidated into a single **Data Action** rather than chained across multiple `OnAfterFetch` client round-trips?
- [ ] **Single Server Call per Client Action**: Were all server queries/writes from each old `Screen Action` encapsulated into **1 Orchestrator Server Action**?
- [ ] **No `HTTPRequestHandler\RunJavaScript` in Server Actions**: Were all DOM/JS manipulations moved to Client Action `JavaScript` nodes?
- [ ] **`Download` & `Upload` Refactored**: Are all `Download` nodes placed inside Client Actions, and `Upload` widgets bound to local `Binary Data` / `Text` variables?
- [ ] **`Client Variables` Security Audit**: Are all sensitive `Session Variables` (`IsAdmin`, `NIF`, `Email`, tokens) excluded from `Client Variables` and validated server-side via `GetUserId()`?
- [ ] **Popups & Events Modernized**: Were `Popup_Editor` screens replaced by Reactive `Popup` widgets/Blocks and `Notify` calls replaced by typed Block `Events`?
