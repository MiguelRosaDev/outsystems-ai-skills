---
name: timers-and-bpt-patterns
description: Best practices for OutSystems 11 background processing, including heavy Timers (soft timeouts, batching, CommitTransaction, WakeTimer, kill-switches), Standard BPT, and Light BPT. Use whenever creating or modifying timers, batch jobs, or asynchronous processes.
---

# timers-and-bpt-patterns

Background processing in OutSystems 11 is handled through **Timers** (scheduled jobs) and **BPT / Light BPT** (event-driven workflows). Improper design of heavy background jobs causes database locks, transaction rollbacks, scheduler thread starvation, and duplicate processing.

---

## 1. Heavy Timers: The Soft Timeout & `WakeTimer` Pattern

### The Problem
* OutSystems timers have a default timeout of **20 minutes**.
* If a timer exceeds the timeout (or throws an unhandled exception), **the entire database transaction rolls back**, losing all work done during those 20 minutes.
* Furthermore, the Scheduler Service automatically retries failed timers up to **3 times**, repeating the same heavy workload and locking database tables.
* **Never** fix a slow timer simply by increasing its timeout in Service Center to hours.

### The Solution Blueprint (`Timer_<ActionName>`)

Every timer that processes unbounded or large datasets must implement **Batching + `CommitTransaction` + Soft Timeout + `WakeTimer` + Kill-Switch**:

```text
[Start]
   │
   ▼
[If Not Site.IsTimerEnabled] ──(True)──► [End (Kill-Switch Active)]
   │ (False)
   ▼
[Assign: TimeoutLimit = AddMinutes(CurrDateTime(), 15)]  // 5-min safety margin before 20m limit
   │
   ▼
[Aggregate: GetUnprocessedBatch (Filter: IsProcessed = False, MaxRecords = 500)]
   │
   ▼
[If GetUnprocessedBatch.List.Empty] ──(True)──► [End (All Work Completed)]
   │ (False)
   ▼
[ForEach Item in GetUnprocessedBatch.List]
   ├──(Body)──► [Process Item & Mark Item.IsProcessed = True, UpdateEntity]
   │
   ▼ (After Loop)
[CommitTransaction]  // Persists the batch of 500 records permanently
   │
   ▼
[If CurrDateTime() >= TimeoutLimit]
   ├──(True)──► [WakeTimer_<Name>] ──► [End (Graceful handover to next run)]
   │
   └──(False)─► (Loop back to GetUnprocessedBatch Aggregate)
```

### Mandatory Rules for Timers:
1. **Action Naming**: Prefix every timer's target Server Action with `Timer_` (e.g., `Timer_SyncCustomerInvoices`).
2. **Kill-Switch Site Property**: Include a boolean Site Property (e.g. `IsSyncTimerEnabled`, default `True`) checked at the start of the timer and before each batch, allowing operations teams to halt a runaway timer in Service Center without republishing.
3. **Batch Commit**: Call `CommitTransaction` (from `System`) **once per batch** (e.g. every 200–500 records), **never** inside the inner `ForEach` per single record (micro-commits degrade database throughput).
4. **Idempotency Flag**: Always mark records as processed (`IsProcessed = True` or `ProcessedOn = CurrDateTime()`) *before* calling `CommitTransaction` so subsequent runs never re-process the same items.
5. **Audit Logging**: Log start, number of processed records, and completion/re-wake status using `LogMessage`.

---

## 2. BPT vs. Light BPT (Business Process Technology)

Choose the right asynchronous engine based on the workflow characteristics:

| Feature | **Light BPT** (Recommended for Scale) | **Standard BPT** |
| :--- | :--- | :--- |
| **Use Case** | High-volume, event-driven asynchronous tasks triggered by entity insert/update | Complex, long-running workflows with human approvals, waits, or decisions |
| **Execution** | Single `Automatic Activity` (max 3-minute timeout) | Multiple Automatic & Human Activities, Decisions, Waits |
| **Database Overhead** | Minimal (deletes process metadata immediately upon completion) | High (retains full state history in `OSSYS_BPM_*` tables) |
| **Throughput** | Thousands of events per minute | Lower throughput, stateful orchestration |

### Rules for Light BPT:
1. Enable **Light Process** by creating a Process with **only one Automatic Activity** triggered by `Expose Process Entity = Yes` (`On Create` or `On Update`) and no other flow nodes.
2. Keep the Automatic Activity fast (**well under 3 minutes**).
3. Never use Standard BPT for simple fire-and-forget queue items (e.g. sending emails or syncing a single record)—always use Light BPT or a queued Timer.
