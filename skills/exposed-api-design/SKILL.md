---
name: exposed-api-design
description: Design, security, and contract standards for REST APIs exposed by OutSystems 11 modules. Covers authentication (Basic vs Custom/OnAuthentication), authorization per record, versioning, HTTP status codes, error contracts without stack traces, pagination, input validation, payload limits, logging levels, and documentation (Swagger). Use whenever creating or changing an Exposed REST API, its methods, or the structures it returns.
---

# exposed-api-design

`integration-service-patterns` covers **consuming** external services. This skill covers APIs that an OutSystems module **exposes** to other systems (mobile apps, partners, other platforms). An exposed API is a public contract: once consumers depend on it, every breaking change, data leak, or vague error becomes a production incident on someone else's side.

---

## 1. Choose the Right Exposure Mechanism

| Consumer | Use | Why |
| :--- | :--- | :--- |
| Another OutSystems module in the same environment | **Service Actions** (or public Server Actions) | Strongly typed, no manual contract maintenance, no public HTTP surface. |
| External system, partner, non-OutSystems app | **Exposed REST API** | Standard HTTP/JSON contract. |
| Internal-only integration on the private network | Exposed REST API with **`Internal Access Only = Yes`** | Rejects requests from outside the internal network. |

Never expose a REST API "just in case". Every endpoint is attack surface.

---

## 2. Authentication & Authorization

1. **Never `Authentication = None`** for endpoints that return or change non-public data.
2. **`Basic`** is acceptable only over HTTPS for system-to-system integrations with dedicated technical users (never a real person's credentials).
3. **`Custom`** (implement the `OnAuthentication` callback) for tokens/JWT/API keys:
   - Read the credential from the request header (never from the query string — query strings are logged).
   - Validate signature, expiry, audience, and issuer; compare API keys in constant time against **hashed** stored keys.
   - On failure, return `401` and stop processing.
4. **Authorization per record**: authenticating the caller is not enough. Every method that receives an identifier must check that the caller is allowed to access **that** record (IDOR — see `reactive-data-action-security`).
5. Store API credentials and signing keys as secrets — see `configuration-and-secrets-management`.

---

## 3. Contract Design

1. **Versioning from day one**: expose `CustomerAPI_v1` (URL path `/v1/`). Breaking changes (renamed/removed fields, changed types, changed semantics) require a new version; keep the old one until consumers migrate.
2. **Dedicated contract Structures** (`ST_API_Customer_v1`): never return Entity records directly. This prevents leaking new columns automatically and decouples the database from the contract (see the Lean Projection pattern in `reactive-data-action-security`).
3. **Consistent naming**: resources as nouns (`/customers/{id}/orders`), HTTP verbs for actions (`GET` read, `POST` create, `PUT`/`PATCH` update, `DELETE` remove). `GET` must never change data.
4. **Pagination for every list**: `offset`/`limit` (or cursor) parameters with a **server-enforced maximum** (e.g. `limit <= 100`), and return the total or a next-page indicator.
5. **Dates in ISO 8601 UTC**, money as decimal with explicit currency, enums as stable string codes (not database Ids).
6. **Fill in descriptions** for the API, methods, parameters, and structure attributes: OutSystems generates the Swagger/OpenAPI document from them.

---

## 4. Status Codes and Error Contract

Use correct HTTP status codes (set them explicitly, e.g. with `SetStatusCode` from `HTTPRequestHandler`) and one error structure for every method:

| Situation | Status |
| :--- | :--- |
| Success with body / created | `200` / `201` |
| Invalid input (validation failed) | `400` (with field-level details) |
| Missing/invalid credentials | `401` |
| Authenticated but not allowed | `403` (or `404` to avoid revealing existence) |
| Resource not found | `404` |
| Conflict (duplicate, version mismatch) | `409` |
| Too many requests | `429` |
| Unexpected failure | `500` |

```json
{ "errorCode": "CUSTOMER_NOT_FOUND", "message": "Customer not found.", "correlationId": "7f3c..." }
```

* **Never return exception messages, stack traces, SQL errors, or internal names** to the consumer. Catch exceptions in the method, log the details server-side with the `correlationId`, and return the generic error contract.

---

## 5. Input Validation and Abuse Protection

1. Validate every input on the server: required fields, length, format, ranges, allowed enum values. Reject unknown or invalid values with `400`.
2. Limit payload sizes (lists, text, binary) and the maximum page size.
3. **Rate limiting** is not built into OutSystems: place public APIs behind an API gateway / WAF that enforces quotas and throttling.
4. Make create operations idempotent when consumers may retry (accept an `Idempotency-Key` header — see `concurrency-and-idempotency`).

---

## 6. Logging and Observability

* Use the API **logging level** in Service Center deliberately: `Full` request/response logging in production stores payloads (including PII and credentials) in the integration logs. Prefer the default level and log only identifiers and status codes (see `log-security-and-pii-masking`).
* Use `OnRequest`/`OnResponse` callbacks to attach a `correlationId` and to strip sensitive headers from any custom log.

---

## 7. Pre-Commit Checklist

- [ ] Authentication is `Basic` (technical users, HTTPS) or `Custom` with validated tokens — never `None` for private data.
- [ ] Every method that takes an Id checks the caller's access to that record.
- [ ] The API is versioned and returns dedicated contract Structures, not Entities.
- [ ] Correct status codes and a single error contract; no stack traces or internal messages leak.
- [ ] Lists are paginated with a server-side maximum; inputs validated.
- [ ] Production logging level does not store sensitive payloads.
