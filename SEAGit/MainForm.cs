using System;
using System.Collections.Generic;
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
            this.Icon = new System.Drawing.Icon("seagit.ico");
            InitializeComponent();
            _storageService = new StorageService();
            _gitService = new GitProcessService();
            _repositories = new List<GitRepository>();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _repositories = _storageService.LoadRepositories();
            RefreshRepoList();
            LogMessage("SEAGit initialized and ready.");
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