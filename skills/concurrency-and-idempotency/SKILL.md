---
name: concurrency-and-idempotency
description: Concurrency control and idempotency patterns for OutSystems 11. Prevents lost updates (last write wins), duplicate submissions (double-click), race conditions on counters and sequential numbers, parallel processing collisions in Timers/BPT, and duplicated side-effects on integration retries. Covers optimistic locking with a Version attribute, GetForUpdate pessimistic locking, idempotency keys, and safe queue claiming. Use whenever writing update flows, payments/orders, counters, background processing, or integration retries.
---

# concurrency-and-idempotency

OutSystems applications are multi-user and multi-front-end: the same Server Action can run **at the same time** for different users, on different front-end servers, and in parallel Timers or BPT activities. Logic that is correct for a single user often breaks under concurrency:

| Symptom in Production | Root Cause |
| :--- | :--- |
| A user's changes "disappear" | **Lost update**: two users edited the same record; the last save silently overwrote the first. |
| Order or payment created twice | **Duplicate submission**: double-click, network retry, or the user pressing back/refresh. |
| Two invoices with the same number | **Race on `Max() + 1`**: two transactions read the same max value before either inserts. |
| Records processed twice by background jobs | **Parallel workers** picked the same pending rows. |
| Customer charged twice by an external API | **Retry without idempotency**: the first call succeeded but the response timed out. |

---

## 1. Lost Updates: Optimistic Locking with a `Version` Attribute

Use for edit screens where two users may open the same record.

1. Add a `Version` (`Integer`, default `0`) attribute to the Entity (or reuse `UpdatedOn` with full precision).
2. Return `Version` to the edit screen together with the record and send it back on save.
3. In the save Server Action (inside the CRUD wrapper):
   ```text
   [Customer_Update(Customer, ExpectedVersion)]
     Start
       -> GetCustomerForUpdate(Customer.Id)         // row lock until the transaction ends
       -> If GetCustomerForUpdate.Record.Version <> ExpectedVersion
            True  -> Raise ConcurrencyConflictException ("Record changed by another user")
            False -> Customer.Version = ExpectedVersion + 1
                     UpdateCustomer(Customer) -> End
   ```
4. In the Client Action, catch `ConcurrencyConflictException` and tell the user to reload the latest data (never auto-overwrite).

> `Get<Entity>ForUpdate` locks the row until the transaction commits, so the version check and the update are atomic. A plain `Get<Entity>` followed by `Update<Entity>` is **not** safe.

---

## 2. Duplicate Submissions: UI Guard + Server Idempotency Key

**Both layers are mandatory**; the UI guard improves UX, the server key guarantees correctness.

1. **UI guard**: set `IsSubmitting = True` at the start of the Client Action, bind the button `Enabled = not IsSubmitting`, and reset it on every exit path (success, error, and the exception handler).
2. **Idempotency key**:
   - Generate a GUID (`GenerateGuid()`) when the screen/form initializes and send it with the submit.
   - Store it in the operation Entity (`Order.IdempotencyKey`) with a **Unique Index**.
   - In the Server Action, first look up an existing record with that key: if found, **return the existing result** instead of creating a new one. The unique index is the final guard if two requests race.

---

## 3. Counters and Sequential Numbers

* **❌ Never** compute the next number with an Aggregate `Max(Number) + 1`.
* **✅ Use a counter Entity** (`DocumentSequence { SequenceName, LastValue }`):
  ```text
  GetDocumentSequenceForUpdate(SequenceId) -> NextValue = Record.LastValue + 1
  -> UpdateDocumentSequence(LastValue = NextValue) -> use NextValue
  ```
  The row lock serializes concurrent requests. Keep this transaction **short** (do not call external APIs while holding the lock).
* For very high throughput, use a native database sequence (SQL Server `SEQUENCE` / Oracle `SEQUENCE`) through Advanced SQL — see `advanced-sql-and-index-tuning`.
* **Stock / balances**: never `read -> subtract in memory -> update`. Lock with `GetForUpdate`, or use a single conditional statement (`UPDATE ... SET Qty = Qty - @N WHERE Id = @Id AND Qty >= @N`) and verify that a row was affected.

---

## 4. Background Processing: Safe Claiming of Work

When several Timers, BPT activities, or front-ends process the same queue table:

1. **Claim before processing**: move rows from `Pending` to `Processing` with an owner/claim token and timestamp in one short transaction, and `CommitTransaction` immediately.
2. Process only the rows you claimed; set `Done` or `Failed` (with `RetryCount` and `LastError`) at the end.
3. **Recover stuck rows**: a row stuck in `Processing` longer than the expected processing time goes back to `Pending` (worker crashed or timed out).
4. Design each item's processing to be **idempotent**, because it may run more than once after a crash. See `timers-and-bpt-patterns` for batching and soft timeouts.

---

## 5. Integration Retries Without Duplicated Side Effects

1. **Only retry operations that are safe to repeat**: reads (`GET`), or writes protected by an idempotency key the provider supports (for example an `Idempotency-Key` header or a unique external reference).
2. **Persist the outbound request first** (status `Pending` + your reference), then call the API, then mark it `Sent`. On timeout, query the provider by your reference **before** retrying.
3. **Exponential backoff**: retry from a Timer with increasing delay and a maximum retry count; never retry in a tight loop inside the user's request.
4. **Never retry on 4xx validation errors** — only on timeouts, 429, and 5xx.

---

## 6. Transaction Hygiene

* Keep transactions that hold locks (`GetForUpdate`) **short**: no external calls, no user waits, no heavy loops while locked.
* Always lock rows in a **consistent order** (e.g. parent before child, ascending Id) to avoid deadlocks.
* Remember: each Server Action called from a Client Action is its own transaction (see `client-action-single-server-call`).

---

## 7. Pre-Commit Checklist

- [ ] Edit flows on shared records use a `Version` check with `Get<Entity>ForUpdate`.
- [ ] Create/payment flows have a disabled-while-submitting button **and** a server idempotency key with a unique index.
- [ ] No `Max() + 1` for numbering; counters use `GetForUpdate` or a database sequence.
- [ ] Background workers claim rows before processing and recover stuck rows.
- [ ] Integration retries only for safe operations, with backoff and a retry limit.
