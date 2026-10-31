using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SEAGit.Services
{
    /// <summary>What one run of git.exe returned.</summary>
    public sealed record GitResult(int ExitCode, string Output, string Error)
    {
        public bool Ok => ExitCode == 0;

        /// <summary>Everything Git printed, for the Activity Log.</summary>
        public string Raw => string.Join("\n", new[] { Output.Trim(), Error.Trim() }.Where(s => s.Length > 0));
    }

    /// <summary>How a press of Publish Selected ended.</summary>
    public enum PublishOutcome
    {
        /// <summary>New changes were saved and sent to GitHub.</summary>
        Published,
        /// <summary>Nothing new in the folder, but changes saved earlier had not reached GitHub; they were sent now.</summary>
        PublishedEarlierChanges,
        /// <summary>Everything is already on GitHub.</summary>
        NothingToPublish,
        /// <summary>The folder has no files to publish at all.</summary>
        FolderEmpty,
        /// <summary>Git reported a problem; <see cref="PublishResult.RawError"/> has its words.</summary>
        Failed,
    }

    public sealed record PublishResult(PublishOutcome Outcome, int FileCount, string RawError = "")
    {
        public bool Succeeded => Outcome is PublishOutcome.Published or PublishOutcome.PublishedEarlierChanges;
    }

    /// <summary>One file that would go to GitHub at the next publish.</summary>
    public sealed record PendingFile(string RelativePath, long Bytes);

    public class GitProcessService
    {
        /// <summary>GitHub refuses any single file larger than this (100 MB).</summary>
        public const long GitHubFileLimitBytes = 100L * 1024 * 1024;

        /// <summary>
        /// Used for a commit only when this computer has no Git name/e-mail set (the bundled MinGit has none, so the
        /// first commit on a new PC failed with "Author identity unknown"). The .invalid address can never belong to a
        /// GitHub account, so GitHub will not show the commits as somebody else's.
        /// </summary>
        public const string FallbackEmail = "seagit-user@seagit.invalid";

        public const string DefaultCommitMessage = "Published with SEAGit";

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

        private readonly string _gitExe;
        private readonly IReadOnlyDictionary<string, string>? _extraEnvironment;

        /// <param name="gitExe">git.exe to run; the bundled MinGit when omitted. Tests pass their own.</param>
        /// <param name="extraEnvironment">Extra environment variables for every git run (tests use it to hide the
        /// computer's own Git settings).</param>
        public GitProcessService(string? gitExe = null, IReadOnlyDictionary<string, string>? extraEnvironment = null)
        {
            _gitExe = gitExe ?? GitExePath;
            _extraEnvironment = extraEnvironment;
        }

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
            return File.Exists(_gitExe);
        }

        /// <summary>Makes the folder ready for publishing (git init, on a branch called main).</summary>
        public async Task<GitResult> InitializeRepoAsync(string targetDirectory)
        {
            var init = await RunAsync(targetDirectory, "init").ConfigureAwait(false);
            if (!init.Ok) return init;
            // Name the first branch 'main', like GitHub. (symbolic-ref works before the first commit, on any Git.)
            return await RunAsync(targetDirectory, "symbolic-ref", "HEAD", "refs/heads/main").ConfigureAwait(false);
        }

        public async Task<bool> HasRemoteAsync(string targetDirectory)
        {
            return !string.IsNullOrWhiteSpace(await GetRemoteUrlAsync(targetDirectory).ConfigureAwait(false));
        }

        /// <summary>The GitHub address the folder publishes to, or "" when it has none.</summary>
        public async Task<string> GetRemoteUrlAsync(string targetDirectory)
        {
            var r = await RunAsync(targetDirectory, "remote", "get-url", "origin").ConfigureAwait(false);
            return r.Ok ? r.Output.Trim() : "";
        }

        /// <summary>Links the folder to a GitHub address, or changes the address it is linked to.</summary>
        public async Task<GitResult> SetRemoteAsync(string targetDirectory, string url)
        {
            bool has = await HasRemoteAsync(targetDirectory).ConfigureAwait(false);
            return has
                ? await RunAsync(targetDirectory, "remote", "set-url", "origin", url).ConfigureAwait(false)
                : await RunAsync(targetDirectory, "remote", "add", "origin", url).ConfigureAwait(false);
        }

        /// <summary>True once the folder has been published at least once (its branch has an upstream on GitHub).</summary>
        public async Task<bool> HasBeenPublishedAsync(string targetDirectory)
        {
            var r = await RunAsync(targetDirectory, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}").ConfigureAwait(false);
            return r.Ok;
        }

        /// <summary>When GitHub's copy last changed (the date of its latest commit); null if never published.</summary>
        public async Task<DateTime?> LastPublishedAsync(string targetDirectory)
        {
            if (!IsValidGitRepo(targetDirectory) || !await HasBeenPublishedAsync(targetDirectory).ConfigureAwait(false))
                return null;
            var r = await RunAsync(targetDirectory, "log", "-1", "--format=%cI", "@{u}").ConfigureAwait(false);
            return r.Ok && DateTimeOffset.TryParse(r.Output.Trim(), out var when) ? when.LocalDateTime : null;
        }

        /// <summary>
        /// The files a publish would send. For a folder never published: every file Git would keep (not ignored).
        /// Afterwards: the files that changed since the last save. Sizes are read from disk (0 for a deleted file).
        /// </summary>
        public async Task<List<PendingFile>> GetFilesToPublishAsync(string targetDirectory, bool firstPublish)
        {
            var files = new List<PendingFile>();
            GitResult r;
            if (firstPublish)
            {
                r = await RunAsync(targetDirectory, "ls-files", "-z", "--cached", "--others", "--exclude-standard").ConfigureAwait(false);
                if (!r.Ok) return files;
                foreach (var path in r.Output.Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct())
                    files.Add(new PendingFile(path, SizeOf(targetDirectory, path)));
                return files;
            }

            r = await RunAsync(targetDirectory, "status", "--porcelain", "-z", "--untracked-files=all").ConfigureAwait(false);
            if (!r.Ok) return files;
            var parts = r.Output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string entry = parts[i];
                if (entry.Length < 4) continue;
                char x = entry[0];
                string path = entry.Substring(3);
                if (x is 'R' or 'C') i++; // a rename/copy is followed by its old name
                files.Add(new PendingFile(path, SizeOf(targetDirectory, path)));
            }
            return files;
        }

        private static long SizeOf(string root, string relative)
        {
            try
            {
                var info = new FileInfo(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
                return info.Exists ? info.Length : 0;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return 0;
            }
        }

        /// <summary>
        /// Saves every change in the folder and sends it to GitHub. When there is nothing new, it still sends changes
        /// saved earlier that never reached GitHub (a publish whose sign-in or connection failed), so they are not left
        /// behind on this computer. <paramref name="progress"/> receives plain-words steps for the Activity Log.
        /// </summary>
        public async Task<PublishResult> PublishAsync(string targetDirectory, IProgress<string>? progress = null,
            string commitMessage = DefaultCommitMessage)
        {
            var status = await RunAsync(targetDirectory, "status", "--porcelain", "--untracked-files=all").ConfigureAwait(false);
            if (!status.Ok) return Fail(status);

            int fileCount = status.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

            if (fileCount > 0)
            {
                progress?.Report($"Saving {Files(fileCount)}…");
                var add = await RunAsync(targetDirectory, "add", "-A").ConfigureAwait(false);
                if (!add.Ok) return Fail(add);

                var identity = await IdentityArgumentsAsync(targetDirectory).ConfigureAwait(false);
                var commitArgs = identity.Concat(new[] { "commit", "-m", commitMessage }).ToArray();
                var commit = await RunAsync(targetDirectory, commitArgs).ConfigureAwait(false);
                if (!commit.Ok) return Fail(commit);

                progress?.Report("Sending to GitHub…");
                var push = await PushAsync(targetDirectory).ConfigureAwait(false);
                return push.Ok ? new PublishResult(PublishOutcome.Published, fileCount) : Fail(push);
            }

            // Nothing new in the folder. Is there anything saved here that GitHub does not have yet?
            var head = await RunAsync(targetDirectory, "rev-parse", "--verify", "--quiet", "HEAD").ConfigureAwait(false);
            if (!head.Ok)
                return new PublishResult(PublishOutcome.FolderEmpty, 0);

            if (!await HasBeenPublishedAsync(targetDirectory).ConfigureAwait(false))
            {
                // Saved here but this GitHub repository has none of it: the first publish did not reach GitHub, or the
                // GitHub address was just changed. The whole folder goes.
                var all = await RunAsync(targetDirectory, "ls-files").ConfigureAwait(false);
                progress?.Report("Sending the folder to GitHub…");
                var first = await PushAsync(targetDirectory).ConfigureAwait(false);
                return first.Ok ? new PublishResult(PublishOutcome.Published, CountLines(all.Output)) : Fail(first);
            }

            var ahead = await RunAsync(targetDirectory, "rev-list", "--count", "@{u}..HEAD").ConfigureAwait(false);
            if (!ahead.Ok) return Fail(ahead);
            if (!int.TryParse(ahead.Output.Trim(), out int commits) || commits == 0)
                return new PublishResult(PublishOutcome.NothingToPublish, 0);
            var changed = await RunAsync(targetDirectory, "diff", "--name-only", "@{u}", "HEAD").ConfigureAwait(false);

            progress?.Report("Publishing changes saved earlier that had not reached GitHub yet…");
            var resend = await PushAsync(targetDirectory).ConfigureAwait(false);
            return resend.Ok ? new PublishResult(PublishOutcome.PublishedEarlierChanges, CountLines(changed.Output)) : Fail(resend);
        }

        /// <summary>
        /// Sends the current branch, whatever it is called (main on folders SEAGit prepared, often master on older
        /// ones), and remembers GitHub's copy as its upstream.
        /// </summary>
        private Task<GitResult> PushAsync(string targetDirectory) =>
            RunAsync(targetDirectory, "push", "-u", "origin", "HEAD");

        /// <summary>
        /// "-c user.name=… -c user.email=…" when this computer has no Git name or e-mail, so the commit does not fail
        /// with "Author identity unknown". A name or e-mail the person set themselves is always used instead.
        /// </summary>
        private async Task<string[]> IdentityArgumentsAsync(string targetDirectory)
        {
            var args = new List<string>();
            var name = await RunAsync(targetDirectory, "config", "user.name").ConfigureAwait(false);
            if (!name.Ok || string.IsNullOrWhiteSpace(name.Output))
            {
                string who = string.IsNullOrWhiteSpace(Environment.UserName) ? "SEAGit user" : Environment.UserName;
                args.Add("-c"); args.Add("user.name=" + who);
            }
            var email = await RunAsync(targetDirectory, "config", "user.email").ConfigureAwait(false);
            if (!email.Ok || string.IsNullOrWhiteSpace(email.Output))
            {
                args.Add("-c"); args.Add("user.email=" + FallbackEmail);
            }
            return args.ToArray();
        }

        private static PublishResult Fail(GitResult r) => new(PublishOutcome.Failed, 0, r.Raw);

        private static int CountLines(string text) =>
            text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

        public static string Files(int n) => n == 1 ? "1 file" : $"{n} files";

        /// <summary>
        /// Runs git.exe without blocking the window. Output and errors are read while Git runs (reading them only
        /// after it ended could deadlock when Git wrote a lot). Git may never ask a question in a console nobody can
        /// see (GIT_TERMINAL_PROMPT=0); GitHub sign-in still opens through Git Credential Manager in the browser.
        /// </summary>
        public async Task<GitResult> RunAsync(string workingDirectory, params string[] arguments)
        {
            var processInfo = new ProcessStartInfo(_gitExe)
            {
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var a in arguments) processInfo.ArgumentList.Add(a);
            processInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
            if (_extraEnvironment != null)
                foreach (var kv in _extraEnvironment) processInfo.Environment[kv.Key] = kv.Value;

            try
            {
                using var process = Process.Start(processInfo);
                if (process == null) return new GitResult(-1, "", MissingGitMessage);
                process.StandardInput.Close();

                Task<string> output = process.StandardOutput.ReadToEndAsync();
                Task<string> error = process.StandardError.ReadToEndAsync();
                await Task.WhenAll(output, error, process.WaitForExitAsync()).ConfigureAwait(false);
                return new GitResult(process.ExitCode, output.Result, error.Result);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // The bundled git.exe could not be started — the MinGit component
                // appears to be missing from the install. Surface a clean, internal
                // message; do not link to a third-party download (Store policy 10.1.5).
                return new GitResult(-1, "", MissingGitMessage);
            }
        }

        public const string MissingGitMessage =
            "SEAGit's built-in Git could not be started. Its files may be missing from the install. Please reinstall SEAGit.";
    }
}
