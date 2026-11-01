using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SEAGit.Forms;
using SEAGit.Models;
using SEAGit.Services;

namespace SEAGit
{
    /// <summary>
    /// The one window. Words used everywhere (UX review, 2026-10-31): "folder" is the thing on this computer,
    /// "GitHub repository" is the copy on github.com, "Publish" is the action. Every outcome of a publish is told in a
    /// message box in plain words; Git's own text goes to the Activity Log.
    /// </summary>
    public partial class MainForm : DpiAwareForm
    {
        private const string AppTitle = "SEAGit";

        private readonly StorageService _storageService;
        private readonly GitProcessService _gitService;
        private List<GitRepository> _repositories;
        private bool _busy;

        public MainForm()
        {
            // Load the window icon from the app folder (AppContext.BaseDirectory),
            // never a bare relative path: launched from the Start menu (MSIX) the
            // working directory is not the app folder, so "seagit.ico" was not
            // found and the constructor threw — crashing the app at launch
            // (0xe0434352). Guarded so a missing icon can never crash startup.
            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "seagit.ico");
                if (File.Exists(iconPath))
                    this.Icon = new System.Drawing.Icon(iconPath);
            }
            catch { /* fall back to the embedded application icon */ }

            InitializeComponent();
            _storageService = new StorageService();
            _gitService = new GitProcessService();
            _repositories = new List<GitRepository>();

            // Subscribed once (it used to be added again on every refresh of the list).
            lstRepos.Format += LstRepos_Format;
            FormClosing += MainForm_FormClosing;
            FormClosed += (_, _) => _tips.Dispose();
            SetToolTips();
            AddEmptyListHint();
        }

        private readonly ToolTip _tips = new() { AutoPopDelay = 15000, InitialDelay = 500 };

        /// <summary>Every control says what it does and what follows (usability checklist 2.2).</summary>
        private void SetToolTips()
        {
            _tips.SetToolTip(lblIntro, "What SEAGit does, in three steps. Press F1 for more help about this window.");
            _tips.SetToolTip(btnAddRepo, "Choose a folder on your computer to add to the list. SEAGit asks before it prepares a " +
                "folder it has not used before. Nothing is sent to GitHub until you press Publish Selected.");
            _tips.SetToolTip(btnPublish, "Copies the selected folder's changes to its GitHub repository. The first time, it asks " +
                "for the GitHub repository's address, and you sign in to GitHub in your browser.");
            _tips.SetToolTip(lstRepos, ListTip);
            lstRepos.MouseMove += LstRepos_MouseMove;
            lstRepos.MouseLeave += (_, _) => ShowListTip(ListTip);
            lstRepos.SizeChanged += (_, _) =>
            {
                // The path part of each row is shortened to the list's width, so a new width re-formats the rows.
                if (lstRepos.ClientSize.Width == _formattedWidth) return;
                _formattedWidth = lstRepos.ClientSize.Width;
                RefreshRepoList();
            };
            _tips.SetToolTip(btnOpenFolder, "Opens the selected folder in File Explorer, so you can add or change its files.");
            _tips.SetToolTip(btnChangeAddress, "Publishes the selected folder to a different GitHub repository from now on. " +
                "Nothing already on GitHub is changed or deleted.");
            _tips.SetToolTip(btnRemove, "Takes the selected folder off this list. Your files are not deleted, and nothing on GitHub changes.");
            _tips.SetToolTip(lblStatus, "What SEAGit is doing now. While it works, the buttons are turned off.");
            _tips.SetToolTip(txtLog, "Each step SEAGit takes. When something goes wrong, Git's own words are here, for anyone helping you.");
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Keep launch resilient: any failure here is logged and surfaced, but
            // never allowed to escape and terminate the app before the window is
            // shown. Git is bundled with the app (MinGit under the install folder),
            // so there is no longer a startup check that talks the user through
            // installing it — see GitProcessService.GitExePath.
            try
            {
                _repositories = _storageService.LoadRepositories();
                RefreshRepoList();
                LogMessage(_repositories.Count == 0
                    ? "SEAGit is ready. Your list of folders is empty: press + Add Local Folder… to start. (Press F1 for help.)"
                    : "SEAGit is ready. Select a folder, then press Publish Selected. (Press F1 for help.)");

                if (_storageService.ListWasUnreadable)
                {
                    string kept = _storageService.BrokenListBackupPath != null
                        ? "A copy of the old list was kept at:\n" + _storageService.BrokenListBackupPath
                        : "SEAGit could not keep a copy of the old list.";
                    LogMessage("The saved list of folders could not be read. " + kept);
                    MessageBox.Show(this,
                        "SEAGit's saved list of your folders could not be read (the file is damaged), so the list starts empty.\n\n" +
                        "Your folders and files are not affected, and what you published is still on GitHub. Add your folders " +
                        "again with the + Add Local Folder… button; each one keeps its GitHub address.\n\n" + kept,
                        AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LogMessage("Startup warning: " + ex.Message);
                MessageBox.Show(this,
                    "SEAGit started, but could not load its saved list of your folders. Your folders and files are not " +
                    "affected; add them again with the + Add Local Folder… button. " + PlainWords.SeeLog,
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!_busy || e.CloseReason != CloseReason.UserClosing) return;
            if (MessageBox.Show(this,
                    "SEAGit is still publishing. If you close it now, the publish stops; your files stay as they are, and " +
                    "the next Publish Selected finishes the job.\n\nClose SEAGit anyway?",
                    AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                e.Cancel = true;
        }

        /// <summary>
        /// Returns true if SEAGit's bundled Git component is present; otherwise
        /// shows a "bundled component missing — please reinstall SEAGit" message
        /// and returns false. Called before any action that shells out to git.
        /// Does not link to a third-party download (Store policy 10.1.5); Git is
        /// shipped inside the MSIX, so a missing exe means the install is broken.
        /// </summary>
        private bool EnsureGitOrWarn()
        {
            if (_gitService.IsGitInstalled()) return true;

            MessageBox.Show(this,
                "SEAGit's built-in Git appears to be missing from the install.\n\n" +
                "Please reinstall SEAGit from the Microsoft Store to restore it.",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        /// <summary>The selected folder, or null after telling the person to select one first.</summary>
        private GitRepository? SelectedOrWarn(string buttonName)
        {
            if (lstRepos.SelectedItem is GitRepository repo) return repo;
            MessageBox.Show(this, $"Select a folder in the list first, then press {buttonName}.", AppTitle,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        /// <summary>True if the folder is still where it was added; otherwise says what to do.</summary>
        private bool FolderStillThere(GitRepository repo)
        {
            if (Directory.Exists(repo.LocalPath)) return true;
            MessageBox.Show(this,
                $"The folder \"{repo.Name}\" is no longer at\n{repo.LocalPath}\n\nIt may have been moved, renamed or deleted. " +
                "Press Remove from List, then add the folder again from where it is now with the + Add Local Folder… button.",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        /// <summary>
        /// Asks before SEAGit prepares a folder it has not used before (git init), and prepares it on Yes.
        /// </summary>
        private async Task<bool> PrepareFolderAsync(string path)
        {
            var answer = MessageBox.Show(this,
                "SEAGit has not used this folder before. Prepare it for publishing?\n\n" +
                "Everything inside it will be copied to GitHub when you press Publish Selected.\n\n" + path,
                AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return false;

            if (!EnsureGitOrWarn()) return false;
            var init = await _gitService.InitializeRepoAsync(path);
            if (!init.Ok)
            {
                LogMessage("Preparing the folder failed:\n" + init.Raw);
                MessageBox.Show(this, "SEAGit could not prepare the folder for publishing.\n\n" + PlainWords.ExplainGitError(init.Raw),
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            LogMessage($"Prepared the folder for publishing: {path}");
            return true;
        }

        private async void BtnAddRepo_Click(object? sender, EventArgs e)
        {
            string selectedPath;
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Choose the folder you want to publish to GitHub";
                folderDialog.UseDescriptionForTitle = true;
                if (folderDialog.ShowDialog(this) != DialogResult.OK) return;
                selectedPath = folderDialog.SelectedPath;
            }

            string? refused = PlainWords.WhyFolderIsRefused(selectedPath);
            if (refused != null)
            {
                MessageBox.Show(this, refused, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existing = _repositories.FirstOrDefault(r => r.LocalPath.Equals(selectedPath, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                lstRepos.SelectedItem = existing;
                MessageBox.Show(this, $"\"{existing.Name}\" is already in your list. It is selected now.", AppTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true, "");
            try
            {
                string name = new DirectoryInfo(selectedPath).Name;
                if (!_gitService.IsValidGitRepo(selectedPath) && !await PrepareFolderAsync(selectedPath))
                {
                    LogMessage($"Not added: \"{name}\" was not prepared for publishing, so it is not in your list.");
                    return;
                }

                var newRepo = new GitRepository
                {
                    Name = name,
                    LocalPath = selectedPath,
                    // A folder added again (after Remove from List) shows when it was really last published.
                    LastPublished = await _gitService.LastPublishedAsync(selectedPath) ?? DateTime.MinValue,
                };

                _repositories.Add(newRepo);
                _storageService.SaveRepositories(_repositories);
                RefreshRepoList();
                lstRepos.SelectedItem = newRepo;

                LogMessage($"Added to your folders: {newRepo.Name}. Press Publish Selected to copy it to GitHub.");
            }
            finally
            {
                SetBusy(false, "");
            }
        }

        private async void BtnPublish_Click(object? sender, EventArgs e)
        {
            var repo = SelectedOrWarn("Publish Selected");
            if (repo == null) return;
            if (!EnsureGitOrWarn() || !FolderStillThere(repo)) return;

            SetBusy(true, $"Checking \"{repo.Name}\"…");
            try
            {
                await PublishAsync(repo);
            }
            catch (Exception ex)
            {
                LogMessage($"Unexpected error: {ex}");
                MessageBox.Show(this, "Something unexpected went wrong while publishing. " + PlainWords.SeeLog,
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false, "");
            }
        }

        private async Task PublishAsync(GitRepository repo)
        {
            string path = repo.LocalPath;
            LogMessage($"--- Publishing {repo.Name} ---");

            if (!_gitService.IsValidGitRepo(path) && !await PrepareFolderAsync(path))
            {
                LogMessage("Publish cancelled: the folder was not prepared.");
                return;
            }

            // 1. Where on GitHub? Asked the first time.
            string url = await _gitService.GetRemoteUrlAsync(path);
            if (string.IsNullOrWhiteSpace(url))
            {
                string? chosen = AskForGitHubAddress(repo, "", publishingNow: true);
                if (chosen == null)
                {
                    LogMessage("Publish cancelled: no GitHub repository address was given.");
                    return;
                }
                var link = await _gitService.SetRemoteAsync(path, chosen);
                if (!link.Ok)
                {
                    LogMessage(link.Raw);
                    MessageBox.Show(this, "SEAGit could not save the GitHub address.\n\n" + PlainWords.ExplainGitError(link.Raw),
                        AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                url = chosen;
                LogMessage($"Linked {repo.Name} to {url}");
            }

            // 2. What would go? Refuse files GitHub cannot take; confirm the very first publish.
            bool firstPublish = !await _gitService.HasBeenPublishedAsync(path);
            var files = await _gitService.GetFilesToPublishAsync(path, firstPublish);

            var tooBig = files.Where(f => f.Bytes > GitProcessService.GitHubFileLimitBytes).ToList();
            if (tooBig.Count > 0)
            {
                string list = string.Join("\n", tooBig.Take(5).Select(f => $"   {f.RelativePath}  ({PlainWords.Size(f.Bytes)})"));
                if (tooBig.Count > 5) list += $"\n   …and {tooBig.Count - 5} more";
                LogMessage("Publish stopped: files over 100 MB:\n" + list);
                MessageBox.Show(this,
                    $"Nothing was published. GitHub does not accept files over 100 MB, and \"{repo.Name}\" has " +
                    (tooBig.Count == 1 ? "one:" : $"{tooBig.Count}:") + $"\n\n{list}\n\n" +
                    "Move " + (tooBig.Count == 1 ? "it" : "them") + " out of the folder, then press Publish Selected again.",
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // The name to show on GitHub, asked once, before the last question ("Publish now?"), so that one really is
            // the last thing before anything is sent (UX review 2026-10-31).
            CommitIdentity? saved = _storageService.LoadIdentity();
            if (files.Count > 0 && (await _gitService.DecideIdentityAsync(path, saved)).Source == IdentitySource.AskThePerson)
            {
                saved = AskForNameDuringPublish();
                if (saved == null) return;
            }

            if (firstPublish && files.Count > 0)
            {
                long total = files.Sum(f => f.Bytes);
                if (MessageBox.Show(this,
                        $"{GitProcessService.Files(files.Count)} ({PlainWords.Size(total)}) in \"{repo.Name}\" will be published to the " +
                        $"GitHub repository\n{url}\n\n" +
                        "Everything in the folder is copied, so check that nothing private is in it. Files over 100 MB cannot go to GitHub.\n\n" +
                        "Publish now?",
                        AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    LogMessage("Publish cancelled before anything was sent.");
                    return;
                }
            }

            // 3. Publish. Git runs in the background; the window stays responsive.
            SetBusy(true, $"Publishing \"{repo.Name}\"… If a GitHub sign-in window opens in your browser, sign in there; " +
                "if you close it without signing in, SEAGit says so here.");
            var progress = new Progress<string>(LogMessage);
            PublishResult result = await _gitService.PublishAsync(path, progress, savedIdentity: saved);

            if (result.Outcome == PublishOutcome.NeedsName)
            {
                // Only when the files changed after the check above; nothing has been saved yet.
                saved = AskForNameDuringPublish();
                if (saved == null) return;
                result = await _gitService.PublishAsync(path, progress, savedIdentity: saved);
            }

            switch (result.Outcome)
            {
                case PublishOutcome.Published:
                case PublishOutcome.PublishedEarlierChanges:
                    repo.LastPublished = DateTime.Now;
                    _storageService.SaveRepositories(_repositories);
                    RefreshRepoList();
                    string done = result.Outcome == PublishOutcome.Published
                        ? $"Published {GitProcessService.Files(result.FileCount)} to GitHub."
                        : "Published the changes saved earlier that had not reached GitHub yet" +
                          (result.FileCount > 0 ? $" ({GitProcessService.Files(result.FileCount)})." : ".");
                    LogMessage(done + " " + url);
                    MessageBox.Show(this, done + $"\n\nThey are in the GitHub repository\n{url}", AppTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                case PublishOutcome.NothingToPublish:
                    LogMessage($"Nothing to publish: everything in {repo.Name} is already on GitHub.");
                    MessageBox.Show(this, $"Nothing to publish: everything in \"{repo.Name}\" is already on GitHub.", AppTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                case PublishOutcome.FolderEmpty:
                    LogMessage($"Nothing to publish: {repo.Name} has no files yet.");
                    MessageBox.Show(this, $"There is nothing in \"{repo.Name}\" to publish yet. Put your files in the folder " +
                        "(Open Folder shows it), then press Publish Selected.", AppTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                case PublishOutcome.NeedsName:
                    // Only if Git and the saved setting still have no name after asking; nothing was saved or sent.
                    LogMessage("Publish stopped: SEAGit still has no name to show on GitHub.");
                    MessageBox.Show(this, "Nothing was published: SEAGit has no name to show on GitHub beside your changes. " +
                        "Use Settings ▸ Your name on GitHub… to type one, then press Publish Selected again.",
                        AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;

                default:
                    LogMessage("Publish failed. Git said:\n" + result.RawError);
                    MessageBox.Show(this, "The folder was not published.\n\n" + PlainWords.ExplainGitError(result.RawError, publishedBefore: !firstPublish),
                        AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        /// <summary>Shows "Your name on GitHub"; what was typed, or null on Cancel.</summary>
        private CommitIdentity? AskForIdentity(CommitIdentity? current, bool publishingNow, string gitOwnName)
        {
            using var dialog = new IdentityForm(current, publishingNow, gitOwnName);
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.Identity : null;
        }

        /// <summary>
        /// Neither Git nor SEAGit has a name to put on the changes: ask once (Ron, 2026-10-31). The saved name, or null
        /// when the person pressed Cancel (the publish stops; no files are sent) or it could not be saved.
        /// </summary>
        private CommitIdentity? AskForNameDuringPublish()
        {
            var identity = AskForIdentity(null, publishingNow: true, gitOwnName: "");
            if (identity == null)
            {
                const string cancelled = "Publish cancelled: no name to show on GitHub was given. No files were sent; " +
                    "press Publish Selected when you are ready.";
                LogMessage(cancelled);
                return null;
            }
            return SaveIdentityOrWarn(identity, publishingNow: true) ? identity : null;
        }

        /// <summary>Keeps the name in SEAGit's settings (identity.json beside repos.json); says so if it cannot.</summary>
        private bool SaveIdentityOrWarn(CommitIdentity identity, bool publishingNow)
        {
            try
            {
                _storageService.SaveIdentity(identity);
                LogMessage($"Your name on GitHub is now \"{identity.Name}\"" +
                    (identity.Email.Length > 0 ? $" ({identity.Email})." : " (no e-mail: SEAGit uses a placeholder address)."));
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LogMessage("Could not save your name on GitHub: " + ex.Message);
                MessageBox.Show(this, "SEAGit could not save your name on GitHub" +
                    (publishingNow ? ", so nothing was published. " : ". Your name was not changed. ") + PlainWords.SeeLog,
                    AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async void MnuSettingsName_Click(object? sender, EventArgs e)
        {
            if (_busy) return;
            string gitOwnName = _gitService.IsGitInstalled() ? await _gitService.GitOwnNameAsync() : "";
            var identity = AskForIdentity(_storageService.LoadIdentity(), publishingNow: false, gitOwnName);
            if (identity == null || !SaveIdentityOrWarn(identity, publishingNow: false)) return;
            MessageBox.Show(this, gitOwnName.Length > 0
                    ? $"Saved. Git on this computer has a name of its own, \"{gitOwnName}\", so GitHub shows \"{gitOwnName}\", " +
                      $"not \"{identity.Name}\", beside the changes you publish."
                    : $"Saved. GitHub will show \"{identity.Name}\" beside the changes you publish from now on.",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Shows the GitHub address window; the checked address, or null on Cancel.</summary>
        private string? AskForGitHubAddress(GitRepository repo, string current, bool publishingNow)
        {
            using var dialog = new GitHubAddressForm(repo.Name, current, publishingNow);
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.Address : null;
        }

        private async void BtnChangeAddress_Click(object? sender, EventArgs e)
        {
            var repo = SelectedOrWarn("Change GitHub Address…");
            if (repo == null) return;
            if (!EnsureGitOrWarn() || !FolderStillThere(repo)) return;

            if (!_gitService.IsValidGitRepo(repo.LocalPath))
            {
                MessageBox.Show(this, $"\"{repo.Name}\" has not been prepared for publishing yet. Press Publish Selected: " +
                    "SEAGit asks for its GitHub address then.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true, "");
            try
            {
                string current = await _gitService.GetRemoteUrlAsync(repo.LocalPath);
                string? chosen = AskForGitHubAddress(repo, current, publishingNow: false);
                // The same address written another way (without .git, say) is not a change.
                string currentTidy = PlainWords.NormaliseGitHubAddress(current, out _) ?? current;
                if (chosen == null || string.Equals(chosen, currentTidy, StringComparison.OrdinalIgnoreCase)) return;

                var r = await _gitService.SetRemoteAsync(repo.LocalPath, chosen);
                if (!r.Ok)
                {
                    LogMessage(r.Raw);
                    MessageBox.Show(this, "SEAGit could not change the GitHub address.\n\n" + PlainWords.ExplainGitError(r.Raw),
                        AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // The new GitHub repository has none of this folder yet: the next Publish Selected sends all of it.
                await _gitService.RunAsync(repo.LocalPath, "branch", "--unset-upstream");
                LogMessage($"{repo.Name} now publishes to {chosen}");
                MessageBox.Show(this, $"From now on, \"{repo.Name}\" publishes to\n{chosen}\n\n" +
                    "Press Publish Selected to copy the folder there.", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                SetBusy(false, "");
            }
        }

        private void BtnRemove_Click(object? sender, EventArgs e)
        {
            var repo = SelectedOrWarn("Remove from List");
            if (repo == null) return;

            if (MessageBox.Show(this,
                    $"Remove \"{repo.Name}\" from SEAGit's list?\n\n" +
                    "Your files are not deleted: the folder stays on your computer, and anything already published stays on GitHub. " +
                    "You can add the folder again at any time with the + Add Local Folder… button.",
                    AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _repositories.Remove(repo);
            _storageService.SaveRepositories(_repositories);
            RefreshRepoList();
            LogMessage($"Removed from the list (the folder itself was not touched): {repo.LocalPath}");
        }

        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            var repo = SelectedOrWarn("Open Folder");
            if (repo == null || !FolderStillThere(repo)) return;
            try
            {
                Process.Start(new ProcessStartInfo(repo.LocalPath) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                MessageBox.Show(this, $"Windows could not open the folder. It is at\n{repo.LocalPath}", AppTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MnuHelpScreen_Click(object? sender, EventArgs e)
        {
            using var help = new HelpForm("the SEAGit window", HelpForm.MainWindowHelp, "index.html");
            help.ShowDialog(this);
        }

        private void MnuHelpWeb_Click(object? sender, EventArgs e) =>
            HelpForm.OpenInBrowser(this, "https://softwareplus.ai/seagit/");

        private async void MnuCheckUpdates_Click(object? sender, EventArgs e)
        {
            if (_busy)
            {
                MessageBox.Show(this, "SEAGit is publishing. Check for a new version when it has finished.", AppTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SetBusy(true, "Asking the Microsoft Store…");
            try
            {
                await StoreUpdates.CheckAsync(this);
            }
            finally
            {
                SetBusy(false, "");
            }
        }

        private void MnuAbout_Click(object? sender, EventArgs e)
        {
            using var about = new AboutForm();
            about.ShowDialog(this);
        }

        /// <summary>While Git works, the buttons that start another Git step are off and the status line says why.</summary>
        private void SetBusy(bool busy, string status)
        {
            _busy = busy;
            btnAddRepo.Enabled = btnPublish.Enabled = btnChangeAddress.Enabled = btnRemove.Enabled = !busy;
            mnuSettingsName.Enabled = !busy;
            lstRepos.Enabled = !busy;
            btnPublish.Text = busy && status.StartsWith("Publishing", StringComparison.Ordinal) ? "Publishing…" : "&Publish Selected";
            // Disabled buttons cannot show a tooltip in WinForms, so the status line says why they are off (checklist 2.3).
            lblStatus.Text = !busy ? IdleStatus()
                : status.Length > 0 ? status + " The buttons come back when SEAGit has finished." : "";
            UseWaitCursor = false;
            Cursor = busy ? Cursors.AppStarting : Cursors.Default;
        }

        private void RefreshRepoList()
        {
            var selected = lstRepos.SelectedItem;
            lstRepos.DataSource = null;
            lstRepos.DataSource = _repositories;
            lstRepos.DisplayMember = nameof(GitRepository.Name);
            if (selected != null && _repositories.Contains(selected)) lstRepos.SelectedItem = selected;
            _emptyList.Visible = _repositories.Count == 0;
            if (!_busy) lblStatus.Text = IdleStatus();
        }

        /// <summary>Shown over the empty list, so it is never a blank white box (usability checklist 1.4).</summary>
        private readonly Label _emptyList = new()
        {
            Text = "No folders yet. Press + Add Local Folder… to choose the folder you want to publish.",
            AutoSize = false,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            BackColor = System.Drawing.SystemColors.Window,
            ForeColor = System.Drawing.SystemColors.GrayText,
        };

        private void AddEmptyListHint()
        {
            _emptyList.Bounds = new System.Drawing.Rectangle(lstRepos.Left + 2, lstRepos.Top + 2, lstRepos.Width - 4, lstRepos.Height - 4);
            _emptyList.Anchor = lstRepos.Anchor;
            _emptyList.Click += (_, _) => btnAddRepo.PerformClick();
            _tips.SetToolTip(_emptyList, "Your list of folders is empty. Click here, or press + Add Local Folder…, to add one.");
            Controls.Add(_emptyList);
            _emptyList.BringToFront();
        }

        private const string ListTip = "Your folders: each one's name, when it was last published, and where it is on this " +
            "computer. Select one, then use the buttons. Double-click one to open it.";

        private int _formattedWidth = -1;
        private string _shownListTip = ListTip;

        /// <summary>
        /// One row: name, when it was last published, and where it is. A long place is shortened in the middle to fit
        /// the list's width (no sideways scrolling); the row's tooltip and Open Folder use the full place.
        /// </summary>
        private void LstRepos_Format(object? sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is not GitRepository repo) return;
            string dateStr = repo.LastPublished == DateTime.MinValue ? "never published" : "last published " + repo.LastPublished.ToString("g");
            string head = $"{repo.Name}   —   {dateStr}   —   ";
            int Measure(string s) => TextRenderer.MeasureText(s, lstRepos.Font, System.Drawing.Size.Empty,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
            int room = lstRepos.ClientSize.Width - 8 - Measure(head);
            e.Value = head + PlainWords.ShortenMiddle(repo.LocalPath, room, Measure);
        }

        /// <summary>Over a row, the tooltip gives that folder's full place on this computer.</summary>
        private void LstRepos_MouseMove(object? sender, MouseEventArgs e)
        {
            int i = lstRepos.IndexFromPoint(e.Location);
            ShowListTip(i >= 0 && i < _repositories.Count
                ? $"{_repositories[i].Name}\n{_repositories[i].LocalPath}\nDouble-click to open it in File Explorer."
                : ListTip);
        }

        private void ShowListTip(string tip)
        {
            if (tip == _shownListTip) return;
            _shownListTip = tip;
            _tips.SetToolTip(lstRepos, tip);
        }

        /// <summary>What the status line says when SEAGit is not working, so it is never a blank gap.</summary>
        private string IdleStatus() => _repositories.Count == 0
            ? "Ready. Press + Add Local Folder… to start."
            : "Ready. Select a folder, then press Publish Selected.";

        private void LogMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message.Replace("\r\n", "\n").TrimEnd()}\n");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
    }
}
