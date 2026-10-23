---
name: documentation-and-code-hygiene
description: Standards for 100% element documentation coverage and TrueChange zero-warning hygiene in OutSystems 11 modules. Use whenever creating or editing modules, actions, entities, structures, parameters, or running coverage and set_descriptions tools.
---

# documentation-and-code-hygiene

OutSystems AI Mentor Studio (Architecture Dashboard) and TrueChange evaluate module maintainability based on documentation completeness and code cleanliness. Every module created or touched by an AI assistant must target **100% description coverage** and **0 TrueChange warnings**.

---

## 1. The Golden Rule of Descriptions: Never Repeat the Name

Writing `"Customer Id"` as the description of an attribute named `CustomerId` is useless noise. Every description must state **meaning, constraints, units/timezone, or caller contract** in one or two concise sentences.

### What to Write by Element Kind:

| Element Kind | What the Description Must State | Example |
| :--- | :--- | :--- |
| **Module (`eSpace`)** | Layer (4LC), domain ownership, and core responsibility | `"Core Service module managing customer profiles, tax validation, and billing addresses."` |
| **Action (Server / Client / Data)** | What it does, key guardrail/limit, and who calls it | `"Validates and persists a new invoice, enforcing tax rules. Called by Order_BL during checkout."` |
| **Input / Output Parameter** | Meaning, unit or timezone (e.g. UTC), and what empty/`NullIdentifier()` means | `"Start date of the billing period (UTC). When NullDate(), defaults to the first day of the current month."` |
| **Local Variable** | Role in the algorithm (read assignments first before describing!) | `"Running counter of processed batch items; resets to 0 after each CommitTransaction."` |
| **Entity / Structure** | Business concept represented and lifecycle/mutability | `"Immutable audit log entry recorded whenever a user's security role is modified."` |
| **Entity / Structure Attribute** | Meaning, allowed values/format, PII flag, or unit | `"Customer's 9-digit tax identification number (NIF). Personal data; masked in logs."` |
| **Site Property** | Purpose, unit, and exact runtime behavior when empty or 0 | `"Maximum batch size for Timer_SyncInvoices. Set to 0 to disable batch processing (kill-switch)."` |

---

## 2. Using MCP `coverage` and `set_descriptions`

When using the OutSystems MCP on a module:
1. Run `coverage` to inspect undescribed counts across all element kinds (`Action`, `ActionInputParameter`, `ActionOutputParameter`, `ActionLocalVariable`, `Entity`, `EntityAttribute`, `Structure`, `StructureAttribute`, `SiteProperty`, `Role`, `Timer`, etc.).
2. Read the element's implementation (`read_action` or `read_element`) **before** writing its description—never guess what an action or variable does from its name alone.
3. Do not overwrite meaningful descriptions already written by another developer unless the underlying behavior changed.
4. Apply descriptions in batches using `set_descriptions` and re-run `coverage` until undescribed count reaches `0`.

---

## 3. TrueChange Zero-Warning Hygiene Checklist

Before saving or publishing a module, ensure zero hygiene warnings remain:
1. **No Unused Local Variables or Parameters**: Delete any local variable or parameter that is declared but never read or assigned.
2. **No Unused References**: Remove imported producer actions, structures, or entities that are no longer referenced in the module.
3. **No Dead Code Branches**: Never leave `If` conditions hardcoded to `True` or `False`, or unreachable nodes disconnected from the flow.
4. **No Empty Exception Handlers**: Every exception handler must at minimum log the error (`LogMessage`) or set an explicit error output/feedback message.
