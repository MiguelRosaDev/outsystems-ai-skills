---
name: multilanguage-translations
description: Internationalization (i18n) and multi-language translation rules for OutSystems 11 modules, including string interpolation, Translate vs DoNotTranslate behavior, and MCP translation tools (list_untranslated, write_translations, add_locale). Use whenever authoring UI text, messages, or translating modules.
---

# multilanguage-translations

OutSystems 11 includes a built-in multilingual engine that translates static UI labels, validation messages, and static entity records by locale (e.g. `pt-PT`, `en-US`, `es-ES`).

When using AI or MCP translation tools (`list_locales`, `add_locale`, `list_untranslated`, `write_translations`), follow these rules so translations never break UI layout or application logic.

---

## 1. Never Concatenate Translatable Strings

### ❌ The Anti-Pattern
Splitting a sentence across multiple text literals concatenated with variables:
```text
"Foram encontrados " + IntegerToText(TotalCount) + " registos ativos."
```
* **Why it breaks**: In the translation table, this creates two disconnected fragments (`"Foram encontrados "` and `" registos ativos."`). Translators lack context, word order cannot be inverted for languages with different grammar, and trailing/leading spaces get lost.

### ✅ The Solution
Use a single translatable template string with placeholders and replace them using `Replace()` or `FormatText()`:
```text
Replace("Found {0} active records.", "{0}", IntegerToText(TotalCount))
```

---

## 2. `Translate` vs. `DoNotTranslate` Behavior

When auditing untranslated strings (`list_untranslated`), every literal slot must be explicitly classified in `write_translations`:

### Set `Behavior = Translate` for:
* Screen titles, headings, and navigation labels
* Button captions and link text
* Form input labels, placeholders, and hints
* Feedback messages (`Message` nodes: Info, Success, Warning, Error)
* Custom validation messages
* Static Entity `Label` attributes displayed in the UI

### Set `Behavior = DoNotTranslate` for:
* CSS class names, inline styles, and HTML tag attributes (`"btn-primary"`, `"flex-row"`)
* Technical codes, status keys, and API identifiers (`"BEARER"`, `"UTF-8"`, `"ISO-8601"`)
* URLs, route paths, and email templates keys
* Date/Number format masks (`"yyyy-MM-dd"`, `"#,##0.00"`)
* JavaScript object keys and internal `LogMessage` diagnostic strings
* Punctuation-only literals (`"/"`, `" - "`, `":"`, `"•"`)

---

## 3. MCP Translation Workflow

When translating an OutSystems 11 module via MCP:
1. **Check Locales**: Call `list_locales`. If the target language (e.g. `pt-PT` or `en-GB`) is missing, call `add_locale`.
2. **Audit Untranslated Strings**: Call `list_untranslated` to view all untranslated literals grouped by source string.
3. **Mark Technical Literals**: Set `DoNotTranslate` on all technical codes, CSS classes, and format strings so the untranslated queue reaches 0 false positives.
4. **Apply Translations**: Use `write_translations` with consistent domain terminology (e.g. keep button verbs in the infinitive or imperative consistently across the module).
5. **Preserve Placeholders**: Ensure any `{0}`, `{1}`, or `%s` tokens in the source string remain intact in the translated text.
