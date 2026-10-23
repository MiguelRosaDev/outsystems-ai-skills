---
name: integration-service-patterns
description: Architecture and resilience patterns for OutSystems 11 REST and SOAP integrations, including _IS wrapper modules, canonical structure decoupling, OnBeforeRequest/OnAfterResponse callbacks, error handling, and credential security. Use whenever consuming or exposing external APIs.
---

# integration-service-patterns

External integrations (REST and SOAP APIs) are points of failure and contract volatility. In OutSystems 11, external APIs must never be called directly from UI screens or core domain modules without an abstraction boundary.

---

## 1. Isolate in Dedicated Integration Service (`_IS`) Modules

1. **Never Consume REST/SOAP Directly in End-User Modules**:
   * All consumed external APIs belong in a dedicated **`<System>_IS`** module in the Core layer (e.g., `Stripe_IS`, `SAP_IS`, `Salesforce_IS`).
2. **Never Expose Connector Structures Directly**:
   * OutSystems auto-generates structures for REST methods (e.g. `PostPaymentRequest`, `PostPaymentResponse`).
   * Keep those auto-generated structures `Public = No`.
   * Create clean, canonical **Domain Structures** (`ST_PaymentRequest`, `ST_PaymentResult`) with `Public = Yes`, and map between them inside a public Wrapper Server Action.
   * **Why?** When the external vendor renames a JSON field or versions their API, you only update the mapping inside `<System>_IS` without breaking or republishing any consumer modules.

---

## 2. Wrapper Action Pattern

Every consumed integration method must be wrapped in a public Server Action inside the `_IS` module:

```text
[Start]
   │
   ▼
[Validate Input Parameters]
   │
   ▼
[Assign: Map ST_DomainRequest ──► REST_VendorRequest]
   │
   ▼
[Call Consumed REST Method] ──(CommunicationException / AllExceptions)──► [Handle Error & Mask Logs]
   │
   ▼
[Assign: Map REST_VendorResponse ──► ST_DomainResponse, IsSuccess = True]
   │
   ▼
[End]
```

* Return a consistent contract (`IsSuccess`, `ErrorCode`, `ErrorMessage`, `ResponseData`) rather than letting raw HTTP exceptions crash the caller's UI.

---

## 3. Using `OnBeforeRequest` and `OnAfterResponse`

Leverage the platform callbacks on consumed REST APIs for cross-cutting concerns:

### `OnBeforeRequest`
* Inject dynamic authentication headers (`Authorization: Bearer ...`, `X-Api-Key`) read from secure Site Properties or token caches.
* Inject tracing/correlation headers (`X-Correlation-Id`) to trace requests across systems.

### `OnAfterResponse`
* Inspect `Response.StatusCode`:
  * Handle expected business status codes (e.g., `400 Bad Request`, `404 Not Found`, `422 Unprocessable Entity`, `429 Too Many Requests`) cleanly.
* **Sanitize Logs**: Before logging payloads or when raising custom errors, strip or mask sensitive fields (`Authorization` headers, `access_token`, `password`, `cvv`, `nif`/`ssn`) from `RequestText` and `ResponseText`.

---

## 4. Timeouts, Retries & Resilience

1. **Explicit Timeouts**:
   * Never leave external REST calls with infinite or excessive timeouts when called synchronously from a user screen. Set reasonable timeouts (e.g., 5–15 seconds for interactive calls; 30–60 seconds for background jobs).
2. **Idempotency Keys**:
   * For state-changing calls (payments, order creation), include an idempotency key so retries do not duplicate transactions.
3. **Environment Configuration**:
   * Configure the Base URL via **Service Center** (Integrations ➔ Consumed REST APIs ➔ Effective URL) rather than hardcoding URLs in expressions, ensuring smooth promotion across Dev, QA, and Production via LifeTime.

---

## 5. Exposing REST APIs (`_API` Modules)

When exposing REST endpoints to external consumers:
* Place exposed APIs in a dedicated **`<Domain>_API`** module.
* Set `Authentication` (`Custom` or `Basic` / OAuth) and validate credentials inside the **`OnAuthentication`** callback.
* Use explicit HTTP status codes via `SetStatusCode` (from `HTTPRequestHandler`)—return `201 Created`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found` appropriately instead of returning `200 OK` for errors.
