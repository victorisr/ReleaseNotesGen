using System;
using System.IO;
using Newtonsoft.Json;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater
{
    public class JsonFileHandler
    {
        private readonly string _downloadPath;

        public JsonFileHandler(string downloadPath)
        {
            _downloadPath = downloadPath;
        }

        public string? FindJsonFile(string runtimeId, string jsonFileNamePattern)
        {
            string runtimeFolderPath = Path.Combine(_downloadPath, $"release-manifests_{runtimeId}", "release-manifests");

            if (Directory.Exists(runtimeFolderPath))
            {
                string[] files = Directory.GetFiles(runtimeFolderPath, jsonFileNamePattern, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    return files[0]; // Return the first matching file
                }
            }

            return null; // Return null if no matching file is found
        }

        public T? DeserializeJsonFile<T>(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"JSON file not found: {jsonFilePath}");
            }

            string jsonContent = File.ReadAllText(jsonFilePath);
            return JsonConvert.DeserializeObject<T>(jsonContent);
        }

        public ReleasesConfiguration? DeserializeReleasesConfiguration(string jsonFilePath)
        {
            return DeserializeJsonFile<ReleasesConfiguration>(jsonFilePath);
        }

        public ReleaseNotes? DeserializeReleaseNotes(string jsonFilePath)
        {
            return DeserializeJsonFile<ReleaseNotes>(jsonFilePath);
        }
    }
}