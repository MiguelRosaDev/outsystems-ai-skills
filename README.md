# OutSystems Skills for AI Coding Assistants

[![GitHub repo](https://img.shields.io/badge/GitHub-MiguelRosaDev%2Foutsystems--skills-blue.svg)](https://github.com/MiguelRosaDev/outsystems-skills)
[![OutSystems ModelAPI](https://img.shields.io/badge/OutSystems-ModelAPI%20%2F%20O11%20%2F%20ODC-red.svg)](https://www.outsystems.com/)
[![AI Skills](https://img.shields.io/badge/AI%20Skills-Agentic%20Workflows-green.svg)](https://github.com/MiguelRosaDev)

A comprehensive suite of specialized skills, best practices, and automated workflows for AI models and coding assistants (Claude, Cursor, Copilot, ChatGPT, or custom AI agents) interacting with **OutSystems 11 / ODC** via the **ModelAPI** and MCP servers (such as `os11-oml-transformer`).

---

## 🚀 Why This Repository Exists

When AI coding models generate or modify OutSystems applications through ModelAPI / MCP tools, they lack visual and architectural awareness by default:
* **Visual Knotting**: Flow nodes are placed on top of each other at coordinate `(0,0)`, creating tangled spaghetti lines in Service Studio.
* **Architecture Debt**: Models easily create circular dependencies, upward module references, or violate 4-Layer Canvas (4LC) standards.
* **Database Bottlenecks**: Models tend to write database aggregates inside `ForEach` loops ($N+1$ queries), fail to index filter attributes, or write runtime updates to Site Properties.
* **Security & Encapsulation Flaws**: Entities are accidentally exposed with public write access instead of read-only wrappers, and server actions lack role checks (`Check<Role>Role`).

This repository provides **executable guardrails and instructions** that guide AI models to produce clean, maintainable, performant, and secure OutSystems code.

---

## 📦 Included Skills

### 🎨 1. `outsystems-flow-layout`
Visual flow layout engine and structural health checker for Server, Screen, and Data Actions.
* **Deterministic Grid**: Standardized spacing (columns $\Delta X = 2286$, rows $\Delta Y = 1829$; Start at `2743, 1371`).
* **Zero Crossings**: Connectors that would cross nodes are cleanly routed through dummy Assign nodes (`Route` / `Loop back`).
* **Branch Isolation**: 1 Assign and 1 End per branch to eliminate cross-screen convergence lines.
* **Semantic Labels & Linked Comments**: Human-readable labels on every Assign/If, and comments linked to nodes via `comment.CreateConnector(node)`.
* **Structural Checker (`check-flow.cs`)**: Verifies 0 crossings, 0 overlapping nodes, 0 unreachable nodes, and 0 unlabelled logic elements.

### 🏛️ 2. `architecture-canvas-4lc`
Guards against architectural erosion and enforces the official OutSystems 4-Layer Canvas.
* **Layer Hierarchy**: Orchestration (O) ➔ End-User (E) ➔ Core (C) ➔ Foundation (F).
* **The 3 Golden Rules**: No upward dependencies, no side dependencies in End-User, and zero circular dependencies in Core.
* **Module Suffixes**: Enforces standardized naming (`_CS`, `_BL`, `_IS`, `_CW`, `_API`, `_Th`, `_Lib`).

### 🔒 3. `crud-wrapper-pattern`
Encapsulates Core Services (`_CS`) data entities with secure CRUD wrapper actions.
* **Read-Only Entities**: Enforces `Public = Yes` with `Expose Read Only = Yes`.
* **Mandatory Audit Attributes**: Injects `CreatedBy`, `CreatedOn`, `UpdatedBy`, `UpdatedOn`, and `IsActive` (soft-delete).
* **Standard Action Suite**: Generates `<Entity>_Create`, `<Entity>_Update`, `<Entity>_Get`, `<Entity>_GetForUpdate`, and `<Entity>_Delete`.
* **Integrity & Rollback**: Validates business constraints and handles `Abort Transaction` safely.

### ⚡ 4. `performance-anti-patterns`
Performance guardrails and database optimization rules.
* **Zero Queries in Loops**: Prohibits Aggregates, SQL queries, or server action fetches inside `ForEach` loops ($N+1$ anti-pattern).
* **Aggregate Optimization**: Rules for `Fetch = At start` vs `Fetch = Only on demand`, pagination, and `Max Records`.
* **Runtime Site Properties**: Forbids runtime programmatic writes to Site Properties (which invalidate cluster cache).
* **Indexing Guidelines**: Mandatory single/composite indexes on foreign keys and search filters.

### 🏷️ 5. `outsystems-naming-conventions`
Official OutSystems naming standards and folder organization.
* **Logic Actions**: PascalCase with `Verb + Noun` formula (`Customer_GetById`, `Invoice_CalculateTotal`).
* **Folder Structure**: Clean organization in the Logic tab (`Public`, `Internal`, `CRUD`, `Validations`, `Sync`).
* **Variables & DTOs**: Prefix `ST_` for structures, `Is/Has/Can` for booleans, and `...List` for list collections.

### 🛡️ 6. `security-rbac-guardrails`
Role-Based Access Control (RBAC) and data protection.
* **Server-Side Validation**: Mandatory `Check<Role>Role(UserId: GetUserId())` at the start of all public server actions.
* **SQL Injection Prevention**: Safe parameter binding (`@Param` with `Expand Inline = No`) and whitelisting dynamic clauses.
* **Data Masking**: Prevents logging sensitive tokens, passwords, or PII in `LogMessage` and exception logs.

### 📱 7. `cordova-plugin-wrapper`
Patterns for wrapping Cordova and Capacitor native plugins in OutSystems mobile modules.
* **Extensibility Configurations**: JSON structure with pinned repository release tags and preferences.
* **Plugin Availability Checks**: Graceful fallback and error reporting if running on desktop browser preview.
* **Asynchronous Callbacks**: Strict usage of `$resolve()` and `$reject()` in JavaScript nodes to prevent UI freezes.

### 💅 8. `css-format`
Formatting standards for OutSystems StyleSheets (themes, screens, and blocks).
* 4-space indentation, one declaration per line, opening brace on selector line.
* Full stylesheet replacement instead of appending competing one-line overrides.

---

## 🛠️ Repository Structure

```text
outsystems-skills/
├── README.md
├── .gitignore
└── skills/
    ├── outsystems-flow-layout/
    │   ├── SKILL.md
    │   ├── check-flow.cs
    │   └── polish-flow.cs
    ├── architecture-canvas-4lc/
    │   └── SKILL.md
    ├── crud-wrapper-pattern/
    │   └── SKILL.md
    ├── performance-anti-patterns/
    │   └── SKILL.md
    ├── outsystems-naming-conventions/
    │   └── SKILL.md
    ├── security-rbac-guardrails/
    │   └── SKILL.md
    ├── cordova-plugin-wrapper/
    │   └── SKILL.md
    └── css-format/
        └── SKILL.md
```

---

## 💻 How to Use with Any AI Model or Agent

These skills can be used with **any AI coding assistant, LLM, or agentic IDE** (Cursor, Claude, Copilot, ChatGPT, Antigravity, or custom agents):

### Option A: Use as System Instructions / Agent Skills
Include the instructions from any of the `skills/<skill-name>/SKILL.md` files into your AI environment:
* **Cursor / Windsurf**: Add to `.cursorrules` or `.windsurfrules`.
* **GitHub Copilot**: Add to `.github/copilot-instructions.md`.
* **Claude / ChatGPT**: Add as Project Knowledge, Custom Instructions, or directly into system prompts.
* **Agentic Workspaces**: Mount the `skills/` directory into your agent's skills path (`.agents/skills/` or global config).

### Option B: Direct Execution with OutSystems ModelAPI / MCP
For ModelAPI C# scripts (e.g. in `outsystems-flow-layout`):
1. Paste [`polish-flow.cs`](file:///skills/outsystems-flow-layout/polish-flow.cs) into `applyModelApiCode` (customize the `Spec` dictionary with action names and labels), then run `omlMerge`.
2. Paste [`check-flow.cs`](file:///skills/outsystems-flow-layout/check-flow.cs) into `applyModelApiCode` to get an instant health diagnostic.

---

## 📄 License

MIT License. Feel free to use, modify, and share!
