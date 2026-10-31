using System;

namespace SEAGit.Models
{
    /// <summary>A folder on this computer that SEAGit publishes to a GitHub repository.</summary>
    public class GitRepository
    {
        public string Name { get; set; } = "";
        public string LocalPath { get; set; } = "";

        // Storing the date helps users know when they last published their changes
        public DateTime LastPublished { get; set; }
    }
}
