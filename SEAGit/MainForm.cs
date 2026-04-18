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

                    // Check if it's already in the list
                    if (_repositories.Any(r => r.LocalPath.Equals(selectedPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        LogMessage($"Folder is already tracked: {selectedPath}");
                        return;
                    }

                    // Check if it's actually a Git repo
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

            btnPublish.Enabled = false;
            btnPublish.Text = "Publishing...";
            LogMessage($"\n--- Publishing {selectedRepo.Name} ---");
            Application.DoEvents(); // Force UI update before blocking thread

            try
            {
                string result = _gitService.CommitAndPush(selectedRepo.LocalPath);
                LogMessage(result);

                // Update the last published timestamp
                selectedRepo.LastPublished = DateTime.Now;
                _storageService.SaveRepositories(_repositories);
                RefreshRepoList();
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

        private void RefreshRepoList()
        {
            lstRepos.DataSource = null;
            lstRepos.DataSource = _repositories;
            
            // Format how the object looks in the ListBox
            lstRepos.DisplayMember = "Name"; 
            
            // Optional: You could create a custom string format here to show the LocalPath or LastPublished date
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
            // Auto-scroll to bottom
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
    }
}