---
name: client-action-single-server-call
description: Enforces the strict OutSystems 11 Reactive and Mobile performance and transactional rule that a Client Action must never execute more than ONE (1) Server Action call. If multiple server operations are required, they must be encapsulated into a single Orchestrator Server Action. Use whenever creating, editing, or reviewing Client Actions, Screen Actions, or Mobile/Reactive UI flows.
---

# client-action-single-server-call

In **OutSystems 11 (O11) Reactive Web and Mobile** applications, every Server Action invoked from a **Client Action** triggers a separate **HTTP network round-trip** between the client device (browser or mobile WebView) and the OutSystems server, and opens a **separate database transaction**.

**Strict Rule**: **A Client Action must execute at most ONE (1) Server Action call per execution path.** Never place two or more Server Action calls in sequence, across sub-Client Actions, or inside a `ForEach` loop within a Client Action.

---

## 1. Why Multiple Server Calls in a Client Action Are Forbidden

1. **Severe Network Latency (Chatty UI)**:
   - Each Server Action call from a Client Action serializes parameters to JSON, sends an HTTP `POST` over the network, authenticates the user session, executes on the server, commits the database transaction, and returns the HTTP response.
   - Calling 3 Server Actions sequentially in a Client Action multiplies network round-trip latency by $3\times$ (causing multi-second freezes on mobile networks).
2. **Broken Database Transactions (No Rollback Across Requests)**:
   - OutSystems automatically commits the database transaction at the end of **every** Server Action call made from the client.
   - If a Client Action calls `Customer_Update` (Server Action #1) and then `Subscription_Create` (Server Action #2), and Server Action #2 raises an exception or the user loses mobile connectivity between the two calls, **Server Action #1 is already permanently committed** to the database, leaving inconsistent data.
3. **AI Mentor Studio High-Severity Code Smell**:
   - OutSystems AI Mentor Studio / Architecture Dashboard explicitly flags multiple server requests inside a single Client Action as a high-impact **Performance** debt item.

---

## 2. What Counts as a Violation

All of the following violate the **Single Server Call** rule:

### ❌ Violation A: Sequential Server Calls in the Same Client Action
```text
[Client Action: SaveCheckoutOnClick]
  Start -> ValidateAddress (Server Action #1)
        -> CalculateTaxes  (Server Action #2)
        -> Order_Create    (Server Action #3) -> End
```
* **Result**: 3 separate HTTP requests and 3 separate database transactions.

### ❌ Violation B: Server Call Inside a `ForEach` Loop in a Client Action
```text
[Client Action: ApproveSelectedItemsOnClick]
  Start -> ForEach (SelectedList)
             | (Cycle)
             v
           Item_Approve (Server Action) -> (back to ForEach)
```
* **Result**: If the user selects 20 items, the browser/mobile app fires **20 sequential HTTP requests** to the server.

### ❌ Violation C: Hidden Server Calls Across Nested Client Actions
```text
[Client Action: SubmitFormOnClick]
  Start -> ValidateFormClientAction (which internally calls Server Action #1)
        -> SaveFormServerAction     (Server Action #2) -> End
```
* **Result**: Even though each Client Action only shows 1 direct server node on its canvas, the execution path performs **2 HTTP round-trips**.

---

## 3. The Solution: Encapsulate into a Single Orchestrator Server Action

Whenever a Client Action needs to execute multiple server-side validations, queries, or writes, **encapsulate all server calls into a single Server Action** and call only that one Server Action from the Client Action.

### ✅ Best Practice (1 Client Call -> 1 Orchestrator Server Action):

1. **Create a single Orchestrator Server Action** (in the End-User module's `Server Actions` or in the corresponding `_BL` Business Logic module):
   ```text
   [Server Action: Order_ProcessCheckout]
     Start -> ValidateAddress (Server Action)
           -> CalculateTaxes  (Server Action)
           -> Order_Create    (Server Action) -> End
   ```
   * **Why this works**: Because `Order_ProcessCheckout` runs entirely on the server:
     - `ValidateAddress`, `CalculateTaxes`, and `Order_Create` execute **in-memory** on the application server with zero network overhead between them.
     - All three operations share **one single ACID database transaction**: if `Order_Create` fails, any writes made earlier in the flow are automatically rolled back together.

2. **Call ONLY the Orchestrator Server Action from the Client Action**:
   ```text
   [Client Action: SaveCheckoutOnClick]
     Start -> Client-side Input Validation (JS / If)
           -> Order_ProcessCheckout (SINGLE Server Action call)
           -> FeedbackMessage / Destination -> End
   ```

### ✅ Best Practice for Lists (`ForEach` Moved to the Server):
Instead of looping in the Client Action, pass the **List** (or a `List of Ids`) as an Input Parameter to a **single bulk Server Action**:
```text
[Client Action: ApproveSelectedItemsOnClick]
  Start -> ListFilter (Filter selected items in client memory)
        -> Items_BulkApprove (SINGLE Server Action receiving ItemIdList)
        -> Refresh Data -> End
```
Inside `Items_BulkApprove` (on the server), run the `ForEach` loop or bulk SQL update in a single request and transaction.

---

## 4. Handling Data Refreshes (`Refresh Data`) After a Server Action

A common source of a *second* server round-trip in a Client Action is calling a Server Action to save/update data and then calling `Refresh Data` on a Screen Aggregate or Data Action:
* If the screen remains on the same page and only needs the updated values returned by the save operation, **return the updated record/fields as Output Parameters of the single Server Action** and assign them directly to the local state, avoiding a second round-trip when possible.
* Never chain multiple dependent `Refresh Data` nodes sequentially when their data can be fetched together in a single **Data Action** on the server.

---

## 5. Pre-Commit Checklist for Every Client Action

Before finishing any Client Action in OutSystems 11:
- [ ] **Count the Server Action nodes (`RunServerAction`)**: Does any path from `Start` to `End` pass through more than **1** Server Action? If yes, encapsulate them into a single Server Action.
- [ ] **Check `ForEach` loops**: Is there any Server Action inside a `ForEach` loop in a Client Action? If yes, move the `ForEach` loop into a Server Action that accepts a `List` input parameter.
- [ ] **Check called Client Actions**: Does this Client Action call another Client Action that *also* makes a Server Action call? If yes, consolidate the server work into 1 Server Action call.
