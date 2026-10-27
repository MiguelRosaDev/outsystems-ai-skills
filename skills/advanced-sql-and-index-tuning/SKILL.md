---
name: advanced-sql-and-index-tuning
description: High-performance SQL query authoring, SARGability optimization, and Entity Index validation for OutSystems 11 across both Microsoft SQL Server (T-SQL) and Oracle (SQL / PL/SQL) database engines. Use whenever writing or reviewing Advanced SQL nodes, inspecting Aggregate SQL (getAggregateSql), testing queries (testQuery), or designing Entity Indexes (getDataModel).
---

# advanced-sql-and-index-tuning

OutSystems 11 (O11) enterprise environments run on either **Microsoft SQL Server ( / Azure SQL — T-SQL)** or **Oracle Database (Oracle SQL / PL/SQL)**. While OutSystems **Aggregates** automatically translate queries to the underlying database engine, **Advanced SQL (`SQL` nodes)** execute raw engine-specific SQL and are the #1 source of dialect syntax errors, full table scans, and database locks when written without dialect and index checks.

Whenever creating, reviewing, or optimizing queries in OutSystems 11, follow these strict rules.

---

## 1. Mandatory Pre-Flight Workflow with OutSystems 11 MCP

Before writing or modifying any Advanced SQL node or heavy Aggregate:
1. **Check the Database Engine (`getSqlDialect`)**:
   - Always verify whether the target environment is **SQL Server** or **Oracle** before writing a single line of Advanced SQL.
2. **Inspect Entities & Existing Indexes (`getDataModel`)**:
   - Check the attributes, data types, and existing **Indexes** of every Entity involved in `FROM`, `JOIN`, `WHERE`, `GROUP BY`, and `ORDER BY`.
3. **Inspect Generated Aggregate SQL (`getAggregateSql`) & Test (`testQuery`)**:
   - When optimizing an Aggregate, inspect its compiled SQL via `getAggregateSql`. When authoring an Advanced SQL query, validate syntax and execution using `testQuery`.

---

## 2. OutSystems 11 Advanced SQL Syntax Rules (Both Engines)

1. **Always Use Dynamic Entity & Attribute Brackets**:
   - Reference tables as **`{EntityName}`** and columns as **`{EntityName}.[AttributeName]`** (or `alias.[AttributeName]`).
   - **Never** hardcode physical table names (`OSUSR_abc_Customer`), which change across environments and break OutSystems TrueChange impact analysis.
   ```sql
   SELECT c.[Id], c.[Name], o.[OrderNumber], o.[TotalAmount]
   FROM {Customer} c
   INNER JOIN {Order} o ON o.[CustomerId] = c.[Id]
   WHERE c.[IsActive] = 1
     AND o.[StatusId] = @StatusId
   ```
2. **Never Use `SELECT *`**:
   - Explicitly select only the columns that match the attributes of your **Output Structure** in the exact same order, or `SELECT {Entity}.*` only when the Output Structure is the full Entity itself.
3. **Never Use `Expand Inline = Yes` Without Sanitization**:
   - Keep `Expand Inline = No` so `@Parameter` is compiled as a bind variable (`@param` in SQL Server, `:param` in Oracle), enabling execution plan caching and preventing SQL Injection.
   - If a dynamic `IN (...)` list or `ORDER BY` clause strictly requires `Expand Inline = Yes`, sanitize text with `EncodeSql()` or build comma-separated integer IDs via `BuildSafe_InClauseIntegerList`.

---

## 3. SQL Server (T-SQL) vs. Oracle (SQL / PL/SQL) Dialect Matrix

Write dialect-compliant SQL according to the environment's database engine:

| Feature | Microsoft SQL Server (T-SQL) | Oracle Database (SQL / PL/SQL) | Portable / Best Practice in O11 |
| :--- | :--- | :--- | :--- |
| **Pagination / Top N** | `SELECT TOP (@Max) ...` or `ORDER BY ... OFFSET @Skip ROWS FETCH NEXT @Max ROWS ONLY` | `ORDER BY ... OFFSET @Skip ROWS FETCH NEXT @Max ROWS ONLY` *(12c+)* or `WHERE ROWNUM <= @Max` | Use `OFFSET @StartIndex ROWS FETCH NEXT @MaxRecords ROWS ONLY` ( always requires `ORDER BY`). Never use `TOP` in Oracle. |
| **NULL Coalescing** | `ISNULL(col, default)` or `COALESCE(col, default)` | `NVL(col, default)` or `COALESCE(col, default)` | Prefer **`COALESCE(col, default)`** (works in both). |
| **Empty Strings (`''`)** | `''` is a 0-length string (`'' <> NULL`). OutSystems stores empty Text as `''`. | **In Oracle, `''` IS `NULL`!** OutSystems stores empty Text as a single space `' '` or `NULL`. | When checking empty text in Oracle Advanced SQL, check `(col IS NULL OR col = ' ')`. |
| **String Concatenation** | `c.[FirstName] + ' ' + c.[LastName]` or `CONCAT(a, b)` | `c.[FirstName] \|\| ' ' \|\| c.[LastName]` | Use `+` on SQL Server and `\|\|` on Oracle. |
| **Current Date / Time** | `GETDATE()` / `CAST(GETDATE() AS DATE)` | `SYSDATE` / `TRUNC(SYSDATE)` | **Best Practice**: Pass `@CurrentDateTime` (`CurrDateTime()`) or `@CurrentDate` (`CurrDate()`) as an Input Parameter! |
| **Date Arithmetic** | `DATEADD(day, -7, @Today)` / `DATEDIFF(day, d1, d2)` | `@Today - 7` / `ADD_MONTHS(@Today, -1)` / `d2 - d1` | Prefer computing target boundary dates in OutSystems (`AddDays(CurrDate(), -7)`) and passing `@MinDate` as a parameter! |
| **Single-Row Dummy Select** | `SELECT 1` | `SELECT 1 FROM DUAL` | `FROM DUAL` is **mandatory** in Oracle whenever there is no target table. |
| **`UPDATE` with `JOIN`** | `UPDATE o SET o.[StatusId] = @NewStatus FROM {Order} o INNER JOIN {Customer} c ON c.[Id] = o.[CustomerId] WHERE c.[Tier] = @Tier` | **Oracle forbids `UPDATE ... FROM`!** Use `MERGE INTO {Order} o USING {Customer} c ON (o.[CustomerId] = c.[Id]) WHEN MATCHED THEN UPDATE SET ...` or `WHERE EXISTS (...)`. | Use `WHERE EXISTS (...)` or `MERGE INTO` on Oracle. |
| **Procedural Block** | `BEGIN ... END` (no semicolon after `END`) | `BEGIN ... commit/logic; END;` (**trailing semicolon after `END;` is mandatory** in PL/SQL) | Only use procedural blocks when multiple DML statements must run in one round-trip. |
| **Uncommitted Reads** | `WITH (NOLOCK)` hint on high-concurrency read-only reporting tables (beware dirty reads). | **Never use `NOLOCK` on Oracle.** Oracle uses multi-version read consistency (MVCC / Undo) natively—readers never block writers. | Do not add table hints on Oracle. |

---

## 4. High-Performance Query Rules (SARGability & Execution Plans)

Every query must be **SARGable (Search Argument Able)** so the database optimizer can perform an **Index Seek** (`INDEX RANGE SCAN` in Oracle) instead of a **Full Table Scan** (`TABLE ACCESS FULL`).

### Rule 1: Never Wrap Indexed Columns in Functions in `WHERE` or `JOIN`
Applying a function (`UPPER`, `LOWER`, `YEAR`, `MONTH`, `TRUNC`, `CAST`, `ISNULL`, `NVL`, `SUBSTR`) to a table column invalidates standard B-Tree indexes and forces the database to scan every row in the table.

* **❌ Non-SARGable (Full Table Scan)**:
  ```sql
  WHERE YEAR(o.[CreatedOn]) = 2026 AND MONTH(o.[CreatedOn]) = 10
  ```
* **✅ SARGable (Index Seek)**:
  ```sql
  WHERE o.[CreatedOn] >= @StartOfMonth
    AND o.[CreatedOn] < @StartOfNextMonth
  ```

* **❌ Non-SARGable**:
  ```sql
  WHERE ISNULL(o.[StatusId], 0) = @StatusId
  ```
* **✅ SARGable**:
  ```sql
  WHERE (@StatusId = 0 AND o.[StatusId] IS NULL) OR (o.[StatusId] = @StatusId)
  ```

### Rule 2: Avoid Optional Filter "Catch-All" Anti-Patterns on Huge Tables
In OutSystems, it is common to write optional search filters as:
```sql
WHERE (@CustomerId = 0 OR o.[CustomerId] = @CustomerId)
  AND (@SearchText = '' OR c.[Name] LIKE '%' + @SearchText + '%')
```
* On large tables (>1M rows), multiple `@Param = '' OR ...` predicates cause **parameter sniffing** and suboptimal cached execution plans.
* Always include at least one **mandatory selective indexed predicate** (e.g., date range `o.[CreatedOn] >= @FromDate`, `TenantId`, or `StatusId`), and avoid leading wildcards (`LIKE '%text%'`) on unindexed or massive columns—prefer prefix search (`LIKE @Prefix + '%'`) when possible.

### Rule 3: Use `EXISTS` Instead of `IN` / `DISTINCT` for 1-to-Many Filtering
When filtering parent records (`{Customer}`) that have matching child records (`{Order}`):
* **❌ Bad (`INNER JOIN` + `SELECT DISTINCT`)**: Multiplies rows in memory and forces an expensive `SORT UNIQUE` operation.
* **✅ Good (`WHERE EXISTS`)**: Stops scanning child index on the very first matching row:
  ```sql
  SELECT c.[Id], c.[Name]
  FROM {Customer} c
  WHERE c.[IsActive] = 1
    AND EXISTS (
        SELECT 1
        FROM {Order} o
        WHERE o.[CustomerId] = c.[Id]
          AND o.[StatusId] = @ActiveOrderStatusId
    )
  ```

### Rule 4: Lean Pagination `Count` Queries
When writing a manual pagination query pair (`Data` query + `Count` query):
* In the `Count` query (`SELECT COUNT(1) FROM ...`), **remove `ORDER BY`** and **remove all `LEFT JOIN`s** to lookup/reference tables whose columns are only used in the `SELECT` list and do not filter rows.

---

## 5. Entity Index Validation & Design Guide

Whenever you create or modify a query, **always validate the indexes on the queried Entities** (`getDataModel`) and suggest missing indexes to the developer:

### 1. What OutSystems Indexes Automatically vs. What You Must Add
* **Automatically Indexed**:
  - Primary Key (`Id`).
  - Foreign Key attributes (`EntityId` with a relationship to another Entity in the same database catalog).
* **NOT Automatically Indexed (Must Be Added Manually)**:
  - **External / Loose Foreign Keys**: Attributes of type `Long Integer` or `Text` that store IDs from external systems, different database catalogs, or soft-coupled modules without a hard FK constraint.
  - **Business Lookup Codes**: `Code`, `ExternalId`, `StatusId` + `CreatedOn`, `CorrelationId`, `TaxNumber`/`NIF`, `Email`.
  - **Sorting / Date Range Columns**: `CreatedOn`, `UpdatedOn`, `ScheduledDate` used in `WHERE` ranges or `ORDER BY`.

### 2. How to Design Composite Indexes (The ESR Rule)
When a query filters and sorts by multiple columns on the same Entity, recommend a **Composite Index** ordered by **E -> S -> R**:
1. **E — Equality columns first**: Columns compared with `=` (e.g., `[IsActive]`, `[StatusId]`, `[CustomerId]`), most selective first.
2. **S — Sort / Group columns second**: Columns in `ORDER BY` or `GROUP BY` (e.g., `[CreatedOn]`) so the engine avoids an in-memory Sort operator.
3. **R — Range columns last**: Columns compared with `>`, `<`, `BETWEEN` (because any column after a range column in the index cannot be seeked directly).

* **Example**:
  For query:
  ```sql
  SELECT o.[Id], o.[TotalAmount]
  FROM {Order} o
  WHERE o.[CustomerId] = @CustomerId
    AND o.[StatusId] = @StatusId
  ORDER BY o.[CreatedOn] DESC
  ```
  - **Recommended Composite Index on `{Order}`**: `Idx_Order_CustomerId_StatusId_CreatedOn` -> `(CustomerId, StatusId, CreatedOn)`.

### 3. Index Guardrails (Preventing Write Degradation)
* **Max 4–5 Custom Indexes per High-Write Entity**: Every index on a transactional table (`{Order}`, `{AuditLog}`, `{Transaction}`) adds overhead to `Create`, `Update`, and `Delete` operations.
* **Never Duplicate Prefix Indexes**: If an Entity already has a composite index on `(CustomerId, StatusId)`, the single-column auto-index on `(CustomerId)` is redundant (or vice versa—do not create two overlapping composite indexes with the same leading columns).
* **Unique Indexes for Data Integrity**: Recommend `Unique = Yes` on natural business keys (e.g., `(CustomerId, ExternalReference)`) so the database enforces uniqueness under concurrent requests.
