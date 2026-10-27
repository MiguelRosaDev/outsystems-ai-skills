---
name: testing-strategy
description: Testing strategy for OutSystems 11 applications applying the test pyramid. Covers designing logic for testability, unit/BDD tests of Server Actions (BDDFramework, Unit Testing Framework), API tests for exposed/consumed REST, UI end-to-end tests for critical journeys only, safe test data without real PII, regression tests for every bug fix, and what must pass before deploying. Use whenever implementing business logic, fixing bugs, or preparing a release.
---

# testing-strategy

Low-code does not mean low-risk. Business rules in Server Actions break silently when someone changes an Aggregate filter, a Static Entity value, or a shared Core action used by ten modules. Without automated tests, every deployment depends on manual clicking.

Apply the **test pyramid**: many fast tests on business logic, fewer API tests, very few UI end-to-end tests.

```text
          /  UI E2E  \        few: critical journeys only (login, checkout, submit)
         /  API tests  \      exposed and consumed REST contracts
        / Logic tests    \    many: Server Actions / business rules (BDD / unit)
```

---

## 1. Design Logic for Testability

Tests are only cheap when the logic is testable:
1. **Business rules in Server Actions with explicit inputs/outputs** (in `_BL`/`_CS` modules) — not inside Screen/Client Actions, where they cannot be tested without a browser.
2. **Pure calculation actions** (pricing, eligibility, validation, date rules) receive everything as inputs and do not read the database, the clock, or the session directly. Pass `CurrentDateTime`, `UserId`, or configuration as parameters from an outer orchestrator action.
3. **Isolate integrations** behind `_IS` wrappers (see `integration-service-patterns`) so logic can be tested with a stub/fake response, and so a test never calls a real external system.
4. Return **structured results** (`IsSuccess`, `ErrorCode`) for business validation failures so tests assert codes, not translated messages.

---

## 2. Logic Tests (Base of the Pyramid)

* Use a dedicated **test module** per tested module (e.g. `Order_BL_Tests`) — never put test code in production modules — with a testing framework from Forge such as **BDDFramework** (Given/When/Then scenarios) or the **Unit Testing Framework**.
* For each business rule, cover: the happy path, each boundary value (limits, empty, zero, max length), each validation failure, and each role/permission variant.
* Tests must be **independent and repeatable**: create their own data in *Given*, and clean it up (or use unique identifiers) so they can run in any order and many times.
* Name scenarios by behavior: `Given an expired contract, When renewing, Then ErrorCode = CONTRACT_EXPIRED`.

---

## 3. API Tests (Middle of the Pyramid)

* For each **exposed REST API** (see `exposed-api-design`), automate: authentication failure (`401`), forbidden record (`403/404`), validation error (`400`), success contract shape, and pagination limits.
* For each **consumed integration**, test the `_IS` mapping against recorded sample responses (including error responses and timeouts).
* Run API tests with a tool that fits your pipeline (Postman/Newman collections, or an equivalent HTTP test runner).

---

## 4. UI End-to-End Tests (Top of the Pyramid)

* Automate only the **critical journeys** that would stop the business if broken (login, main transaction, payment/submit).
* Use stable selectors: set explicit `Name`/`id` or `data-testid` attributes on key widgets instead of relying on generated ids or CSS classes.
* Keep them few: UI tests are slow and brittle; if a rule can be tested at the logic level, test it there.

---

## 5. Test Data Rules

1. **Never use real customer data** (production copies) in DEV/QA without anonymization — this is a GDPR/RGPD violation (see `data-lifecycle-and-gdpr-retention`).
2. Use synthetic but realistic data: valid-format fake NIFs, `@example.com` emails, reserved test phone ranges.
3. Seed reference data with idempotent bootstrap logic so every environment starts consistent.
4. Never point tests at production integrations or production credentials.

---

## 6. Bug Fix Rule

**Every bug fix includes a test that fails before the fix and passes after it.** This prevents the same bug from returning in the next refactor and documents the expected behavior.

---

## 7. Before Deploying

- [ ] All logic tests of changed modules and of their **consumers** pass (use `findElementUsages` / dependency analysis to find impacted modules).
- [ ] API contract tests pass for any changed exposed/consumed API.
- [ ] Critical-journey UI tests pass in QA.
- [ ] No TrueChange errors or warnings (see `documentation-and-code-hygiene`).
- [ ] New business rules and bug fixes came with tests.
