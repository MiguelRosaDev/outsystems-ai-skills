// polish-flow.cs - the flow-layout pass. Paste as the `code` of Kings3 applyModelApiCode (imports: []), then omlMerge.
// Replace Spec(...) and the action list at the bottom. Everything else is the verified engine.
// Works on IAction = server actions AND screen (client) actions. Data actions (IDataAction): place by hand, see SKILL.md.
//
// Spec rows:
//   { "L", "kind:key", "label" }                         label every node matching the anchor
//   { "C", "comment text" }                              whole-action comment: no link, on top, a little aside
//   { "C", "comment text", "kind:key", "kind:key" ... }  comment linked to every node matching any anchor
// Anchor kinds: start | assign (substring of "var = value; var = value", or "^exact") | if (condition substring) |
//               foreach (record list substring) | exec (node name, server or client call) | agg (name) |
//               js (name) | handler | raise
eSpace => {
    string[][] Spec(string name) {
        if (name == "AgentCancelClick") return new[] {
            new[] { "L", "if:UseAgentChat and", "Conversation started?" },
            new[] { "L", "assign:PrevState = ChatState", "Ask: discard the conversation?" },
            new[] { "C", "X / Esc handler. Closes at once when nothing was said; otherwise asks for confirmation (state ConfirmCancel) so a conversation is not lost by accident." }
        };
        if (name == "ScrubText") return new[] {
            new[] { "L", "assign:Marker =", "Pick marker K, reset counters" },
            new[] { "C", "Removes secrets from log text BEFORE it leaves O11. Over-masking is accepted; leaking is not." },
            new[] { "C", "K walks the marker list. Order matters: bearer runs before authorization.", "assign:Marker =", "assign:^K = K + 1", "if:K < 11" }
        };
        return new string[0][];
    }

    bool Matches(OutSystems.Model.Logic.Nodes.IActionNode n, string anchor) {
        int ix = anchor.IndexOf(':');
        string kind = anchor.Substring(0, ix); string key = anchor.Substring(ix + 1);
        if (kind == "start") return n is OutSystems.Model.Logic.Nodes.IStartNode;
        if (kind == "raise") return n is OutSystems.Model.Logic.Nodes.IRaiseExceptionNode;
        if (kind == "handler") return n is OutSystems.Model.Logic.Nodes.IExceptionHandlerNode;
        if (kind == "js") return n is OutSystems.Model.Logic.Nodes.IJavaScriptNode j && j.Name == key;
        if (kind == "exec") return (n is OutSystems.Model.Logic.Nodes.IExecuteServerActionNode e && e.Name == key) || (n is OutSystems.Model.Logic.Nodes.IExecuteClientActionNode c && c.Name == key);
        if (kind == "agg") return n is OutSystems.Model.Logic.Nodes.IAggregateNode g && g.Name == key;
        if (kind == "if") return n is OutSystems.Model.Logic.Nodes.IIfNode i && i.Condition.Text.Contains(key);
        if (kind == "foreach") return n is OutSystems.Model.Logic.Nodes.IForEachNode f && f.RecordList.Text.Contains(key);
        if (kind == "assign" && n is OutSystems.Model.Logic.Nodes.IAssignNode a) {
            var j2 = "";
            foreach (var x in a.Assignments) j2 += (j2 == "" ? "" : "; ") + x.Variable.Text + " = " + x.Value.Text;
            return key.StartsWith("^") ? j2 == key.Substring(1) : j2.Contains(key);
        }
        return false;
    }

    void Polish(OutSystems.Model.Logic.IAction act, string actName) {
        var spec = Spec(actName);
        OutSystems.Model.Logic.Nodes.IActionNode MainT(OutSystems.Model.Logic.Nodes.IActionNode n) {
            if (n is OutSystems.Model.Logic.Nodes.IStartNode s) return s.Target;
            if (n is OutSystems.Model.Logic.Nodes.IExceptionHandlerNode h) return h.Target;
            if (n is OutSystems.Model.Logic.Nodes.IAssignNode a) return a.Target;
            if (n is OutSystems.Model.Logic.Nodes.IExecuteServerActionNode e) return e.Target;
            if (n is OutSystems.Model.Logic.Nodes.IExecuteClientActionNode ce) return ce.Target;
            if (n is OutSystems.Model.Logic.Nodes.IJavaScriptNode js) return js.Target;
            if (n is OutSystems.Model.Logic.Nodes.IMessageNode m) return m.Target;
            if (n is OutSystems.Model.Logic.Nodes.IAggregateNode g) return g.Target;
            if (n is OutSystems.Model.Logic.Nodes.IIfNode i) return i.FalseTarget;
            if (n is OutSystems.Model.Logic.Nodes.IForEachNode f) return f.Target;
            return null;
        }
        OutSystems.Model.Logic.Nodes.IActionNode SideT(OutSystems.Model.Logic.Nodes.IActionNode n) {
            if (n is OutSystems.Model.Logic.Nodes.IIfNode i) return i.TrueTarget;
            if (n is OutSystems.Model.Logic.Nodes.IForEachNode f) return f.CycleTarget;
            return null;
        }
        void SetMain(OutSystems.Model.Logic.Nodes.IActionNode n, OutSystems.Model.Logic.Nodes.IActionNode t) {
            if (n is OutSystems.Model.Logic.Nodes.IStartNode s) s.Target = t;
            else if (n is OutSystems.Model.Logic.Nodes.IExceptionHandlerNode h) h.Target = t;
            else if (n is OutSystems.Model.Logic.Nodes.IAssignNode a) a.Target = t;
            else if (n is OutSystems.Model.Logic.Nodes.IExecuteServerActionNode e) e.Target = t;
            else if (n is OutSystems.Model.Logic.Nodes.IExecuteClientActionNode ce) ce.Target = t;
            else if (n is OutSystems.Model.Logic.Nodes.IJavaScriptNode js) js.Target = t;
            else if (n is OutSystems.Model.Logic.Nodes.IMessageNode m) m.Target = t;
            else if (n is OutSystems.Model.Logic.Nodes.IAggregateNode g) g.Target = t;
            else if (n is OutSystems.Model.Logic.Nodes.IIfNode i) i.FalseTarget = t;
            else if (n is OutSystems.Model.Logic.Nodes.IForEachNode f) f.Target = t;
        }
        void SetSide(OutSystems.Model.Logic.Nodes.IActionNode n, OutSystems.Model.Logic.Nodes.IActionNode t) {
            if (n is OutSystems.Model.Logic.Nodes.IIfNode i) i.TrueTarget = t;
            else if (n is OutSystems.Model.Logic.Nodes.IForEachNode f) f.CycleTarget = t;
        }
        string Trunc(string s, int n) { return s.Length > n ? s.Substring(0, n) + "..." : s; }
        bool IsDummy(OutSystems.Model.Logic.Nodes.IActionNode n) {
            return n is OutSystems.Model.Logic.Nodes.IAssignNode a && !a.Assignments.Any() && (a.Label == "Loop back" || a.Label == "Route");
        }
        bool IsMyComment(OutSystems.Model.Logic.Nodes.IActionNode n) {
            if (!(n is OutSystems.Model.Logic.Nodes.ICommentNode c)) return false;
            foreach (var sp in spec) if (sp[0] == "C" && sp[1] == c.Text) return true;
            return false;
        }

        // ---- 0. clean up an earlier pass: remove my dummies and my comments (never the original author's) ----
        foreach (var P in act.Nodes.ToList()) {
            if (P is OutSystems.Model.Logic.Nodes.ICommentNode || IsDummy(P)) continue;
            var m = MainT(P);
            int guard = 0;
            while (m != null && IsDummy(m) && guard++ < 40) m = MainT(m);
            if (m != MainT(P) && MainT(P) != null) SetMain(P, m);
            var sd = SideT(P);
            guard = 0;
            while (sd != null && IsDummy(sd) && guard++ < 40) sd = MainT(sd);
            if (sd != SideT(P) && SideT(P) != null) SetSide(P, sd);
        }
        foreach (var n in act.Nodes.ToList()) if (IsDummy(n) || IsMyComment(n)) n.Delete();

        // ---- 1. one Assign + End per branch ----
        for (int phase = 0; phase < 2; phase++) {
            var cur = act.Nodes.Where(n0 => !(n0 is OutSystems.Model.Logic.Nodes.ICommentNode)).ToList();
            var incs = new int[cur.Count];
            for (int q = 0; q < cur.Count; q++) {
                var m0 = MainT(cur[q]); if (m0 != null && cur.IndexOf(m0) >= 0) incs[cur.IndexOf(m0)]++;
                var s0 = SideT(cur[q]); if (s0 != null && cur.IndexOf(s0) >= 0) incs[cur.IndexOf(s0)]++;
            }
            for (int x = 0; x < cur.Count; x++) {
                var X = cur[x];
                bool qual = phase == 0 ? (X is OutSystems.Model.Logic.Nodes.IAssignNode && MainT(X) is OutSystems.Model.Logic.Nodes.IEndNode) : (X is OutSystems.Model.Logic.Nodes.IEndNode);
                if (!qual || incs[x] <= 1) continue;
                bool first = true;
                for (int p = 0; p < cur.Count; p++) {
                    var P = cur[p];
                    bool viaMain = MainT(P) == X;
                    bool viaSide = SideT(P) == X;
                    if (!viaMain && !viaSide) continue;
                    if (first) { first = false; continue; }
                    OutSystems.Model.Logic.Nodes.IActionNode clone;
                    if (X is OutSystems.Model.Logic.Nodes.IEndNode) clone = act.CreateNode<OutSystems.Model.Logic.Nodes.IEndNode>();
                    else {
                        var xa = (OutSystems.Model.Logic.Nodes.IAssignNode)X;
                        var ca = act.CreateNode<OutSystems.Model.Logic.Nodes.IAssignNode>();
                        foreach (var asg in xa.Assignments) ca.CreateAssignment(asg.Variable.Text, asg.Value.Text);
                        ca.Label = xa.Label;
                        ca.Target = act.CreateNode<OutSystems.Model.Logic.Nodes.IEndNode>();
                        clone = ca;
                    }
                    if (viaMain) SetMain(P, clone); else SetSide(P, clone);
                }
            }
        }

        // ---- 2. layout ----
        var all = act.Nodes.Where(n0 => !(n0 is OutSystems.Model.Logic.Nodes.ICommentNode)).ToList();
        int N = all.Count;
        var col = new int[N]; var row = new int[N]; var placed = new bool[N];
        var resC = new int[400]; var resR = new int[400]; int nres = 0;
        int maxCol = 0, maxRow = 0;
        bool Occ(int c, int r) {
            for (int q = 0; q < N; q++) if (placed[q] && col[q] == c && row[q] == r) return true;
            for (int q = 0; q < nres; q++) if (resC[q] == c && resR[q] == r) return true;
            return false;
        }
        bool IsLeafLike(OutSystems.Model.Logic.Nodes.IActionNode n) {
            if (n is OutSystems.Model.Logic.Nodes.IIfNode || n is OutSystems.Model.Logic.Nodes.IForEachNode) return false;
            var m = MainT(n);
            return m == null || m is OutSystems.Model.Logic.Nodes.IEndNode;
        }
        void Put(OutSystems.Model.Logic.Nodes.IActionNode n, int c, int r) {
            int ix = all.IndexOf(n);
            col[ix] = c; row[ix] = r; placed[ix] = true;
            if (c > maxCol) maxCol = c;
            if (r > maxRow) maxRow = r;
        }
        void Place(OutSystems.Model.Logic.Nodes.IActionNode n, int c, int r) {
            int ix = all.IndexOf(n);
            if (ix < 0 || placed[ix]) return;
            while (Occ(c, r)) c++;
            Put(n, c, r);
            if (n is OutSystems.Model.Logic.Nodes.IForEachNode fe) {
                // loop body runs to the right in the same row; the row below it is reserved for the way back
                var b = fe.CycleTarget; int bc = c + 1; int lastc = c;
                while (b != null && b != n && all.IndexOf(b) >= 0 && !placed[all.IndexOf(b)] && !(b is OutSystems.Model.Logic.Nodes.IIfNode) && !(b is OutSystems.Model.Logic.Nodes.IForEachNode)) {
                    Put(b, bc, r);
                    lastc = bc; bc++;
                    b = MainT(b);
                }
                for (int rc = c + 1; rc <= lastc; rc++) { resC[nres] = rc; resR[nres] = r + 1; nres++; }
                if (r + 1 > maxRow) maxRow = r + 1;
                if (b != null && b != n) Place(b, bc, r);
                var m2 = MainT(n);
                if (m2 != null) Place(m2, c, r + 1);
                return;
            }
            var m = MainT(n);
            var sd = SideT(n);
            bool leafSide = sd != null && IsLeafLike(sd) && all.IndexOf(sd) >= 0 && !placed[all.IndexOf(sd)];
            if (leafSide) {
                // short exit: Assign then End side by side next to the If
                int sc = c + 1;
                while (Occ(sc, r)) sc++;
                Put(sd, sc, r);
                var tail = MainT(sd);
                if (tail != null && all.IndexOf(tail) >= 0 && !placed[all.IndexOf(tail)]) {
                    int tc = sc + 1;
                    while (Occ(tc, r)) tc++;
                    Put(tail, tc, r);
                }
            }
            if (m != null) Place(m, c, r + 1);
            if (sd != null && !leafSide) Place(sd, maxCol + 1, r);
        }
        Place(all.OfType<OutSystems.Model.Logic.Nodes.IStartNode>().First(), 0, 0);
        foreach (var h in all.OfType<OutSystems.Model.Logic.Nodes.IExceptionHandlerNode>().ToList()) Place(h, maxCol + 2, 1);
        int strayCol = 0;
        for (int q = 0; q < N; q++) if (!placed[q]) { col[q] = strayCol++; row[q] = maxRow + 3; placed[q] = true; }

        // ---- 3. routing: shortest path through free cells, a dummy assign at every turn ----
        int W = maxCol + 10; int Hh = maxRow + 5;
        var hard = new bool[W, Hh];
        var soft = new bool[W, Hh];
        for (int q = 0; q < N; q++) hard[col[q] + 5, row[q] + 1] = true;
        for (int q = 0; q < nres; q++) hard[resC[q] + 5, resR[q] + 1] = true;
        var dCol = new int[200]; var dRow = new int[200]; var hubT = new int[200];
        var dNode = new OutSystems.Model.Logic.Nodes.IAssignNode[200]; int nd = 0;
        OutSystems.Model.Logic.Nodes.IAssignNode NewDummy(string label, int c, int r, int target) {
            var d = act.CreateNode<OutSystems.Model.Logic.Nodes.IAssignNode>();
            d.Label = label;
            dCol[nd] = c; dRow[nd] = r; hubT[nd] = target; dNode[nd] = d; nd++;
            d.HorizontalPosition = 2743 + c * 2286;
            d.VerticalPosition = 1371 + r * 1829;
            hard[c + 5, r + 1] = true;
            return d;
        }
        bool Near(double ax, double ay, double bx, double by, double px, double py) {
            double dx = bx - ax, dy = by - ay; double len2 = dx * dx + dy * dy;
            double t = len2 == 0 ? 0 : ((px - ax) * dx + (py - ay) * dy) / len2;
            if (t < 0) t = 0; if (t > 1) t = 1;
            double ex = px - (ax + t * dx), ey = py - (ay + t * dy);
            return ex * ex + ey * ey < 0.38 * 0.38;
        }
        bool Crosses(int ac, int ar, int bc, int br, int skip1, int skip2) {
            for (int q = 0; q < N; q++) {
                if (q == skip1 || q == skip2) continue;
                if (Near(ac, ar, bc, br, col[q], row[q])) return true;
            }
            for (int q = 0; q < nd; q++) if (Near(ac, ar, bc, br, dCol[q], dRow[q])) return true;
            return false;
        }
        int[] dx4 = new[] { 1, 0, -1, 0 }; int[] dy4 = new[] { 0, 1, 0, -1 };
        bool Route(int p, int t, bool viaMain, string label, bool allowSoftTurn) {
            int sx = col[p] + 5, sy = row[p] + 1;
            var gxs = new int[200]; var gys = new int[200]; var gnode = new OutSystems.Model.Logic.Nodes.IActionNode[200]; int ng = 0;
            gxs[ng] = col[t] + 5; gys[ng] = row[t] + 1; gnode[ng] = all[t]; ng++;
            // goals: the target itself, or a dummy of an earlier route to the same target (merge)
            for (int q = 0; q < nd; q++) if (hubT[q] == t) { gxs[ng] = dCol[q] + 5; gys[ng] = dRow[q] + 1; gnode[ng] = dNode[q]; ng++; }
            bool IsGoal(int x, int y) { for (int g = 0; g < ng; g++) if (gxs[g] == x && gys[g] == y) return true; return false; }
            int[,,] dist = new int[W, Hh, 4];
            int[,,] prev = new int[W, Hh, 4];
            for (int x = 0; x < W; x++) for (int y = 0; y < Hh; y++) for (int d = 0; d < 4; d++) { dist[x, y, d] = 1000000; prev[x, y, d] = -1; }
            for (int d = 0; d < 4; d++) {
                int nx = sx + dx4[d], ny = sy + dy4[d];
                if (nx < 0 || ny < 0 || nx >= W || ny >= Hh) continue;
                if (hard[nx, ny] && !IsGoal(nx, ny)) continue;
                dist[nx, ny, d] = 1 + (soft[nx, ny] ? 6 : 0);
                prev[nx, ny, d] = -2;
            }
            bool changed = true; int iter = 0;
            while (changed && iter++ < 400) {
                changed = false;
                for (int x = 0; x < W; x++) for (int y = 0; y < Hh; y++) for (int d = 0; d < 4; d++) {
                    int cur = dist[x, y, d];
                    if (cur >= 1000000) continue;
                    if (IsGoal(x, y)) continue;
                    for (int nd2 = 0; nd2 < 4; nd2++) {
                        if ((nd2 + 2) % 4 == d) continue;
                        if (nd2 != d && soft[x, y] && !allowSoftTurn) continue;
                        int nx = x + dx4[nd2], ny = y + dy4[nd2];
                        if (nx < 0 || ny < 0 || nx >= W || ny >= Hh) continue;
                        if (hard[nx, ny] && !IsGoal(nx, ny)) continue;
                        int cost = cur + 1 + (nd2 != d ? (5 + (soft[x, y] ? 12 : 0)) : 0) + (soft[nx, ny] ? 6 : 0);
                        if (cost < dist[nx, ny, nd2]) { dist[nx, ny, nd2] = cost; prev[nx, ny, nd2] = (x * Hh + y) * 4 + d; changed = true; }
                    }
                }
            }
            int best = 1000000; int bd = -1; int bg = -1;
            for (int g = 0; g < ng; g++) for (int d = 0; d < 4; d++) if (dist[gxs[g], gys[g], d] < best) { best = dist[gxs[g], gys[g], d]; bd = d; bg = g; }
            if (bd < 0) return false;
            int[] px = new int[400]; int[] py = new int[400]; int[] pd = new int[400]; int np = 0;
            int cx = gxs[bg], cy = gys[bg], cd = bd;
            while (true) {
                px[np] = cx; py[np] = cy; pd[np] = cd; np++;
                int pv = prev[cx, cy, cd];
                if (pv == -2 || pv < 0) break;
                int st = pv / 4; cd = pv % 4; cx = st / Hh; cy = st % Hh;
                if (np > 390) return false;
            }
            OutSystems.Model.Logic.Nodes.IAssignNode headD = null; OutSystems.Model.Logic.Nodes.IAssignNode lastD = null;
            for (int k = np - 1; k >= 1; k--) {
                if (pd[k] != pd[k - 1]) {
                    var d = NewDummy(label, px[k] - 5, py[k] - 1, t);
                    if (lastD != null) lastD.Target = d; else headD = d;
                    lastD = d;
                }
            }
            for (int k = 0; k < np; k++) if (!IsGoal(px[k], py[k])) soft[px[k], py[k]] = true;
            if (headD == null) return false;
            lastD.Target = gnode[bg];
            if (viaMain) SetMain(all[p], headD); else SetSide(all[p], headD);
            return true;
        }
        for (int p = 0; p < N; p++) {
            var P = all[p];
            for (int slot = 0; slot < 2; slot++) {
                var T = slot == 0 ? MainT(P) : SideT(P);
                if (T == null) continue;
                int t = all.IndexOf(T);
                if (t < 0) continue;
                if (!Crosses(col[p], row[p], col[t], row[t], p, t)) continue;
                string lbl = row[t] <= row[p] ? "Loop back" : "Route";
                if (!Route(p, t, slot == 0, lbl, false)) Route(p, t, slot == 0, lbl, true);
            }
        }
        for (int q = 0; q < N; q++) {
            all[q].HorizontalPosition = 2743 + col[q] * 2286;
            all[q].VerticalPosition = 1371 + row[q] * 1829;
        }

        // ---- 4. labels (Assign, If, ForEach, Message only; the API gives no label to other nodes) ----
        var outs = act.OutputParameters.Select(o => o.Name).ToList();
        foreach (var n in act.Nodes.ToList()) {
            if (n is OutSystems.Model.Logic.Nodes.ICommentNode || IsDummy(n)) continue;
            bool done = false;
            foreach (var sp in spec) {
                if (sp[0] != "L" || !Matches(n, sp[1])) continue;
                if (n is OutSystems.Model.Logic.Nodes.IAssignNode a0) a0.Label = sp[2];
                else if (n is OutSystems.Model.Logic.Nodes.IIfNode i0) i0.Label = sp[2];
                else if (n is OutSystems.Model.Logic.Nodes.IForEachNode f0) f0.Label = sp[2];
                done = true; break;
            }
            if (done) continue;
            // fallback labels when the Spec has no row: better than "Assign", but write real ones in the Spec
            if (n is OutSystems.Model.Logic.Nodes.IAssignNode a && !(a.Label != null && a.Label != "")) {
                var vars = a.Assignments.Select(x => x.Variable.Text).ToList();
                if (vars.Count == 0) continue;
                var roots = vars.Select(v => v.Contains(".") ? v.Substring(0, v.IndexOf(".")) : v).Distinct().ToList();
                bool allOut = roots.All(r0 => outs.Contains(r0));
                a.Label = (allOut ? "Set output: " : "Set ") + string.Join(", ", roots.Take(3)) + (roots.Count > 3 ? ", ..." : "");
            }
            else if (n is OutSystems.Model.Logic.Nodes.IIfNode i && !(i.Label != null && i.Label != "")) i.Label = Trunc(i.Condition.Text, 40) + "?";
            else if (n is OutSystems.Model.Logic.Nodes.IForEachNode f && !(f.Label != null && f.Label != "")) f.Label = "For each " + f.RecordList.Text.Replace(".List", "") + " row";
            else if (n is OutSystems.Model.Logic.Nodes.IMessageNode mg && !(mg.Label != null && mg.Label != "")) mg.Label = mg.Type == OutSystems.Model.Enumerations.MessageType.Error ? "Show error message" : "Show message";
        }

        // ---- 5. comments linked to the nodes they explain; whole-action comments on top, a little aside ----
        var real = act.Nodes.Where(n0 => !(n0 is OutSystems.Model.Logic.Nodes.ICommentNode) && !IsDummy(n0)).ToList();
        var startN = real.OfType<OutSystems.Model.Logic.Nodes.IStartNode>().First();
        foreach (var sp in spec) {
            if (sp[0] != "C") continue;
            var c = act.CreateNode<OutSystems.Model.Logic.Nodes.ICommentNode>();
            c.Text = sp[1];
            if (sp.Length == 2) { c.HorizontalPosition = startN.HorizontalPosition + 1700; c.VerticalPosition = 250; continue; }
            bool first = true;
            for (int a = 2; a < sp.Length; a++) {
                foreach (var n in real) {
                    if (!Matches(n, sp[a])) continue;
                    if (first) {
                        if (n.HorizontalPosition <= 2744) { c.HorizontalPosition = n.HorizontalPosition - 2286; c.VerticalPosition = n.VerticalPosition - 914; }
                        else { c.HorizontalPosition = n.HorizontalPosition + 1143; c.VerticalPosition = n.VerticalPosition - 914; }
                        first = false;
                    }
                    c.CreateConnector(n);
                }
            }
        }
    }

    // ---- action list: server actions and screen actions are both IAction ----
    Polish(eSpace.ServerActions.Named("ScrubText"), "ScrubText");
    // var blk = eSpace.MobileFlows.Named("MainFlow").Children.OfType<OutSystems.Model.UI.Mobile.IMobileBlock>().Named("<BlockName>");
    // Polish(blk.ScreenActions.Named("AgentCancelClick"), "AgentCancelClick");
}
