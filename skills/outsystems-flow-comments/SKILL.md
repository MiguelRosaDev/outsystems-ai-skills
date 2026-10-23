---
name: outsystems-flow-comments
description: Rules and layout patterns for flow comments and canvas annotations in OutSystems 11 actions (via MCP write_flows or ModelAPI ICommentNode): placing comments beside the nodes they explain, linking with connectors, whole-action header notes, and keeping boxes compact on the Service Studio canvas. Use whenever adding or editing comments in logic flows.
---

# outsystems-flow-comments

In OutSystems Service Studio, flow comments (`ICommentNode`) are visual boxes rendered directly on the action canvas. When an AI model creates comments without positioning, linking, or length rules, three things go wrong:
1. Comments pile up at coordinate `(0,0)` disconnected from the nodes they describe.
2. Multi-paragraph comments turn into giant yellow banners that obscure connectors and neighbouring nodes across the canvas.
3. Comments repeat what the node label already says instead of explaining the non-obvious rule.

Apply these rules whenever authoring or editing actions with `write_flows` or `applyModelApiCode`.

---

## 1. Where Comments Sit on the Canvas (3 Patterns)

```text
                  [Start]                     ┌────────────────────────────────────────────┐
                     │                        │ Whole-action overview comment (unlinked):   │
                     ▼                        │ Placed at top-right of Start (X+1700, Y=250)│
            [If: Stale reply?]                └────────────────────────────────────────────┘
                     │ (False)
                     ▼
          [Call: ScrubBearerToken] ─────────► ┌────────────────────────────────────────────┐
                     │                        │ Pinned node comment (same row, 1 col right)│
                     ▼                        │ "Order matters: Bearer runs before Auth."  │
          [Assign: Set Output]                └────────────────────────────────────────────┘
```

### Pattern A: Node-Linked Comment (Beside the Step)
* **Position**: Placed **immediately to the right of the node it explains, on that node's own row**.
  * If that grid cell is already occupied (for example, by an `If`'s True branch or a `ForEach` loop body), walk one column further right on the same row until a free cell is found.
  * **Never** use a single shared gutter far past the widest branch of the whole action—otherwise one wide loop drags every comment in the flow far away from the main spine.
* **Connection**: Always link the comment to the step(s) it explains with a connector line so the visual relationship is unambiguous.

### Pattern B: Whole-Action Overview Comment (Header Note)
* **Purpose**: Explains the overall contract, caller, or non-obvious invariant of the entire action.
* **Position**: Placed at the **top-right of the `Start` node** (`X = Start.X + 1700`, `Y = 250`), slightly aside from the vertical spine.
* **Connection**: **Unlinked** (0 connectors). Because it sits at the top beside `Start`, readers immediately recognize it as the action's header note.

### Pattern C: Missing-Dependency Placeholder Comment
* **Purpose**: When building a flow that requires an external extension or producer action not yet referenced in the module, place a comment step where the call belongs and link it to the step (`next`).
* **Content**: State the exact Producer Module, Action Name, and input/output mapping needed so the flow remains valid and ready to wire.

---

## 2. How to Write Comments in OutSystems MCP (`write_flows`)

In `os11-oml-transformer`'s `write_flows` tool, comments are steps with `"kind": "comment"`:

### Single-Node Pinned Comment
Use `next` to pin the comment to the step it explains. The MCP automatically places the comment immediately to the right of that step on the same row:
```json
{
    "id": "note_order",
    "kind": "comment",
    "text": "Order matters: Bearer token scrub must run before generic Authorization header.",
    "next": "scrub_bearer"
}
```

### Multi-Node Pinned Comment
When one comment explains two or more related steps (e.g. a loop initialization and its condition), put the first target step in `next` and the remaining target steps in `pins`:
```json
{
    "id": "note_retry_loop",
    "kind": "comment",
    "text": "Retries up to 3 times on HTTP 429 before raising CommunicationException.",
    "next": "init_attempt",
    "pins": ["check_attempt_limit"]
}
```
* **Engine Guardrail**: Never list the same step twice across `next` and `pins`, and never target the comment itself—Service Studio's engine rejects duplicate connectors between the same pair of nodes.

---

## 3. How to Write Comments in ModelAPI (`applyModelApiCode`)

When manipulating `IAction` directly in C#:
```csharp
var comment = act.CreateNode<OutSystems.Model.Logic.Nodes.ICommentNode>();
comment.Text = "SAP returns HTTP 200 even on business errors; inspect ResultCode.";
comment.HorizontalPosition = targetNode.HorizontalPosition + 2286; // 1 column right
comment.VerticalPosition = targetNode.VerticalPosition;            // Same row
comment.CreateConnector(targetNode);
```

---

## 4. Content & Length Rules (Canvas Readability)

1. **Keep It Short (1 to 2 Sentences Maximum)**:
   * Service Studio renders comment text inside a fixed-width box that grows vertically with every line. A paragraph of 6+ lines becomes a massive banner that collides with connectors below it.
   * Aim for **under 160 characters** (1–2 concise sentences). If deeper architectural detail is needed, state a short caption and point to the architecture doc or ADR.
2. **State *Why*, Never *What***:
   * ❌ Bad: `"Checks if Requested <= 0 and assigns MaxRows"` (the `If` and `Assign` labels already say this).
   * ✅ Good: `"Zero or negative requests fall back to Site.MaxRowsDefault to prevent unbounded table scans."`
3. **What Deserves a Comment**:
   * **Ordering constraints**: Why Step A must run before Step B.
   * **External API quirks**: Why a status code or field is handled in a non-obvious way.
   * **Transaction / Rollback intent**: Why `CommitTransaction` or `Abort Transaction = No` is used at that point.
   * **Security / Privacy guardrails**: Why a value is masked or checked before proceeding.
4. **Never Delete Developer Comments**:
   * When editing an existing action, preserve comments written by human developers unless the logic they describe was intentionally removed.
