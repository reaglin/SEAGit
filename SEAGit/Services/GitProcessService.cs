using System;
using System.Diagnostics;
using System.IO;

namespace SEAGit.Services
{
    public class GitProcessService
    {
        public bool IsValidGitRepo(string path)
        {
            return Directory.Exists(Path.Combine(path, ".git"));
        }

        public string InitializeRepo(string targetDirectory)
        {
            return RunGitCommand(targetDirectory, "init");
        }

        public string CommitAndPush(string targetDirectory, string commitMessage = "Auto-published via SEAGit")
        {
            // 1. Stage all changes
            RunGitCommand(targetDirectory, "add -A");

            // 2. Commit the changes
            string commitCommand = $"commit -m \"{commitMessage}\"";
            RunGitCommand(targetDirectory, commitCommand);

            // 3. Push to GitHub (Relies on Git Credential Manager for auth)
            // Note: Assumes 'main' is the default branch. 
            return RunGitCommand(targetDirectory, "push origin main");
        }

        private string RunGitCommand(string workingDirectory, string arguments)
        {
            var processInfo = new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process == null) return "Error: Could not initialize the Git process.";

                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // Git often writes progress and warnings to StandardError even on a successful exit.
                // We check the ExitCode to determine true success.
                return process.ExitCode == 0 ? output : $"Error executing '{arguments}':\n{error}";
            }
        }
    }
}