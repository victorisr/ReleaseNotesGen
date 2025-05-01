using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater.CoreDirectoryUpdaters
{
    /// <summary>
    /// Generates the releases-index.json file in the output directory
    /// </summary>
    public class ReleasesIndexJsonUpdater : CoreDirectoryBaseUpdater
    {
        private readonly List<string> _runtimeIds;

        public ReleasesIndexJsonUpdater(
            string coreDirectory,
            string outputDirectory,
            string downloadPath,
            string logFileLocation,
            JsonFileHandler jsonFileHandler)
            : base(coreDirectory, outputDirectory, downloadPath, logFileLocation, jsonFileHandler)
        {
            _runtimeIds = new List<string>();
        }

        /// <summary>
        /// Generates the releases-index.json file with new information from JSON-CDN files
        /// </summary>
        public override void Update()
        {
            try
            {
                // Define output path for releases-index.json
                string releaseIndexPath = Path.Combine(OutputDirectory, "releases-index.json");
                LogMessage($"Creating releases-index.json at: {releaseIndexPath}");

                // Create directory if it doesn't exist
                CreateDirectoryIfNotExists(OutputDirectory);

                // Load existing release index from core directory as reference, if available
                JsonNode? releaseIndexRoot = null;
                string coreReleaseIndexPath = Path.Combine(CoreDirectory, "core", "release-notes", "releases-index.json");
                
                if (File.Exists(coreReleaseIndexPath))
                {
                    try
                    {
                        string json = File.ReadAllText(coreReleaseIndexPath);
                        releaseIndexRoot = JsonNode.Parse(json);
                        LogMessage("Successfully loaded existing releases-index.json from core directory as reference");
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error loading existing releases-index.json from core directory: {ex.Message}");
                        // Create new index if loading fails
                        releaseIndexRoot = CreateNewReleasesIndex();
                        LogMessage("Created new releases-index.json structure");
                    }
                }
                else
                {
                    // Create new if file doesn't exist
                    releaseIndexRoot = CreateNewReleasesIndex();
                    LogMessage("Created new releases-index.json structure");
                }

                // Update release index with available runtime IDs from download path
                string[] runtimeFolders = Directory.GetDirectories(DownloadPath, "release-manifests_*");
                foreach (var runtimeFolder in runtimeFolders)
                {
                    // Extract runtime ID from folder name
                    string folderName = Path.GetFileName(runtimeFolder);
                    if (folderName.StartsWith("release-manifests_"))
                    {
                        string runtimeId = folderName.Substring("release-manifests_".Length);
                        _runtimeIds.Add(runtimeId);
                        
                        // Update release index for this runtime ID
                        UpdateReleaseIndexForRuntime(runtimeId, releaseIndexRoot);
                    }
                }

                // Write the updated release index to the output directory
                string updatedJson = JsonSerializer.Serialize(releaseIndexRoot, JsonOptions);
                File.WriteAllText(releaseIndexPath, updatedJson);
                LogMessage("Successfully created releases-index.json in output directory");
            }
            catch (Exception ex)
            {
                LogError($"Error creating releases-index.json: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a new releases-index.json structure
        /// </summary>
        private JsonNode CreateNewReleasesIndex()
        {
            var jsonObject = new JsonObject();
            jsonObject.Add("$schema", "https://json.schemastore.org/dotnet-releases-index.json");
            jsonObject.Add("releases-index", new JsonArray());
            return jsonObject;
        }

        /// <summary>
        /// Updates the release index for a specific runtime
        /// </summary>
        private void UpdateReleaseIndexForRuntime(string runtimeId, JsonNode releaseIndexRoot)
        {
            try
            {
                // Find the JSON-CDN file for the runtime ID
                string jsonFilePath = JsonFileHandler.FindJsonFile(runtimeId, $"releases-json-CDN-{runtimeId}.json");
                if (jsonFilePath == null)
                {
                    LogError($"JSON-CDN file not found for runtime ID: {runtimeId}");
                    return;
                }

                // Load the JSON-CDN file
                var configData = JsonFileHandler.DeserializeReleasesConfiguration(jsonFilePath);
                if (configData == null)
                {
                    LogError($"Failed to deserialize JSON-CDN file for runtime ID: {runtimeId}");
                    return;
                }

                // Extract the channel version (e.g., "8.0" from "8.0.15")
                string channelVersion = ExtractChannelVersion(runtimeId);

                // Update or add the channel version in the release index
                bool updated = UpdateReleaseIndexEntry(releaseIndexRoot, configData, channelVersion, runtimeId);
                
                if (updated)
                {
                    LogMessage($"Updated releases index data for runtime ID: {runtimeId}, channel: {channelVersion}");
                }
                else
                {
                    LogMessage($"Added new releases index entry for runtime ID: {runtimeId}, channel: {channelVersion}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error updating release index for runtime {runtimeId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates an entry in the release index
        /// </summary>
        private bool UpdateReleaseIndexEntry(JsonNode releaseIndexRoot, ReleasesConfiguration configData, string channelVersion, string runtimeId)
        {
            if (releaseIndexRoot == null || configData == null)
            {
                LogError("Invalid release index or configuration data");
                return false;
            }

            var indexArray = releaseIndexRoot["releases-index"]?.AsArray();
            if (indexArray == null)
            {
                LogError("Invalid releases-index structure");
                return false;
            }

            // Find the entry with the matching channel-version
            JsonNode? existingEntry = null;
            int existingEntryIndex = -1;

            for (int i = 0; i < indexArray.Count; i++)
            {
                var entry = indexArray[i];
                if (entry != null && entry["channel-version"]?.GetValue<string>() == channelVersion)
                {
                    existingEntry = entry;
                    existingEntryIndex = i;
                    break;
                }
            }

            // Get security value from the first release (latest release)
            bool security = false;
            if (configData.Releases != null && configData.Releases.Count > 0)
            {
                security = configData.Releases[0].Security;
            }

            // Determine if this is a new entry or an update to an existing one
            if (existingEntry == null)
            {
                // Create a new entry
                var newEntry = new JsonObject
                {
                    ["channel-version"] = channelVersion,
                    ["latest-release"] = configData.LatestRelease,
                    ["latest-release-date"] = configData.LatestReleaseDate,
                    ["security"] = security,
                    ["latest-runtime"] = configData.LatestRuntime,
                    ["latest-sdk"] = configData.LatestSdk,
                    ["product"] = ".NET", // Default to current product name
                    ["support-phase"] = configData.SupportPhase,
                    ["release-type"] = configData.ReleaseType
                };

                // Add URLs for releases.json and supported-os.json
                newEntry.Add("releases.json", $"https://builds.dotnet.microsoft.com/dotnet/release-metadata/{channelVersion}/releases.json");
                newEntry.Add("supported-os.json", $"https://builds.dotnet.microsoft.com/dotnet/release-metadata/{channelVersion}/supported-os.json");

                // Add new entry to the array
                indexArray.Add(newEntry);
                
                // Return false to indicate a new entry was added
                return false;
            }
            else
            {
                // Update existing entry with the latest values
                existingEntry["latest-release"] = configData.LatestRelease;
                existingEntry["latest-release-date"] = configData.LatestReleaseDate;
                existingEntry["security"] = security;
                existingEntry["latest-runtime"] = configData.LatestRuntime;
                existingEntry["latest-sdk"] = configData.LatestSdk;
                existingEntry["support-phase"] = configData.SupportPhase;
                
                // Only update release-type if it's defined in the config
                if (!string.IsNullOrEmpty(configData.ReleaseType))
                {
                    existingEntry["release-type"] = configData.ReleaseType;
                }
                
                // Return true to indicate an existing entry was updated
                return true;
            }
        }
    }
}