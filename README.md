# OutSystems Skills for AI Coding Assistants

[![GitHub repo](https://img.shields.io/badge/GitHub-MiguelRosaDev%2Foutsystems--skills-blue.svg)](https://github.com/MiguelRosaDev/outsystems-skills)
[![OutSystems ModelAPI](https://img.shields.io/badge/OutSystems-ModelAPI%20%2F%20O11-red.svg)](https://www.outsystems.com/)
[![Antigravity Skills](https://img.shields.io/badge/Antigravity-Custom%20Skills-green.svg)](https://github.com/MiguelRosaDev)

A collection of specialized agentic skills and automated workflows for AI coding assistants (such as **Google Antigravity**) interacting with **OutSystems 11 / ODC** via the **ModelAPI** and MCP servers (e.g., `os11-oml-transformer` / Kings3).

---

## 🚀 The Problem Solved

When AI coding assistants generate or edit OutSystems logic flows programmatically through the ModelAPI:
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
    │   ├── SKILL.md            # Comprehensive instructions for the AI assistant
    │   ├── check-flow.cs       # Read-only ModelAPI checker script
    │   └── polish-flow.cs      # Layout engine, routing, and labeling script
    └── css-format/
        └── SKILL.md            # CSS formatting rules and workflows
```

---

## 💻 How to Install & Use

### Option A: Global Installation in Antigravity (Recommended)
Make these skills available to the AI assistant across all your projects:

```bash
# 1. Create your Antigravity skills directory if it doesn't exist
mkdir -p ~/.gemini/config/skills

# 2. Clone the repository
git clone https://github.com/MiguelRosaDev/outsystems-skills.git /tmp/outsystems-skills

# 3. Copy the skills into your global config
cp -r /tmp/outsystems-skills/skills/* ~/.gemini/config/skills/
```

### Option B: Project-Level Installation
To use these skills only in a specific workspace/repository:

```bash
# In your project root:
mkdir -p .agents/skills
cp -r /path/to/outsystems-skills/skills/* .agents/skills/
```

Antigravity will automatically discover `.agents/skills/flow-layout` and `.agents/skills/css-format`.

### Option C: Manual Execution with OutSystems MCP
If running scripts directly through the `os11-oml-transformer` MCP:

1. **Format & Layout Flow**:
   Paste [`polish-flow.cs`](file:///skills/flow-layout/polish-flow.cs) into `applyModelApiCode` (customize the `Spec` dictionary with your action name and node labels), then run `omlMerge`.
2. **Verify Flow Health**:
   Paste [`check-flow.cs`](file:///skills/flow-layout/check-flow.cs) into `applyModelApiCode` to get an instant report on crossing connectors, overlapping nodes, and missing descriptions.

---

## 📄 License

MIT License. Feel free to use, modify, and share!
