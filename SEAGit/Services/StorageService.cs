using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SEAGit.Models;

namespace SEAGit.Services
{
    public class StorageService
    {
        private readonly string _filePath;

        /// <summary>
        /// Set by <see cref="LoadRepositories"/> when the saved list could not be read: the path of the copy kept of
        /// the unreadable file. Null when the list loaded normally.
        /// </summary>
        public string? BrokenListBackupPath { get; private set; }

        /// <summary>True when the saved list could not be read (see <see cref="BrokenListBackupPath"/>).</summary>
        public bool ListWasUnreadable { get; private set; }

        /// <param name="directory">Where repos.json is kept; %LocalAppData%\SEAGit when omitted. Tests pass their own.</param>
        public StorageService(string? directory = null)
        {
            // repos.json lives in the user's local app data, NOT next to the
            // executable. Under MSIX the install folder
            // (C:\Program Files\WindowsApps\...) is read-only, so writing there
            // threw UnauthorizedAccessException. %LocalAppData%\SEAGit is writable.
            var dir = directory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SEAGit");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "repos.json");
        }

        public string FilePath => _filePath;

        public List<GitRepository> LoadRepositories()
        {
            ListWasUnreadable = false;
            BrokenListBackupPath = null;
            if (!File.Exists(_filePath))
                return new List<GitRepository>();

            try
            {
                string json = File.ReadAllText(_filePath);
                var list = JsonSerializer.Deserialize<List<GitRepository>>(json) ?? new List<GitRepository>();
                return list.Where(r => !string.IsNullOrWhiteSpace(r.LocalPath)).ToList();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                // The file is damaged. Keep a copy before the list starts empty, so the next save cannot destroy
                // the only record of the person's folders.
                ListWasUnreadable = true;
                try
                {
                    string backup = Path.Combine(Path.GetDirectoryName(_filePath)!,
                        $"repos.unreadable-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                    File.Copy(_filePath, backup, overwrite: true);
                    BrokenListBackupPath = backup;
                }
                catch (Exception copyEx) when (copyEx is IOException or UnauthorizedAccessException)
                {
                    BrokenListBackupPath = null;
                }
                return new List<GitRepository>();
            }
        }

        public void SaveRepositories(List<GitRepository> repositories) => SafeWrite(_filePath, repositories);

        /// <summary>identity.json, beside repos.json: the name (and e-mail) to show on GitHub, from "Your name on GitHub".</summary>
        public string IdentityFilePath => Path.Combine(Path.GetDirectoryName(_filePath)!, "identity.json");

        /// <summary>The name saved in SEAGit's settings, or null when none was saved (or the file cannot be read, in
        /// which case SEAGit simply asks again).</summary>
        public CommitIdentity? LoadIdentity()
        {
            if (!File.Exists(IdentityFilePath)) return null;
            try
            {
                var identity = JsonSerializer.Deserialize<CommitIdentity>(File.ReadAllText(IdentityFilePath));
                return identity == null || string.IsNullOrWhiteSpace(identity.Name) ? null : identity;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                return null;
            }
        }

        public void SaveIdentity(CommitIdentity identity) =>
            SafeWrite(IdentityFilePath, new CommitIdentity { Name = identity.Name.Trim(), Email = identity.Email.Trim() });

        /// <summary>Writes beside the file first, then swaps, so a crash mid-write cannot leave it half-written.</summary>
        private static void SafeWrite<T>(string path, T value)
        {
            string json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
            string temp = path + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, path, overwrite: true);
        }
    }
}
