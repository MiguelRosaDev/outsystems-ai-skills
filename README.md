# OutSystems Skills for AI Coding Assistants

[![GitHub repo](https://img.shields.io/badge/GitHub-MiguelRosaDev%2Foutsystems--skills-blue.svg)](https://github.com/MiguelRosaDev/outsystems-skills)
[![OutSystems ModelAPI](https://img.shields.io/badge/OutSystems-ModelAPI%20%2F%20O11-red.svg)](https://www.outsystems.com/)
[![AI Skills](https://img.shields.io/badge/AI%20Skills-Agentic%20Workflows-green.svg)](https://github.com/MiguelRosaDev)

A collection of specialized skills and automated workflows for AI models and coding assistants (Claude, Cursor, Copilot, ChatGPT, or custom AI agents) interacting with **OutSystems 11 / ODC** via the **ModelAPI** and MCP servers (such as `os11-oml-transformer`).

---

## 🚀 The Problem Solved

When AI coding models generate or edit OutSystems logic flows programmatically through the ModelAPI:
- **No Automatic Positioning**: By default, created nodes (Start, Assigns, Ifs, Action Calls, Message, Ends) share identical coordinates or sit directly on top of each other.
- **Visual "Spaghetti"**: Connectors cross through nodes, loop backs tangle across branches, and multiple branches converge into messy shared tails.
- **Missing Context**: Assigns and If conditions retain default or blank labels in Service Studio (`Assign`, `If`), requiring developers to open every node to inspect what it does.
- **Unlinked Comments**: Explanatory comments float disconnected from the nodes they describe.

Opening an action modified this way in **Service Studio** results in an unreadable visual knot.

The **`flow-layout`** skill completely fixes this by enforcing deterministic grid layout, clean branch routing, semantic labeling, and automated structural validation.

---

## 📦 Included Skills

### 1. `flow-layout`
Ensures all Server Actions, Screen (Client) Actions, and Data Actions follow clean visual architecture before changes are merged.

* **Grid-Based Alignment**:
  * Node coordinates follow a standardized grid: columns are **2286** units apart, rows **1829** units apart (Start node placed at `X=2743, Y=1371`).
  * Main execution path flows **straight down**.
  * True branches of `If` nodes branch to the **right**.
  * `ForEach` loop bodies branch right and resume downwards after the loop.
  * Exception Handlers occupy a dedicated column to the right.
* **Zero Connector Crossings**:
  * When a connector would cross over another node (e.g., a loop back or far merge), it is automatically routed through **dummy Assign nodes** (empty assigns labelled `Route` or `Loop back`) placed at every turn through free grid cells.
* **Branch Isolation (1 Assign + 1 End per branch)**:
  * Eliminates long cross-screen connector lines by giving each branch its own dedicated Assign and End node side by side.
* **Readable Labels**:
  * Every Assign, If, ForEach, and Message node receives a concise, human-readable label explaining its action (e.g., `Set output: Reply, IsDone`, `Stale reply?`).
* **Connected Comments**:
  * Action-level comments sit at the top right of the flow.
  * Logic comments are programmatically linked to the exact nodes they explain via `comment.CreateConnector(node)`.
* **Automated Checker (`check-flow.cs`)**:
  * Read-only structural verification run via ModelAPI diagnostics.
  * Target: **0 crossing connectors, 0 overlapping nodes, 0 unreachable nodes, 0 open-ended branches, 0 unlabelled nodes**.

### 2. `css-format`
Enforces clean, maintainable CSS formatting whenever stylesheets (themes, screens, or blocks) are created or updated in OutSystems:
* **One declaration per line** with 4-space indentation (matching native OutSystems Studio convention).
* **Opening brace on selector line**, closing brace on its own line.
* **Full-sheet replacement** rather than appending messy one-line override blocks.
* Automated formatting support via Prettier (`npx prettier --parser css --tab-width 4`).

---

## 🛠️ Repository Structure

```text
outsystems-skills/
├── README.md
├── .gitignore
└── skills/
    ├── flow-layout/
    │   ├── SKILL.md            # Comprehensive instructions for any AI model
    │   ├── check-flow.cs       # Read-only ModelAPI checker script
    │   └── polish-flow.cs      # Layout engine, routing, and labeling script
    └── css-format/
        └── SKILL.md            # CSS formatting rules and workflows
```

---

## 💻 How to Use with Any AI Model or Agent

These skills can be used with **any AI coding assistant, LLM, or agentic IDE**:

### Option A: Use as System Instructions / Agent Skills
Include the instructions from [`skills/flow-layout/SKILL.md`](file:///skills/flow-layout/SKILL.md) and [`skills/css-format/SKILL.md`](file:///skills/css-format/SKILL.md) into your AI setup:
* **Cursor / Windsurf**: Add to `.cursorrules` or `.windsurfrules`.
* **GitHub Copilot**: Add to `.github/copilot-instructions.md`.
* **Claude / ChatGPT**: Add as Project Knowledge, Custom Instructions, or directly in your system prompt.
* **Agentic Frameworks & MCP Clients**: Mount the `skills/` directory into your agent's skills path or workspace instructions.

Whenever the model creates or updates an OutSystems action via ModelAPI or MCP, instruct it to apply the `flow-layout` rules and scripts.

### Option B: Direct Execution with OutSystems ModelAPI / MCP
If running scripts directly through an OutSystems ModelAPI tool or MCP server (e.g. `os11-oml-transformer`):

1. **Format & Layout Flow**:
   Paste [`polish-flow.cs`](file:///skills/flow-layout/polish-flow.cs) into `applyModelApiCode` (customize the `Spec` dictionary with your action name and node labels), then run `omlMerge`.
2. **Verify Flow Health**:
   Paste [`check-flow.cs`](file:///skills/flow-layout/check-flow.cs) into `applyModelApiCode` to get an instant report on crossing connectors, overlapping nodes, and missing descriptions.

---

## 📄 License

MIT License. Feel free to use, modify, and share!
