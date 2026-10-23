---
name: security-rbac-guardrails
description: Security standards and role-based access control (RBAC) guardrails for OutSystems applications. Use whenever exposing public server actions, creating screen actions, building APIs, or writing custom SQL.
---

# security-rbac-guardrails

Security cannot be treated as an afterthought in enterprise OutSystems applications. AI models must apply defensive security practices, role-based authorization, and injection prevention.

---

## 1. Mandatory Role Validation on Server Actions

### Golden Rule: Client-Side Role Checks Are Not Sufficient
Checking `Check<Role>Role` inside a Client Action or hiding a button on a screen with `If` is purely a UI convenience. A malicious user can trigger Server Actions directly via network requests or DevTools.

### Implementation
Every sensitive or public Server Action must validate permissions at the very start:

```text
[Start]
   │
   ▼
[If CheckAdminRole(UserId: GetUserId()) or CheckManagerRole(UserId: GetUserId())]
   ├── True  ──► [Proceed with Business Logic...]
   │
   └── False ──► [Raise Exception: NotRegisteredException or SecurityException]
```

* If unauthorized, **immediately** raise a `NotRegisteredException`, `SecurityException`, or return `IsSuccess = False`, `ErrorMessage = "Unauthorized action"`.
* Never execute queries or database updates before the role check completes.

---

## 2. Advanced SQL Injection Prevention

When writing queries in **Advanced SQL** nodes:

### Rules:
1. **Always Use Query Parameters**:
   * Bind variables using `@ParameterName` (e.g. `WHERE Customer.Id = @CustomerId`).
   * Parameters must have `Expand Inline = No` by default.
2. **When `Expand Inline = Yes` is Absolutely Necessary**:
   * Only use `Expand Inline = Yes` for dynamic `ORDER BY` clauses or dynamic `IN (@IdList)`.
   * **Never** concatenate unvalidated user text into an inline parameter.
   * Whitelist dynamic values in OutSystems logic before passing them to the query:
     ```text
     If (SortField = "Name" or SortField = "CreatedOn")
     Then: SortClause = SortField + " " + (If(IsAscending, "ASC", "DESC"))
     Else: SortClause = "Id ASC"  // Safe default fallback
     ```
3. **Use EncodeSql**: If dynamically assembling query fragments, pass text through `EncodeSql()` to escape dangerous characters.

---

## 3. Screen and Endpoint Authorization

* **Screen Roles**:
  * In the Screen properties, explicitly select the allowed Roles in the `Roles` property.
  * Avoid leaving screens with only `Registered` if they contain role-restricted administrative data.
* **REST / SOAP Endpoints**:
  * Set appropriate authentication (Basic, OAuth, Token-based, or Custom).
  * In the `OnRequest` callback of exposed REST APIs, validate the bearer token or API key before processing the body.

---

## 4. Sensitive Data Masking & Logs

* **Do Not Log Secrets**:
  * Never write passwords, credit card numbers, authorization tokens, or sensitive personal data (PII) to `LogMessage` or exception messages.
  * In `LogMessage`, mask sensitive fields (e.g. `Bearer ****`, `Card ending in 1234`).
* **Entity Attributes**:
  * For password or secret storage, store cryptographic hashes (salted SHA-256 or bcrypt) rather than plaintext.
