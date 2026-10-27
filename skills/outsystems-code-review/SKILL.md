---
name: outsystems-code-review
description: Comprehensive meta-skill and master code review checklist for OutSystems 11 applications. Dispatches and chains specialized skills across Security (P0), Data Integrity & Concurrency (P1), Performance (P2), Architecture & API Design (P3), and Code Hygiene & Maintainability (P4). Use whenever reviewing an OutSystems 11 module, screen, action, API, or pull request.
---

# outsystems-code-review

This skill is the **central entry point** for reviewing any OutSystems 11 (O11) module, screen, action, API, or pull request. It organizes code review into five prioritized severity gates (P0 to P4) and points directly to the specialized skills that enforce each rule.

When asked to review an application, an eSpace, or a specific feature, evaluate the code through these gates **in order of severity**.

---

## 🚦 The 5-Gate Review Protocol

```text
[Start Review]
   │
   ├─► [Gate 0: Security & Privacy (P0 - Blocker)]
   │     RBAC, SQL Injection, Data Action leaks, URL Tampering, XSS, Secrets, PII in Logs
   │
   ├─► [Gate 1: Data Integrity & Concurrency (P1 - Blocker)]
   │     Optimistic locking, Idempotency, CRUD wrappers, Delete Rules, Transactions
   │
   ├─► [Gate 2: Performance & Scalability (P2 - Major)]
   │     Single Server Call, N+1 in loops, SARGable queries & Indexes, Timers, UI rendering
   │
   ├─► [Gate 3: Architecture & Contracts (P3 - Major)]
   │     4LC compliance, Screen lifecycles, Exposed/Consumed APIs, Data Retention & Purge
   │
   └─► [Gate 4: Hygiene, a11y & Testing (P4 - Minor/Maintainability)]
         Naming, 100% Descriptions, Flow layout/comments, WCAG, Unit tests
```

---

## Gate 0: Security & Privacy (P0 — Blocker)

*Any violation in this gate must block deployment to staging or production.*

1. **Server-Side RBAC**: Every public/sensitive Server Action validates roles with `Check<Role>Role(GetUserId())` at the start, never relying solely on client-side UI visibility.
   * *See skill*: [`security-rbac-guardrails`](../security-rbac-guardrails/SKILL.md)
2. **SQL Injection**: All Advanced SQL nodes use bind parameters (`Expand Inline = No`). Any unavoidable inline parameter is strictly sanitized or whitelisted.
   * *See skill*: [`security-rbac-guardrails`](../security-rbac-guardrails/SKILL.md), [`advanced-sql-and-index-tuning`](../advanced-sql-and-index-tuning/SKILL.md)
3. **Data Action Payload Security**: Data Actions and Screen Aggregates return lean custom structures (`ST_*`), never full Entity records or sensitive fields (`PasswordHash`, tokens, internal keys).
   * *See skill*: [`reactive-data-action-security`](../reactive-data-action-security/SKILL.md)
4. **URL & Screen Input Security**: Screen Input Parameters exclude roles (`IsAdmin`), prices/amounts, tokens, and PII. Record IDs are validated against `GetUserId()` (IDOR check).
   * *See skill*: [`screen-input-and-url-security`](../screen-input-and-url-security/SKILL.md)
5. **No PII or Secrets in Logs**: `LogMessage`, exception handlers, and mobile console logs exclude customer NIF, Email, Phone, IBAN, and credentials. Internal surrogate IDs and correlation IDs are used instead.
   * *See skill*: [`log-security-and-pii-masking`](../log-security-and-pii-masking/SKILL.md)
6. **XSS & Content Security**: No `Escape Content = No` on user data, no unsafe `innerHTML` in JS nodes, user HTML sanitized server-side, safe file upload validation on server.
   * *See skill*: [`frontend-security-xss-csp`](../frontend-security-xss-csp/SKILL.md)
7. **Secrets Management**: No API keys, credentials, or client secrets hard-coded in logic, JS, or Site Properties. Secrets encrypted at rest or stored in a vault.
   * *See skill*: [`configuration-and-secrets-management`](../configuration-and-secrets-management/SKILL.md)
8. **Mobile Offline Security**: Sensitive local storage encrypted; tokens in Keychain/Keystore; sync-down filtered on server; local data wiped on logout.
   * *See skill*: [`mobile-offline-security`](../mobile-offline-security/SKILL.md)

---

## Gate 1: Data Integrity & Concurrency (P1 — Blocker)

*Prevents silent data loss, duplicate financial records, and transaction deadlocks.*

1. **Concurrency & Lost Updates**: Multi-user edit flows verify a `Version` attribute using `Get<Entity>ForUpdate` to detect conflicts instead of blindly overwriting.
   * *See skill*: [`concurrency-and-idempotency`](../concurrency-and-idempotency/SKILL.md)
2. **Double-Click & Retry Idempotency**: Creation, payment, and submission flows use a disabled UI state plus a server-validated `IdempotencyKey` with a Unique Index.
   * *See skill*: [`concurrency-and-idempotency`](../concurrency-and-idempotency/SKILL.md)
3. **Sequential Numbers & Counters**: No `Max() + 1` queries. Counters use dedicated locked sequence records or database sequences.
   * *See skill*: [`concurrency-and-idempotency`](../concurrency-and-idempotency/SKILL.md), [`advanced-sql-and-index-tuning`](../advanced-sql-and-index-tuning/SKILL.md)
4. **CRUD Wrapper Encapsulation**: Entities set to `Public = Yes` have `Expose Read Only = Yes`. All writes occur via `<Entity>_Create/Update/Delete` wrapper actions managing audit attributes.
   * *See skill*: [`crud-wrapper-pattern`](../crud-wrapper-pattern/SKILL.md)
5. **Foreign Key Integrity & Binary Isolation**: Correct delete rules (`Protect`, `Delete`, `Ignore`) applied. `Binary Data` attributes isolated into dedicated 1-to-1 extension entities.
   * *See skill*: [`data-modeling-best-practices`](../data-modeling-best-practices/SKILL.md)
6. **Transaction Hygiene**: Unhandled exceptions trigger `Abort Transaction`. Custom audit log tables committed safely with separate transactions where appropriate.
   * *See skill*: [`exception-handling-and-logging`](../exception-handling-and-logging/SKILL.md)

---

## Gate 2: Performance & Scalability (P2 — Major)

*Eliminates bottlenecks, UI freezes, and runaway database load.*

1. **Single Server Call per Client Action**: Client Actions execute at most ONE (1) Server Action call per execution path. Multiple server operations consolidated into an Orchestrator Server Action.
   * *See skill*: [`client-action-single-server-call`](../client-action-single-server-call/SKILL.md)
2. **Loop Anti-Patterns**: Zero database Aggregates, SQL nodes, or Server Actions inside `ForEach` loops ($N+1$ killer).
   * *See skill*: [`performance-anti-patterns`](../performance-anti-patterns/SKILL.md)
3. **SARGable Queries & Indexes**: No wrapping indexed columns in functions (`YEAR()`, `UPPER()`, `ISNULL()`). ESR composite indexes validated for frequent filter/sort patterns.
   * *See skill*: [`advanced-sql-and-index-tuning`](../advanced-sql-and-index-tuning/SKILL.md)
4. **Front-End Data Fetching & Lists**: Data in tabs/popups set to `Fetch = Only on demand`. `Max Records` defined on all queries. No heavy blocks with internal fetches inside list items.
   * *See skill*: [`frontend-performance`](../frontend-performance/SKILL.md)
5. **Site Properties**: Read-only at runtime. Never modified dynamically via Assign statements.
   * *See skill*: [`performance-anti-patterns`](../performance-anti-patterns/SKILL.md), [`configuration-and-secrets-management`](../configuration-and-secrets-management/SKILL.md)
6. **Heavy Timers & BPT**: Batched processing (`MaxRecords = 50-100`), soft timeout checks (`DateTimeAdd(..., 18 mins)`), explicit `CommitTransaction`, and kill switches.
   * *See skill*: [`timers-and-bpt-patterns`](../timers-and-bpt-patterns/SKILL.md)

---

## Gate 3: Architecture & Contracts (P3 — Major)

*Ensures clean module boundaries and long-term maintainability.*

1. **Architecture Canvas (4LC)**: Strict compliance with Orchestration, End-User, Core, and Foundation layers. No upward or circular dependencies. Correct module suffixes applied.
   * *See skill*: [`architecture-canvas-4lc`](../architecture-canvas-4lc/SKILL.md)
2. **Reactive Lifecycle**: No state modifications in `OnRender`. DOM manipulations in `OnReady`. Clean event destruction in `OnDestroy`.
   * *See skill*: [`reactive-lifecycle-and-sync`](../reactive-lifecycle-and-sync/SKILL.md), [`frontend-performance`](../frontend-performance/SKILL.md)
3. **Exposed REST API Design**: Versioned endpoints (`/v1/`), explicit authentication/authorization, dedicated contract structures, standard HTTP status codes, lean error payloads.
   * *See skill*: [`exposed-api-design`](../exposed-api-design/SKILL.md)
4. **Consumed Integrations**: Wrapped inside dedicated `_IS` modules with canonical structures, custom timeouts, and error handling.
   * *See skill*: [`integration-service-patterns`](../integration-service-patterns/SKILL.md)
5. **Mobile Plugin Wrappers**: Safe `Extensibility Configurations`, defensive `$resolve()` calls in JS nodes, and proper Cordova architecture.
   * *See skill*: [`cordova-plugin-wrapper`](../cordova-plugin-wrapper/SKILL.md), [`cordova-plugin-builder`](../cordova-plugin-builder/SKILL.md)
6. **Data Lifecycle & Retention**: Documented retention periods. Batched purge/archive Timers for high-volume operational/log tables. Anonymization workflows for GDPR erasure.
   * *See skill*: [`data-lifecycle-and-gdpr-retention`](../data-lifecycle-and-gdpr-retention/SKILL.md)

---

## Gate 4: Code Hygiene, a11y & Testing (P4 — Quality & Maintainability)

*Ensures production durability, accessibility, and smooth pair development.*

1. **Naming Conventions**: PascalCase `Verb + Noun` actions, `ST_` structures, `Is/Has/Can` booleans, and standardized Logic folder hierarchies.
   * *See skill*: [`outsystems-naming-conventions`](../outsystems-naming-conventions/SKILL.md)
2. **Documentation & TrueChange**: 100% meaningful element description coverage (`coverage` tool). Zero TrueChange errors and warnings.
   * *See skill*: [`documentation-and-code-hygiene`](../documentation-and-code-hygiene/SKILL.md)
3. **Canvas Comments & Layout**: Canvas comments placed 1 column to the right on the same row, pinned with connectors (`next`/`pins`). Clean grid layout without crossing connectors.
   * *See skill*: [`outsystems-flow-comments`](../outsystems-flow-comments/SKILL.md), [`outsystems-flow-layout`](../outsystems-flow-layout/SKILL.md)
4. **Theme & CSS Formatting**: 4-space formatted CSS, stylesheet centralization in themes, no inline style overrides.
   * *See skill*: [`css-format`](../css-format/SKILL.md)
5. **Multilanguage & i18n**: No hardcoded text concatenations; parameterized messages via `Replace()`; explicit `Translate` vs `DoNotTranslate`.
   * *See skill*: [`multilanguage-translations`](../multilanguage-translations/SKILL.md)
6. **Accessibility (WCAG 2.1/2.2 AA)**: Form labels properly linked (`Input Widget`), `aria-label` on icon buttons, contrast ≥ 4.5:1, keyboard operability, and popup focus management.
   * *See skill*: [`accessibility-wcag`](../accessibility-wcag/SKILL.md)
7. **Automated Testing Strategy**: Business logic isolated in testable Server Actions; unit/BDD tests covering happy and boundary paths; tests for every bug fix.
   * *See skill*: [`testing-strategy`](../testing-strategy/SKILL.md)

---

## 📝 Review Output Template

When reporting code review findings to the team, use this structured format:

```markdown
### 📋 OutSystems Code Review Summary

- **Module / Feature**: `<ModuleName>`
- **Verdict**: [APPROVED | CHANGES REQUESTED | BLOCKED]

#### 🚨 Gate 0 & 1 Findings (Blockers - Security & Data Integrity)
- **[P0 - Security]** `<Finding title>`: `<Description, risk, and reference to skill>`
  - *Fix*: `<Concrete refactoring instruction>`

#### ⚠️ Gate 2 & 3 Findings (Major - Performance & Architecture)
- **[P2 - Performance]** `<Finding title>`: `<Description, impact, and reference to skill>`
  - *Fix*: `<Concrete refactoring instruction>`

#### 💡 Gate 4 Findings (Minor / Hygiene & Testing)
- **[P4 - Hygiene]** `<Finding title>`: `<Description>`

#### ✅ Passed Checks
- List of verified gates and rules that had zero findings.
```
