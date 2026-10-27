---
name: accessibility-wcag
description: Accessibility (a11y) standards for OutSystems 11 Reactive Web and Mobile UIs aligned with WCAG 2.1/2.2 level AA. Covers labels linked to inputs, accessible names for icon buttons, image alternative text, color contrast, keyboard navigation and focus, popups/modals, error messages, page language and titles, and ARIA attributes. Use whenever building or reviewing screens, blocks, forms, and custom widgets.
---

# accessibility-wcag

Accessibility is a quality and legal requirement, not a cosmetic one. Many organizations must meet **WCAG 2.1/2.2 level AA**, and the **European Accessibility Act (EAA)** applies to many consumer-facing digital services in the EU (e-commerce, banking, electronic communications, transport). Accessible UIs also work better for everyone: keyboard users, screen-reader users, older users, users in bright sunlight, and automated UI tests.

OutSystems UI patterns provide a good baseline, but custom HTML, icon-only buttons, and CSS overrides frequently break it.

---

## 1. Forms

1. **Every input has a visible `Label`** whose `Input Widget` property points to the input (this links `<label for>` to the field). Placeholders are **not** labels.
2. Mark required fields both visually and programmatically (`Mandatory = Yes`), not only with a red asterisk.
3. **Error messages**: specific and next to the field ("Enter a valid postal code: 0000-000"), using the built-in `Valid`/`ValidationMessage` properties so they are announced; never only a red border.
4. Group related controls (radio groups, address blocks) with a fieldset/legend or `role="group"` + `aria-labelledby`.
5. Use the correct input types (`Email`, `Phone Number`, `Number`, `Date`) so mobile keyboards and autofill work.

---

## 2. Accessible Names and Images

1. **Icon-only buttons/links** (trash, edit, close, menu) must have an accessible name: add `aria-label` in `Attributes` (e.g. `aria-label = "Delete order"`), translated like any other text.
2. **Images**: informative images get alternative text describing their content/purpose (`alt` attribute); decorative images get `alt=""` so screen readers skip them.
3. Link texts must make sense out of context: avoid "Click here" / "More"; use "Download invoice PDF".
4. Don't build buttons from `Container` + `OnClick`: use `Button`/`Link` widgets so they are focusable and keyboard-operable. If unavoidable, add `role="button"`, `tabindex="0"`, and handle Enter/Space.

---

## 3. Color and Visual Design

1. **Contrast**: text at least **4.5:1** against its background (3:1 for large text and for UI component borders/icons). Check theme colors and custom CSS overrides.
2. **Never convey information by color alone** (status chips, chart series, validation): add text, icons, or patterns.
3. Support zoom up to 200% and text spacing changes without loss of content; avoid fixed heights on text containers.
4. Respect `prefers-reduced-motion` for animations.

---

## 4. Keyboard and Focus

1. Every interactive element is reachable and operable with the keyboard (Tab, Shift+Tab, Enter, Space, Esc), in a logical order that follows the visual order. Don't use positive `tabindex` values.
2. **Visible focus indicator**: never remove `outline` in CSS without providing an equally visible replacement.
3. **Popups/modals**: move focus into the popup when it opens, keep focus inside while open, close with Esc, and return focus to the element that opened it.
4. After client-side navigation or a major content update, move focus to the main heading or announce the change (ARIA live region for toasts and async results).

---

## 5. Page Structure

1. Set the page **language** correctly (and update it when the user switches locale — see `multilanguage-translations`).
2. Every screen has a meaningful, unique **title**.
3. One `h1` per screen, and headings in hierarchical order (`h1` → `h2` → `h3`) — use heading styles, not bold text, for section titles.
4. Use landmarks (header, nav, main, footer) — the OutSystems UI layouts provide them; keep custom layouts equivalent.
5. Tables of data use real table headers; don't use tables for layout.

---

## 6. ARIA Rules

1. **First rule of ARIA**: prefer native elements/widgets over ARIA. Wrong ARIA is worse than none.
2. Keep `aria-expanded`, `aria-selected`, `aria-checked` in sync with state in custom widgets.
3. Use `aria-live="polite"` for asynchronous messages (search results count, saved confirmation).

---

## 7. Pre-Commit Checklist

- [ ] All inputs have linked visible labels; errors are specific and announced.
- [ ] Icon-only buttons have translated `aria-label`; images have appropriate `alt`.
- [ ] Text contrast ≥ 4.5:1; no information conveyed by color alone.
- [ ] Full keyboard operation with a visible focus indicator; popups manage focus and close with Esc.
- [ ] Screen title, language, and heading hierarchy are correct.
- [ ] Verified with an automated checker (e.g. axe/Lighthouse) plus a quick keyboard-only pass.
