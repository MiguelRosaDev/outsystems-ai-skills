---
name: data-lifecycle-and-gdpr-retention
description: Data lifecycle, retention, and GDPR/RGPD erasure standards for OutSystems 11 data models. Covers soft delete vs hard delete, anonymization instead of deletion, legal retention periods, purge and archive Timers for logs/audit/staging tables, unique indexes with soft delete, binary storage growth, and right-to-erasure workflows. Use whenever designing Entities that store personal or high-volume data, implementing delete features, or building cleanup jobs.
---

# data-lifecycle-and-gdpr-retention

Every table grows forever unless someone designs its end of life. In OutSystems factories this shows up as multi-hundred-GB log and staging tables, slow queries on "active" data, and personal data kept long after the legal basis expired. GDPR/RGPD requires **storage limitation** (keep personal data only as long as necessary) and the **right to erasure**, balanced with legal retention obligations (invoicing, tax, telecom, and financial records).

**Rule**: When creating an Entity, decide and document in its description: *What is its retention period? How does a record end its life (archive, anonymize, delete)? Which job does it?*

---

## 1. Classify Every Entity

| Class | Examples | Lifecycle |
| :--- | :--- | :--- |
| **Reference / configuration** | Static Entities, product catalog, settings | Kept; deactivate (`IsActive = False`) instead of delete. |
| **Business records with legal retention** | Orders, invoices, contracts, payments | Keep for the legal period; **anonymize** personal fields when the person requests erasure, keep the financial facts. |
| **Personal data without legal retention** | Leads, marketing consents, profile preferences | Delete or anonymize when the purpose ends or on erasure request. |
| **Operational / technical high-volume** | Custom logs, integration payload logs, audit trails, staging/import tables, notifications queue | **Purge** automatically after a defined period (e.g. 30/90/365 days). |
| **Binary content** | Documents, images, attachments | Stored in a separate Entity; purged with its parent; consider external storage for large volumes. |

---

## 2. Soft Delete vs Hard Delete

* **Soft delete** (`IsActive`/`DeletedOn` + `DeletedBy`): use when records are referenced by history, need undo, or must stay for audit.
  - Every Aggregate and SQL query on the Entity must filter `IsActive = True` — encapsulate this in the CRUD wrapper and read actions (see `crud-wrapper-pattern`).
  - **Unique indexes conflict with soft delete**: a deleted `Email` still blocks a new record with the same value. Either include a discriminator in the unique index or release the unique value on delete.
  - Soft delete is **not** erasure: soft-deleted rows still contain personal data.
* **Hard delete**: use for technical data, staging, and personal data whose purpose ended. Respect Delete Rules (`Protect`/`Delete`/`Ignore`) — see `data-modeling-best-practices`.

---

## 3. Anonymization (Right to Erasure with Legal Retention)

When a record must be kept but the person must be "forgotten":
1. Overwrite direct identifiers with non-reversible values: `Name = "Anonymized"`, `Email = ""`, `NIF = ""`, `Phone = ""`, `Address = ""`, `BirthDate = NullDate()`.
2. Remove or regenerate indirect identifiers (free-text notes, uploaded ID documents, IP addresses).
3. Keep the facts required by law (amounts, dates, product, tax values) and the internal Id so references stay valid.
4. Set `AnonymizedOn` and record the erasure request (date, requester, scope) in an audit Entity **without** storing the erased data again.
5. Cover **all copies**: related Entities, custom log tables, integration payload logs, files, exports, and downstream systems notified through integrations.

> Hashing an email or NIF is **pseudonymization**, not anonymization: it can be reversed by brute force. Use it only for matching, never as "deleted".

---

## 4. Purge and Archive Jobs

1. One **purge Timer** per data domain, scheduled outside business hours, deleting in **batches** with `CommitTransaction` between batches, a soft timeout, and a kill switch (see `timers-and-bpt-patterns`).
2. Purge by an **indexed date column** (`CreatedOn < @CutoffDate`) — see `advanced-sql-and-index-tuning`.
3. Retention periods come from configuration (Site Property / configuration Entity), not literals.
4. **Archive** when data must be kept but is rarely read: move rows older than the active window to an archive Entity or external storage so day-to-day queries stay on a small table.
5. Never purge platform log tables directly with SQL; use platform-provided log retention/configuration, and keep your **own** custom log tables under the purge policy above.

---

## 5. Design Rules for New Entities

- Add `CreatedOn` (indexed when used for purge), `CreatedBy`, `UpdatedOn`, `UpdatedBy`.
- Do not copy personal data into many Entities "for convenience": reference the owner record by Id, so erasure has one place to act.
- Keep free-text fields to the minimum: they accumulate personal data nobody can find later.
- Staging/import tables must be cleaned after successful processing, not kept forever.

---

## 6. Pre-Commit Checklist

- [ ] Every new Entity has a documented retention period and end-of-life action.
- [ ] High-volume technical tables have a batched purge Timer with configurable retention and an indexed date filter.
- [ ] Soft-deleted Entities are filtered in all reads and unique indexes account for deleted rows.
- [ ] Erasure requests anonymize all copies of personal data while keeping legally required facts.
- [ ] No pseudonymized (hashed) value is treated as anonymized.
