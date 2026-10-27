# OutSystems 11 AI Skills (`outsystems-ai-skills`)

[![GitHub repo](https://img.shields.io/badge/GitHub-MiguelRosaDev%2Foutsystems--ai--skills-blue.svg)](https://github.com/MiguelRosaDev/outsystems-ai-skills)
[![OutSystems 11](https://img.shields.io/badge/OutSystems%2011-ModelAPI%20%2F%20MCP-red.svg)](https://www.outsystems.com/)
[![AI Skills](https://img.shields.io/badge/AI%20Skills-Agentic%20Workflows-green.svg)](https://github.com/MiguelRosaDev/outsystems-ai-skills)

A comprehensive collection of **specialized AI skills, software engineering guardrails, and automated workflows** for AI models and coding assistants (Claude, Cursor, Copilot, ChatGPT, or custom MCP agents) interacting with **OutSystems 11 (O11)** via the **ModelAPI** and MCP servers (such as `os11-oml-transformer`).

---

## 🚀 Why This Repository Exists

When AI coding models generate or modify **OutSystems 11** applications through ModelAPI / MCP tools without specialized software engineering instructions, they introduce severe architectural, operational, and visual problems:
* **Security & Privacy Breaches**: Models expose customer PII (NIF, Email, IBAN) and tokens in `/screenservices` REST payloads, leak sensitive data into URLs and logs, omit server-side RBAC checks, and introduce XSS vulnerabilities through unescaped content.
* **Concurrency & Race Conditions**: Multi-user edit flows overwrite each other (lost updates), payment submissions duplicate on double-click, and counter logic breaks under parallel load.
* **Architecture & Data Debt**: Models create circular dependencies, violate 4-Layer Canvas (4LC) standards, misuse foreign key `Delete Rules`, place `Binary Data` directly inside operational tables, and leave tables growing indefinitely without purge strategies.
* **Runtime & Database Bottlenecks**: Models place database aggregates inside `ForEach` loops ($N+1$ queries), make multiple sequential server calls inside Client Actions, cause infinite UI re-render loops in `OnRender`, or build monolithic timers that roll back after 20-minute timeouts.
* **Visual Knotting & Unlinked Comments**: Flow nodes generated via API sit stacked on top of each other at coordinate `(0,0)`, and comments either float unpinned or turn into giant multi-paragraph banners across the canvas.

This repository equips any AI model with **production-grade software engineering rules applied to OutSystems 11**, aligned with official OutSystems 11 documentation, OWASP, TrueChange, and AI Mentor Studio.

---

## 🧭 Skills Catalog

| Category | Skill | Purpose |
| :--- | :--- | :--- |
| **Master Review** | [`outsystems-code-review`](skills/outsystems-code-review/SKILL.md) | Central entry point and master 5-gate code review checklist (Security P0, Concurrency P1, Performance P2, Architecture P3, Hygiene P4) |
| **Security & Privacy** | [`security-rbac-guardrails`](skills/security-rbac-guardrails/SKILL.md) | Server-side `Check<Role>Role` validation, Advanced SQL injection prevention, and secure coding |
| **Security & Privacy** | [`reactive-data-action-security`](skills/reactive-data-action-security/SKILL.md) | Guardrails for Reactive Data Actions: prevents leaking secrets/tokens & customer PII over `/screenservices` JSON payloads |
| **Security & Privacy** | [`screen-input-and-url-security`](skills/screen-input-and-url-security/SKILL.md) | Screen Input & URL security: prevents parameter tampering, privilege escalation, and leaking PII/tokens in query strings |
| **Security & Privacy** | [`frontend-security-xss-csp`](skills/frontend-security-xss-csp/SKILL.md) | Front-end XSS prevention, unescaped Expressions, HTML sanitization, Content Security Policy (CSP), and safe file upload validation |
| **Security & Privacy** | [`log-security-and-pii-masking`](skills/log-security-and-pii-masking/SKILL.md) | GDPR/RGPD guardrails preventing customer PII (NIF, Email, Name, Phone/MSISDN, IBAN) & secrets in logs |
| **Security & Privacy** | [`mobile-offline-security`](skills/mobile-offline-security/SKILL.md) | Mobile offline storage encryption, Keychain/Keystore token storage, server-side sync filtering, and logout data wipe |
| **Architecture & Data** | [`architecture-canvas-4lc`](skills/architecture-canvas-4lc/SKILL.md) | 4-Layer Canvas (Orchestration, End-User, Core, Foundation), the 3 Golden Rules, and module suffixes |
| **Architecture & Data** | [`data-modeling-best-practices`](skills/data-modeling-best-practices/SKILL.md) | Foreign Key Delete Rules (`Protect`, `Delete`, `Ignore`), 1-to-1 Binary Data isolation, and Static Entities |
| **Architecture & Data** | [`crud-wrapper-pattern`](skills/crud-wrapper-pattern/SKILL.md) | Read-only entity exposure, audit attributes, and `<Entity>_Create/Update/Get/Delete` wrapper actions |
| **Architecture & Data** | [`concurrency-and-idempotency`](skills/concurrency-and-idempotency/SKILL.md) | Optimistic locking (`Version` attribute), double-click UI & idempotency keys, race-free counters, and safe queue claiming |
| **Architecture & Data** | [`advanced-sql-and-index-tuning`](skills/advanced-sql-and-index-tuning/SKILL.md) | High-performance Advanced SQL for **SQL Server (T-SQL)** & **Oracle (PL/SQL)**, SARGability, and Entity Index design |
| **Architecture & Data** | [`data-lifecycle-and-gdpr-retention`](skills/data-lifecycle-and-gdpr-retention/SKILL.md) | Data retention policies, soft delete vs hard delete, GDPR anonymization workflows, and batched table purge Timers |
| **Logic & Performance** | [`performance-anti-patterns`](skills/performance-anti-patterns/SKILL.md) | Prevents $N+1$ queries inside loops, enforces Aggregate fetch optimization, indexing, and forbids runtime Site Property writes |
| **Logic & Performance** | [`client-action-single-server-call`](skills/client-action-single-server-call/SKILL.md) | Enforces at most 1 Server Action call per Client Action by encapsulating multiple server calls into a single orchestrator Server Action |
| **Logic & Performance** | [`timers-and-bpt-patterns`](skills/timers-and-bpt-patterns/SKILL.md) | Heavy Timers (`WakeTimer` + soft timeout + batch `CommitTransaction` + kill-switch) & Light BPT vs Standard BPT |
| **Logic & Performance** | [`exception-handling-and-logging`](skills/exception-handling-and-logging/SKILL.md) | Exception hierarchy, `Abort Transaction` rollback rules for custom error tables, and structured logging |
| **Logic & Performance** | [`testing-strategy`](skills/testing-strategy/SKILL.md) | Test pyramid in OutSystems: testable Server Action design, BDD/unit testing, API contract verification, and bug fix tests |
| **Integrations & APIs** | [`integration-service-patterns`](skills/integration-service-patterns/SKILL.md) | `_IS` wrapper modules, canonical domain structures, `OnBeforeRequest`/`OnAfterResponse`, and timeouts |
| **Integrations & APIs** | [`exposed-api-design`](skills/exposed-api-design/SKILL.md) | REST API design standards: Basic vs Custom/token auth, versioning (`/v1/`), standard HTTP status codes, and lean error contracts |
| **Integrations & APIs** | [`configuration-and-secrets-management`](skills/configuration-and-secrets-management/SKILL.md) | 12-Factor config in OutSystems: Site Properties vs config Entities, secrets encryption, LifeTime environments, and kill-switches |
| **Visual & UI** | [`outsystems-flow-layout`](skills/outsystems-flow-layout/SKILL.md) | Grid layout engine (`polish-flow.cs`) & checker (`check-flow.cs`) to eliminate node overlap and crossing connectors |
| **Visual & UI** | [`outsystems-flow-comments`](skills/outsystems-flow-comments/SKILL.md) | Canvas comment positioning (1 column right on the same row), connector pinning (`next`/`pins`), header notes, and compact sizing |
| **Visual & UI** | [`css-format`](skills/css-format/SKILL.md) | 4-space multi-line CSS formatting and full-sheet replacement for OutSystems themes, screens, and blocks |
| **Visual & UI** | [`reactive-lifecycle-and-sync`](skills/reactive-lifecycle-and-sync/SKILL.md) | Screen/Block lifecycle rules (`OnInitialize`, `OnReady`, `OnRender`, `OnParametersChanged`) & Mobile Offline Sync patterns |
| **Visual & UI** | [`traditional-to-reactive-migration`](skills/traditional-to-reactive-migration/SKILL.md) | Step-by-step migration from Traditional Web to Reactive Web (`Preparation`, `Session`, `Screen Actions`, `Notify`, `Popup_Editor`) |
| **Visual & UI** | [`frontend-performance`](skills/frontend-performance/SKILL.md) | Browser and device performance: on-demand data fetching, list virtualization, image optimization, and debounce |
| **Visual & UI** | [`accessibility-wcag`](skills/accessibility-wcag/SKILL.md) | WCAG 2.1/2.2 AA accessibility: linked form labels, accessible names on icon buttons, contrast, keyboard navigation, and popups |
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
    ├── accessibility-wcag/
    │   └── SKILL.md
    ├── advanced-sql-and-index-tuning/
    │   └── SKILL.md
    ├── architecture-canvas-4lc/
    │   └── SKILL.md
    ├── client-action-single-server-call/
    │   └── SKILL.md
    ├── concurrency-and-idempotency/
    │   └── SKILL.md
    ├── configuration-and-secrets-management/
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
    ├── data-lifecycle-and-gdpr-retention/
    │   └── SKILL.md
    ├── data-modeling-best-practices/
    │   └── SKILL.md
    ├── documentation-and-code-hygiene/
    │   └── SKILL.md
    ├── exception-handling-and-logging/
    │   └── SKILL.md
    ├── exposed-api-design/
    │   └── SKILL.md
    ├── frontend-performance/
    │   └── SKILL.md
    ├── frontend-security-xss-csp/
    │   └── SKILL.md
    ├── integration-service-patterns/
    │   └── SKILL.md
    ├── log-security-and-pii-masking/
    │   └── SKILL.md
    ├── mobile-offline-security/
    │   └── SKILL.md
    ├── multilanguage-translations/
    │   └── SKILL.md
    ├── outsystems-code-review/
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
    ├── reactive-data-action-security/
    │   └── SKILL.md
    ├── reactive-lifecycle-and-sync/
    │   └── SKILL.md
    ├── screen-input-and-url-security/
    │   └── SKILL.md
    ├── security-rbac-guardrails/
    │   └── SKILL.md
    ├── testing-strategy/
    │   └── SKILL.md
    ├── timers-and-bpt-patterns/
    │   └── SKILL.md
    └── traditional-to-reactive-migration/
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

### Option B: Master Code Review Trigger
Instruct the AI assistant to perform an end-to-end audit:
> *"Perform an OutSystems 11 code review on this module using the `outsystems-code-review` skill across Gates P0 through P4."*

### Option C: Direct Execution with OutSystems ModelAPI / MCP
For visual flow layout and verification (`outsystems-flow-layout`):
1. **Format & Route Flow**: Paste [`polish-flow.cs`](skills/outsystems-flow-layout/polish-flow.cs) into `applyModelApiCode` (customize the `Spec` dictionary with your action name and node labels), then run `omlMerge`.
2. **Verify Flow Health**: Paste [`check-flow.cs`](skills/outsystems-flow-layout/check-flow.cs) into `applyModelApiCode` to verify 0 crossing connectors, 0 overlapping nodes, and 0 missing descriptions.

---

## 📄 License

MIT License. Feel free to use, modify, and share!
