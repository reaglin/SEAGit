using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SEAGit.Models;
using SEAGit.Services;

namespace SEAGit
{
    public partial class MainForm : Form
    {
        private readonly StorageService _storageService;
        private readonly GitProcessService _gitService;
        private List<GitRepository> _repositories;

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
        }

        private bool _gitCheckDone;

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Keep launch resilient: any failure here is logged and surfaced, but
            // never allowed to escape and terminate the app before the window is
            // shown. The Git-for-Windows check is deferred to OnShown so the main
            // window paints first (see below).
            try
            {
                _repositories = _storageService.LoadRepositories();
                RefreshRepoList();
                LogMessage("SEAGit initialized and ready.");
            }
            catch (Exception ex)
            {
                LogMessage("Startup warning: " + ex.Message);
                MessageBox.Show(
                    "SEAGit started but could not load its saved repository list.\n\n" + ex.Message,
                    "SEAGit — Startup Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Run the Git-for-Windows check only AFTER the main window is painted,
            // so the app visibly launches before any modal dialog appears. The
            // Store cert lab runs on a machine with no Git installed; showing this
            // dialog from Load — before the window was drawn — made the app look
            // like it failed to launch. SEAGit drives Git for Windows, so if it is
            // missing we tell the user up front rather than failing later.
            if (_gitCheckDone) return;
            _gitCheckDone = true;

            try
            {
                if (!_gitService.IsGitInstalled())
                {
                    LogMessage("⚠ Git for Windows was not found. SEAGit needs it to publish folders to " +
                               "GitHub. Install it free from https://gitforwindows.org/, then restart SEAGit.");
                    ShowGitRequiredDialog();
                }
            }
            catch (Exception ex)
            {
                LogMessage("Could not check for Git for Windows: " + ex.Message);
            }
        }

        /// <summary>
        /// Returns true if Git for Windows is available; otherwise shows the
        /// "Git for Windows required" dialog and returns false. Call before any
        /// action that shells out to git.
        /// </summary>
        private bool EnsureGitOrWarn()
        {
            if (_gitService.IsGitInstalled()) return true;
            ShowGitRequiredDialog();
            return false;
        }

        private void ShowGitRequiredDialog()
        {
            var choice = MessageBox.Show(
                "Git for Windows is required.\n\n" +
                "SEAGit publishes your folders to GitHub using Git for Windows, which does not " +
                "appear to be installed on this PC.\n\n" +
                "Install Git for Windows (it is free), then restart SEAGit.\n\n" +
                "Open the Git for Windows download page now?",
                "SEAGit — Git for Windows Required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (choice == DialogResult.Yes)
                OpenUrl("https://gitforwindows.org/");
        }

        private static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* nothing useful to do if the browser cannot be opened */ }
        }

        private void BtnAddRepo_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a local Git repository folder";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;

                    if (_repositories.Any(r => r.LocalPath.Equals(selectedPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        LogMessage($"Folder is already tracked: {selectedPath}");
                        return;
                    }

                    if (!_gitService.IsValidGitRepo(selectedPath))
                    {
                        var result = MessageBox.Show(
                            "This folder is not a Git repository. Would you like SEAGit to initialize it for you?",
                            "Initialize Repository?",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            if (!EnsureGitOrWarn()) return;
                            string initLog = _gitService.InitializeRepo(selectedPath);
                            LogMessage(initLog);
                        }
                        else
                        {
                            return;
                        }
                    }

                    var newRepo = new GitRepository
                    {
                        Name = new DirectoryInfo(selectedPath).Name,
                        LocalPath = selectedPath,
                        LastPublished = DateTime.MinValue
                    };

                    _repositories.Add(newRepo);
                    _storageService.SaveRepositories(_repositories);
                    RefreshRepoList();

                    LogMessage($"Added tracking for: {newRepo.Name}");
                }
            }
        }

        private void BtnPublish_Click(object sender, EventArgs e)
        {
            if (lstRepos.SelectedItem == null)
            {
                MessageBox.Show("Please select a repository from the list first.", "Select Repo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureGitOrWarn()) return;

            var selectedRepo = (GitRepository)lstRepos.SelectedItem;

            // Check if remote URL exists before trying to push
            if (!_gitService.HasRemote(selectedRepo.LocalPath))
            {
                string url = PromptForGitHubUrl();
                if (string.IsNullOrWhiteSpace(url))
                {
                    LogMessage("Publish cancelled. A remote URL is required to publish.");
                    return;
                }

                string remoteResult = _gitService.AddRemote(selectedRepo.LocalPath, url);
                if (remoteResult.StartsWith("Error"))
                {
                    LogMessage(remoteResult);
                    return;
                }
                LogMessage($"Successfully linked {selectedRepo.Name} to GitHub.");
            }

            btnPublish.Enabled = false;
            btnPublish.Text = "Publishing...";
            LogMessage($"\n--- Analyzing {selectedRepo.Name} ---");
            Application.DoEvents();

            try
            {
                string result = _gitService.CommitAndPush(selectedRepo.LocalPath, selectedRepo.Name);
                LogMessage(result);

                // Only update the timestamp if it actually pushed changes (avoids errors being flagged as success)
                if (!result.StartsWith("Error") && !result.StartsWith("No changes"))
                {
                    selectedRepo.LastPublished = DateTime.Now;
                    _storageService.SaveRepositories(_repositories);
                    RefreshRepoList();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"CRITICAL ERROR: {ex.Message}");
            }
            finally
            {
                btnPublish.Enabled = true;
                btnPublish.Text = "Publish Selected";
            }
        }

        // Programmatically generate an input form for the GitHub URL
        private string PromptForGitHubUrl()
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 500;
                prompt.Height = 160;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Link to GitHub";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Label textLabel = new Label() { Left = 20, Top = 20, Width = 440, Text = "This repository isn't linked to GitHub yet.\nPlease paste your empty GitHub Repository URL:" };
                TextBox textBox = new TextBox() { Left = 20, Top = 60, Width = 440 };
                Button confirmation = new Button() { Text = "Link and Publish", Left = 340, Width = 120, Top = 90, DialogResult = DialogResult.OK };

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : string.Empty;
            }
        }

        private void RefreshRepoList()
        {
            lstRepos.DataSource = null;
            lstRepos.DataSource = _repositories;
            lstRepos.DisplayMember = "Name";

            lstRepos.Format += (s, e) =>
            {
                var repo = (GitRepository)e.ListItem;
                string dateStr = repo.LastPublished == DateTime.MinValue ? "Never" : repo.LastPublished.ToString("g");
                e.Value = $"{repo.Name}   [Last Published: {dateStr}]   -> {repo.LocalPath}";
            };
        }

        private void LogMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
    }
}