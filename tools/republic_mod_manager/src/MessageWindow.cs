// Republic Mod Manager - message window in the RMM look (0.4.38): replaces the
// Windows message box for questions, warnings, errors and notes, so a message
// is recognisable as the manager's own - navy title strip, icon, wrapped text,
// buttons like everywhere else.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TesmioAutoload
{
    static class MessageWindow
    {
        public enum Kind { Info, Warning, Error, Question }

        // Default captions: OK / Yes / No / Cancel from the language file.
        public static DialogResult Show(IWin32Window owner, Language language, string title, string text, Kind kind, params DialogResult[] buttons)
        {
            if (buttons == null || buttons.Length == 0) buttons = new[] { DialogResult.OK };
            return Show(owner, language, title, text, kind, buttons.Select(b => new KeyValuePair<DialogResult, string>(b, Caption(language, b))).ToArray());
        }

        // Buttons in reading order (the last one is the rightmost). The first button that is
        // not Cancel/No is the primary (blue) one and the default.
        public static DialogResult Show(IWin32Window owner, Language language, string title, string text, Kind kind, KeyValuePair<DialogResult, string>[] buttons)
        {
            var ownerForm = owner as Form;
            using (var dialog = new Form { Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = ownerForm != null ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = ownerForm == null, BackColor = Color.White, Font = ownerForm != null ? ownerForm.Font : new Font("Segoe UI", 10) })
            {
                if (ownerForm != null && ownerForm.Icon != null) dialog.Icon = ownerForm.Icon;
                Theme.ApplyWindowChrome(dialog);
                const int width = 640;
                var header = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Theme.Navy };
                var caption = Theme.Label(title, 12, true); caption.ForeColor = Color.White; caption.BackColor = Theme.Navy; caption.Location = new Point(18, 11); header.Controls.Add(caption);
                var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(18, 18, 18, 6), BackColor = Color.White };
                body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                var icon = new NoticeIcon { Warning = kind == Kind.Warning || kind == Kind.Error, BackColor = Color.White, Margin = new Padding(0, 4, 0, 0) };
                if (kind == Kind.Error) { icon.Tone = Theme.Danger; icon.Symbol = "!"; } else if (kind == Kind.Question) { icon.Tone = Theme.Blue; icon.Symbol = "?"; }
                body.Controls.Add(icon, 0, 0);
                int textWidth = width - 44 - 36 - 12, textHeight;
                if (Marked(text)) body.Controls.Add(Paragraphs(text, textWidth, out textHeight), 1, 0);
                else
                {
                    var label = Theme.Label(text, 10.5f, false); label.AutoSize = true; label.MaximumSize = new Size(textWidth, 0); label.Margin = new Padding(0, 4, 0, 0); label.BackColor = Color.White;
                    body.Controls.Add(label, 1, 0);
                    textHeight = TextRenderer.MeasureText(text, label.Font, new Size(textWidth, 0), TextFormatFlags.WordBreak).Height;
                }
                var row = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(12, 10, 12, 10), BackColor = Theme.Chrome };
                bool primaryTaken = false;
                foreach (var entry in buttons.Reverse())
                {
                    DialogResult value = entry.Key; bool secondary = value == DialogResult.Cancel || value == DialogResult.No;
                    bool primary = !secondary && !primaryTaken; if (primary) primaryTaken = true;
                    var button = Theme.Button(entry.Value, () => { dialog.DialogResult = value; dialog.Close(); }, primary);
                    if (!primary) button.BackColor = Color.White;
                    button.Margin = new Padding(8, 0, 0, 0); row.Controls.Add(button);
                    if (value == DialogResult.Cancel || (value == DialogResult.No && !buttons.Any(b => b.Key == DialogResult.Cancel))) dialog.CancelButton = button;
                    if (primary) dialog.AcceptButton = button;
                }
                dialog.Controls.Add(body); dialog.Controls.Add(row); dialog.Controls.Add(header);
                dialog.ClientSize = new Size(width, Math.Min(760, 46 + 64 + 30 + Math.Max(40, textHeight + 10)));
                return ownerForm != null ? Theme.Modal(ownerForm, dialog) : dialog.ShowDialog();
            }
        }

        // Paragraph marks for messages that need more than one block of text: every line is a
        // paragraph of its own, a line that starts with [b] is set bold, one that starts with [!]
        // is set in the warning red, and an empty line leaves a gap. A text without a mark is
        // shown as one label, as before.
        public const string Bold = "[b]", Alert = "[!]";
        public static bool Marked(string text)
        {
            if (String.IsNullOrEmpty(text)) return false;
            return text.Replace("\r\n", "\n").Split('\n').Any(line => line.StartsWith(Bold, StringComparison.Ordinal) || line.StartsWith(Alert, StringComparison.Ordinal));
        }
        // The same text without the marks, for logs and tests.
        public static string Plain(string text)
        {
            if (!Marked(text)) return text;
            return String.Join("\n", text.Replace("\r\n", "\n").Split('\n').Select(line => line.StartsWith(Bold, StringComparison.Ordinal) || line.StartsWith(Alert, StringComparison.Ordinal) ? line.Substring(3) : line));
        }
        static Control Paragraphs(string text, int width, out int height)
        {
            var stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 4, 0, 0), Padding = Padding.Empty, BackColor = Color.White };
            height = 0; int gap = 0; bool first = true;
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                if (raw.Trim().Length == 0) { gap += 16; continue; }
                bool bold = raw.StartsWith(Bold, StringComparison.Ordinal), alert = raw.StartsWith(Alert, StringComparison.Ordinal);
                string line = bold || alert ? raw.Substring(3) : raw;
                var part = Theme.Label(line, 10.5f, bold); part.AutoSize = true; part.MaximumSize = new Size(width, 0); part.BackColor = Color.White;
                if (alert) part.ForeColor = Theme.Danger;
                int top = first ? 0 : 8 + gap; part.Margin = new Padding(0, top, 0, 0);
                stack.Controls.Add(part);
                height += top + TextRenderer.MeasureText(line, part.Font, new Size(width, 0), TextFormatFlags.WordBreak).Height + 2;
                first = false; gap = 0;
            }
            return stack;
        }

        public static string Caption(Language language, DialogResult result)
        {
            string key = result == DialogResult.Cancel ? "cancel" : result == DialogResult.No ? "no_plain" : result == DialogResult.Yes ? "yes_plain" : "ok";
            if (language != null) { string text = language.T(key); if (text != key) return text; }
            return result == DialogResult.Cancel ? "Cancel" : result == DialogResult.No ? "No" : result == DialogResult.Yes ? "Yes" : "OK";
        }
    }
}
