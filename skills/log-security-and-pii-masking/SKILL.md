---
name: log-security-and-pii-masking
description: Strict GDPR/RGPD and security guardrails for OutSystems 11 logging (LogMessage, Error Logs, REST/SOAP integration logs, and mobile console/native logs). Prevents leaking customer PII (NIF, Email, Name, Phone/MSISDN, IBAN, Address) and secrets (Tokens, Passwords, OTPs) into Service Center or custom log tables. Use whenever creating or reviewing LogMessage calls, Exception Handlers, REST OnBeforeRequest/OnAfterResponse callbacks, or custom audit logs.
---

# log-security-and-pii-masking

In **OutSystems 11 (O11)**, every call to the `LogMessage` system action, unhandled exception, or custom integration log writes to **Service Center** (`oslog_General`, `oslog_Error`, `oslog_Int_Detail`) or application log tables. Service Center logs are accessible to broad operations and support teams, are frequently exported to external SIEM/monitoring platforms (Splunk, Datadog, Elastic), and **cannot be easily purged per-customer** upon a GDPR/RGPD "Right to Erasure" request.

Therefore, **customer Personally Identifiable Information (PII) and authentication secrets must NEVER be written in plain text to logs**.

---

## 1. Strictly Forbidden Data in Logs (Zero-Tolerance List)

Never concatenate, serialize, or pass any of the following fields into `LogMessage`, custom log Entities, exception messages, or mobile `console.log` / `Log.d` / `NSLog` statements:

| Category | Forbidden Fields & Examples |
| :--- | :--- |
| **Personal Identity (PII)** | **NIF** (Tax Identification Number / *Número de Contribuinte*), Citizen Card / BI / Passport number, Full Name (`Name`, `FirstName`, `LastName`), Date of Birth, Home/Billing Address, Postal Code |
| **Contact & Financial Data** | **Email address** (`Email`), **Phone / Mobile Number** (`Phone`, `MSISDN`, `Telemovel`), **IBAN** / Bank Account Number, Credit/Debit Card numbers (PAN, CVV), MBWay / Multibanco reference details tied to identity |
| **Authentication & Secrets** | Passwords, PINs, **OTP / SMS verification codes**, `Authorization: Bearer ...` JWTs, `X-API-Key`, OAuth `client_secret`, Session IDs, Refresh Tokens, Firebase Attestation Tokens |
| **Telecom & Hardware IDs** | eSIM Activation Codes (`LPA:1$...`), ICCID, IMSI, IMEI, Customer Account PUK |

---

## 2. Golden Rule: Log Internal Surrogate IDs & Correlation IDs Instead

Support and debugging teams do not need a customer's NIF, Email, or Name in `oslog_General` to trace an error — they only need a **non-sensitive internal identifier** that can be looked up inside an authorized, access-controlled backoffice screen.

### ❌ Anti-Pattern (Leaking PII in `LogMessage`):
```text
Message: "Failed to activate subscription for customer " + Customer.Name +
         " (NIF: " + Customer.NIF + ", Email: " + Customer.Email +
         ", Phone: " + Customer.Phone + ")"
ModuleName: "Subscription_CS"
```

### ✅ Best Practice (Safe Surrogate & Correlation IDs):
```text
Message: "[Subscription_CS].[Subscription_Activate] CorrelationId=" + CorrelationId +
         " | CustomerId=" + CustomerId +
         " | SubscriptionId=" + SubscriptionId +
         " | Status=Failed | ReasonCode=" + ErrorCode
ModuleName: "Subscription_CS"
```

* Always log **internal database Primary Keys** (`CustomerId`, `UserId`, `AccountId`, `OrderId`), **Correlation/Request IDs** (`CorrelationId`), **HTTP Status Codes**, and **technical error codes**.
* Never log business identifiers that are also personal data (`NIF`, `Email`, `MSISDN`, `Username` when username is an email).

---

## 3. Never Use Raw `JSONSerialize` in Logs

One of the most common accidental PII leaks occurs when an AI model or developer serializes an entire input Structure or Entity record into JSON for debugging:

### ❌ Anti-Pattern (Blind Serialization Leak):
1. `JSONSerialize(CustomerRequest)` (where `CustomerRequest` contains `Name`, `NIF`, `Email`, `IBAN`).
2. `LogMessage(Message = "Request payload: " + JSONSerialize.JSON, ModuleName = "Customer_CS")`.
* **Why it fails**: Every customer attribute in that structure is permanently dumped into `oslog_General` in plain text.

### ✅ Best Practice:
* **Option A (Preferred)**: Log only discrete, non-PII diagnostic fields (`CustomerId`, `OperationType`, `Channel`, `DurationMs`).
* **Option B (Sanitized Structure)**: If a structured payload must be logged for audit/troubleshooting, map it to a dedicated sanitized logging structure or run a masking action **before** calling `JSONSerialize`, replacing sensitive fields with masked values or `"[REDACTED]"`.

---

## 4. Standard Masking Rules (When Partial Context Is Strictly Required)

If an operational requirement explicitly demands partial visibility in a custom audit trail, apply deterministic masking **in memory before logging**:

| Data Type | Masking Rule | Plain Text (Forbidden) | Masked Output (Allowed) |
| :--- | :--- | :--- | :--- |
| **NIF (Tax ID)** | Mask all except last 2 digits (or fully `[REDACTED]`) | `123456789` | `*******89` |
| **Email** | Keep first char of local-part + `@domain` | `maria.silva@exemplo.pt` | `m***@exemplo.pt` |
| **Phone / MSISDN** | Mask all except last 3 digits | `+351912345678` | `**********678` |
| **IBAN** | Mask all except last 4 digits | `PT50000201231234567890154` | `*********************0154` |
| **Full Name** | Omit completely (use `CustomerId`) | `Maria João Silva` | *(Do not log — use `CustomerId`)* |
| **OTP / Password / Token** | Full redaction (0 characters exposed) | `849201` / `Bearer eyJ...` | `[REDACTED]` |

### OutSystems Expression Examples for Masking:
* **Mask NIF / Phone (keep last 2 or 3 chars)**:
  ```text
  If(Length(Trim(NIF)) > 2,
      "*******" + Substr(Trim(NIF), Length(Trim(NIF)) - 2, 2),
     "[REDACTED]")
  ```
* **Mask Email**:
  ```text
  If(Index(Email, "@") > 1,
     Substr(Email, 0, 1) + "***" + Substr(Email, Index(Email, "@"), Length(Email) - Index(Email, "@")),
     "[REDACTED]")
  ```

---

## 5. REST / SOAP Integration Logging (`OnBeforeRequest` & `OnAfterResponse`)

In `_IS` Integration Service modules:
1. **Service Center Integration Log Level**: Never leave REST API logging set to `Full` in Production if the endpoint exchanges customer PII or credentials.
2. **Custom Integration Payload Logs**: If implementing custom payload logging inside `OnAfterResponse`:
   - Strip or replace `Authorization`, `Cookie`, `Set-Cookie`, and `X-API-Key` headers with `"[REDACTED]"`.
   - Scrub JSON keys matching `(?i)(nif|taxId|vatNumber|email|name|firstName|lastName|phone|msisdn|iban|password|pin|otp|token|secret)` before writing `RequestText` or `ResponseText` to any database table or `LogMessage`.

---

## 6. Mobile & Cordova Plugin Logs (`console.log`, `Log.d`, `NSLog`)

On mobile devices (OutSystems Mobile / Reactive & Cordova plugins):
1. **JavaScript Nodes**: Never leave `console.log($parameters.NIF)`, `console.log($parameters.Email)`, or `console.log(token)` in Client Actions. Device logs can be read via Safari Web Inspector or Chrome DevTools.
2. **Native Cordova Plugins (`Kotlin` & `Swift`)**:
   - In Android (`android.util.Log`) and iOS (`NSLog`), log action execution (`Log.d(TAG, "execute: action=$action")`) and boolean/status outcomes, **never** the raw customer arguments, OTP codes, authentication tokens, or eSIM activation codes.

---

## 7. Pre-Commit Log Security Checklist

Before finishing any OutSystems 11 action, timer, integration, or mobile plugin:
- [ ] Inspect every `LogMessage` node and custom error log call: does the `Message` expression contain `NIF`, `Email`, `Name`, `Phone`/`MSISDN`, `IBAN`, `Address`, `Password`, `OTP`, or `Token`?
- [ ] Ensure no `JSONSerialize` output of a customer Entity or Structure is wired directly into `LogMessage` or an error logging action.
- [ ] Replace any personal identifier in log messages with `CustomerId`, `UserId`, or `CorrelationId`.
- [ ] Verify that Exception Handlers (`AllExceptions`) do not echo raw user-submitted PII inside the logged error string.
