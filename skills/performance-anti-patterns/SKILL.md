---
name: performance-anti-patterns
description: Performance rules and anti-pattern guardrails for OutSystems logic, aggregates, and database operations. Use whenever writing server actions, screen queries, or loops interacting with database entities or external services.
---

# performance-anti-patterns

AI models frequently introduce database bottlenecks when generating OutSystems logic. This skill enforces strict performance guardrails aligned with OutSystems Architecture Dashboard and AI Mentor best practices.

---

## 1. Zero Database Queries Inside Loops ($N+1$ Problem)

### ❌ The Anti-Pattern
Placing an **Aggregate**, an **Advanced SQL query**, or a **Server Action that performs queries** inside a `ForEach` loop:

```text
[ForEach Item in OrderList]
       │
       ▼
 [Aggregate GetProductDetails (Filtered by Item.ProductId)]  <-- FATAL BOTTLENECK: N queries
       │
       ▼
 [Next]
```
If `OrderList` has 500 items, this triggers 500 individual roundtrips to the database server.

### ✅ The Solution
1. **Join Before the Loop**:
   Fetch all necessary data in a single Aggregate upfront using a Join between `Order` and `Product`.
2. **Batch Query with `IN` Filter**:
   If joining isn't possible, collect all IDs into a list and execute a single query with an `IN` condition (or Advanced SQL `IN (@IdList)`).
3. **In-Memory Operations**:
   Iterate over the already fetched list in memory. Use `ListFilter` or `ListSort` client/server-side instead of querying repeatedly.

---

## 2. Screen Aggregate Fetch Optimization

* **`Fetch = At start`**: Use **only** for data required immediately to render the above-the-fold screen content.
* **`Fetch = Only on demand`**:
  * Use for data inside tabs, modals/popups, accordions, or user-triggered interactions.
  * Trigger via `Refresh Data` only when the user opens the corresponding UI section.
* **Max Records**:
  * Always specify `Max Records` (e.g. 10, 25, 50) when fetching records for tables, lists, or dropdowns.
  * Never query unbounded tables without pagination (`StartIndex` + `MaxRecords`).
* **Minimize Attribute Loading**:
  * Avoid fetching large Text attributes or Binary Data attributes in list aggregates.
  * OutSystems optimizes unused attributes automatically, but explicitly avoid querying unused entities in joins.

---

## 3. Site Properties Runtime Writing

### ❌ The Anti-Pattern
Modifying `Site.<Property>` values programmatically at runtime.
* In OutSystems, updating a Site Property forces an immediate application cache invalidation across all front-end nodes in the farm, degrading performance for all active users.

### ✅ The Solution
* Site Properties are reserved for static environmental constants (e.g. API base URLs, integration keys).
* For dynamic configuration values that change at runtime, use a dedicated **Configuration Entity**.

---

## 4. Entity Indexing Guidelines

Always create indexes in the Data tab for:
1. **Foreign Keys**: Every attribute referencing another entity must have an index.
2. **High-Frequency Filter Attributes**: Columns frequently used in `Where` clauses (e.g. `StatusId`, `CreatedOn`, `IsActive`).
3. **Composite Indexes**: When an aggregate consistently filters by multiple attributes together (e.g. `TenantId` + `CustomerId` + `IsActive`), define a composite index in the exact search order.

---

## 5. Bulk Inserts and Updates

When processing large datasets (e.g. importing hundreds or thousands of records):
* Avoid calling `Create<Entity>` or `Update<Entity>` one-by-one inside a loop.
* Use a single **Advanced SQL** statement with bulk insert (`INSERT INTO ... SELECT` or batch syntax) or a certified OutSystems batch extension.
