---
name: frontend-performance
description: Front-end and rendering performance standards for OutSystems 11 Reactive Web and Mobile. Covers data fetch strategy (At start vs Only on demand), Max Records, list virtualization, heavy Blocks inside lists, OnRender and OnParametersChanged loops, image weight, scripts and third-party libraries, CSS, and Client Variable size. Use whenever building or reviewing screens, blocks, lists, and UI logic.
---

# frontend-performance

Server-side performance (queries, indexes, round-trips) is covered by `performance-anti-patterns`, `advanced-sql-and-index-tuning`, and `client-action-single-server-call`. This skill covers what happens **in the browser/device**: how much data arrives, how much the UI renders, and how often it re-renders. On mid-range mobile phones and slow networks this is often the dominant cost.

---

## 1. Fetch Only What the User Sees, When They See It

1. **`Fetch = Only on demand`** for data shown in tabs, popups, accordions, sidebars, or after a user action; call `Refresh Data` when that area is opened. `At start` only for what is visible on first paint.
2. **Always set `Max Records`** on list Aggregates and Data Actions to the page size (with `Pagination`) — never fetch the full table "and filter in the UI".
3. **Lean outputs**: fetch only the attributes the widgets use (Aggregates already optimize used columns; Data Actions must use lean Structures — see `reactive-data-action-security`).
4. **Filter, sort, and count on the server** (Aggregate/SQL), never with `ListFilter`/`ListSort` over large client-side lists.
5. Show skeletons/loading states bound to `IsDataFetched` instead of blocking the screen.

---

## 2. Lists

1. Keep **list virtualization** enabled (default in Reactive `List`) for long lists; disable it only when there is a concrete reason (e.g. printing, anchor navigation) and the list is small.
2. **Avoid heavy Blocks inside list items**: each item instantiates the Block with its own lifecycle. A Block with its own Aggregate/Data Action inside a list creates **N server requests** (one per row). Fetch the data once at screen level and pass it to the Block as input.
3. Avoid complex expressions per row that call functions or format large text repeatedly; precompute in the Aggregate/Data Action when possible.
4. Use pagination or "load more" (infinite scroll with a cap) instead of rendering thousands of rows.

---

## 3. Re-render and Lifecycle Pitfalls

1. **Never change screen state in `OnRender`** (assigning variables there triggers another render → render loops). Use `OnReady` for one-time DOM work and event handlers for changes (see `reactive-lifecycle-and-sync`).
2. **`OnParametersChanged`** in Blocks must only react to real input changes; never assign the Block's own inputs or trigger the parent to change them back.
3. Don't call `Refresh Data` in loops or on every keystroke; **debounce** search inputs (e.g. refresh after the user stops typing for ~300 ms).
4. Destroy what you create: remove event listeners, intervals, and third-party widget instances in `OnDestroy`.

---

## 4. Images and Media

1. Serve images at display size (no 4000px photos in a 200px card) and in modern formats (WebP/AVIF where supported, SVG for icons).
2. Don't store large images as module **Resources/Images** used across many screens without compression; for user content, generate thumbnails on the server.
3. Lazy-load below-the-fold images (`loading="lazy"` attribute) and set explicit width/height to avoid layout shifts.
4. Don't send `Binary Data` images inside list Data Actions; return a URL or a thumbnail Id instead.

---

## 5. Scripts, Libraries, and CSS

1. Load third-party scripts only on the screens that need them (`Required Scripts` on the Screen/Block or `RequireScript`), not globally in the layout.
2. Avoid adding large libraries (jQuery, moment.js, full charting bundles) for small tasks; prefer native APIs and OutSystems UI patterns.
3. Keep CSS in the **Theme** (cached once) rather than large inline styles or per-screen CSS duplicated across screens (see `css-format`).
4. Avoid CSS that forces expensive layout on every frame (animating `width/height/top/left`; prefer `transform`/`opacity`).

---

## 6. Client State

1. **Client Variables** are stored in browser storage and read/written synchronously: keep them small (Ids, flags, preferences) — never serialized lists or large JSON (also a security risk, see `screen-input-and-url-security`).
2. Large Local Variable lists on a screen increase memory and render cost; keep only the current page.

---

## 7. Pre-Commit Checklist

- [ ] Data in hidden areas uses `Only on demand`; every list query has `Max Records`.
- [ ] No Block with its own server fetch inside a list item.
- [ ] No state assignments in `OnRender`; searches are debounced.
- [ ] Images sized and lazy-loaded; no binary images in list payloads.
- [ ] Third-party scripts loaded only where used; CSS lives in the Theme.
- [ ] Client Variables hold only small, non-sensitive values.
