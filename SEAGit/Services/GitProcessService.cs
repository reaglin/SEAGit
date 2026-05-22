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

        /// <summary>
        /// True if Git for Windows is available (git --version succeeds). SEAGit
        /// shells out to git.exe; when it is not installed Process.Start throws
        /// Win32Exception ("The system cannot find the file specified"), which
        /// previously surfaced as an unexpected-error crash during "initialize
        /// repository". Used to warn the user up front and gate git actions.
        /// </summary>
        public bool IsGitInstalled()
        {
            try
            {
                var psi = new ProcessStartInfo("git", "--version")
                {
                    CreateNoWindow         = true,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };
                using var p = Process.Start(psi);
                if (p == null) return false;
                p.WaitForExit();
                return p.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public string InitializeRepo(string targetDirectory)
        {
            string initResult = RunGitCommand(targetDirectory, "init");

            // Immediately rename the default branch to 'main' to match modern GitHub standards
            RunGitCommand(targetDirectory, "branch -M main");

            return initResult.StartsWith("Error") ? initResult : "Repository initialized successfully.";
        }

        public bool HasRemote(string targetDirectory)
        {
            string output = RunGitCommand(targetDirectory, "remote -v");
            return !string.IsNullOrWhiteSpace(output) && !output.StartsWith("Error");
        }

        public string AddRemote(string targetDirectory, string url)
        {
            return RunGitCommand(targetDirectory, $"remote add origin \"{url}\"");
        }

        public string CommitAndPush(string targetDirectory, string repoName, string commitMessage = "Auto-published via SEAGit")
        {
            // 1. Check for changes using 'porcelain' (machine-readable format)
            string statusOutput = RunGitCommand(targetDirectory, "status --porcelain");

            if (statusOutput.StartsWith("Error"))
                return statusOutput;

            if (string.IsNullOrWhiteSpace(statusOutput))
            {
                return $"No changes made to {repoName}. Everything is up to date.";
            }

            // Count the lines in the porcelain output to get the number of modified/added files
            int fileCount = statusOutput.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

            // 2. Stage all changes
            RunGitCommand(targetDirectory, "add -A");

            // 3. Commit the changes
            string commitCommand = $"commit -m \"{commitMessage}\"";
            string commitRes = RunGitCommand(targetDirectory, commitCommand);

            if (commitRes.StartsWith("Error"))
                return commitRes;

            // 4. Push to GitHub (Using -u to set upstream tracking if this is the very first push)
            string pushRes = RunGitCommand(targetDirectory, "push -u origin main");

            if (pushRes.StartsWith("Error"))
                return pushRes;

            return $"{fileCount} file(s) successfully pushed to repository {repoName}.";
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

            try
            {
                using (var process = Process.Start(processInfo))
                {
                    if (process == null) return "Error: Could not start the Git process.";

                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Git pushes often write progress to StandardError. We check ExitCode for true success.
                    return process.ExitCode == 0 ? output : $"Error executing '{arguments}':\n{error}";
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // git.exe could not be started — Git for Windows is not installed
                // or not on the PATH. Return a clear, actionable message instead
                // of letting the Win32Exception crash the app.
                return "Error: Git for Windows is not installed (or not on the PATH). " +
                       "SEAGit uses Git for Windows to publish your folders to GitHub. " +
                       "Install it from https://gitforwindows.org/ and restart SEAGit.";
            }
        }
    }
}