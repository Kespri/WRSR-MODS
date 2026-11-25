using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TesmioAutoload
{
    // The commands tab of a Vanilla Buildings rule set: the target file on the left, the commands
    // on the right, and below them what the selected command does to THAT file. A rule set can
    // carry many targets and every command is checked against each one separately - so the file is
    // picked at the top and the whole picture changes with it.
    //
    // Why it matters: a command whose anchor does not sit exactly once in the original is rejected
    // for that target at game start and only mentioned in the log. Here you click the line instead
    // of typing it, and a target where a command does not fit says so before the game runs.
    sealed class TargetLinesEditor : TableLayoutPanel
    {
        readonly Language language;
        readonly ComboBox targets = new ComboBox();
        readonly ListBox left = new ListBox(), commandList = new ListBox();
        readonly TextBox search = new TextBox(), edit = new TextBox(), picked = new TextBox();
        readonly Panel effect = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
        readonly Label count = new Label(), missing = new Label(), readOnly = new Label(), hint = new Label(), spreadNote = new Label();
        readonly Button originalView, resultView;
        // The bottom part says in three steps what a command is made of: the line picked in the
        // file (read only - it is the anchor, and it is clicked, never typed), the new line, and
        // the buttons that put the two together. A button is grey while its ingredients are
        // missing, and the hint line below says which.
        Button addButton, replaceButton, beforeButton, afterButton, removeButton;
        TableLayoutPanel foot;
        bool editable, syncing;
        const int FootBase = 262, PickedOne = 40, PickedBlockExtra = 24;
        readonly List<Button> editButtons = new List<Button>();
        readonly Func<string, List<string>> read;
        readonly List<string> add = new List<string>(), replace = new List<string>(), remove = new List<string>(), insert = new List<string>();
        readonly List<string> addLink = new List<string>(), replaceLink = new List<string>(), removeLink = new List<string>();
        readonly List<string> targetList = new List<string>();
        // Targets where at least one command does not fit: they lose the whole rule set.
        readonly HashSet<string> badTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, List<string>> otherFiles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        List<ConnectionBlock> blocks = new List<ConnectionBlock>();
        List<string> lines = new List<string>();
        TargetOutcome outcome;
        List<TargetResultLine> result = new List<TargetResultLine>();
        List<int> shown = new List<int>();
        bool showResult, fileHere;

        public IEnumerable<string> Add { get { return add; } }
        public IEnumerable<string> Replace { get { return replace; } }
        public IEnumerable<string> Remove { get { return remove; } }
        public IEnumerable<string> Insert { get { return insert; } }
        public IEnumerable<string> AddLink { get { return addLink; } }
        public IEnumerable<string> ReplaceLink { get { return replaceLink; } }
        public IEnumerable<string> RemoveLink { get { return removeLink; } }
        public Action Changed;
        bool ready;

        public TargetLinesEditor(Language language, IEnumerable<string> targetFiles, Func<string, List<string>> readTarget,
            IEnumerable<string> adds, IEnumerable<string> replaces, IEnumerable<string> removes, IEnumerable<string> inserts, Font font)
            : this(language, targetFiles, readTarget, adds, replaces, removes, inserts, null, null, null, font) { }
        public TargetLinesEditor(Language language, IEnumerable<string> targetFiles, Func<string, List<string>> readTarget,
            IEnumerable<string> adds, IEnumerable<string> replaces, IEnumerable<string> removes, IEnumerable<string> inserts,
            IEnumerable<string> addLinks, IEnumerable<string> replaceLinks, IEnumerable<string> removeLinks, Font font)
            : this(language, targetFiles, readTarget, adds, replaces, removes, inserts, addLinks, replaceLinks, removeLinks, font, false, null) { }
        // clone = the lines tab of a Buildings Plus building: one "target" - the donor's
        // building.ini - and the plugin's own command rule (ClonePlan): blocks of several lines
        // are ordinary commands there, and one command that does not fit stops the building.
        // nameLine gives the $NAME line the name field writes, or "".
        public TargetLinesEditor(Language language, IEnumerable<string> targetFiles, Func<string, List<string>> readTarget,
            IEnumerable<string> adds, IEnumerable<string> replaces, IEnumerable<string> removes, IEnumerable<string> inserts,
            IEnumerable<string> addLinks, IEnumerable<string> replaceLinks, IEnumerable<string> removeLinks, Font font, bool clone, Func<string> nameLine)
        {
            this.clone = clone; this.nameLine = nameLine; footBase = FootBase;
            this.language = language; this.read = readTarget;
            targetList.AddRange((targetFiles ?? Enumerable.Empty<string>()).Select(x => (x ?? "").Trim()).Where(x => x.Length > 0));
            add.AddRange(Clean(adds)); replace.AddRange(Clean(replaces)); remove.AddRange(Clean(removes)); insert.AddRange(Clean(inserts));
            addLink.AddRange(Clean(addLinks)); replaceLink.AddRange(Clean(replaceLinks)); removeLink.AddRange(Clean(removeLinks));
            Dock = DockStyle.Fill; ColumnCount = 2; RowCount = 4; Font = font; BackColor = Color.White;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52)); ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            RowStyles.Add(new RowStyle(SizeType.Percent, 100)); RowStyles.Add(new RowStyle(SizeType.Absolute, footBase));

            var head = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
            head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            // The same two views as the donor editor: the unchanged file, and what the game will
            // read once the commands have run.
            var views = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoSize = true, Margin = new Padding(0, 0, 12, 0) };
            originalView = Chip(language.T(clone ? "donor_view_donor" : "target_view_original"), () => { showResult = false; Refill(); });
            resultView = Chip(language.T("target_view_result"), () => { showResult = true; Refill(); });
            views.Controls.Add(originalView); views.Controls.Add(resultView);
            head.Controls.Add(views, 0, 0);
            targets.DropDownStyle = ComboBoxStyle.DropDownList; targets.AccessibleName = "target:file";
            // Drawn here instead of by Fields.Tall: a target that loses the rule set stands red
            // with an x in front, in the closed box and in the open list.
            targets.Font = Fields.Font; targets.Height = Fields.Height; targets.DrawMode = DrawMode.OwnerDrawFixed; targets.ItemHeight = Fields.Height - 6;
            targets.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index >= 0)
                {
                    string file = targets.Items[e.Index].ToString();
                    bool bad = badTargets.Contains(file), selected = (e.State & DrawItemState.Selected) != 0;
                    TextRenderer.DrawText(e.Graphics, (bad ? "x  " : "") + file, targets.Font, new Rectangle(e.Bounds.X + 3, e.Bounds.Y, e.Bounds.Width - 3, e.Bounds.Height),
                        bad && !selected ? Color.FromArgb(150, 28, 28) : e.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
                e.DrawFocusRectangle();
            };
            foreach (string file in targetList) targets.Items.Add(file);
            if (targets.Items.Count > 0) targets.SelectedIndex = 0;
            // The box is narrow and the paths are long: the open list is as wide as its longest entry.
            int widest = targets.Width;
            foreach (string file in targetList) widest = Math.Max(widest, TextRenderer.MeasureText("x  " + file, Fields.Font).Width + 28);
            targets.DropDownWidth = Math.Min(900, widest);
            targets.SelectedIndexChanged += (s, e) => { if (ready) Recompute(); };
            targets.Margin = new Padding(0, 0, 12, 0); targets.Dock = DockStyle.Fill;
            head.Controls.Add(targets, 1, 0);
            search.BorderStyle = BorderStyle.None; search.Font = Fields.Font; search.AccessibleName = "target:search";
            var wrapped = Fields.Wrap(search); wrapped.Width = 200; wrapped.Margin = Padding.Empty;
            search.TextChanged += (s, e) => Refill();
            head.Controls.Add(wrapped, 2, 0);
            Controls.Add(head, 0, 0);

            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(14, 0, 0, 0) };
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); right.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var title = Theme.Label(language.T("target_commands"), 10, true); title.Anchor = AnchorStyles.Left;
            count.AutoSize = true; count.ForeColor = Theme.Muted; count.Font = new Font("Segoe UI", 9); count.Anchor = AnchorStyles.Right; count.AccessibleName = "target:count";
            right.Controls.Add(title, 0, 0); right.Controls.Add(count, 1, 0);
            Controls.Add(right, 1, 0);

            missing.AutoSize = true; missing.Font = new Font("Segoe UI", 9, FontStyle.Bold); missing.ForeColor = Color.FromArgb(150, 28, 28);
            missing.Margin = new Padding(2, 4, 0, 4); missing.Visible = false; missing.AccessibleName = "target:missing";
            readOnly.AutoSize = true; readOnly.Font = new Font("Segoe UI", 9, FontStyle.Bold); readOnly.ForeColor = Color.FromArgb(139, 81, 0);
            readOnly.Margin = new Padding(2, 4, 0, 4); readOnly.Visible = false; readOnly.AccessibleName = "target:read-only";
            var notes = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
            spreadNote.AutoSize = true; spreadNote.Font = new Font("Segoe UI", 9, FontStyle.Bold); spreadNote.ForeColor = Color.FromArgb(150, 28, 28);
            spreadNote.Margin = new Padding(2, 4, 12, 4); spreadNote.Visible = false; spreadNote.UseMnemonic = false; spreadNote.AccessibleName = "target:spread";
            notes.Controls.Add(missing); notes.Controls.Add(readOnly); notes.Controls.Add(spreadNote);
            SetColumnSpan(notes, 2); Controls.Add(notes, 0, 1);

            Style(left, "target:lines"); Style(commandList, "target:list");
            // A line one of the commands touches is marked; the file itself is never changed here.
            left.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index < 0 || e.Index >= shown.Count) return;
                bool selected = (e.State & DrawItemState.Selected) != 0;
                bool touched = CommandOf(shown[e.Index]) >= 0;
                Color colour = selected ? Color.White : touched ? Color.FromArgb(0, 92, 175) : Theme.Ink;
                using (var glyph = new Font("Consolas", 10, touched ? FontStyle.Bold : FontStyle.Regular))
                    TextRenderer.DrawText(e.Graphics, LineLabel(shown[e.Index]), glyph, e.Bounds, colour, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
            };
            commandList.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index < 0 || e.Index >= outcome.Commands.Count) return;
                TargetCommand command = outcome.Commands[e.Index];
                bool selected = (e.State & DrawItemState.Selected) != 0;
                Color colour = selected ? Color.White : command.Problem.Length > 0 ? Color.FromArgb(150, 28, 28) : Theme.Ink;
                using (var glyph = new Font("Consolas", 10))
                    TextRenderer.DrawText(e.Graphics, CommandLabel(e.Index), glyph, e.Bounds, colour, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
            };
            left.SelectedIndexChanged += (s, e) => ShowLeft();
            left.DoubleClick += (s, e) => CopyPicked();
            commandList.SelectedIndexChanged += (s, e) => ShowCommand();
            Controls.Add(left, 0, 2);

            var rightSplit = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(14, 0, 0, 0) };
            rightSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 54)); rightSplit.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); rightSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
            rightSplit.Controls.Add(commandList, 0, 0);
            var order = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 4, 0, 4) };
            order.Controls.Add(Small(language.T("donor_up"), () => Shift(-1), "target:up"));
            order.Controls.Add(Small(language.T("donor_down"), () => Shift(1), "target:down"));
            order.Controls.Add(Small(language.T("target_drop"), Drop, "target:drop"));
            rightSplit.Controls.Add(order, 0, 1);
            var box = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            box.Controls.Add(effect); rightSplit.Controls.Add(box, 0, 2);
            Controls.Add(rightSplit, 1, 2);

            // Rows: caption, picked line + its Remove button, caption, new line, the four buttons
            // that need a new line, the hint.
            foot = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Margin = new Padding(0, 8, 0, 0) };
            foot.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); foot.RowStyles.Add(new RowStyle(SizeType.Absolute, PickedOne));
            foot.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); foot.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            foot.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); foot.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            foot.Controls.Add(Caption(language.T("target_picked_caption")), 0, 0);
            var pickedRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            pickedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); pickedRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            // Read only: the anchor is always a click in the file. Multiline, because a connection
            // is picked as a block of up to three lines.
            picked.Font = new Font("Consolas", 10); picked.Dock = DockStyle.Fill; picked.Multiline = true; picked.WordWrap = false; picked.ReadOnly = true;
            picked.TabStop = false; picked.BackColor = Theme.Pale; picked.BorderStyle = BorderStyle.FixedSingle; picked.Margin = new Padding(3, 3, 8, 3);
            picked.AccessibleName = "target:picked";
            pickedRow.Controls.Add(picked, 0, 0);
            removeButton = Wide(language.T("target_remove_picked"), MakeRemove, "target:make-remove"); removeButton.Margin = new Padding(0, 0, 0, 3);
            pickedRow.Controls.Add(removeButton, 1, 0);
            foot.Controls.Add(pickedRow, 0, 1);
            var freshHead = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            freshHead.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); freshHead.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            freshHead.Controls.Add(Caption(language.T(clone ? "donor_new_caption" : "target_new_caption")), 0, 0);
            // The box takes several lines at once, one command each - the caption says so,
            // because nothing else on the box does.
            {
                var freshNote = Theme.Label(language.T(clone ? "clone_new_note" : "target_new_note"), 9, false); freshNote.ForeColor = Theme.Muted;
                freshNote.Margin = new Padding(8, 2, 0, 0); freshNote.Anchor = AnchorStyles.Left | AnchorStyles.Bottom; freshNote.AccessibleName = "target:new-note";
                freshHead.Controls.Add(freshNote, 1, 0);
            }
            foot.Controls.Add(freshHead, 0, 2);
            edit.Font = new Font("Consolas", 10); edit.Dock = DockStyle.Fill; edit.Multiline = true; edit.ScrollBars = ScrollBars.Vertical; edit.AccessibleName = "target:edit";
            edit.TextChanged += (s, e) => { if (!settingEdit) editFromCommand = false; UpdateActions(); };
            foot.Controls.Add(edit, 0, 3);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 6, 0, 0) };
            actions.Controls.Add(addButton = Wide(language.T("target_add"), MakeAdd, "target:make-add"));
            actions.Controls.Add(replaceButton = Wide(language.T("target_replace"), MakeReplace, "target:make-replace"));
            actions.Controls.Add(beforeButton = Wide(language.T("target_before"), () => MakeInsert(false), "target:make-before"));
            actions.Controls.Add(afterButton = Wide(language.T("target_after"), () => MakeInsert(true), "target:make-after"));
            foot.Controls.Add(actions, 0, 4);
            hint.AutoSize = false; hint.Dock = DockStyle.Fill; hint.Font = new Font("Segoe UI", 9); hint.ForeColor = Theme.Muted; hint.UseMnemonic = false;
            hint.Margin = new Padding(2, 0, 0, 0); hint.AccessibleName = "target:hint";
            foot.Controls.Add(hint, 0, 5);
            // A clone has one file, the donor's: nothing to choose and nothing to check elsewhere.
            if (clone) targets.Enabled = false;
            SetColumnSpan(foot, 2); Controls.Add(foot, 0, 3);

            Recompute(); ready = true;
        }
        static IEnumerable<string> Clean(IEnumerable<string> raw)
        { return (raw ?? Enumerable.Empty<string>()).Select(x => (x ?? "").Trim()).Where(x => x.Length > 0); }
        void Style(ListBox list, string name)
        {
            list.Dock = DockStyle.Fill; list.BorderStyle = BorderStyle.FixedSingle; list.Font = new Font("Consolas", 10);
            list.IntegralHeight = false; list.DrawMode = DrawMode.OwnerDrawFixed; list.ItemHeight = 18;
            list.HorizontalScrollbar = true; list.AccessibleName = name;
        }
        Button Chip(string text, Action action)
        {
            var button = Theme.Button(text, action, false);
            button.Height = 30; button.MinimumSize = Size.Empty; button.Margin = new Padding(0, 0, 6, 0);
            button.AccessibleName = "target:view:" + text;
            return button;
        }
        Button Small(string text, Action action, string name)
        {
            var button = Theme.Button(text, action, false);
            button.MinimumSize = new Size(0, 32); button.Margin = new Padding(0, 0, 6, 0); button.AccessibleName = name;
            editButtons.Add(button); return button;
        }
        Button Wide(string text, Action action, string name)
        {
            var button = Theme.Button(text, action, false);
            button.MinimumSize = new Size(0, Fields.Height); button.Margin = new Padding(0, 0, 8, 0); button.AccessibleName = name; Theme.DimWhenOff(button);
            editButtons.Add(button); return button;
        }
        static Label Caption(string text)
        {
            var label = Theme.Label(text, 9, true); label.Margin = new Padding(2, 2, 0, 0); label.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            return label;
        }

        // ---- what is picked, what is typed, and what that allows ----
        // The picked box shows the anchor; nothing reads it back - Picked and PickedBlock stay the
        // source. Empty, it says what it is waiting for.
        void ShowPicked()
        {
            ConnectionBlock block = PickedBlock;
            string text = block != null ? BlockText(block) : Picked;
            bool none = text.Length == 0;
            picked.Text = none ? (editable ? language.T("target_picked_none") : "") : text;
            picked.ForeColor = none ? Theme.Muted : Theme.Ink;
            // A block of a clone can be longer than a connection: three lines show, more scroll.
            picked.ScrollBars = block != null && block.End - block.Start > 2 ? ScrollBars.Vertical : ScrollBars.None;
            int extra = block != null ? PickedBlockExtra : 0;
            if (foot != null && (int)foot.RowStyles[1].Height != PickedOne + extra)
            { foot.RowStyles[1].Height = PickedOne + extra; RowStyles[3].Height = footBase + extra; }
        }
        // True while the box still holds the untouched copy of the picked line: nothing new yet.
        bool TypedIsPicked
        {
            get
            {
                if (clone) { List<string> have = PickedLines; return have.Count > 0 && have.SequenceEqual(TypedLines); }
                ConnectionBlock block = PickedBlock;
                if (block != null) return TypedIsLink && TypedFields == BlockFields(block);
                return Picked.Length > 0 && Typed == Picked;
            }
        }
        // ---- clone mode: every block is an ordinary command ----
        readonly bool clone;
        readonly Func<string> nameLine;
        readonly int footBase;
        List<int> storageNumbers = new List<int>();
        List<string> cloneWarnings = new List<string>();
        // Every token line that has data lines below it, as a block from Start to End.
        static List<ConnectionBlock> CloneBlocks(IList<string> lines)
        {
            var found = new List<ConnectionBlock>();
            for (int i = 0; i < lines.Count; i++)
            {
                string token = TargetPlan.Token(lines[i]);
                if (token.Length == 0) continue;
                int end = ClonePlan.BlockEnd(lines, i);
                if (end > i) { found.Add(new ConnectionBlock { Start = i, End = end, Token = token }); i = end; }
            }
            return found;
        }
        // The picked line, or the picked block line by line - trimmed, as the plugin compares them.
        List<string> PickedLines
        {
            get
            {
                ConnectionBlock block = PickedBlock;
                if (block == null) return Picked.Length > 0 ? new List<string> { Picked } : new List<string>();
                var rows = new List<string>();
                for (int i = block.Start; i <= block.End && i < outcome.Lines.Count; i++) rows.Add(outcome.Lines[i].Text.Trim());
                return rows;
            }
        }
        List<string> TypedLines { get { return Typed.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToList(); } }
        // The anchor of a command: as few lines of the picked block as tell it apart from every
        // other place in the donor - mostly just its first line.
        string CloneAnchor()
        {
            List<string> rows = PickedLines;
            for (int take = 1; take < rows.Count; take++)
            {
                int at;
                if (ClonePlan.Count(lines, rows.Take(take).ToList(), out at) == 1) return String.Join(" | ", rows.Take(take));
            }
            return String.Join(" | ", rows);
        }
        static bool BeginsToken(string text) { return text.Length > 1 && text[0] == '$'; }
        void UpdateCloneActions()
        {
            List<string> have = PickedLines, fresh = TypedLines;
            bool on = editable, closing = have.Count == 1 && have[0] == "end";
            bool anchor = have.Count > 0 && (BeginsToken(have[0]) || closing), typed = fresh.Count > 0, token = typed && BeginsToken(fresh[0]);
            bool same = TypedIsPicked, ready = typed && token && !same;
            List<List<string>> typedBlocks = TypedBlocks;
            bool many = typedBlocks.Count > 1, firstSame = many && typedBlocks[0].SequenceEqual(have);
            addButton.Enabled = on && ready;
            replaceButton.Enabled = on && anchor && !closing && ready && !firstSame;
            beforeButton.Enabled = on && anchor && ready;
            afterButton.Enabled = on && anchor && !closing && ready;
            removeButton.Enabled = on && anchor && !closing;
            hint.Text = !on ? "" : language.T(
                typed && !token ? "clone_hint_need_token"
                : have.Count > 0 && !anchor ? "clone_hint_no_anchor"
                : have.Count == 0 ? (typed ? (many ? "target_hint_many_typed" : "target_hint_typed") : "target_hint_start")
                : closing ? "clone_hint_end"
                : !typed || same ? "target_hint_picked" : many ? "target_hint_many_ready" : "target_hint_ready");
        }
        // A rule set writes exactly one line per command (a connection aside). Several lines
        // typed below each other are therefore several commands - never one glued line, which is
        // how a command with the anchor in front of the new line once got into a rule set.
        bool TypedIsMany { get { return !clone && !TypedIsLink && TypedLines.Count > 1; } }
        void UpdateActions()
        {
            if (addButton == null || outcome == null) return;
            if (clone) { UpdateCloneActions(); return; }
            bool on = editable, has = Picked.Length > 0, block = PickedBlock != null, typed = Typed.Length > 0, same = TypedIsPicked, link = TypedIsLink, many = TypedIsMany;
            // Replace swaps the picked line for the FIRST typed one, so that one has to differ.
            bool firstSame = many && has && TypedLines[0] == Picked;
            addButton.Enabled = on && typed && !same;
            replaceButton.Enabled = on && has && typed && !same && !firstSame && (!block || link);
            beforeButton.Enabled = afterButton.Enabled = on && has && !block && typed && !same && !link;
            removeButton.Enabled = on && has;
            hint.Text = !on ? "" : language.T(many && !block ? (has ? "target_hint_many_ready" : "target_hint_many_typed") : HintKey(has, block, typed, same, link));
        }
        static string HintKey(bool has, bool block, bool typed, bool same, bool link)
        {
            if (!has) return !typed ? "target_hint_start" : link ? "target_hint_typed_link" : "target_hint_typed";
            if (block) return typed && !same && link ? "target_hint_link_ready" : "target_hint_link_picked";
            if (!typed || same) return "target_hint_picked";
            return link ? "target_link_no_insert" : "target_hint_ready";
        }

        public string Target { get { return targets.SelectedItem == null ? "" : targets.SelectedItem.ToString(); } }
        void Recompute()
        {
            List<string> file = null;
            try { if (Target.Length > 0 && read != null) file = read(Target); } catch (Exception) { }
            lines = file ?? new List<string>();
            fileHere = file != null;
            missing.Text = language.Format(clone ? "clone_donor_missing" : "target_file_missing", Target);
            missing.Visible = file == null;
            if (clone)
            {
                outcome = ClonePlan.Check(lines, add, replace, remove, insert);
                result = ClonePlan.Named(ClonePlan.Result(outcome, lines), nameLine == null ? "" : nameLine());
                blocks = CloneBlocks(lines);
                cloneWarnings = outcome.Any ? new List<string>() : ClonePlan.Warnings(result, out storageNumbers);
                if (outcome.Any) storageNumbers = new List<int>();
            }
            else
            {
                outcome = TargetPlan.Check(lines, add, replace, remove, insert, addLink, replaceLink, removeLink);
                result = TargetPlan.Result(outcome, lines);
                blocks = TargetPlan.Connections(lines);
            }
            CheckEverywhere();
            int bad = outcome.Commands.Count(x => x.Problem.Length > 0);
            count.Text = bad > 0 ? language.Format("target_count_bad", outcome.Commands.Count, bad) : language.Format("target_count", outcome.Commands.Count);
            count.ForeColor = bad > 0 ? Color.FromArgb(150, 28, 28) : Theme.Muted;
            Refill(); FillCommands(); ShowEffect(-1);
            if (ready && Changed != null) Changed();
        }
        // Every target file of the rule set is checked, always. Not to apply anything - the
        // commands of a rule set go to every target anyway - but to say where one of them does not
        // fit, because THAT target then loses the whole rule set and the game only writes it into
        // its log. The other files are read once per window.
        readonly Dictionary<string, int> fits = new Dictionary<string, int>();
        readonly Dictionary<string, string> firstBad = new Dictionary<string, string>();
        int checkedFiles; bool spreadOn;
        void CheckEverywhere()
        {
            fits.Clear(); firstBad.Clear(); badTargets.Clear(); checkedFiles = 0;
            if (clone) return;
            foreach (string file in targetList)
            {
                List<string> other;
                if (!otherFiles.TryGetValue(file, out other))
                {
                    other = null;
                    try { if (read != null) other = read(file); } catch (Exception) { }
                    otherFiles[file] = other;
                }
                if (other == null) continue;
                checkedFiles++;
                TargetOutcome result = TargetPlan.Check(other, add, replace, remove, insert, addLink, replaceLink, removeLink);
                if (result.Any) badTargets.Add(file);
                foreach (TargetCommand command in result.Commands)
                {
                    string key = command.Kind + ":" + command.Index;
                    if (command.Problem.Length == 0) { int had; fits[key] = (fits.TryGetValue(key, out had) ? had : 0) + 1; }
                    else if (!firstBad.ContainsKey(key)) firstBad[key] = file;
                }
            }
            // The one line nobody can overlook: how many targets the rule set reaches at all.
            spreadNote.Text = language.Format("target_spread_rule", checkedFiles - badTargets.Count, checkedFiles);
            spreadOn = badTargets.Count > 0 && checkedFiles > 1; spreadNote.Visible = spreadOn;
            targets.Invalidate();
        }
        void Refill()
        {
            originalView.BackColor = showResult ? Color.White : Theme.SelectionBlue; originalView.ForeColor = showResult ? Theme.Ink : Color.White;
            resultView.BackColor = showResult ? Theme.SelectionBlue : Color.White; resultView.ForeColor = showResult ? Color.White : Theme.Ink;
            SetEditable(!showResult);
            string filter = search.Text.Trim();
            shown = Enumerable.Range(0, SourceCount)
                .Where(i => filter.Length == 0 || TextOf(i).IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
            left.BeginUpdate(); left.Items.Clear();
            foreach (int i in shown) left.Items.Add(LineLabel(i));
            left.EndUpdate(); Extent(left, shown.Select(LineLabel));
            // Refilling the list drops its selection without an event.
            ShowPicked(); UpdateActions();
        }
        // Both views feed the same list: the unchanged file, or the file the game will read.
        int SourceCount { get { return showResult ? result.Count : outcome.Lines.Count; } }
        string TextOf(int index)
        { return showResult ? result[index].Text : outcome.Lines[index].Text; }
        int CommandOf(int index)
        { return showResult ? result[index].Command : outcome.Lines[index].Command; }
        string LineLabel(int index)
        {
            int command = CommandOf(index);
            string prefix = command < 0 ? "   "
                : showResult ? (outcome.Commands[command].Kind.StartsWith("replace", StringComparison.Ordinal) ? ">  " : "+  ")
                : Mark(outcome.Commands[command].Kind) + "  ";
            return prefix + TextOf(index) + (clone && showResult && index < storageNumbers.Count && storageNumbers[index] >= 0 ? "      " + language.Format("donor_storage", storageNumbers[index]) : "");
        }
        static string Mark(string kind)
        { return kind == "replace" ? ">" : kind == "remove" ? "-" : kind == "insert" ? "+" : " "; }
        // The result view only shows what will be written. Everything that would change something
        // is switched off there - a disabled button cannot report a wrong reason.
        void SetEditable(bool on)
        {
            editable = on && fileHere;
            foreach (Button button in editButtons) button.Enabled = editable;
            edit.ReadOnly = !on; edit.BackColor = on ? Color.White : Theme.Pale;
            readOnly.Text = outcome != null && outcome.Any ? language.T(clone ? "clone_result_blocked" : "target_result_blocked") : language.T("target_view_read_only");
            readOnly.ForeColor = outcome != null && outcome.Any ? Color.FromArgb(150, 28, 28) : Color.FromArgb(139, 81, 0);
            readOnly.Visible = !on;
            // Own flags, not Visible: that answers false for every control of a window not shown yet.
            RowStyles[1].Height = !fileHere || !on || spreadOn ? 26 : 0;
        }
        void FillCommands()
        {
            commandList.BeginUpdate(); commandList.Items.Clear();
            for (int i = 0; i < outcome.Commands.Count; i++) commandList.Items.Add(CommandLabel(i));
            commandList.EndUpdate(); Extent(commandList, Enumerable.Range(0, outcome.Commands.Count).Select(CommandLabel));
        }
        string CommandLabel(int index)
        {
            TargetCommand command = outcome.Commands[index];
            return (command.Problem.Length > 0 ? "x  " : "   ") + language.T("target_kind_" + command.Kind) + "  " + command.Text;
        }
        static void Extent(ListBox list, IEnumerable<string> texts)
        {
            int width = 0;
            using (var font = new Font("Consolas", 10))
                foreach (string text in texts) width = Math.Max(width, TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding).Width);
            list.HorizontalExtent = width + 12;
        }

        // A connection is a block: clicking any of its lines picks the whole thing, exactly as the
        // plugin treats it, and the text box then holds token and points together.
        ConnectionBlock BlockAt(int line)
        { return blocks.FirstOrDefault(b => line >= b.Start && line <= b.End); }
        string BlockText(ConnectionBlock block)
        {
            var parts = new List<string>();
            for (int i = block.Start; i <= block.End && i < outcome.Lines.Count; i++) parts.Add(outcome.Lines[i].Text.Trim());
            return String.Join(Environment.NewLine, parts);
        }
        void ShowLeft()
        {
            // Picking a command picks its anchor in the file as well; that is not a click of the user.
            if (syncing) return;
            if (left.SelectedIndex < 0 || left.SelectedIndex >= shown.Count) { ShowPicked(); UpdateActions(); return; }
            commandList.ClearSelected();
            int at = shown[left.SelectedIndex];
            // In the result view a line is only read: it says which command put it there.
            if (showResult) { ShowEffect(CommandOf(at)); ShowPicked(); UpdateActions(); return; }
            // The picked line shows in the read-only box and nowhere else: the new-line box is for
            // the NEW line and keeps what the user typed. Only a text that a picked command put
            // there is taken out again - it belonged to that command, not to this line.
            if (editFromCommand) SetEdit("", false);
            ShowPicked(); UpdateActions();
            ShowEffect(outcome.Lines[at].Command);
        }
        // Double-click copies the picked line (or connection block) into the new-line box: for a
        // Replace that only changes a number nobody should have to type the whole line again.
        void CopyPicked()
        {
            if (!editable || showResult) return;
            ConnectionBlock block = PickedBlock;
            string text = block != null ? BlockText(block) : Picked;
            if (text.Length == 0) return;
            SetEdit(text, false); edit.SelectionStart = edit.TextLength; edit.Focus();
        }
        bool editFromCommand, settingEdit;
        void SetEdit(string text, bool fromCommand)
        {
            settingEdit = true;
            try { edit.Text = text; } finally { settingEdit = false; }
            editFromCommand = fromCommand;
            UpdateActions();
        }
        void ShowCommand()
        {
            if (commandList.SelectedIndex < 0 || commandList.SelectedIndex >= outcome.Commands.Count) return;
            TargetCommand command = outcome.Commands[commandList.SelectedIndex];
            // A command shows what it is made of: its anchor is picked in the file again (and
            // scrolled into view), its new line stands in the box below.
            syncing = true;
            try
            {
                int row = !showResult && command.At >= 0 && !command.FromOwn ? shown.IndexOf(command.At) : -1;
                if (row >= 0) left.SelectedIndex = row; else left.ClearSelected();
            }
            finally { syncing = false; }
            SetEdit(clone ? String.Join(Environment.NewLine, command.FreshLines ?? new List<string>()) : command.Value, true);
            ShowPicked(); UpdateActions();
            ShowEffect(commandList.SelectedIndex);
        }
        // A problem is a language key, plain or with arguments (Msg.Key). Localize alone only
        // translates the second kind and showed the first as its raw key.
        string Said(string problem) { return Msg.IsKey(problem) ? language.Localize(problem) : language.T(problem); }
        // What the selected command does to THIS target, in words.
        void ShowEffect(int index)
        {
            Theme.DisposeChildren(effect);
            var shell = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Padding = new Padding(10, 8, 10, 10), BackColor = Color.White };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            effect.Controls.Add(shell);
            if (clone) foreach (string warning in cloneWarnings) { Label amber = Note(Said(warning), Color.FromArgb(139, 81, 0)); amber.AccessibleName = "target:warning"; AddLine(shell, amber); }
            if (index < 0 || index >= outcome.Commands.Count) { AddLine(shell, Note(language.T(clone ? "clone_pick" : "target_pick"), Theme.Muted)); return; }
            TargetCommand command = outcome.Commands[index];
            if (command.Problem.Length > 0)
            {
                AddLine(shell, Note(Said(command.Problem), Color.FromArgb(150, 28, 28)));
                AddLine(shell, Note(clone ? language.T("clone_problem_where") : language.Format("target_problem_where", Target), Theme.Muted));
                return;
            }
            if (command.Kind == "add" || command.Kind == "add_connection") AddLine(shell, Note(language.T("target_effect_add"), Theme.Ink));
            else if (command.FromOwn) AddLine(shell, Note(language.T("target_effect_own"), Theme.Ink));
            else
            {
                AddLine(shell, Note(language.Format("target_effect_at", command.At + 1), Theme.Ink));
                AddLine(shell, Note(outcome.Lines[command.At].Text.Trim(), Theme.Muted));
            }
            Spread(shell, command);
        }
        // How many of the rule set's targets this command fits.
        void Spread(TableLayoutPanel shell, TargetCommand command)
        {
            if (clone || checkedFiles < 2) return;
            string key = command.Kind + ":" + command.Index;
            int good; fits.TryGetValue(key, out good);
            bool all = good >= checkedFiles;
            AddLine(shell, Note(language.Format(all ? "target_spread_all" : "target_spread_some", good, checkedFiles),
                all ? Theme.Ink : Color.FromArgb(150, 28, 28)));
            string bad;
            if (!all && firstBad.TryGetValue(key, out bad)) AddLine(shell, Note(language.Format("target_spread_first", bad), Theme.Muted));
        }
        Label Note(string text, Color colour)
        {
            var label = Theme.Label(text, 9, false); label.ForeColor = colour;
            label.MaximumSize = new Size(Math.Max(240, effect.ClientSize.Width - 30), 0); label.Margin = new Padding(0, 0, 0, 6);
            return label;
        }
        static void AddLine(TableLayoutPanel shell, Control control)
        { int row = shell.RowCount++; shell.RowStyles.Add(new RowStyle(SizeType.AutoSize)); control.Dock = DockStyle.Top; shell.Controls.Add(control, 0, row); }

        string Typed { get { return edit.Text.Replace("\r\n", "\n").Trim(); } }
        string Picked
        {
            get { return !showResult && left.SelectedIndex >= 0 && left.SelectedIndex < shown.Count ? outcome.Lines[shown[left.SelectedIndex]].Text.Trim() : ""; }
        }
        ConnectionBlock PickedBlock
        { get { return !showResult && left.SelectedIndex >= 0 && left.SelectedIndex < shown.Count ? BlockAt(shown[left.SelectedIndex]) : null; } }
        // A connection is written as one command field: token | point [| point].
        static string BlockFields(ConnectionBlock block)
        {
            string text = block.Token + " | " + Numbers(block.First);
            if (block.Points == 2) text += " | " + Numbers(block.Second);
            return text;
        }
        static string Numbers(double[] point)
        { return point == null ? "" : String.Join(" ", point.Select(x => x.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture))); }
        // What the text box holds, as command fields: a block becomes "token | point [| point]",
        // a single line stays a single line.
        string TypedFields
        {
            get
            {
                var rows = Typed.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                if (rows.Count == 0) return "";
                string token = TargetPlan.Token(rows[0]);
                if (!TargetPlan.ConnectionToken(token)) return Typed.Replace("\n", " ").Trim();
                if (rows.Count == 1) return rows[0] == token ? token : token + " | " + rows[0].Substring(token.Length).Trim();
                return token + " | " + String.Join(" | ", rows.Skip(1));
            }
        }
        bool TypedIsLink { get { return TargetPlan.ConnectionToken(TargetPlan.Token(Typed.Split('\n').FirstOrDefault() ?? "")); } }
        void ShowError(string text)
        { MessageWindow.Show(FindForm(), language, language.T("note"), text, MessageWindow.Kind.Warning, DialogResult.OK); }
        void Push(string kind, string value)
        {
            List<string> field = Field(kind);
            if (field.Contains(value)) { Pick(kind, field.IndexOf(value)); return; }
            field.Add(value); Recompute(); Pick(kind, field.Count - 1);
        }
        // Clone mode: the same four buttons write the plugin's four commands, a block being one
        // field per line. The buttons are off while something is missing, so these only guard
        // against a call that comes without them.
        bool CloneReady(bool needAnchor)
        {
            List<string> fresh = TypedLines;
            if (fresh.Count == 0 || !BeginsToken(fresh[0]) || TypedIsPicked) return false;
            return !needAnchor || PickedLines.Count > 0;
        }
        // Clone mode: what is typed, cut into blocks - every line that begins with $ starts one,
        // the lines below it are its data. Each block becomes a command of its own.
        List<List<string>> TypedBlocks
        {
            get
            {
                var found = new List<List<string>>();
                foreach (string row in TypedLines)
                {
                    if (BeginsToken(row) || found.Count == 0) found.Add(new List<string>());
                    found[found.Count - 1].Add(row);
                }
                return found;
            }
        }
        static string Joined(List<string> block) { return String.Join(" | ", block); }
        // Several blocks for one place: the first goes to the anchor, each further one behind the
        // block before it, named by that block's first line (the plugin lets an insert hang on a
        // block an earlier command produces). That cannot be said when such a first line stands
        // in the donor too, comes twice, or an older command already produces it - then all
        // blocks stay in ONE command, which the plugin takes just as well.
        bool CloneChainable(List<List<string>> typed)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i + 1 < typed.Count; i++)
            {
                string row = typed[i][0];
                if (!seen.Add(row) || lines.Any(x => x.Trim() == row)) return false;
                if (outcome.Commands.Any(c => c.FreshLines != null && c.FreshLines.Count > 0 && c.FreshLines[0] == row)) return false;
            }
            return true;
        }
        static IEnumerable<string> CloneChain(List<List<string>> typed)
        { for (int i = 1; i < typed.Count; i++) yield return "1 | " + typed[i - 1][0] + " | " + Joined(typed[i]); }
        void MakeAdd()
        {
            if (clone) { if (CloneReady(false)) PushMany("add", TypedBlocks.Select(Joined).Distinct().ToList()); return; }
            if (Typed.Length == 0) return;
            if (TypedIsLink) { Push("add_connection", TypedFields); return; }
            PushMany("add", TypedLines.Distinct().ToList());
        }
        // Several commands at once: all of them are written, the last one is picked.
        void PushMany(string kind, List<string> values)
        {
            List<string> field = Field(kind); int last = -1;
            foreach (string value in values) { int had = field.IndexOf(value); if (had < 0) { field.Add(value); had = field.Count - 1; } last = had; }
            if (last < 0) return;
            Recompute(); Pick(kind, last);
        }
        // The lines behind the first one follow it one by one: each is inserted behind the line
        // before it, which an earlier command of this rule set produces (the plugin allows an
        // insert to hang on such a line). That only holds while the line is not in the file
        // already - the plugin would hang the next one on the file's own copy instead.
        bool ChainFits(List<string> rows)
        {
            for (int i = 0; i + 1 < rows.Count; i++)
            {
                string row = rows[i];
                if (lines.Any(x => x.Trim() == row)) { ShowError(language.Format("target_chain_exists", row)); return false; }
            }
            return true;
        }
        static IEnumerable<string> Chain(List<string> rows)
        { for (int i = 1; i < rows.Count; i++) yield return "1 | " + rows[i - 1] + " | " + rows[i]; }
        void MakeRemove()
        {
            if (clone)
            {
                List<string> have = PickedLines;
                if (have.Count > 0 && BeginsToken(have[0])) Push("remove", CloneAnchor());
                return;
            }
            ConnectionBlock block = PickedBlock;
            if (block != null) { Push("remove_connection", BlockFields(block)); return; }
            // Only a line picked in the file: the box below holds the NEW line and is no anchor.
            string anchor = Picked;
            if (anchor.Length == 0) return;
            if (TargetPlan.ConnectionToken(TargetPlan.Token(anchor))) { ShowError(language.T("target_link_pick")); return; }
            Push("remove", anchor);
        }
        void MakeReplace()
        {
            if (clone)
            {
                if (!CloneReady(true) || !BeginsToken(PickedLines[0])) return;
                List<List<string>> typed = TypedBlocks;
                if (typed.Count > 1 && typed[0].SequenceEqual(PickedLines)) return;
                bool chain = CloneChainable(typed);
                string anchor = CloneAnchor(), value = anchor + " | " + (chain ? Joined(typed[0]) : String.Join(" | ", TypedLines));
                // One replace per place: a second one for the same anchor takes the first one's row.
                int had = replace.FindIndex(x => x == anchor || x.StartsWith(anchor + " | $", StringComparison.Ordinal));
                if (had >= 0) replace[had] = value; else { replace.Add(value); had = replace.Count - 1; }
                if (chain) foreach (string next in CloneChain(typed)) if (!insert.Contains(next)) insert.Add(next);
                Recompute(); Pick("replace", had); return;
            }
            ConnectionBlock block = PickedBlock;
            if (block != null)
            {
                if (!TypedIsLink) { ShowError(language.T("target_link_need_new")); return; }
                string fresh = TypedFields;
                if (fresh == BlockFields(block)) { ShowError(language.T("target_need_new")); return; }
                // token | point [| point] | new token [| new point ...]
                string[] parts = fresh.Split('|').Select(x => x.Trim()).ToArray();
                string value = BlockFields(block) + " | " + parts[0] + String.Join("", parts.Skip(1).Select(x => " | " + x));
                int at = replaceLink.FindIndex(x => x.StartsWith(BlockFields(block) + " |", StringComparison.Ordinal));
                if (at >= 0) replaceLink[at] = value; else { replaceLink.Add(value); at = replaceLink.Count - 1; }
                Recompute(); Pick("replace_connection", at); return;
            }
            if (Picked.Length == 0) { ShowError(language.T("target_need_line")); return; }
            List<string> rows = TypedLines.Distinct().ToList();
            if (rows.Count == 0 || rows[0] == Picked) { ShowError(language.T("target_need_new")); return; }
            if (!ChainFits(rows)) return;
            // The first line takes the place of the picked one, the others follow below it.
            string line = Picked + " | " + rows[0];
            int where = replace.FindIndex(x => x.Split('|')[0].Trim() == Picked);
            if (where >= 0) replace[where] = line; else { replace.Add(line); where = replace.Count - 1; }
            foreach (string next in Chain(rows)) if (!insert.Contains(next)) insert.Add(next);
            Recompute(); Pick("replace", where);
        }
        void MakeInsert(bool after)
        {
            if (clone)
            {
                if (!CloneReady(true)) return;
                List<string> have = PickedLines;
                bool closing = have.Count == 1 && have[0] == "end";
                if ((closing && after) || (!closing && !BeginsToken(have[0]))) return;
                List<List<string>> typed = TypedBlocks;
                // The picked block copied down with more below it: the copy is the anchor itself.
                if (typed.Count > 1 && typed[0].SequenceEqual(have)) typed.RemoveAt(0);
                string head = (after ? "1" : "0") + " | " + CloneAnchor() + " | ";
                if (!CloneChainable(typed)) { Push("insert", head + String.Join(" | ", typed.SelectMany(x => x))); return; }
                var blocksOut = new List<string> { head + Joined(typed[0]) };
                blocksOut.AddRange(CloneChain(typed));
                PushMany("insert", blocksOut);
                return;
            }
            if (PickedBlock != null) { ShowError(language.T("target_link_no_insert")); return; }
            if (Picked.Length == 0) { ShowError(language.T("target_need_line")); return; }
            if (Typed.Length == 0 || Typed == Picked) { ShowError(language.T("target_need_new")); return; }
            if (TypedIsLink) { ShowError(language.T("target_link_no_insert")); return; }
            List<string> rows = TypedLines.Distinct().ToList();
            // The picked line copied down and more written below it: the copy is the anchor
            // itself, not a second line of its kind.
            if (rows.Count > 1 && rows[0] == Picked) rows.RemoveAt(0);
            if (!ChainFits(rows)) return;
            var all = new List<string> { (after ? "1" : "0") + " | " + Picked + " | " + rows[0] };
            all.AddRange(Chain(rows));
            PushMany("insert", all);
        }
        void Pick(string kind, int index)
        {
            for (int i = 0; i < outcome.Commands.Count; i++)
                if (outcome.Commands[i].Kind == kind && outcome.Commands[i].Index == index) { commandList.SelectedIndex = i; return; }
        }
        List<string> Field(string kind)
        {
            switch (kind)
            {
                case "add": return add;
                case "replace": return replace;
                case "remove": return remove;
                case "add_connection": return addLink;
                case "replace_connection": return replaceLink;
                case "remove_connection": return removeLink;
                default: return insert;
            }
        }
        void Drop()
        {
            if (commandList.SelectedIndex < 0) return;
            TargetCommand command = outcome.Commands[commandList.SelectedIndex];
            List<string> field = Field(command.Kind);
            if (command.Index < 0 || command.Index >= field.Count) return;
            field.RemoveAt(command.Index); Recompute();
        }
        // Order matters only inside one command kind - the plugin reads add, replace, remove and
        // insert in that order whatever the rows say, and only an earlier command can be an anchor.
        void Shift(int direction)
        {
            if (commandList.SelectedIndex < 0) return;
            TargetCommand command = outcome.Commands[commandList.SelectedIndex];
            List<string> field = Field(command.Kind);
            int at = command.Index, to = at + direction;
            if (at < 0 || at >= field.Count || to < 0 || to >= field.Count) return;
            string value = field[at]; field.RemoveAt(at); field.Insert(to, value);
            Recompute(); Pick(command.Kind, to);
        }

        // Test hooks: the editor is driven without a message loop.
        internal void TestTarget(int index) { if (index >= 0 && index < targets.Items.Count) targets.SelectedIndex = index; }
        internal void TestSelectLine(int index) { left.SelectedIndex = index; }
        internal void TestSelectCommand(int index) { commandList.SelectedIndex = index; }
        internal void TestType(string text) { edit.Text = text; }
        internal void TestAdd() { MakeAdd(); }
        internal void TestReplace() { MakeReplace(); }
        internal void TestRemove() { MakeRemove(); }
        internal void TestInsert(bool after) { MakeInsert(after); }
        internal void TestDrop() { Drop(); }
        internal IEnumerable<string> TestProblems { get { return outcome.Commands.Where(x => x.Problem.Length > 0).Select(x => x.Problem); } }
        internal bool TestEditable { get { return editable && !edit.ReadOnly && !readOnly.Visible; } }
        internal string TestPicked { get { return Picked.Length == 0 ? "" : picked.Text; } }
        internal string TestHint { get { return hint.Text; } }
        // Which of the five buttons are on: add, replace, before, after, remove - "1" or "0" each.
        internal string TestButtons
        { get { return String.Concat(new[] { addButton, replaceButton, beforeButton, afterButton, removeButton }.Select(x => x.Enabled ? "1" : "0")); } }
        internal void TestView(bool result) { showResult = result; Refill(); }
        internal IEnumerable<string> TestResult { get { return result.Select(x => x.Text); } }
        internal int TestResultCommand(int index) { return index >= 0 && index < result.Count ? result[index].Command : -1; }
        internal string TestTyped { get { return edit.Text; } }
        internal void TestCopyPicked() { CopyPicked(); }
        internal string TestSpreadNote { get { return spreadOn ? spreadNote.Text : ""; } }
        // Every text line of the box below the commands, joined.
        internal string TestEffect { get { return effect.Controls.Count == 0 ? "" : String.Join(" / ", effect.Controls[0].Controls.OfType<Label>().Select(x => x.Text)); } }
        internal IEnumerable<string> TestBadTargets { get { return badTargets; } }
        internal int TestSpread(string kind, int index) { int good; return fits.TryGetValue(kind + ":" + index, out good) ? good : 0; }
        internal string TestCount { get { return count.Text; } }
    }
}
