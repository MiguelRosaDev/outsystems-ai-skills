---
name: mobile-offline-security
description: Security standards for OutSystems 11 Mobile apps (MABS) with local storage and offline sync. Covers what may be stored in Local Storage Entities, encryption of the local database, secure storage for tokens, Client Variables on device, server-side filtering of synchronized data, logout and wipe, sync conflict and tampering protection, and device-level hardening (root/jailbreak detection, certificate pinning). Use whenever designing Local Storage, offline sync, authentication persistence, or releasing a mobile app.
---

# mobile-offline-security

A mobile device is an **untrusted, losable, and inspectable** environment. Local Storage Entities are persisted in a SQLite database on the device; Client Variables and cached data live in the app's WebView storage. On a lost, rooted/jailbroken, or backed-up device, anything stored unprotected can be read. Offline sync also sends data that the client can **modify before it returns** to the server.

---

## 1. Store the Minimum Locally

1. Sync to Local Storage **only the data the user needs offline**, with only the attributes the screens use (lightweight Local Entities, not copies of server Entities).
2. **Never store locally**: passwords, API keys, client secrets, full payment card data, or other customers' data.
3. Avoid storing sensitive personal data (NIF, IBAN, ID documents, health data) offline unless there is a clear business need; if needed, see §2.
4. Keep Client Variables on device for preferences only — never tokens or authorization flags (see `screen-input-and-url-security`).

---

## 2. Encrypt What Must Be Stored

1. When Local Storage holds personal or business-sensitive data, **encrypt the local database** (for example with the Ciphered Local Storage plugin from Forge), with the key protected by the platform keystore (Android Keystore / iOS Keychain).
2. Store tokens and small secrets in **secure storage** (Keychain/Keystore-backed plugin), never in Local Entities, Client Variables, or `localStorage`.
3. Exclude sensitive files from device/cloud backups when possible (Android `allowBackup` — see the hooks patterns in `cordova-plugin-builder`).

---

## 3. Synchronization Security

1. **The server decides what each user receives**: every sync Server Action filters by `GetUserId()` (and tenant/role) on the server. Never send a broad dataset and filter on the device.
2. **Never trust data coming back from the device**: the sync-up Server Action must re-validate permissions, ownership, and business rules for every record, exactly like any other API input (the device copy can be altered).
3. **Never accept prices, totals, statuses, or permissions computed on the device** as authoritative; recompute them on the server.
4. **Conflict handling**: send a `Version`/`UpdatedOn` with offline edits and detect conflicts on the server (see `concurrency-and-idempotency`); define the rule (server wins / user decides), never silently overwrite.
5. Make sync-up idempotent (client-generated GUID per offline operation) so retries on flaky networks do not duplicate records.
6. Apply sync performance rules from `reactive-lifecycle-and-sync` (delta sync by `UpdatedOn`, batches).

---

## 4. Session, Logout, and Wipe

1. On **logout**, delete Local Storage data, cached files, secure-storage tokens, and Client Variables tied to the user — the next user of a shared device must not see previous data.
2. When the server rejects the session (expired/revoked user), wipe local data before showing the login screen.
3. Define a maximum offline period after which the app requires online re-authentication.
4. Consider app-level lock (biometric/PIN) for apps with sensitive data, and hide sensitive screens in the app switcher snapshot.

---

## 5. Device and Transport Hardening

1. HTTPS only; consider **certificate pinning** for high-risk apps (Forge SSL pinning plugin) to prevent interception on compromised networks.
2. For apps handling sensitive data or money, add **root/jailbreak and tampering detection** (e.g. OutSystems AppShield or equivalent), with a defined response (block, limit features, alert).
3. Never ship debug/logging builds to stores; native and console logs must not contain personal data or tokens (see `log-security-and-pii-masking`).
4. Keep plugins updated and remove unused ones (each plugin adds permissions and attack surface) — see `cordova-plugin-wrapper`.

---

## 6. Pre-Release Checklist

- [ ] Only necessary, lean data is synced; no secrets or other users' data stored locally.
- [ ] Sensitive local data is encrypted; tokens are in secure storage.
- [ ] Sync-down filters by user on the server; sync-up re-validates every record and recomputes authoritative values.
- [ ] Offline edits carry a version for conflict detection and a GUID for idempotency.
- [ ] Logout and session revocation wipe local data, tokens, and Client Variables.
- [ ] Release build has no debug logs; pinning/root detection applied where risk requires it.
