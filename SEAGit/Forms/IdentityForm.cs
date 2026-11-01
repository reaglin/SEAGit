using System.Drawing;
using System.Windows.Forms;
using SEAGit.Models;
using SEAGit.Services;

namespace SEAGit.Forms
{
    /// <summary>
    /// "SEAGit — Your name on GitHub" (Ron, 2026-10-31: ask once). Shown during Publish Selected when changes need saving
    /// and neither Git nor SEAGit has a name, and from Settings ▸ Your name on GitHub… to change it. The name is
    /// required; the e-mail is optional and only loosely checked. Cancel leaves everything as it was (during a publish,
    /// it stops the publish and nothing is sent).
    /// </summary>
    public class IdentityForm : DpiAwareForm
    {
        public const string ScreenTitle = "SEAGit — Your name on GitHub";

        private readonly TextBox _name;
        private readonly TextBox _email;
        private readonly Label _nameProblem;
        private readonly Label _emailProblem;

        /// <summary>What was typed, trimmed, after Save.</summary>
        public CommitIdentity Identity { get; private set; } = new();

        /// <param name="current">The saved name and e-mail; null the first time (the name is then filled in from Windows).</param>
        /// <param name="publishingNow">True when asked during Publish Selected (the publish carries on after Save).</param>
        /// <param name="gitOwnName">The name Git itself has, set outside SEAGit ("" for none); it is used instead.</param>
        public IdentityForm(CommitIdentity? current, bool publishingNow, string gitOwnName = "")
        {
            Text = ScreenTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);

            var layout = new TableLayoutPanel
            {
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
            };

            const string isPublic = " Anyone who can see your GitHub repository sees this name, so a first name or a nickname is fine.";
            string intro = publishingNow
                ? "GitHub shows a name beside every change you publish, so people can see who made it. Type the name you " +
                  "want it to show." + isPublic + " SEAGit asks only this once; to change it later, use Settings ▸ Your " +
                  "name on GitHub… in the SEAGit window."
                : "GitHub shows a name beside every change you publish, so people can see who made it. SEAGit puts this " +
                  "name on your changes from now on (changes already on GitHub keep the name they had)." + isPublic;
            layout.Controls.Add(new Label { Text = intro, AutoSize = true, MaximumSize = new Size(500, 0), Margin = new Padding(0, 0, 0, 12) });

            if (gitOwnName.Length > 0)
                layout.Controls.Add(new Label
                {
                    Text = $"Git on this computer already has a name of its own, \"{gitOwnName}\", set outside SEAGit. " +
                           "That name is used on your changes instead of the one here (the e-mail here is still used " +
                           "if Git has none).",
                    AutoSize = true, MaximumSize = new Size(500, 0), ForeColor = Color.FromArgb(0, 70, 140),
                    Margin = new Padding(0, 0, 0, 12),
                });

            const string nameLabel = "Name to show on GitHub";
            layout.Controls.Add(new Label { Text = "&" + nameLabel + ":", AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
            _name = new TextBox
            {
                Width = 500,
                Text = current?.Name ?? WindowsUser.DisplayName(),
                PlaceholderText = "for example Pat Smith",
                AccessibleName = nameLabel,
                MaxLength = 100,
                Margin = new Padding(0, 0, 0, 2),
            };
            _nameProblem = ProblemLabel();
            layout.Controls.Add(_name);
            layout.Controls.Add(_nameProblem);

            const string emailLabel = "E-mail (optional)";
            layout.Controls.Add(new Label { Text = "&" + emailLabel + ":", AutoSize = true, Margin = new Padding(0, 4, 0, 4) });
            _email = new TextBox
            {
                Width = 500,
                Text = current?.Email ?? "",
                PlaceholderText = "your-name@users.noreply.github.com",
                AccessibleName = emailLabel,
                MaxLength = 200,
                Margin = new Padding(0, 0, 0, 2),
            };
            _emailProblem = ProblemLabel();
            const string emailNote =
                "To have GitHub link your changes to your account, use the private address GitHub gives you: github.com ▸ " +
                "Settings ▸ Emails ▸ 'Keep my email addresses private' shows an address ending in @users.noreply.github.com. " +
                "Leave it empty and SEAGit uses a placeholder address.";
            layout.Controls.Add(_email);
            layout.Controls.Add(_emailProblem);
            layout.Controls.Add(new Label { Text = emailNote, AutoSize = true, MaximumSize = new Size(500, 0), Margin = new Padding(0, 2, 0, 14) });

            var save = new Button { Text = "&Save", AutoSize = true, MinimumSize = new Size(100, 30), Margin = new Padding(0, 0, 8, 0) };
            var cancel = new Button
            {
                Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(90, 30), Margin = new Padding(0),
            };
            var help = new Button { Text = "Help (F1)", AutoSize = true, MinimumSize = new Size(90, 30), Margin = new Padding(0, 0, 24, 0) };

            save.Click += (_, _) =>
            {
                string? problem = PlainWords.WhatIsWrongWithIdentity(_name.Text, _email.Text, out bool emailIsTheProblem);
                if (problem != null)
                {
                    var (box, line) = emailIsTheProblem ? (_email, _emailProblem) : (_name, _nameProblem);
                    line.Text = problem;
                    box.AccessibleDescription = problem; // read out by a screen reader when the box gets focus
                    box.Focus();
                    box.SelectAll();
                    return;
                }
                Identity = new CommitIdentity { Name = _name.Text.Trim(), Email = _email.Text.Trim() };
                DialogResult = DialogResult.OK;
            };
            _name.TextChanged += (_, _) => { _nameProblem.Text = ""; _name.AccessibleDescription = null; };
            _email.TextChanged += (_, _) => { _emailProblem.Text = ""; _email.AccessibleDescription = null; };
            // A name filled in from Windows is selected, so it is plain that it can simply be typed over.
            Shown += (_, _) => { _name.Focus(); _name.SelectAll(); };
            help.Click += (_, _) => ShowHelp();
            HelpRequested += (_, e) => { e.Handled = true; ShowHelp(); };

            var tips = new ToolTip { AutoPopDelay = 20000 };
            Disposed += (_, _) => tips.Dispose();
            tips.SetToolTip(_name, "The name GitHub shows beside each change you publish, for example your full name. Required.");
            tips.SetToolTip(_email, "Optional. Your private GitHub address (it ends in @users.noreply.github.com) makes GitHub link " +
                "your changes to your account and show your picture. Leave it empty if you are not sure.");
            tips.SetToolTip(save, publishingNow
                ? "Saves the name in SEAGit's settings and carries on publishing. SEAGit will not ask again."
                : "Saves the name in SEAGit's settings. Your next publishes carry it.");
            tips.SetToolTip(cancel, publishingNow
                ? "Closes this window and stops the publish. Nothing is saved, and nothing is sent to GitHub."
                : "Closes this window without changing anything.");
            tips.SetToolTip(help, "What this name is for, and how to find your private GitHub e-mail address.");

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
                WrapContents = false,
            };
            buttons.Controls.Add(help);
            buttons.Controls.Add(save);
            buttons.Controls.Add(cancel);
            layout.Controls.Add(buttons);

            Controls.Add(layout);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private static Label ProblemLabel() => new()
        {
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            ForeColor = Color.Firebrick,
            Margin = new Padding(0, 0, 0, 4),
            Text = "",
        };

        private void ShowHelp()
        {
            using var h = new HelpForm("the Your name on GitHub window", HelpForm.IdentityHelp, "publish.html");
            h.ShowDialog(this);
        }
    }
}
