namespace SEAGit.Models
{
    /// <summary>
    /// The name (and, optionally, the e-mail) GitHub shows beside every change the person publishes. SEAGit asks for it
    /// once, on a computer where Git has no name set, and keeps it in its own settings (identity.json beside repos.json).
    /// It is passed to Git with "-c" for each commit; the person's own Git configuration is never written.
    /// </summary>
    public class CommitIdentity
    {
        public string Name { get; set; } = "";

        /// <summary>Empty when the person left it out; SEAGit then uses <see cref="Services.GitProcessService.FallbackEmail"/>.</summary>
        public string Email { get; set; } = "";
    }
}
