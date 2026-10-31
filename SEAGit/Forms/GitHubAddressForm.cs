using System.Drawing;
using System.Windows.Forms;
using SEAGit.Services;

namespace SEAGit.Forms
{
    /// <summary>
    /// Asks for the address of the GitHub repository a folder publishes to: the first time a folder is published
    /// (with how to make an empty GitHub repository), or from Change GitHub Address…. The address is checked before
    /// the window closes; Cancel or Esc leaves everything as it was.
    /// </summary>
    public class GitHubAddressForm : DpiAwareForm
    {
        private readonly TextBox _address;
        private readonly Label _problem;
        private readonly string _screenName;

        /// <summary>The checked address (https://github.com/owner/name.git) after OK.</summary>
        public string Address { get; private set; } = "";

        /// <param name="folderName">The folder's name, shown in the instructions.</param>
        /// <param name="currentAddress">The address it publishes to now; empty the first time.</param>
        /// <param name="publishingNow">True when asked during Publish Selected (the publish carries on after OK);
        /// false from Change GitHub Address… (the address is only saved).</param>
        public GitHubAddressForm(string folderName, string currentAddress, bool publishingNow)
        {
            bool changing = !string.IsNullOrWhiteSpace(currentAddress);
            _screenName = publishingNow ? "Link to GitHub" : "Change GitHub Address";

            Text = "SEAGit — " + _screenName;
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

            string intro = changing
                ? $"\"{folderName}\" publishes to the GitHub repository below. To publish it to a different GitHub " +
                  "repository from now on, paste that repository's address here."
                : $"\"{folderName}\" is not linked to a GitHub repository yet. Paste the address of an empty GitHub " +
                  "repository for it to be published to.";
            const string howTo =
                "To make an empty GitHub repository:\n" +
                "1. Sign in at github.com, press + (top right), then New repository.\n" +
                "2. Give it a name. Leave \"Add a README file\", .gitignore and license unticked, so it is empty.\n" +
                "3. Press Create repository, then copy the address shown at the top of the next page " +
                "(it starts with https://github.com/) and paste it below.";

            layout.Controls.Add(new Label { Text = intro, AutoSize = true, MaximumSize = new Size(520, 0), Margin = new Padding(0, 0, 0, 10) });
            if (!changing)
                layout.Controls.Add(new Label { Text = howTo, AutoSize = true, MaximumSize = new Size(520, 0), Margin = new Padding(0, 0, 0, 12) });
            else
                layout.Controls.Add(new Label
                {
                    Text = "Use a new, empty GitHub repository: one that already has files (such as a README) cannot be published over.",
                    AutoSize = true, MaximumSize = new Size(520, 0), Margin = new Padding(0, 0, 0, 12),
                });

            var addressLabel = new Label { Text = "GitHub repository address:", AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
            _address = new TextBox
            {
                Width = 520,
                Text = changing ? currentAddress : "",
                PlaceholderText = "https://github.com/your-name/your-repository",
                AccessibleName = "GitHub repository address",
                Margin = new Padding(0, 0, 0, 4),
            };
            _problem = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(520, 0),
                ForeColor = Color.Firebrick,
                Margin = new Padding(0, 0, 0, 8),
                Text = "",
            };
            layout.Controls.Add(addressLabel);
            layout.Controls.Add(_address);
            layout.Controls.Add(_problem);

            var ok = new Button
            {
                Text = publishingNow ? "Link and Publish" : "Save Address",
                AutoSize = true,
                MinimumSize = new Size(120, 30),
                Margin = new Padding(0, 0, 8, 0),
            };
            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                MinimumSize = new Size(90, 30),
                Margin = new Padding(0),
            };
            ok.Click += (_, _) =>
            {
                string? checkedAddress = PlainWords.NormaliseGitHubAddress(_address.Text, out string problem);
                if (checkedAddress == null)
                {
                    _problem.Text = problem;
                    _address.Focus();
                    _address.SelectAll();
                    return;
                }
                Address = checkedAddress;
                DialogResult = DialogResult.OK;
            };
            _address.TextChanged += (_, _) => _problem.Text = "";

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
                WrapContents = false,
            };
            var help = new Button
            {
                Text = "Help (F1)",
                AutoSize = true,
                MinimumSize = new Size(90, 30),
                Margin = new Padding(0, 0, 24, 0),
            };
            help.Click += (_, _) => ShowHelp();
            HelpRequested += (_, e) => { e.Handled = true; ShowHelp(); };

            var tips = new ToolTip { AutoPopDelay = 15000 };
            Disposed += (_, _) => tips.Dispose();
            tips.SetToolTip(_address, "The address of the GitHub repository, for example https://github.com/your-name/your-repository " +
                "(copy it from the top of the repository's page on GitHub, then paste it here with Ctrl+V).");
            tips.SetToolTip(ok, !publishingNow
                ? "Saves this address. The next Publish Selected copies the folder to this GitHub repository."
                : "Links the folder to this GitHub repository and carries on publishing. You may be asked to sign in to GitHub in your browser.");
            tips.SetToolTip(cancel, "Closes this window without changing anything. Nothing is published.");
            tips.SetToolTip(help, "How to make an empty GitHub repository and find its address.");

            buttons.Controls.Add(help);
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            layout.Controls.Add(buttons);

            Controls.Add(layout);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void ShowHelp()
        {
            using var h = new HelpForm("the " + _screenName + " window", HelpForm.GitHubAddressHelp, "before.html");
            h.ShowDialog(this);
        }
    }
}
