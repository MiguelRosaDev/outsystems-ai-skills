---
name: configuration-and-secrets-management
description: Configuration, environment, and secrets management standards for OutSystems 11 (12-Factor "Config" applied to OutSystems). Covers when to use Site Properties vs configuration Entities vs Effective URLs, keeping secrets out of the model and the browser, encryption at rest, per-environment values across LifeTime deployments, and feature toggles / kill switches. Use whenever adding configurable values, credentials, integration endpoints, or feature flags.
---

# configuration-and-secrets-management

Anything that changes between environments (DEV, QA, PRD) or that operations must change without a new deployment is **configuration**, and it must never be hard-coded in logic. Anything that grants access (passwords, API keys, client secrets, certificates, signing keys) is a **secret**, and it must never live in the model, the source export, the browser, or plain-text logs.

---

## 1. Where Each Kind of Value Belongs

| Value | Store in | Notes |
| :--- | :--- | :--- |
| Consumed REST/SOAP endpoint base URL | **Effective URL** of the consumed integration (Service Center, per environment) | Never hard-code environment hosts in logic or in `OnBeforeRequest`. |
| Simple, rarely changed technical setting (batch size, timeout, feature flag) | **Site Property** | Cached and fast to read. Values are kept per environment across deployments. **Never write Site Properties at runtime** — it invalidates the cache of every front-end (see `performance-anti-patterns`). |
| Business-editable configuration (thresholds, texts, lists of values, per-tenant rules) | **Configuration Entity** with a backoffice screen protected by a role, plus an audit trail | Business users change it without Service Center access. |
| Secrets (API keys, client secrets, passwords, certificates) | **Encrypted storage** (see §2) or an external vault | Never in Site Properties' default values, literals, JavaScript, or Client Variables. |
| Values per user session in the browser | **Client Variables** — UI preferences only | Client Variables live in browser storage and are fully readable by the user. |

---

## 2. Secrets Rules

1. **No secrets in the model**: no literals in Assigns, REST headers, `OnBeforeRequest`, JavaScript nodes, Scripts resources, or default values of Site Properties. Exported `.oml`/`.oap` files and solution packs would contain them, and they would be deployed to every environment.
2. **Encrypt at rest**: when a secret must be stored in OutSystems, store it encrypted (for example with a Forge cryptography component, AES with a key kept outside the database), or reference it from an external vault (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault) through a server-side integration.
3. **Restrict who can read and change** secrets: a dedicated role for the backoffice screen; never expose the decrypted value in any screen, Data Action, or log — show only "configured / last updated on".
4. **Server-side only**: secrets are used exclusively inside Server Actions (integration calls); they never travel to the browser or mobile app (see `reactive-data-action-security`).
5. **Different secrets per environment**: DEV/QA must never use production credentials.
6. **Rotation**: design so that a key can be replaced without a deployment (configuration value + cache refresh), and support two active keys during rotation when the provider allows it.
7. If a secret was ever committed, logged, or shown in a screen, treat it as **compromised and rotate it**.

---

## 3. Per-Environment Configuration and Deployments

1. List every Site Property, Effective URL, and configuration record that must be set per environment in the module/application description or the deployment notes.
2. After the first deployment to a new environment, set the values in Service Center **before** enabling Timers that depend on them.
3. Use safe defaults: a missing value must fail closed (feature disabled, explicit error), not silently call a DEV endpoint from production.
4. Bootstrap reference/configuration data with idempotent bootstrap logic (create if missing, never overwrite operator changes).

---

## 4. Feature Toggles and Kill Switches

1. **Release toggles**: ship unfinished features disabled behind a toggle, enable per environment or per user group, and **remove the toggle and the dead branch** once the feature is stable (toggles are technical debt).
2. **Kill switches**: every heavy Timer, external integration, and non-critical feature with operational risk should have a switch that operations can flip to stop it immediately without a deployment (see `timers-and-bpt-patterns`).
3. Read toggles on the **server** for anything security- or data-relevant; a toggle evaluated only in the browser can be bypassed.
4. Implement toggles with a Site Property (simple on/off), a configuration Entity (per user group/tenant), or a Forge feature-toggle component — consistently across the factory.

---

## 5. Pre-Commit Checklist

- [ ] No environment-specific URL, host, or credential hard-coded in logic.
- [ ] Integration endpoints configured through Effective URLs.
- [ ] No secret in literals, JavaScript, Client Variables, Site Property defaults, or logs; secrets encrypted at rest or in a vault.
- [ ] Site Properties are read-only at runtime.
- [ ] Required per-environment values are documented, and missing values fail closed.
- [ ] Risky Timers/integrations have a kill switch; finished feature toggles are removed.
