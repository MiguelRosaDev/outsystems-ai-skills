---
name: reactive-lifecycle-and-sync
description: Rules for OutSystems 11 Reactive Web and Mobile screen/block lifecycle events (OnInitialize, OnReady, OnRender, OnParametersChanged, OnDestroy) and Mobile Offline Synchronization patterns. Use whenever building screens, blocks, client actions, or offline local storage logic.
---

# reactive-lifecycle-and-sync

In OutSystems 11 Reactive Web and Mobile applications, placing logic in the wrong lifecycle event causes infinite rendering loops, sluggish screen transitions, or stale block data.

---

## 1. Screen & Block Lifecycle Events

Events execute in a strict order. Respect what is permitted and forbidden in each event:

| Event | When It Fires | ✅ Allowed | ❌ Strictly Forbidden |
| :--- | :--- | :--- | :--- |
| **`OnInitialize`** | Before DOM exists and before aggregates finish | Setting fast local variable defaults, synchronous permission checks | Slow synchronous Server Action calls that block screen navigation; DOM access |
| **`OnReady`** | DOM is mounted and ready | Initializing 3rd-party JS libraries, setting input focus, registering DOM listeners | Heavy data fetching that should be in screen Aggregates/Data Actions |
| **`OnRender`** | After `OnReady` AND **every time any screen data or variable changes** | Lightweight DOM adjustments that depend on re-rendered widgets | **`Refresh Data`**, calling Server Actions, or mutating screen state (causes **infinite re-render loops**!) |
| **`OnParametersChanged`** | *(Blocks only)* When parent changes an Input Parameter of the Block | Calling **`Refresh Data`** on the Block's aggregates/data actions or recalculating local state | Forgetting to implement this when a Block displays data filtered by an Input Parameter |
| **`OnAfterFetch`** | When a specific Aggregate or Data Action completes | Chaining a dependent query, setting default selection from fetched list, transforming data | Unrelated heavy logic |
| **`OnDestroy`** | When leaving the screen or unmounting the block | Clearing `setInterval`/`setTimeout`, removing window/document event listeners | Leaving timers or listeners active (causes SPA memory leaks) |

---

## 2. Block-to-Parent Communication

* **Downward Data Flow (Inputs)**: Parent screens pass data into Blocks via Input Parameters. Whenever an Input Parameter changes, the Block **must** implement `OnParametersChanged` to refresh its internal data.
* **Upward Communication (Events)**: A Block must never reach into parent screen elements directly. Instead, define a typed **`Event`** on the Block (e.g. `OnItemSelected`, `OnModalSaved`) and trigger it so the parent Screen handles the state transition.

---

## 3. Reducing Client-to-Server Roundtrips

In Reactive and Mobile apps, every call to a **Server Action** is an HTTP network request over the wire:
* ❌ **Never** call 3 or 4 separate Server Actions sequentially inside a single Client Action (`ServerActionA` ➔ `ServerActionB` ➔ `ServerActionC`).
* ✅ **Always** consolidate them into a single orchestration Server Action so the client makes **1 network roundtrip** instead of 4.
* ❌ **Never** call a Server Action inside a Client Action `ForEach` loop. Pass the entire list to a single Server Action.

---

## 4. Mobile Offline Data Synchronization Patterns

When using Local Storage Entities (`Local<Entity>`) in Mobile apps, implement the appropriate official OutSystems sync pattern inside `OfflineDataSync`:

### Pattern 1: Read-Only Full Refresh
* **Use Case**: Small reference tables or static configurations (< 500 records).
* **Flow**: Fetch all records from Server ➔ `DeleteAllLocal<Entity>` ➔ `CreateOrUpdateAllLocal<Entity>`.

### Pattern 2: Read-Only Delta Sync (Timestamp-Based)
* **Use Case**: Medium-to-large catalogs where re-downloading everything wastes bandwidth.
* **Flow**:
  1. Read `LastSyncTimestamp` from local storage.
  2. Call Server Action passing `LastSyncTimestamp`; Server returns only records where `UpdatedOn > LastSyncTimestamp` plus a list of deleted IDs.
  3. Call `CreateOrUpdateAllLocal<Entity>` for modified records and delete removed IDs.
  4. Update `LastSyncTimestamp`.

### Pattern 3: Read-Write (Last Write Wins)
* **Use Case**: Offline user tasks, notes, or inspections edited by a single user.
* **Local Entity Attributes**: Include `IsModified` (Boolean), `IsCreated` (Boolean), `IsDeleted` (Boolean).
* **Flow**:
  1. Query local records where `IsModified = True or IsCreated = True or IsDeleted = True`.
  2. Send batch of dirty records to Server Action.
  3. Server processes changes in a transaction and returns updated server records.
  4. Update local records (`IsModified = False`) and pull server delta.

### Pattern 4: One-to-Many Offline Master-Detail
* **Use Case**: Creating an `Order` and multiple `OrderItems` while offline.
* **Flow**: Use a local GUID or negative local ID while offline; during sync, when the server creates the `Order` and assigns its permanent `ServerId`, remap the foreign keys on all child `OrderItems` before inserting them on the server.
