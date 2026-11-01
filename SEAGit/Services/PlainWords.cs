using System;
using System.IO;

namespace SEAGit.Services
{
    /// <summary>
    /// Turns Git's own words into words a first-time user can act on. Git's raw text still goes to the Activity Log;
    /// these sentences go in the message box. Words used everywhere: "folder" is the thing on this computer, "GitHub
    /// repository" is the copy on github.com, "Publish" is the action.
    /// </summary>
    public static class PlainWords
    {
        public const string SeeLog = "The details Git gave are in the Activity Log at the bottom of the SEAGit window.";

        /// <summary>A plain explanation of a failed Git step, with what to do next.</summary>
        /// <param name="publishedBefore">True when the folder had been published to this GitHub repository before:
        /// a refusal then means GitHub has changes the folder lacks, not a repository that was never empty.</param>
        public static string ExplainGitError(string raw, bool publishedBefore = false)
        {
            string t = raw ?? "";
            bool Has(string s) => t.Contains(s, StringComparison.OrdinalIgnoreCase);

            if (t == GitProcessService.MissingGitMessage || Has("built-in Git could not be started"))
                return GitProcessService.MissingGitMessage;

            if (Has("Authentication failed") || Has("could not read Username") || Has("could not read Password") ||
                Has("terminal prompts disabled") || Has("Invalid username or password") || Has("Logon failed"))
                return "GitHub sign-in did not finish, so nothing was sent to GitHub.\n\n" +
                       "Press Publish Selected again, and finish signing in to GitHub in the browser window that opens. " +
                       "Your changes are kept on this computer and will be sent then.";

            // Checked first: GitHub's large-file refusal also names files, and a name could contain anything.
            if (Has("GH001") || Has("exceeds GitHub's file size limit") || Has("Large files detected"))
                return "GitHub refused a file because it is over 100 MB. GitHub does not accept files that large.\n\n" +
                       "Move the large file out of the folder, then press Publish Selected again.";

            if (Has("Repository not found") || Has("returned error: 403") || Has("Permission to") || Has("does not appear to be a git repository"))
                return "GitHub could not find that GitHub repository, or your GitHub account is not allowed to publish to it.\n\n" +
                       "Check the address with Change GitHub Address…, and that you signed in as the GitHub account that owns " +
                       "the repository. Your changes are kept on this computer.";

            if (Has("Could not resolve host") || Has("Failed to connect") || Has("Connection timed out") ||
                Has("Couldn't connect") || Has("Connection was reset") || Has("unable to access"))
                return "SEAGit could not reach GitHub. Check that this computer is connected to the internet, then press " +
                       "Publish Selected again. Your changes are kept on this computer and will be sent then.";

            bool refused = Has("[rejected]") || Has("fetch first") || Has("non-fast-forward") || Has("Updates were rejected");
            if (refused && publishedBefore)
                return "The GitHub repository has changes that are not in this folder. Usually a file was edited on github.com, " +
                       "or the folder was published from another computer. SEAGit cannot publish over them, and your files here " +
                       "are not changed.\n\n" +
                       "Undo that change on github.com, or ask someone who knows Git to bring it into this folder, then press " +
                       "Publish Selected again.";

            if (refused)
                return "That GitHub repository already has files that are not in this folder (often a README that GitHub " +
                       "added when the repository was made), so SEAGit cannot publish over them.\n\n" +
                       "Make a new, empty GitHub repository (leave \"Add a README\", .gitignore and license unticked), then " +
                       "use Change GitHub Address… to paste its address and press Publish Selected again.";

            if (Has("dubious ownership"))
                return "Git will not work in this folder because Windows says it belongs to a different user account " +
                       "(this happens with some USB drives and shared folders). Copy the folder to your own Documents " +
                       "folder and add that copy instead.";

            return "Git reported a problem that SEAGit cannot explain in plain words. " + SeeLog;
        }

        /// <summary>
        /// Null when the folder may be published; otherwise why not. A whole drive, the user's own profile folder, or
        /// Windows' own folders would copy far too much (and private files) to GitHub.
        /// </summary>
        public static string? WhyFolderIsRefused(string path)
        {
            string full;
            try { full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)); }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            { return "SEAGit cannot use that folder: Windows did not accept its name."; }

            string? root = Path.GetPathRoot(full);
            if (root != null && string.Equals(Path.TrimEndingDirectorySeparator(root), full, StringComparison.OrdinalIgnoreCase))
                return $"{full} is a whole drive, not a folder. Publishing it would copy everything on the drive to GitHub.\n\n" +
                       "Make a folder for the files you want to publish, and add that folder instead.";

            if (Same(full, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
                return $"{full} is your own user folder: it holds your documents, pictures and settings. Publishing it would " +
                       "copy all of them to GitHub.\n\nMake a folder for the files you want to publish (for example in " +
                       "Documents), and add that folder instead.";

            foreach (var special in new[] { Environment.SpecialFolder.Windows, Environment.SpecialFolder.ProgramFiles,
                                            Environment.SpecialFolder.ProgramFilesX86 })
                if (Same(full, Environment.GetFolderPath(special)))
                    return $"{full} is one of Windows' own folders and cannot be published.\n\n" +
                           "Make a folder for the files you want to publish, and add that folder instead.";

            return null;
        }

        private static bool Same(string a, string b) =>
            !string.IsNullOrEmpty(b) &&
            string.Equals(a, Path.TrimEndingDirectorySeparator(b), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Shortens text to fit a width by putting "…" in the middle, keeping more of the end (for a folder path, the
        /// end is the folder's own name): "C:\Users\you\Doc…\My class website". Unchanged when it fits; "…" when nothing fits.
        /// </summary>
        /// <param name="measure">The width of a string, in the same units as <paramref name="maxWidth"/>.</param>
        public static string ShortenMiddle(string text, int maxWidth, Func<string, int> measure)
        {
            text ??= "";
            if (measure(text) <= maxWidth) return text;
            const string dots = "…";
            string Keep(int k)
            {
                int back = (k * 3 + 4) / 5; // about 60 % from the end
                int front = k - back;
                return text.Substring(0, front) + dots + text.Substring(text.Length - back);
            }
            int lo = 0, hi = text.Length - 1; // the most characters kept that still fit
            if (measure(Keep(0)) > maxWidth) return dots;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (measure(Keep(mid)) <= maxWidth) lo = mid; else hi = mid - 1;
            }
            return Keep(lo);
        }

        /// <summary>"12.3 MB", "640 KB", "17 bytes".</summary>
        public static string Size(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024 * 1024):0.0} GB";
            if (bytes >= 1024 * 1024) return $"{bytes / (1024.0 * 1024):0.0} MB";
            if (bytes >= 1024) return $"{bytes / 1024.0:0} KB";
            return bytes == 1 ? "1 byte" : $"{bytes} bytes";
        }

        /// <summary>
        /// Normalises a pasted GitHub address. Returns null and a reason when it is not one SEAGit can use.
        /// Accepts https://github.com/owner/name, with or without ".git" or a trailing "/".
        /// </summary>
        public static string? NormaliseGitHubAddress(string input, out string problem)
        {
            problem = "";
            string s = (input ?? "").Trim();
            if (s.Length == 0)
            {
                problem = "Paste the address of your GitHub repository in the box first.";
                return null;
            }
            // GitHub's Quick setup box can show the SSH form; the same repository over HTTPS is what SEAGit signs in to.
            if (s.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
                s = "https://github.com/" + s.Substring("git@github.com:".Length);
            foreach (var prefix in new[] { "http://github.com/", "https://www.github.com/", "http://www.github.com/", "www.github.com/" })
                if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { s = "https://github.com/" + s.Substring(prefix.Length); break; }
            if (s.StartsWith("github.com/", StringComparison.OrdinalIgnoreCase)) s = "https://" + s;
            if (!s.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
            {
                problem = "That is not a GitHub repository address. It must start with https://github.com/ — " +
                          "copy it from the top of the repository's page on GitHub.";
                return null;
            }
            string rest = s.Substring("https://github.com/".Length).TrimEnd('/');
            var parts = rest.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || rest.Contains(' '))
            {
                problem = "That address is not quite right. A GitHub repository address looks like " +
                          "https://github.com/your-name/your-repository";
                return null;
            }
            string name = parts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? parts[1][..^4] : parts[1];
            return $"https://github.com/{parts[0]}/{name}.git";
        }

        /// <summary>
        /// Checks what was typed in "Your name on GitHub". Returns null when it can be saved, or what to fix, in words
        /// for the line under the box. The e-mail is optional and checked only loosely (an @, and a dot after it).
        /// </summary>
        public static string? WhatIsWrongWithIdentity(string name, string email, out bool emailIsTheProblem)
        {
            emailIsTheProblem = false;
            if (string.IsNullOrWhiteSpace(name))
                return "Type the name GitHub should show beside your changes, for example Pat Smith.";
            // Git refuses a name with < or > in it, or one made only of punctuation ("empty ident name not allowed").
            if (name.IndexOfAny(new[] { '<', '>' }) >= 0 || !name.Any(char.IsLetterOrDigit))
                return "Use letters in the name (and no < or >), for example Pat Smith.";
            string e = (email ?? "").Trim();
            if (e.Length == 0) return null;
            int at = e.IndexOf('@');
            if (at <= 0 || e.IndexOf('.', at + 1) < 0 || e.EndsWith(".") || e.IndexOfAny(new[] { ' ', '<', '>' }) >= 0)
            {
                emailIsTheProblem = true;
                return "That does not look like an e-mail address: it needs an @ and a dot after it, such as " +
                       "your-name@users.noreply.github.com. Leave the box empty if you are not sure.";
            }
            return null;
        }
    }
}
