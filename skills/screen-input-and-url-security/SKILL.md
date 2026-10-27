---
name: screen-input-and-url-security
description: Security standards for OutSystems Screen Input Parameters and URL query strings. Prevents parameter tampering, privilege escalation, and leaking sensitive data (PII, tokens, authorization flags, pricing) in URLs, browser history, proxy logs, and HTTP Referer headers. Enforces IDOR validation and secure parameter passing.
---

# screen-input-and-url-security

In OutSystems (both **Reactive Web** and **Traditional Web**), **Screen Input Parameters** are mapped directly to URL query strings or path parameters (e.g., `https://example.com/App/Screen?Param1=Value1&Param2=Value2`).

### Why URL Parameters Are Inherently Insecure:
1. **Public & Visible**: Displayed on the user's screen in plain text (shoulder surfing).
2. **Logged & Leaked**: Stored in browser history, proxy logs, CDN/firewall logs, web server logs, and transmitted in `Referer` HTTP headers to external websites.
3. **Freely Modifiable (Tampering)**: Any user can alter URL parameters in the address bar or via automated web tools to manipulate application logic, view other users' records, or escalate privileges.

---

## 1. Strictly Forbidden in Screen Input Parameters / URLs

Never declare a Screen Input Parameter or pass in the URL any of the following:

| Category | Forbidden Parameters | Risk / Vulnerability |
| :--- | :--- | :--- |
| **Authorization & Roles** | `IsAdmin`, `CanEdit`, `Role`, `UserType`, `AccessLevel`, `IsManager` | **Privilege Escalation**: The user changes `?IsAdmin=False` to `?IsAdmin=True` in the browser URL. |
| **Financial & Pricing** | `Price`, `TotalAmount`, `Discount`, `Balance`, `IsPaid`, `InterestRate` | **Price Tampering**: The user alters `?Price=100` to `?Price=1` in the checkout URL. |
| **Authentication & Secrets** | `Password`, `Token`, `ApiKey`, `SessionId`, `OTP`, `SecretKey` | **Credential Theft**: Stored in browser history, proxy logs, and leaked in HTTP `Referer` headers. |
| **Customer PII** | `NIF`, `Email`, `MobilePhone`, `IBAN`, `Address`, `FullName`, `BirthDate` | **Privacy Leak (GDPR/RGPD)**: Personal data persists permanently in browser history, cache, and intermediate proxies. |
| **Internal Execution Flags** | `BypassValidation`, `SkipPayment`, `DebugMode`, `IgnoreErrors` | **Security Bypass**: Users enable debug or bypass paths by altering URL flags. |

---

## 2. Safe vs. Unsafe URL Parameters

| Parameter Type | Safe to Pass in URL? | Rule / Condition |
| :--- | :---: | :--- |
| **Record Identifier (`CustomerId`, `OrderId`, `ProductId`)** | **YES (with validation)** | Pass only the ID (never the full record data). The server **must validate authorization** on destination queries (IDOR check). |
| **Pagination State (`StartIndex`, `MaxRecords`)** | **YES** | Safe for navigation. Always validate on the server that `MaxRecords` is within acceptable bounds (e.g. `<= 100`). |
| **Search & Filter Keywords (`SearchTerm`, `CategoryId`)** | **YES** | Safe for public search terms. Never pass personal data as search terms in the URL. |
| **UI Tab / View Mode (`ActiveTab`, `ViewMode = "Grid"`)** | **YES** | Safe purely for client-side layout preferences. |
| **Single-Use Crypto Token (`InvitationToken = <GUID>`)** | **YES (strictly single-use)** | Allowed for public registration/invite flows only if the token is a random GUID validated, authorized, and immediately invalidated on the server. |

---

## 3. The 3 Golden Rules for Screen Parameters

### Rule 1: Never Trust Client-Supplied Authorization
Always compute roles and permissions on the server inside the destination screen's Aggregates, Data Actions, or Server Actions.
* **❌ Anti-Pattern**: Screen receives Input Parameter `CanEdit (Boolean)`. UI conditionally shows the "Delete" button based on `CanEdit`, and the Save action checks `If CanEdit`.
* **✅ Best Practice**: Do not pass `CanEdit`. On the destination screen, evaluate `CheckCustomerAdminRole(GetUserId())` on the server to determine view and write permissions.

### Rule 2: Guard Against IDOR (Insecure Direct Object Reference)
Passing a record ID (e.g., `OrderId=1042`) is standard, but the server must verify ownership:
* **❌ Anti-Pattern**: Destination screen queries `WHERE Order.Id = OrderId` and immediately renders sensitive invoice data. Any logged-in user can change `OrderId=1042` to `OrderId=1043` to read another user's invoice.
* **✅ Best Practice**: Always validate in the Screen Aggregate / Data Action:
  ```text
  WHERE Order.Id = OrderId
    AND (Order.CustomerId = CurrentCustomer.Id OR CheckAdminRole(GetUserId()))
  ```
  If no record matches, abort and raise a `NotRegisteredException` or navigate to an Unauthorized screen.

### Rule 3: Use Server-Side Drafts for Multi-Step Wizards
When building multi-step forms (e.g., Step 1: Personal Info -> Step 2: Plan -> Step 3: Payment):
* **❌ Anti-Pattern**: Passing all collected data from Step 1 as Screen Input Parameters in the URL to Step 2 (`?Name=...&NIF=...&Email=...&PlanId=...`).
* **✅ Best Practice**:
  - Save Step 1 data to a server-side draft Entity (`OrderDraft`) tied to `GetUserId()` or a session-scoped `DraftId` (GUID).
  - Pass only the `DraftId` to Step 2, and fetch the draft securely on the server.
  - Compute all totals, prices, and discounts on the server at final confirmation.

---

## 4. Pre-Commit Checklist for Screen Parameters

Before adding or finalizing Screen Input Parameters in OutSystems:
- [ ] **No Authorization Flags**: Are parameters free from `IsAdmin`, `CanEdit`, `Role`, or similar privilege flags?
- [ ] **No Secrets or Credentials**: Are tokens, passwords, API keys, and OTPs absent from the parameter list?
- [ ] **No PII**: Are customer NIF, Email, Phone, IBAN, and Addresses excluded from the URL?
- [ ] **No Financial Values**: Are prices, discounts, and payments computed entirely on the server?
- [ ] **IDOR Protection**: Does the screen validate that `GetUserId()` owns or is authorized to view the passed record ID?
