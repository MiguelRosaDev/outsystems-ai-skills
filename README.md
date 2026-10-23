# OutSystems 11 AI Skills (`outsystems-ai-skills`)

[![GitHub repo](https://img.shields.io/badge/GitHub-MiguelRosaDev%2Foutsystems--ai--skills-blue.svg)](https://github.com/MiguelRosaDev/outsystems-ai-skills)
[![OutSystems 11](https://img.shields.io/badge/OutSystems%2011-ModelAPI%20%2F%20MCP-red.svg)](https://www.outsystems.com/)
[![AI Skills](https://img.shields.io/badge/AI%20Skills-Agentic%20Workflows-green.svg)](https://github.com/MiguelRosaDev/outsystems-ai-skills)

A comprehensive collection of **specialized AI skills, architectural guardrails, and automated workflows** for AI models and coding assistants (Claude, Cursor, Copilot, ChatGPT, or custom MCP agents) interacting with **OutSystems 11 (O11)** via the **ModelAPI** and MCP servers (such as `os11-oml-transformer`).

---

## 🚀 Why This Repository Exists

When AI coding models generate or modify **OutSystems 11** applications through ModelAPI / MCP tools without specialized instructions, they introduce severe architectural and visual problems:
* **Visual Knotting & Unlinked Comments**: Flow nodes generated via API sit stacked on top of each other at coordinate `(0,0)`, and comments either float unpinned or turn into giant multi-paragraph banners across the canvas.
* **Architecture & Data Debt**: Models create circular dependencies, violate 4-Layer Canvas (4LC) standards, misuse foreign key `Delete Rules`, or place `Binary Data` directly inside operational tables.
* **Runtime & Database Bottlenecks**: Models place database aggregates inside `ForEach` loops ($N+1$ queries), cause infinite UI re-render loops in `OnRender`, or build monolithic timers that roll back after 20-minute timeouts.
* **Security & Encapsulation Flaws**: Entities are exposed with public write access instead of read-only CRUD wrappers, and server actions omit server-side `Check<Role>Role` checks.

This repository equips any AI model with **production-grade OutSystems 11 engineering rules** aligned with official OutSystems 11 documentation, TrueChange, and AI Mentor Studio.

---

## 🧭 Skills Catalog

| Category | Skill | Purpose |
| :--- | :--- | :--- |
| **Visual & UI** | [`outsystems-flow-layout`](skills/outsystems-flow-layout/SKILL.md) | Grid layout engine (`polish-flow.cs`) & checker (`check-flow.cs`) to eliminate node overlap and crossing connectors |
| **Visual & UI** | [`outsystems-flow-comments`](skills/outsystems-flow-comments/SKILL.md) | Canvas comment positioning (1 column right on the same row), connector pinning (`next`/`pins`), header notes, and compact sizing |
| **Visual & UI** | [`css-format`](skills/css-format/SKILL.md) | 4-space multi-line CSS formatting and full-sheet replacement for OutSystems themes, screens, and blocks |
| **Visual & UI** | [`reactive-lifecycle-and-sync`](skills/reactive-lifecycle-and-sync/SKILL.md) | Screen/Block lifecycle rules (`OnInitialize`, `OnReady`, `OnRender`, `OnParametersChanged`) & Mobile Offline Sync patterns |
| **Architecture & Data** | [`architecture-canvas-4lc`](skills/architecture-canvas-4lc/SKILL.md) | 4-Layer Canvas (Orchestration, End-User, Core, Foundation), the 3 Golden Rules, and module suffixes |
| **Architecture & Data** | [`data-modeling-best-practices`](skills/data-modeling-best-practices/SKILL.md) | Foreign Key Delete Rules (`Protect`, `Delete`, `Ignore`), 1-to-1 Binary Data isolation, and Static Entities |
| **Architecture & Data** | [`crud-wrapper-pattern`](skills/crud-wrapper-pattern/SKILL.md) | Read-only entity exposure, audit attributes, and `<Entity>_Create/Update/Get/Delete` wrapper actions |
| **Logic & Performance** | [`performance-anti-patterns`](skills/performance-anti-patterns/SKILL.md) | Prevents $N+1$ queries inside loops, enforces Aggregate fetch optimization, indexing, and forbids runtime Site Property writes |
| **Logic & Performance** | [`timers-and-bpt-patterns`](skills/timers-and-bpt-patterns/SKILL.md) | Heavy Timers (`WakeTimer` + soft timeout + batch `CommitTransaction` + kill-switch) & Light BPT vs Standard BPT |
| **Logic & Performance** | [`exception-handling-and-logging`](skills/exception-handling-and-logging/SKILL.md) | Exception hierarchy, `Abort Transaction` rollback rules for custom error tables, and structured logging |
| **Integrations & Security** | [`integration-service-patterns`](skills/integration-service-patterns/SKILL.md) | `_IS` wrapper modules, canonical domain structures, `OnBeforeRequest`/`OnAfterResponse`, and timeouts |
| **Integrations & Security** | [`security-rbac-guardrails`](skills/security-rbac-guardrails/SKILL.md) | Server-side `Check<Role>Role` validation, Advanced SQL injection prevention, and secure coding |
| **Integrations & Security** | [`log-security-and-pii-masking`](skills/log-security-and-pii-masking/SKILL.md) | GDPR/RGPD guardrails preventing customer PII (NIF, Email, Name, Phone/MSISDN, IBAN) & secrets in logs |
| **Mobile Plugins** | [`cordova-plugin-wrapper`](skills/cordova-plugin-wrapper/SKILL.md) | OutSystems mobile wrapper modules: Extensibility Configurations JSON, `$resolve()` JS nodes, and browser fallbacks |
| **Mobile Plugins** | [`cordova-plugin-builder`](skills/cordova-plugin-builder/SKILL.md) | Complete GitHub repo template (`templates/`) & patterns for MABS 12/13 (Kotlin + Swift, `<merges>` extensions, SPM/Pods, and hooks) |
| **Standards & i18n** | [`outsystems-naming-conventions`](skills/outsystems-naming-conventions/SKILL.md) | PascalCase `Verb + Noun` actions, `ST_` structures, `Is/Has/Can` booleans, and Logic folder organization |
| **Standards & i18n** | [`multilanguage-translations`](skills/multilanguage-translations/SKILL.md) | Parameterized translatable strings (`Replace`), `Translate` vs `DoNotTranslate`, and MCP translation workflows |
| **Standards & i18n** | [`documentation-and-code-hygiene`](skills/documentation-and-code-hygiene/SKILL.md) | 100% meaningful element description coverage (`coverage` / `set_descriptions`) and zero TrueChange warnings |

---

## 🛠️ Repository Structure

```text
outsystems-ai-skills/
├── README.md
├── .gitignore
└── skills/
    ├── architecture-canvas-4lc/
    │   └── SKILL.md
    ├── cordova-plugin-builder/
    │   ├── SKILL.md
    │   └── templates/
    │       ├── README.md
    │       ├── gitignore.template
    │       ├── package.json
    │       ├── plugin.xml
    │       ├── src/
    │       │   ├── android/PluginName.kt
    │       │   └── ios/PluginName.swift
    │       └── www/PluginName.js
    ├── cordova-plugin-wrapper/
    │   └── SKILL.md
    ├── crud-wrapper-pattern/
    │   └── SKILL.md
    ├── css-format/
    │   └── SKILL.md
    ├── data-modeling-best-practices/
    │   └── SKILL.md
    ├── documentation-and-code-hygiene/
    │   └── SKILL.md
    ├── exception-handling-and-logging/
    │   └── SKILL.md
    ├── integration-service-patterns/
    │   └── SKILL.md
    ├── log-security-and-pii-masking/
    │   └── SKILL.md
    ├── multilanguage-translations/
    │   └── SKILL.md
    ├── outsystems-flow-comments/
    │   └── SKILL.md
    ├── outsystems-flow-layout/
    │   ├── SKILL.md
    │   ├── check-flow.cs
    │   └── polish-flow.cs
    ├── outsystems-naming-conventions/
    │   └── SKILL.md
    ├── performance-anti-patterns/
    │   └── SKILL.md
    ├── reactive-lifecycle-and-sync/
    │   └── SKILL.md
    ├── security-rbac-guardrails/
    │   └── SKILL.md
    └── timers-and-bpt-patterns/
        └── SKILL.md
```

---

## 💻 How to Use with Any AI Model or Agent

These skills are completely tool-agnostic and work with **any AI coding assistant, LLM, or agentic IDE**:

### Option A: Use as Agent Skills / Custom Instructions
Clone the repository and mount the `skills/` directory into your AI coding assistant:
```bash
git clone https://github.com/MiguelRosaDev/outsystems-ai-skills.git
```
* **Cursor / Windsurf**: Reference or include the `SKILL.md` files in `.cursor/rules` or `.windsurfrules`.
* **GitHub Copilot**: Reference in `.github/copilot-instructions.md`.
* **Claude / ChatGPT**: Upload the `skills/` markdown files to Project Knowledge / Custom GPT instructions.
* **Agentic IDEs & MCP Clients**: Copy `skills/*` into your workspace `.agents/skills/` or global skills directory.

### Option B: Direct Execution with OutSystems ModelAPI / MCP
For visual flow layout and verification (`outsystems-flow-layout`):
1. **Format & Route Flow**: Paste [`polish-flow.cs`](skills/outsystems-flow-layout/polish-flow.cs) into `applyModelApiCode` (customize the `Spec` dictionary with your action name and node labels), then run `omlMerge`.
2. **Verify Flow Health**: Paste [`check-flow.cs`](skills/outsystems-flow-layout/check-flow.cs) into `applyModelApiCode` to verify 0 crossing connectors, 0 overlapping nodes, and 0 missing descriptions.

---

## 📄 License

MIT License. Feel free to use, modify, and share!
