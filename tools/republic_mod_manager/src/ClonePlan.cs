using System;
using System.Collections.Generic;
using System.Linq;

namespace TesmioAutoload
{
    // What Buildings Plus will make of a donor's building.ini with the COMMANDS of a section, worked
    // out here so the editor can show it before the game does. This is a second implementation of
    // the rule in buildings_plus.cpp (ParseOp, BlockEndOf, CountBlock, PlanOps, ApplyOps) and must
    // not drift: the same donor and the same cases are checked in the plugin's own self-test
    // (TestCommands) and in CoreTests, and both sides carry this note.
    //
    // The rule, in full, and every command is judged against the UNCHANGED donor:
    //   add     = <block>                       in front of the closing "end"; must not be there yet
    //   replace = <old> | <block>               old exactly once, old and new must differ; one
    //                                           replace or remove per block
    //   remove  = <old>                         exactly once
    //   insert  = 0|1 | <anchor> | <block>      anchor exactly once, or produced by an EARLIER
    //                                           command of the section; "end" = the closing line
    // Fields are separated by "|". A block is a token line plus the lines that belong to it, one
    // field per line. <old> / <anchor>: the line to look for, then - optionally - the data lines
    // that must follow it (fields that do not begin with "$"); the new block starts at the first
    // later field that begins with "$". A block in the donor ends in front of a blank line, a "-"
    // or ";" line, the next token line or the closing "end". Lines are compared trimmed and case
    // sensitive. One command that does not fit and nothing is written at all. Order of the
    // commands: add, replace, remove, insert.
    public static class ClonePlan
    {
        static List<string> Fields(string value) { return (value ?? "").Split('|').Select(x => x.Trim()).ToList(); }
        static bool Begins(string text) { return text.Length > 1 && text[0] == '$'; }
        public static int BlockEnd(IList<string> lines, int at)
        {
            int end = at;
            while (end + 1 < lines.Count && DonorPlan.FirstToken(lines[end + 1]).Length == 0)
            {
                string bare = (lines[end + 1] ?? "").Trim();
                if (DonorPlan.EndsBlock(bare)) break;
                end++;
            }
            return end;
        }
        // The first line of the block a line belongs to: a data line belongs to the token above it.
        public static int BlockStart(IList<string> lines, int at)
        {
            if (at < 0 || at >= lines.Count || DonorPlan.FirstToken(lines[at]).Length > 0) return at;
            for (int i = at - 1; i >= 0; i--)
            {
                if (DonorPlan.FirstToken(lines[i]).Length > 0) return BlockEnd(lines, i) >= at ? i : at;
                string bare = (lines[i] ?? "").Trim();
                if (DonorPlan.EndsBlock(bare)) break;
            }
            return at;
        }
        public static int Count(IList<string> lines, IList<string> want, out int at)
        {
            at = -1; int count = 0;
            if (want == null || want.Count == 0) return 0;
            for (int i = 0; i < lines.Count; i++)
            {
                if ((lines[i] ?? "").Trim() != want[0]) continue;
                bool all = true;
                for (int k = 1; k < want.Count && all; k++) all = i + k < lines.Count && (lines[i + k] ?? "").Trim() == want[k];
                if (all) { count++; at = i; }
            }
            return count;
        }
        static void Parse(TargetCommand command, string value)
        {
            List<string> f = Fields(value);
            command.AnchorLines = new List<string>(); command.FreshLines = new List<string>();
            if (f.Any(x => x.Length == 0)) { command.Problem = "clone_problem_empty_field"; return; }
            int i = 0;
            if (command.Kind == "insert")
            {
                if (f.Count < 3 || (f[0] != "0" && f[0] != "1")) { command.Problem = "clone_problem_insert_form"; return; }
                command.After = f[0] == "1"; i = 1;
            }
            if (command.Kind == "add") command.FreshLines = f;
            else
            {
                command.AnchorLines.Add(f[i++]);
                while (i < f.Count && !Begins(f[i])) command.AnchorLines.Add(f[i++]);
                for (; i < f.Count; i++) command.FreshLines.Add(f[i]);
                bool closing = command.Kind == "insert" && command.AnchorLines.Count == 1 && command.AnchorLines[0] == "end";
                if (!closing && !Begins(command.AnchorLines[0])) { command.Problem = "target_problem_no_token"; }
            }
            command.Anchor = String.Join(" | ", command.AnchorLines); command.Value = String.Join(" | ", command.FreshLines);
            if (command.Problem.Length > 0) return;
            if (command.Kind == "remove")
            {
                if (command.FreshLines.Count > 0) command.Problem = "clone_problem_remove_second";
                return;
            }
            if (command.FreshLines.Count == 0) command.Problem = command.Kind == "replace" ? "clone_problem_replace_form" : "clone_problem_new_missing";
            else if (!Begins(command.FreshLines[0])) command.Problem = "target_problem_no_token";
        }
        public static TargetOutcome Check(IEnumerable<string> donor, IEnumerable<string> adds, IEnumerable<string> replaces,
            IEnumerable<string> removes, IEnumerable<string> inserts)
        {
            var lines = (donor ?? Enumerable.Empty<string>()).Select(x => x ?? "").ToList();
            var outcome = new TargetOutcome();
            foreach (string line in lines) outcome.Lines.Add(new TargetLineMark { Text = line });
            var kinds = new[] { "add", "replace", "remove", "insert" };
            var raw = new[] { adds, replaces, removes, inserts };
            for (int k = 0; k < 4; k++)
            {
                int index = 0;
                foreach (string entry in raw[k] ?? Enumerable.Empty<string>())
                {
                    string value = (entry ?? "").Trim();
                    if (value.Length == 0) continue;
                    var command = new TargetCommand { Kind = kinds[k], Index = index++ };
                    Parse(command, value);
                    outcome.Commands.Add(command);
                }
            }
            int n = lines.Count, lastEnd = -1;
            for (int i = 0; i < n; i++) if (lines[i].Trim() == "end") lastEnd = i;
            int tail = lastEnd >= 0 ? lastEnd : n;
            for (int oi = 0; oi < outcome.Commands.Count; oi++)
            {
                TargetCommand op = outcome.Commands[oi];
                if (op.Problem.Length > 0) continue;
                int where;
                if (op.Kind == "add")
                {
                    if (Count(lines, op.FreshLines, out where) != 0) op.Problem = "target_problem_exists";
                    else op.InsertAt = tail;
                    continue;
                }
                if (op.Kind == "insert" && op.AnchorLines.Count == 1 && op.AnchorLines[0] == "end")
                {
                    if (op.After) op.Problem = "target_problem_end_after";
                    else { op.InsertAt = tail; if (lastEnd >= 0) { op.At = lastEnd; op.End = lastEnd; outcome.Lines[lastEnd].Command = oi; } }
                    continue;
                }
                int count = Count(lines, op.AnchorLines, out where);
                if (count == 1)
                {
                    op.At = where; op.End = Math.Max(BlockEnd(lines, where), where + op.AnchorLines.Count - 1);
                    for (int i = op.At; i <= op.End && i < n; i++) outcome.Lines[i].Command = oi;
                    if (op.Kind == "insert") op.InsertAt = op.After ? op.End + 1 : op.At;
                    else if (op.Kind == "replace")
                    {
                        bool same = op.FreshLines.Count == op.End - op.At + 1;
                        for (int k = 0; k < op.FreshLines.Count && same; k++) same = lines[op.At + k].Trim() == op.FreshLines[k];
                        if (same) op.Problem = "target_problem_same";
                    }
                    // One block, one command: a second replace or remove of the same block would
                    // be passed over without a word when the commands are applied.
                    if (op.Problem.Length == 0 && op.Kind != "insert")
                        for (int i = 0; i < oi; i++)
                        {
                            TargetCommand other = outcome.Commands[i];
                            if (other.Problem.Length == 0 && other.At == op.At && (other.Kind == "replace" || other.Kind == "remove"))
                            { op.Problem = "clone_problem_taken"; break; }
                        }
                }
                else if (count > 1) op.Problem = Msg.Key("clone_problem_many", count);
                else
                {
                    // The block an earlier command writes has to begin with every field of the
                    // anchor, data lines included - and exactly one earlier command may do so.
                    int made = 0;
                    if (op.Kind == "insert")
                        for (int i = 0; i < oi; i++)
                        {
                            TargetCommand other = outcome.Commands[i];
                            if (other.Problem.Length > 0 || other.FreshLines.Count < op.AnchorLines.Count) continue;
                            if (!other.FreshLines.Take(op.AnchorLines.Count).SequenceEqual(op.AnchorLines)) continue;
                            if (made++ == 0) op.AnchorOp = i;
                        }
                    if (made > 1) { op.AnchorOp = -1; op.Problem = Msg.Key("clone_problem_made_many", made); }
                    else if (op.AnchorOp < 0) op.Problem = "target_problem_missing";
                    else op.FromOwn = true;
                }
            }
            return outcome;
        }
        // The file the plugin will write from the donor: the unchanged donor while anything is red.
        public static List<TargetResultLine> Result(TargetOutcome outcome, IList<string> lines)
        {
            var made = new List<TargetResultLine>();
            if (outcome == null) return made;
            if (outcome.Any) { foreach (TargetLineMark mark in outcome.Lines) made.Add(new TargetResultLine { Text = mark.Text }); return made; }
            var ops = outcome.Commands; int n = lines.Count;
            for (int i = 0; i < n; )
            {
                for (int pass = 0; pass < 2; pass++)
                    for (int oi = 0; oi < ops.Count; oi++)
                    {
                        TargetCommand op = ops[oi];
                        if (op.InsertAt != i || op.AnchorOp >= 0) continue;
                        if ((op.Kind == "insert" && op.After) != (pass == 0)) continue;
                        foreach (string text in op.FreshLines) made.Add(new TargetResultLine { Text = text, Command = oi });
                    }
                int action = -1;
                for (int oi = 0; oi < ops.Count && action < 0; oi++)
                    if (ops[oi].At == i && (ops[oi].Kind == "replace" || ops[oi].Kind == "remove")) action = oi;
                if (action < 0) { made.Add(new TargetResultLine { Text = lines[i] }); i++; continue; }
                if (ops[action].Kind == "replace") foreach (string text in ops[action].FreshLines) made.Add(new TargetResultLine { Text = text, Command = action });
                i = ops[action].End + 1;
            }
            for (int oi = 0; oi < ops.Count; oi++)
                if (ops[oi].InsertAt >= n && ops[oi].AnchorOp < 0)
                    foreach (string text in ops[oi].FreshLines) made.Add(new TargetResultLine { Text = text, Command = oi });
            for (int oi = 0; oi < ops.Count; oi++)
            {
                TargetCommand op = ops[oi];
                if (op.Kind != "insert" || op.AnchorOp < 0) continue;
                int first = 0;
                while (first < made.Count && made[first].Command != op.AnchorOp) first++;
                if (first >= made.Count) continue;
                int last = first;
                while (last + 1 < made.Count && made[last + 1].Command == op.AnchorOp) last++;
                int pos = op.After ? last + 1 : first;
                for (int k = 0; k < op.FreshLines.Count; k++) made.Insert(pos + k, new TargetResultLine { Text = op.FreshLines[k], Command = oi });
            }
            return made;
        }
        // The name field: the plugin writes one name line on top and takes the donor's own
        // $NAME lines out of what the commands left. Command -2 marks the line of the name field.
        public static List<TargetResultLine> Named(List<TargetResultLine> result, string nameLine)
        {
            if (String.IsNullOrEmpty(nameLine)) return result;
            var made = new List<TargetResultLine> { new TargetResultLine { Text = nameLine, Command = -2 } };
            bool dropping = false;
            foreach (TargetResultLine line in result)
            {
                string token = DonorPlan.FirstToken(line.Text);
                if (token.Length > 0) dropping = token.StartsWith("$NAME", StringComparison.Ordinal);
                else if (dropping)
                {
                    string bare = line.Text.Trim();
                    if (DonorPlan.EndsBlock(bare)) dropping = false;
                }
                if (!dropping) made.Add(line);
            }
            return made;
        }
        // Storage numbering of the finished file: $RESOURCE_VISUALIZATION <n> counts the $STORAGE*
        // lines from 0, and a shown storage must never sit behind water or sewage. The index of
        // every storage line comes back in `numbers` (-1 for every other line).
        public static List<string> Warnings(IList<TargetResultLine> result, out List<int> numbers)
        {
            var warnings = new List<string>(); numbers = new List<int>();
            int next = 0; var water = new List<int>();
            foreach (TargetResultLine line in result)
            {
                if (!DonorPlan.FirstToken(line.Text).StartsWith("$STORAGE", StringComparison.Ordinal)) { numbers.Add(-1); continue; }
                numbers.Add(next);
                if (line.Text.IndexOf("RESOURCE_TRANSPORT_WATER", StringComparison.OrdinalIgnoreCase) >= 0
                    || line.Text.IndexOf("RESOURCE_TRANSPORT_SEWAGE", StringComparison.OrdinalIgnoreCase) >= 0) water.Add(next);
                next++;
            }
            foreach (TargetResultLine line in result)
            {
                if (DonorPlan.FirstToken(line.Text) != "$RESOURCE_VISUALIZATION") continue;
                string[] parts = line.Text.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int want;
                if (parts.Length < 2 || !Int32.TryParse(parts[1], out want)) continue;
                if (want >= next) { string w = Msg.Key("donor_warn_no_storage", want, next); if (!warnings.Contains(w)) warnings.Add(w); continue; }
                if (water.Any(x => x < want)) { string w = Msg.Key("donor_warn_water_first", want); if (!warnings.Contains(w)) warnings.Add(w); }
            }
            return warnings;
        }
    }
}
