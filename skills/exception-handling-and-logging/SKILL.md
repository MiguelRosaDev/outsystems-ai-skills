---
name: exception-handling-and-logging
description: OutSystems 11 exception handling, transaction rollback (Abort Transaction), custom audit logging, and user feedback patterns. Use whenever adding Exception Handlers, raising exceptions, or writing error logs in actions and UI flows.
---

# exception-handling-and-logging

Proper exception handling in OutSystems 11 guarantees database consistency, prevents leaking technical stack traces to end users, and preserves diagnostic logs for troubleshooting.

---

## 1. Exception Hierarchy in OutSystems

OutSystems resolves exception handlers from most specific to most general:
1. **`DatabaseException`**: Primary key violations, foreign key constraints (`OSFRK`), SQL timeouts.
2. **`CommunicationException`**: REST/SOAP connection failures, network timeouts.
3. **`SecurityException`** (includes `NotRegisteredException`, `InvalidLoginException`): Unauthorized access or role failure.
4. **`UserException`** (Custom defined): Domain-specific exceptions (e.g. `InsufficientFundsException`, `DuplicateTaxIdException`).
5. **`AllExceptions`**: Catch-all handler for any unhandled exception.

---

## 2. The `Abort Transaction` Rule (Critical Trap!)

Every `Exception Handler` node has an **`Abort Transaction`** boolean property (`Yes` by default). Understanding how it interacts with database writes is critical:

### Case A: Standard Rollback (`Abort Transaction = Yes`)
* **When to use**: When a multi-step business transaction fails mid-way and **all** database inserts/updates made so far in the request must be rolled back to leave the database clean.
* *Note*: `LogMessage` (which writes to Service Center logs asynchronously) **still persists** even when `Abort Transaction = Yes`.

### Case B: Persisting Custom Audit/Error Entities (`Abort Transaction = No`)
* **The Trap**: If your Exception Handler calls `CreateErrorLog` (writing to a custom application database entity) and has `Abort Transaction = Yes`, **OutSystems rolls back your `CreateErrorLog` record too!**
* **The Fix**:
  * Either use `Abort Transaction = Yes` for the business transaction, and before raising/returning, call `AbortTransaction` explicitly (from `System`), **then** insert the custom error log record and commit.
  * Or set `Abort Transaction = No` only when no dirty business writes occurred prior to the error.

---

## 3. Server Action vs. Client/Screen Action Error Handling

### In Core / Server Actions (`_CS`, `_BL`, `_IS`)
* Prefer returning structured output parameters (`IsSuccess = False`, `ErrorMessage = "..."`, `ErrorCode = "..."`) for expected business validation failures, or raising a specific `UserException` when a transaction must be aborted across layers.
* In the `AllExceptions` handler of a Server Action:
  1. Log the technical error using `LogMessage` (`ModuleName: "<Module> [<Action>]: " + AllExceptions.ExceptionMessage`).
  2. Set `IsSuccess = False` and assign a **safe, business-friendly message** to `ErrorMessage` (never pass raw SQL errors or stack traces to the UI).

### In Screen / Client Actions
* Catch exceptions or check `ServerAction.IsSuccess = False`.
* Display a clean `Message` node (`Type = Error` or `Warning`) with actionable text for the user.
* In the UI Flow's global **`OnException`** handler:
  * Handle `SecurityException` by redirecting to the `Login` or `InvalidPermissions` screen.
  * Handle `AllExceptions` by logging and redirecting to a friendly error state without crashing the user session.
