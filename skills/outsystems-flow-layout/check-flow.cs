// check-flow.cs - read-only structural check of flow layout. Paste as the `code` of applyModelApiCode (imports: []).
// Nothing is saved: the last line throws on purpose and the exception message carries the report.
// Reports per action: nodes (dummies), comments (unlinked = whole-action overviews, expected), edges,
// connectors crossing a node, unreachable nodes, open-ended nodes, nodes on the same spot, unlabelled assign/if.
// Target: 0 crossing, 0 unreachable, 0 open-ended, 0 overlapping, 0 unlabelled.
eSpace => {
    string report = "";
    string Check(OutSystems.Model.Logic.IAction act, string name) {
        var all = act.Nodes.Where(n0 => !(n0 is OutSystems.Model.Logic.Nodes.ICommentNode)).ToList();
        var comments = act.Nodes.OfType<OutSystems.Model.Logic.Nodes.ICommentNode>().ToList();
        int N = all.Count;
        double[] cx = new double[N]; double[] cy = new double[N];
        for (int q = 0; q < N; q++) { cx[q] = (all[q].HorizontalPosition - 2743) / 2286.0; cy[q] = (all[q].VerticalPosition - 1371) / 1829.0; }
        string NameOf(OutSystems.Model.Logic.Nodes.IActionNode n) {
            if (n is OutSystems.Model.Logic.Nodes.IAssignNode a) return "A[" + a.Label + "]";
            if (n is OutSystems.Model.Logic.Nodes.IIfNode i) return "If[" + i.Label + "]";
            if (n is OutSystems.Model.Logic.Nodes.IMessageNode) return "Msg";
            if (n is OutSystems.Model.Logic.Nodes.IEndNode) return "End";
            if (n is OutSystems.Model.Logic.Nodes.IStartNode) return "Start";
            if (n is OutSystems.Model.Logic.Nodes.IExceptionHandlerNode) return "Handler";
            if (n is OutSystems.Model.Logic.Nodes.IJavaScriptNode j) return "JS[" + j.Name + "]";
            if (n is OutSystems.Model.Logic.Nodes.IExecuteClientActionNode ce) return "Call[" + ce.Name + "]";
            if (n is OutSystems.Model.Logic.Nodes.IExecuteServerActionNode se) return "Call[" + se.Name + "]";
            return n.GetType().Name;
        }
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
        bool Near(double ax, double ay, double bx, double by, double px, double py) {
            double dx = bx - ax, dy = by - ay; double len2 = dx * dx + dy * dy;
            double t = len2 == 0 ? 0 : ((px - ax) * dx + (py - ay) * dy) / len2;
            if (t < 0) t = 0; if (t > 1) t = 1;
            double ex = px - (ax + t * dx), ey = py - (ay + t * dy);
            return ex * ex + ey * ey < 0.38 * 0.38;
        }
        string rep = "";
        int edges = 0, bad = 0, dummies = 0, openEnds = 0, overlap = 0;
        var seen = new bool[N]; var stack = new int[N * 4 + 8]; int sp = 0;
        for (int q = 0; q < N; q++) if (all[q] is OutSystems.Model.Logic.Nodes.IStartNode || all[q] is OutSystems.Model.Logic.Nodes.IExceptionHandlerNode) { seen[q] = true; stack[sp++] = q; }
        while (sp > 0) {
            int u = stack[--sp];
            var m = MainT(all[u]); var s2 = SideT(all[u]);
            if (m != null) { int k = all.IndexOf(m); if (k >= 0 && !seen[k]) { seen[k] = true; stack[sp++] = k; } }
            if (s2 != null) { int k = all.IndexOf(s2); if (k >= 0 && !seen[k]) { seen[k] = true; stack[sp++] = k; } }
        }
        int unreachable = 0; for (int q = 0; q < N; q++) if (!seen[q]) unreachable++;
        for (int p = 0; p < N; p++) {
            if (all[p] is OutSystems.Model.Logic.Nodes.IAssignNode da && !da.Assignments.Any()) dummies++;
            bool needs = !(all[p] is OutSystems.Model.Logic.Nodes.IEndNode) && !(all[p] is OutSystems.Model.Logic.Nodes.IRaiseExceptionNode);
            if (needs && MainT(all[p]) == null) { openEnds++; rep += "\n   open-ended: " + NameOf(all[p]); }
            for (int q = p + 1; q < N; q++) if (all[p].HorizontalPosition == all[q].HorizontalPosition && all[p].VerticalPosition == all[q].VerticalPosition) { overlap++; rep += "\n   same position: " + NameOf(all[p]) + " / " + NameOf(all[q]); }
            for (int slot = 0; slot < 2; slot++) {
                var T = slot == 0 ? MainT(all[p]) : SideT(all[p]);
                if (T == null) continue;
                int t = all.IndexOf(T);
                if (t < 0) continue;
                edges++;
                string hit = "";
                for (int q = 0; q < N; q++) {
                    if (q == p || q == t) continue;
                    if (Near(cx[p], cy[p], cx[t], cy[t], cx[q], cy[q])) hit += NameOf(all[q]) + " ";
                }
                if (hit != "") { bad++; rep += "\n   crossing: " + NameOf(all[p]) + " -> " + NameOf(T) + " crosses " + hit; }
            }
        }
        int unlabelled = all.Count(n0 => (n0 is OutSystems.Model.Logic.Nodes.IAssignNode an && (an.Label == null || an.Label == "")) || (n0 is OutSystems.Model.Logic.Nodes.IIfNode ifn && (ifn.Label == null || ifn.Label == "")));
        int noLinks = comments.Count(c0 => !c0.Connectors.Any());
        int noDesc = act.InputParameters.Count(p0 => p0.Description == null || p0.Description == "") + act.OutputParameters.Count(p0 => p0.Description == null || p0.Description == "") + act.LocalVariables.Count(p0 => p0.Description == null || p0.Description == "");
        if (act.Description == null || act.Description == "") rep += "\n   action has no Description";
        rep += noDesc > 0 ? "\n   " + noDesc + " input/output/local without Description" : "";
        return "\n" + name + ": " + N + " nodes (" + dummies + " dummy), " + comments.Count + " comments (" + noLinks + " unlinked), " + edges + " edges, " + bad + " crossing, " + unreachable + " unreachable, " + openEnds + " open-ended, " + overlap + " overlapping, " + unlabelled + " unlabelled assign/if" + rep;
    }
    // server actions:
    foreach (var nm in new[] { "ScrubText" }) report += Check(eSpace.ServerActions.Named(nm), nm);
    // screen actions of a block:
    // var blk = eSpace.MobileFlows.Named("MainFlow").Children.OfType<OutSystems.Model.UI.Mobile.IMobileBlock>().Named("<BlockName>");
    // foreach (var nm in new[] { "AgentSend" }) report += Check(blk.ScreenActions.Named(nm), nm);
    eSpace.ServerActions.Named("REPORT" + report);
}
