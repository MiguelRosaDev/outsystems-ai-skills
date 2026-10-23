---
name: css-format
description: Formats CSS for this project. Use whenever CSS is written to an OutSystems StyleSheet (block, screen or theme, e.g. via applyModelApiCode) or to a .css file. Enforces one declaration per line, 4-space indentation, and replacing the whole sheet instead of appending one-line override blocks.
---

# css-format

All CSS this project writes must be readable multi-line CSS, never packed on one line.

## Rules
1. **One declaration per line**, each ending with a semicolon.
2. **4-space indentation** (the existing OutSystems sheets already use 4 spaces). No tabs.
3. **Opening brace on the selector line**, closing brace on its own line.
4. **One selector per line** in comma lists.
5. **Space after the colon** in declarations and inside media conditions: `max-width: 700px`.
6. **One blank line between rules** and between logical groups.
7. **At-rules** (`@media`, `@keyframes`) nest their rules with one extra indent level.
8. **Comments on their own line**, above the rule they describe.
9. **No `!important`** unless something in the platform CSS overrides the rule and you have verified it (for example OutSystems UI popup rules). When used, say why in a comment.
10. **Replace the whole sheet, do not append.** Appending "round 2 / round 3" override blocks creates duplicate selectors whose result depends on order. Read the current sheet, edit it, write it back once.

## Example

Wrong:

```css
.agent-composer__send{min-width:44px;min-height:44px}
```

Right:

```css
.agent-composer__send {
    min-width: 44px;
    min-height: 44px;
}
```

## How to apply
1. Build the full CSS text with a blank line between rules.
2. Format it with Prettier (needs Node; the first run downloads the package):

    ```powershell
    $css | npx -y prettier@3 --parser css --tab-width 4
    ```

    - Prettier keeps blank lines that are already in the input (at most one) and does not add them, so put them in before formatting.
    - PowerShell adds a BOM when piping; strip the leading `﻿` before using the result.
    - If Prettier cannot run (offline), apply rules 1-9 by hand and say so.
3. For an OutSystems StyleSheet, assign the result as one string with `\n` line breaks, escaping `"` and `\`:

    ```csharp
    block.StyleSheet = "...rule one {\n    prop: value;\n}\n\n...rule two {\n    ...\n}";
    ```

4. **Verify nothing changed visually**: before and after, compare the computed style of the key elements in the browser (or ask the tester agent). A reformat must not change behaviour; a consolidation of duplicate selectors must keep the value that the cascade produced before.

## Checks before finishing
- No line contains more than one `;`.
- No selector appears twice at the same nesting level and condition unless the second is deliberately different (state, media query).
- Every class used by the widgets still has its rule.
