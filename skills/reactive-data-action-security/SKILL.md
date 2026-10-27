---
name: reactive-data-action-security
description: Security and privacy guardrails for Reactive Web and Mobile Data Actions and Screen Aggregates. Prevents leaking secrets (API keys, tokens, credentials, password hashes) and unintended customer PII (NIF, email, phone, IBAN, addresses) over the network via /screenservices JSON payloads. Enforces lean projection structures, PII confirmation alerts, server-side data masking, and IDOR prevention.
---

# reactive-data-action-security

In **OutSystems 11 (O11) Reactive Web and Mobile**, every **Screen Aggregate** and **Data Action** runs on the server and serializes its output to the browser/mobile client over HTTP as a **JSON REST endpoint** (`/screenservices/<Module>/<Screen>/DataAction<ActionName>`).

### The Critical Vulnerability (Overfetching & Client Exposure):
Any attribute returned in a Data Action Output Parameter or Aggregate record is **100% visible in plain text in browser DevTools** (Network tab and React state in browser memory).
* **UI widgets do NOT filter the network payload**: If an Aggregate or Data Action fetches a full `{User}` or `{Customer}` record, but the screen only displays `Customer.Name`, **the entire record (including `NIF`, `Phone`, `Email`, `PasswordHash`, `SessionToken`) is transmitted in plain text across the network**.
* This is classified under **OWASP API Security Top 10** as **Broken Object Property Level Authorization (BOPLA / Overfetching)**.

---

## 1. Zero-Tolerance List (Never Transmit to Client)

The following must **NEVER** be returned in Data Action outputs, Screen Aggregates, or Client Variables:

| Category | Forbidden Attributes & Payloads |
| :--- | :--- |
| **Authentication & Credentials** | `Password`, `PasswordHash`, `PasswordSalt`, PINs, Security question answers |
| **Integration Secrets** | API Keys (`X-API-Key`), OAuth `client_secret`, Private Keys, Webhook Secrets, Cloud Service Credentials |
| **Session & Authorization Tokens** | Internal Bearer Tokens, Admin Session IDs, Refresh Tokens, Backend Service Tokens |
| **Payment Details** | Full Credit/Debit Card PAN, CVV/CVC, Payment Gateway Secret Keys |

> **Backend Isolation Rule**: If an external REST/SOAP API requires a secret API key or Bearer token, that call **must execute entirely on the server** (inside a Server Action or Service Action). The secret key must never travel to or from the client.

---

## 2. Customer PII Guardrail & Mandatory AI Alerting

Customer **Personally Identifiable Information (PII)** is protected by GDPR/RGPD and corporate privacy policies.

### The PII Field List:
* **Tax Identification Number (NIF / Contribuinte / SSN / VAT)**
* **Identity Documents (Citizen Card, BI, Passport Number)**
* **Financial Details (IBAN, Bank Account Numbers)**
* **Contact Information (Personal Email, Mobile/Phone Number)**
* **Personal Addresses (Home Address, Billing Address, Postal Code)**
* **Sensitive Personal Data (Date of Birth, Salary, Health / Medical status)**

### Mandatory AI Alerting Protocol:
Whenever an AI model designs, writes, or reviews a Data Action or Aggregate that outputs any customer PII to the client, the AI **must explicitly alert the developer**:

> ⚠️ **Privacy & Security Alert (PII Exposure)**:
> This Data Action outputs customer personal data (`[NIF, Email, Phone, IBAN, etc.]`) to the browser client via `/screenservices`.
> - **Verify Need**: Does the user interface truly require these fields on this screen?
> - **Lean Projection**: Avoid returning full Entity records; use a lean output structure with only the attributes rendered on the screen.
> - **Server-Side Masking**: If fields are for identification/display only, mask them on the server (`*******89`) before assigning to the output.

---

## 3. The Lean Projection Pattern (Anti-Overfetching)

Never return a full Entity record or a broad structure when only 2–3 attributes are needed by the screen widgets.

### ❌ Anti-Pattern (Returning Full Entity Records):
```text
[Data Action: GetCustomerDetails]
  Output Parameter: CustomerRecord (Type: Customer Record)
```
* **Vulnerability**: Every column in the `Customer` database table (e.g. `NIF`, `MobilePhone`, `IBAN`, `InternalNotes`, `CreditLimit`) is serialized into JSON and sent to the browser, even if the screen only displays `Customer.Name`.

### ✅ Best Practice (Dedicated Lean Output Structure):
1. Create a dedicated output Structure specific to the screen's requirements:
   - `ST_Customer_CardView` containing only:
     - `CustomerId` (`Customer Identifier`)
     - `DisplayName` (`Text`)
     - `StatusLabel` (`Text`)
2. In the Data Action, map **only** those specific attributes to the output:
   ```text
   [Data Action: GetCustomerCardDetails]
     Output Parameter: CustomerCard (Type: ST_Customer_CardView)
   ```
* **Result**: Zero excess columns leave the server. Network payloads are minimal, fast, and secure.

---

## 4. Server-Side Data Masking for Display Fields

When sensitive customer data (like NIF, Phone, IBAN, or Email) must be displayed on a screen for user verification or profile preview, **apply the mask on the server inside the Data Action before assigning to the output parameter**:

| Field | Server-Side Masking Formula in Data Action | Network Payload (Safe) |
| :--- | :--- | :--- |
| **NIF** | `If(Length(NIF) > 2, "*******" + Substr(NIF, Length(NIF)-2, 2), "[REDACTED]")` | `"*******89"` |
| **Phone** | `If(Length(Phone) > 3, "******" + Substr(Phone, Length(Phone)-3, 3), "[REDACTED]")` | `"******789"` |
| **IBAN** | `If(Length(IBAN) > 4, "********************" + Substr(IBAN, Length(IBAN)-4, 4), "[REDACTED]")` | `"********************1234"` |
| **Email** | `Substr(Email, 0, 1) + "***" + Substr(Email, Index(Email, "@"), Length(Email) - Index(Email, "@"))` | `"m***@domain.com"` |

* **Why Server-Side Masking is Critical**: If you send the plain-text NIF in the Data Action and mask it using a client-side expression or CSS in the browser, **the raw NIF is still fully exposed in the DevTools Network JSON response**. Server-side masking guarantees the real PII never leaves the server.

---

## 5. IDOR (Insecure Direct Object Reference) Prevention

Data Actions often receive an input parameter identifying a record (e.g. `CustomerId`, `OrderId`, `DocumentId`).

### ❌ The Vulnerability:
A malicious user modifies the `CustomerId` parameter in browser network requests to view another customer's data:
```sql
-- Vulnerable Aggregate / SQL in Data Action:
WHERE Customer.Id = @CustomerId
```

### ✅ The Fix (Mandatory Server-Side Ownership / Permission Check):
Every Data Action taking an identifier must validate that the currently authenticated user (`GetUserId()`) has legitimate access to that specific record:

```text
[Start]
   │
   ▼
[Check: Customer.UserId = GetUserId() or CheckAdminRole(GetUserId())]
   ├── True  ──► [Proceed to fetch lean customer data]
   │
   └── False ──► [Raise SecurityException / Abort]
```

Never rely on the client to pass the "correct" user ID. Resolve the user context on the server via `GetUserId()`.

---

## 6. Pre-Commit Checklist for Reactive Data Actions

Before finalizing any Reactive or Mobile Data Action:
- [ ] **Zero Secrets Check**: Does any output parameter contain an API key, token, credential, password hash, or private key? (Must be 100% NO).
- [ ] **No Full Entity Records**: Are all output parameters using lean custom structures (`ST_*`) containing only the fields displayed on the screen?
- [ ] **PII Review & Alerting**: If NIF, Email, Phone, IBAN, or Address are included, has the developer confirmed the business need?
- [ ] **Server-Side Masking**: Are sensitive display-only identifiers masked on the server before leaving the Data Action?
- [ ] **IDOR / Ownership Check**: Does the Data Action verify that `GetUserId()` is authorized to view the requested record ID?
