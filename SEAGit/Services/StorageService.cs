using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SEAGit.Models;

namespace SEAGit.Services
{
    public class StorageService
    {
        private readonly string _filePath;

        public StorageService()
        {
            // repos.json lives in the user's local app data, NOT next to the
            // executable. Under MSIX the install folder
            // (C:\Program Files\WindowsApps\...) is read-only, so writing there
            // threw UnauthorizedAccessException. %LocalAppData%\SEAGit is writable.
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SEAGit");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "repos.json");
        }

        public List<GitRepository> LoadRepositories()
        {
            if (!File.Exists(_filePath))
                return new List<GitRepository>();

            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<GitRepository>>(json) ?? new List<GitRepository>();
            }
            catch
            {
                // If the file is corrupted, return a fresh list
                return new List<GitRepository>();
            }
        }

        public void SaveRepositories(List<GitRepository> repositories)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(repositories, options);
            File.WriteAllText(_filePath, json);
        }
    }
}