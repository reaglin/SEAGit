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
            // Saves a repos.json file right next to the SEAGit executable
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repos.json");
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