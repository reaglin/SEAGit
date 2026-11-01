using System.Drawing;
using System.Windows.Forms;

namespace SEAGit.Forms
{
    /// <summary>
    /// Help ▸ Help for this screen (F1): help about the window the person is in, not a general index, with a link to
    /// the matching page of SEAGit's help on SoftwarePlus.ai (usability checklist 1.2–1.3, 2026-10-31).
    /// </summary>
    public class HelpForm : DpiAwareForm
    {
        public const string HelpRoot = "https://softwareplus.ai/seagit/help/";

        /// <param name="screenName">The window the help is about, e.g. "the SEAGit window".</param>
        /// <param name="body">The help, in plain words. Lines starting "## " become headings.</param>
        /// <param name="webPage">The page under softwareplus.ai/seagit/help/ (e.g. "publish.html").</param>
        public HelpForm(string screenName, string body, string webPage)
        {
            Text = "SEAGit — Help for this screen";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);

            var layout = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
            };

            layout.Controls.Add(new Label
            {
                Text = "Help: " + screenName,
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8),
            });

            foreach (var paragraph in body.Split("\n\n"))
            {
                bool heading = paragraph.StartsWith("## ");
                layout.Controls.Add(new Label
                {
                    Text = heading ? paragraph[3..] : paragraph,
                    AutoSize = true,
                    MaximumSize = new Size(540, 0),
                    Font = heading ? new Font("Segoe UI", 10F, FontStyle.Bold) : null,
                    Margin = new Padding(0, heading ? 6 : 0, 0, heading ? 2 : 8),
                });
            }

            string url = HelpRoot + webPage;
            const string lead = "More help, with pictures, on the web: ";
            string shown = "softwareplus.ai/seagit/help/" + webPage;
            var link = new LinkLabel
            {
                Text = lead + shown,
                AutoSize = true,
                MaximumSize = new Size(540, 0),
                Margin = new Padding(0, 4, 0, 14),
                TabStop = true,
                AccessibleName = lead + shown,
                AccessibleDescription = "Opens " + url + " in your web browser",
            };
            link.LinkArea = new LinkArea(lead.Length, shown.Length);
            link.LinkClicked += (_, _) => OpenInBrowser(this, url);
            layout.Controls.Add(link);

            var close = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                MinimumSize = new Size(90, 30),
                Margin = new Padding(0),
            };
            layout.Controls.Add(close);

            Controls.Add(layout);
            AcceptButton = close;
            CancelButton = close;
        }

        /// <summary>Opens an address in the person's web browser; says so if Windows cannot.</summary>
        public static void OpenInBrowser(IWin32Window owner, string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                MessageBox.Show(owner, "Windows could not open a web browser. The address is\n" + url,
                    "SEAGit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ---- The help for each screen (kept here so the words are in one place) ----

        public const string MainWindowHelp =
            "This window lists the folders on your computer that SEAGit publishes to GitHub, and publishes them.\n\n" +
            "## To publish a folder\n\n" +
            "1. Press + Add Local Folder… and choose the folder. SEAGit asks before it prepares a folder it has not used before.\n" +
            "2. Select the folder in Your folders.\n" +
            "3. Press Publish Selected. The first time, paste the address of an empty GitHub repository, then sign in to " +
            "GitHub in the browser window that opens.\n\n" +
            "After that, press Publish Selected whenever you have changed your files. Only the changes are sent, and " +
            "GitHub ends up matching your folder (a file you delete from the folder is removed from GitHub too).\n\n" +
            "## The other buttons\n\n" +
            "Open Folder opens the selected folder in File Explorer (double-clicking it in the list does the same).\n" +
            "Change GitHub Address… publishes the selected folder to a different GitHub repository from now on.\n" +
            "Remove from List takes the folder off SEAGit's list. Your files are not deleted.\n" +
            "Settings ▸ Your name on GitHub… changes the name GitHub shows beside your changes.\n\n" +
            "## When something goes wrong\n\n" +
            "SEAGit tells you in plain words what happened and what to do next. Git's own words are in the Activity Log at " +
            "the bottom, for anyone helping you. While SEAGit is working, its buttons are turned off; they come back when it has finished.";

        public const string GitHubAddressHelp =
            "This window asks where on GitHub the folder is published: the address of a GitHub repository.\n\n" +
            "## Make an empty GitHub repository\n\n" +
            "1. Sign in at github.com, press + (top right), then New repository.\n" +
            "2. Give it a name. Leave \"Add a README file\", .gitignore and license unticked, so the repository is empty.\n" +
            "3. Press Create repository. Copy the address at the top of the next page; it looks like " +
            "https://github.com/your-name/your-repository.git\n\n" +
            "## Then\n\n" +
            "Paste the address in the box (Ctrl+V) and press the button. Cancel or Esc leaves everything as it was. " +
            "An address that does not start with https://github.com/ is refused, and the window says why.";

        public const string IdentityHelp =
            "GitHub shows a name beside every change you publish, so the people who see your GitHub repository know who " +
            "made it. This window asks for that name. SEAGit asks only once and keeps it in its own settings; it never " +
            "changes Git's settings on your computer. If Git already has a name of its own (set outside SEAGit), that name is used.\n\n" +
            "## Name to show on GitHub\n\n" +
            "Required. Usually your full name, for example Pat Smith. Anyone who can see the GitHub repository can see it.\n\n" +
            "## E-mail (optional)\n\n" +
            "Leave it empty and SEAGit puts a placeholder address on your changes; GitHub then shows the name without your " +
            "picture. To have GitHub link your changes to your account, use the private address GitHub gives you: sign in " +
            "at github.com, open Settings (your picture, top right) ▸ Emails, and tick \"Keep my email addresses private\". " +
            "GitHub then shows an address ending in @users.noreply.github.com; copy it into this box.\n\n" +
            "## Save, Cancel and changing it later\n\n" +
            "Save keeps the name and, during a publish, carries on publishing. Cancel closes the window; during a publish " +
            "it stops the publish and nothing is sent. To change the name later, use Settings ▸ Your name on GitHub… in the " +
            "SEAGit window. Changes already on GitHub keep the name they had.";
    }
}
