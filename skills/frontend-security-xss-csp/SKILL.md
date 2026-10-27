---
name: frontend-security-xss-csp
description: Front-end security guardrails for OutSystems 11 Reactive Web, Traditional Web, and Mobile. Prevents Cross-Site Scripting (XSS) through unescaped Expressions, raw HTML rendering, and unsafe JavaScript nodes; enforces Content Security Policy (CSP), safe file Upload validation, and safe external links. Use whenever rendering user- or database-supplied content, writing JavaScript nodes, handling file uploads, or configuring application security settings.
---

# frontend-security-xss-csp

OutSystems escapes most output by default, but **every escape hatch is an XSS vector**: an `Expression` with escaping disabled, HTML injected through a JavaScript node, a URL built from user input, or an uploaded file served back to other users. Cross-Site Scripting is part of **OWASP Top 10 A03 (Injection)** and lets an attacker run JavaScript in the victim's session: steal data shown on screen, call Server Actions as the victim, or redirect to phishing pages.

**Golden Rule**: Treat every value that originated outside your code (user input, database text, REST responses, URL parameters, file names) as **untrusted** until it is encoded for the exact context where it is rendered.

---

## 1. Rendering Text and HTML Safely

| Situation | ❌ Unsafe | ✅ Safe |
| :--- | :--- | :--- |
| Show user/DB text (Traditional Web) | `Expression` with `Escape Content = No` | Keep `Escape Content = Yes` (default). |
| Show user/DB text (Reactive/Mobile) | JavaScript node: `el.innerHTML = $parameters.Text` | Bind the value to an `Expression` widget (React escapes it), or use `el.textContent = ...` in JS. |
| Rich text that **must** keep formatting (e.g. CMS content, email body preview) | Render raw HTML from the database | Sanitize **on the server** with the `Sanitization` module (`SanitizeHtml`) using an allow-list of tags/attributes **before** storing or returning it, then render. |
| Value inserted into a JavaScript string | `"var name = '" + Name + "';"` | `EncodeJavaScript(Name)`, or pass it through `$parameters` of the JavaScript node (never concatenate code). |
| Value inserted into a URL | `"https://site/?q=" + SearchText` | `EncodeUrl(SearchText)` for each query value. |
| Value inserted into HTML built as text (e.g. email templates, HTML strings) | `"<b>" + CustomerName + "</b>"` | `EncodeHtml(CustomerName)`. |

### JavaScript Node Rules
1. **Never** use `innerHTML`, `outerHTML`, `insertAdjacentHTML`, `document.write`, `eval`, `new Function(...)`, or `setTimeout("string")` with data that is not a hard-coded literal.
2. Read inputs only from `$parameters.<Name>`; never build JavaScript source code by concatenating OutSystems values.
3. Prefer `textContent`, `setAttribute` (for non-event, non-`href` attributes), and `classList` for DOM changes.
4. Never put secrets, tokens, or API keys inside JavaScript nodes or script resources: everything shipped to the browser is public.

---

## 2. Links, Redirects, and URLs

1. **No `javascript:` URLs**: when a link `href`, `Image` URL, or `RedirectToURL` value comes from data, validate that it starts with `https://` (or an allow-listed relative path) before using it.
2. **Open Redirect prevention**: never redirect to a URL taken directly from a Screen Input Parameter (`?ReturnUrl=...`). Accept only relative paths or a whitelisted set of hosts, validated on the server.
3. **External links in a new tab**: add `rel="noopener noreferrer"` (via the widget `Attributes`) when using `target="_blank"`, so the opened page cannot control the original tab or receive the full URL in the `Referer` header.

---

## 3. Content Security Policy (CSP)

CSP is the safety net that stops injected scripts from running even if an encoding bug slips through.

1. **Enable CSP** for production applications in the environment/application security settings (LifeTime **Security** settings in OutSystems 11), starting in report-only/test environments.
2. Keep directives as strict as the app allows:
   - `script-src`: `'self'` plus only the explicit CDNs you truly use. Avoid adding `'unsafe-eval'`; never add wildcards such as `*` or `https:`.
   - `frame-ancestors`: `'self'` (or the explicit portal host) to prevent **clickjacking**.
   - `connect-src`: `'self'` plus the explicit API hosts called from the browser.
3. When adding a third-party script (analytics, chat widget, maps), **add only its exact host** to CSP and document why in the module description.
4. Prefer loading third-party libraries as module **Scripts** resources (served from your own domain) over remote CDNs.

---

## 4. File Upload Security

The `Upload` widget accepts any bytes the user sends. Validation in the browser is UX only; **all checks must be repeated on the server**:

1. **Allow-list extensions** (`.pdf`, `.png`, `.jpg`) — never a block-list.
2. **Check the real content type** by inspecting the file signature (magic bytes) on the server, not just the file name or the browser-sent MIME type.
3. **Enforce a maximum size** on the server before processing or storing.
4. **Never trust the file name**: generate a new internal name/GUID; never use the uploaded name to build paths or as HTML without `EncodeHtml`.
5. **Store binaries in a separate Entity** (1-to-1 with the business record) — see `data-modeling-best-practices`.
6. **Serve files back with a safe type**: download HTML/SVG/XML user files as attachments (`Download` node) rather than rendering them inline, because SVG and HTML can carry scripts.
7. For high-risk flows (public uploads, documents shared between customers), add **antivirus scanning** before the file becomes available to other users.

---

## 5. Pre-Commit Checklist

- [ ] No `Escape Content = No` (Traditional) or `innerHTML` (Reactive/Mobile) with untrusted data.
- [ ] Rich text is sanitized server-side with an allow-list before being rendered.
- [ ] JavaScript nodes take data only through `$parameters` and never build code from strings.
- [ ] URLs from data are validated (`https://` / allow-listed hosts); no open redirects.
- [ ] CSP enabled with no wildcard `script-src`; `frame-ancestors` restricted.
- [ ] Uploads validated on the server (extension allow-list, magic bytes, size) and served as attachments.
