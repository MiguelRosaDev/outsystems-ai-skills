---
name: architecture-canvas-4lc
description: Enforces OutSystems Architecture Canvas (4-Layer Canvas - 4LC) rules and validates module dependencies to prevent circular references, upward dependencies, and architecture debt. Use whenever designing modules, adding references, or organizing logic.
---

# architecture-canvas-4lc

The OutSystems **Architecture Canvas** (4-Layer Canvas or 4LC) is the official framework to design solid, maintainable, and decoupled enterprise applications.

When an AI model adds module references (`add_references`) or organizes elements, it must strictly obey the 4LC layers and the 3 Golden Rules.

---

## 1. The 4 Layers

```text
┌──────────────────────────────────────────────────────────┐
│  1. ORCHESTRATION (O)                                    │
│     Cross-application business processes & workflows     │
├──────────────────────────────────────────────────────────┤
│  2. END-USER (E)                                         │
│     Screens, UI flows, Web/Mobile views, user journeys   │
├──────────────────────────────────────────────────────────┤
│  3. CORE (C)                                             │
│     Core services, business concepts, entities, APIs     │
├──────────────────────────────────────────────────────────┤
│  4. FOUNDATION (F)                                       │
│     Reusable technical libraries, UI themes, connectors │
└──────────────────────────────────────────────────────────┘
```

---

## 2. The 3 Golden Rules of Architecture

### Rule 1: No Upward Dependencies (Strictly Forbidden)
A module in a lower layer can **NEVER** consume a module from a higher layer:
* ❌ Foundation cannot consume Core, End-User, or Orchestration.
* ❌ Core cannot consume End-User or Orchestration.
* ❌ End-User cannot consume Orchestration.

Dependencies must always flow **downward**.

### Rule 2: No Side Dependencies in End-User Layer
An End-User module must **NEVER** reference another End-User module:
* ❌ `Portal_Customer` cannot reference `Portal_Backoffice`.
* If two End-User modules need to share UI components, business logic, or data, that common logic must be extracted down to the **Core** or **Foundation** layer.

### Rule 3: No Circular Dependencies in Core Layer
Two modules in the same layer can never reference each other mutually:
* ❌ `Order_CS` references `Customer_CS`, and `Customer_CS` references `Order_CS`.
* **Fix**: If two core modules depend on each other:
  1. Merge them if they represent the exact same functional concept.
  2. Or extract the shared concept (e.g. DTO structures, common entities) into a new, lower-level Core Service module.

---

## 3. Standard Module Naming Conventions

Use these standardized suffixes to immediately identify a module's layer and responsibility:

| Suffix | Layer | Purpose | Example |
| :--- | :---: | :--- | :--- |
| `_CS` | Core | **Core Service**: Entities, CRUD wrappers, local business logic | `Customer_CS`, `Billing_CS` |
| `_BL` | Core | **Business Logic**: Complex cross-entity calculations and rules | `OrderProcessing_BL` |
| `_IS` | Core | **Integration Service**: Wrapper around external REST/SOAP APIs | `SAP_IS`, `PaymentGateway_IS` |
| `_CW` | Core | **Core Widget**: Reusable domain-specific UI blocks (e.g. Customer Card) | `Customer_CW` |
| `_API` | Core | **Exposed API**: Exposes REST/SOAP endpoints to external systems | `Customer_API` |
| `_Th` | Foundation | **UI Theme**: Base CSS, layouts, branding assets | `Corporate_Th` |
| `_Lib` | Foundation | **Library**: Generic technical helpers without business logic | `TextUtilities_Lib`, `Crypto_Lib` |
| `_Eng` | Foundation | **Engine**: Reusable technical engines (e.g. Audit engine) | `Notification_Eng` |

---

## 4. Verification Checklist Before Adding References

Before creating a dependency via `add_references`:
1. What is the source module's layer?
2. What is the target module's layer?
3. Does this dependency violate Rule 1 (upward)?
4. Does this dependency violate Rule 2 (side-by-side in End-User)?
5. Will this create a circular loop with existing references?
If any rule is violated, refactor the architecture before proceeding.
