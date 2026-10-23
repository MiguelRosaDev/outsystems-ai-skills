---
name: data-modeling-best-practices
description: OutSystems 11 data modeling best practices, including Foreign Key Delete Rules (Protect, Delete, Ignore), Binary Data and Large Text 1-to-1 isolation, Static Entities vs Catalogs, and Entity Update Behavior. Use whenever designing or modifying database entities and relationships.
---

# data-modeling-best-practices

A poorly structured data model is the primary cause of database locks, bloated table scans, and foreign key constraint failures in OutSystems 11. Follow these rules whenever creating or modifying entities.

---

## 1. Foreign Key Delete Rules (`Protect`, `Delete`, `Ignore`)

Every reference attribute (foreign key) in an OutSystems entity must explicitly set its `Delete Rule`. Never leave all foreign keys on the default without evaluating their lifecycle:

### A. `Protect`
* **Behavior**: Creates a physical database constraint (`OSFRK`). Prevents deleting a parent record if any child record references it.
* **When to use**: Strict master-detail business relationships where deleting the parent must be blocked while children exist (e.g. `Customer` referenced by `Invoice`; `Order` referenced by `OrderItem`).

### B. `Delete` (Cascade)
* **Behavior**: Creates an `ON DELETE CASCADE` database constraint. Deleting the parent automatically deletes all child records.
* **When to use**: Subordinate composition entities or temporary staging/log detail tables where purging the header record should cleanly wipe its dependent rows in a single step.
* **Caution**: Do not chain deep cascade trees (A ➔ B ➔ C ➔ D), as deleting a top-level record can lock multiple tables simultaneously.

### C. `Ignore`
* **Behavior**: Does **not** create a physical database constraint (`OSFRK`), though an index should still be added. Deleting the parent leaves the child's foreign key value untouched.
* **When to use**:
  1. **Audit & History Tables**: If a `User` or `Product` is deleted, audit logs must retain the historical ID without blocking the deletion.
  2. **Cross-Layer / Loosely Coupled References**: High-volume transactional tables referencing external or loosely coupled catalogs where physical foreign key checks degrade high-throughput inserts.
  3. **Soft-Deleted Parents**: When the parent entity strictly uses soft-delete (`IsActive = False`).

---

## 2. Isolate Binary Data and Large Text (`1-to-1 Extension Entity Pattern`)

### ❌ The Anti-Pattern
Adding `Binary Data` (PDFs, images, attachments) or large `Text` attributes (> 2000 characters, such as base64 payloads or raw JSON blobs) directly inside a core operational entity (e.g., `User`, `Order`, `Employee`).
* Database pages swell, buffer cache efficiency drops, and every status update on the main entity locks heavy storage pages.

### ✅ The Solution: 1-to-1 Extension Entity
Split the binary or large text into a dedicated satellite entity whose Primary Key (`Id`) has its Data Type set to **`<MainEntity> Identifier`**:

```text
┌─────────────────────────┐          1 : 1          ┌───────────────────────────┐
│        Employee         │◄────────────────────────│      EmployeePhoto        │
├─────────────────────────┤                         ├───────────────────────────┤
│ PK  Id (Autonumber)     │                         │ PK,FK Id (EmployeeId)     │
│     Name (Text 100)     │                         │       FileName (Text 250) │
│     Email (Text 250)    │                         │       FileContent (Binary)│
│     IsActive (Boolean)  │                         │       MimeType (Text 100) │
└─────────────────────────┘                         └───────────────────────────┘
```

---

## 3. Static Entities vs. Regular Lookup Entities

* **Use a `Static Entity`** when:
  * The list of values is fixed at compile time and logic branches (`If` / `Switch`) depend directly on specific records (e.g. `OrderStatus.Pending`, `ApprovalType.Automatic`).
  * Always include `Order`, `Is_Active`, and `Label` attributes.
* **Use a Regular `Entity`** when:
  * Business administrators need to add, rename, or deactivate lookup values at runtime via a Backoffice screen without deploying code (e.g. `Department`, `ProductCategory`, `Country`).

---

## 4. Entity Update Behavior (`Changed Attributes`)

For entities with many attributes (typically > 10 attributes) or entities updated concurrently by different processes:
* Set **`Update Behavior = Changed Attributes`** (`UpdateEntityBehaviorType = 1` in ModelAPI).
* Instead of executing an `UPDATE` statement that overwrites every column in the row, OutSystems will only include the columns whose values actually changed in memory, preventing lost updates.

---

## 5. Attribute Sizing & Data Types

* **Text Lengths**: Size `Text` attributes realistically (e.g. `Code` = 20, `Name` = 100, `Email` = 250, `Description` = 500). Avoid defaulting every attribute to `50` or `2000+`.
* **Currency & Decimals**: Always use `Currency` or `Decimal` for monetary and financial quantities—never `Float` (floating-point precision errors break financial calculations).
