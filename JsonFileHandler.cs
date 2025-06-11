using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater
{
    public class JsonFileHandler
    {
        private readonly string _downloadPath;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonFileHandler(string downloadPath)
        {
            _downloadPath = downloadPath;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
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
            return JsonSerializer.Deserialize<T>(jsonContent, _jsonOptions);
        }

        public ReleasesConfiguration? DeserializeReleasesConfiguration(string jsonFilePath)
        {
            return DeserializeJsonFile<ReleasesConfiguration>(jsonFilePath);
        }

        public ReleaseNotes? DeserializeReleaseNotes(string jsonFilePath)
        {
            return DeserializeJsonFile<ReleaseNotes>(jsonFilePath);
        }

        public CoreReleasesConfiguration? DeserializeCoreReleasesConfiguration(string jsonFilePath)
        {
            return DeserializeJsonFile<CoreReleasesConfiguration>(jsonFilePath);
        }        public List<MsrcConfig> LoadMsrcInformation(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                Console.WriteLine($"WARNING: Config file not found at: {configFilePath}");
                return new List<MsrcConfig>();
            }

            try
            {
                string jsonContent = File.ReadAllText(configFilePath);
                using var document = JsonDocument.Parse(jsonContent);
                
                if (document.RootElement.TryGetProperty("MsrcInformation", out var msrcElement))
                {
                    var result = JsonSerializer.Deserialize<List<MsrcConfig>>(msrcElement.GetRawText(), _jsonOptions);
                    return result ?? new List<MsrcConfig>();
                }
                
                return new List<MsrcConfig>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to parse MSRC information from config: {ex.Message}");
                return new List<MsrcConfig>();
            }
        }

        /// <summary>
        /// Load release reference configuration from separate JSON files
        /// </summary>
        /// <param name="configDirectory">Directory containing the configuration JSON files</param>
        /// <returns>ReleaseReferenceConfiguration with loaded data</returns>
        public ReleaseReferenceConfiguration LoadReleaseReferenceConfiguration(string configDirectory)
        {
            var config = new ReleaseReferenceConfiguration();

            try
            {
                // Load launch dates
                string launchDatesPath = Path.Combine(configDirectory, "launch-dates.json");
                if (File.Exists(launchDatesPath))
                {
                    string jsonContent = File.ReadAllText(launchDatesPath);
                    var launchDates = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, _jsonOptions);
                    if (launchDates != null)
                    {
                        config.LaunchDates = launchDates;
                    }
                }
                else
                {
                    Console.WriteLine($"WARNING: Launch dates configuration file not found at: {launchDatesPath}");
                }

                // Load announcement links
                string announcementLinksPath = Path.Combine(configDirectory, "announcement-links.json");
                if (File.Exists(announcementLinksPath))
                {
                    string jsonContent = File.ReadAllText(announcementLinksPath);
                    var announcementLinks = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, _jsonOptions);
                    if (announcementLinks != null)
                    {
                        config.AnnouncementLinks = announcementLinks;
                    }
                }
                else
                {
                    Console.WriteLine($"WARNING: Announcement links configuration file not found at: {announcementLinksPath}");
                }

                // Load EOL announcement links
                string eolAnnouncementLinksPath = Path.Combine(configDirectory, "eol-announcement-links.json");
                if (File.Exists(eolAnnouncementLinksPath))
                {
                    string jsonContent = File.ReadAllText(eolAnnouncementLinksPath);
                    var eolAnnouncementLinks = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, _jsonOptions);
                    if (eolAnnouncementLinks != null)
                    {
                        config.EolAnnouncementLinks = eolAnnouncementLinks;
                    }
                }
                else
                {
                    Console.WriteLine($"WARNING: EOL announcement links configuration file not found at: {eolAnnouncementLinksPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to load release reference configuration: {ex.Message}");
            }

            return config;
        }
    }
}