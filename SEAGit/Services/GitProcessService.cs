using System;
using System.Diagnostics;
using System.IO;

namespace SEAGit.Services
{
    public class GitProcessService
    {
        /// <summary>
        /// Path to the bundled Git for Windows launcher (MinGit). SEAGit ships
        /// MinGit alongside the executable so users do not need a separate Git
        /// install — and so the app does not have to direct users to a third-
        /// party download, which violates Microsoft Store policy 10.1.5. The
        /// cmd\git.exe wrapper sets up PATH/exec-path for the real binary at
        /// mingw64\bin\git.exe.
        /// </summary>
        public static string GitExePath { get; } =
            Path.Combine(AppContext.BaseDirectory, "MinGit", "cmd", "git.exe");

        public bool IsValidGitRepo(string path)
        {
            return Directory.Exists(Path.Combine(path, ".git"));
        }

        /// <summary>
        /// True if the bundled MinGit component is present on disk. With Git
        /// shipped inside the MSIX this is effectively always true, but the
        /// check stays so a corrupted install can be reported cleanly rather
        /// than throwing Win32Exception ("file not found") from Process.Start.
        /// </summary>
        public bool IsGitInstalled()
        {
            return File.Exists(GitExePath);
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
            var processInfo = new ProcessStartInfo(GitExePath, arguments)
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
                    if (process == null) return "Error: Could not start the bundled Git process.";

                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Git pushes often write progress to StandardError. We check ExitCode for true success.
                    return process.ExitCode == 0 ? output : $"Error executing '{arguments}':\n{error}";
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // The bundled git.exe could not be started — the MinGit component
                // appears to be missing from the install. Surface a clean, internal
                // message; do not link to a third-party download (Store policy 10.1.5).
                return "Error: SEAGit's bundled Git component could not be started. " +
                       "The MinGit files may be missing from the install. Please reinstall SEAGit.";
            }
        }
    }
}
