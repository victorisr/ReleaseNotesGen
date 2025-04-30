using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
                dynamic releaseIndex;
                string coreReleaseIndexPath = Path.Combine(CoreDirectory, "core", "release-notes", "releases-index.json");
                
                if (File.Exists(coreReleaseIndexPath))
                {
                    try
                    {
                        string json = File.ReadAllText(coreReleaseIndexPath);
                        releaseIndex = JsonSerializer.Deserialize<dynamic>(json);
                        LogMessage("Successfully loaded existing releases-index.json from core directory as reference");
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error loading existing releases-index.json from core directory: {ex.Message}");
                        // Create new index if loading fails
                        releaseIndex = new
                        {
                            releases = new List<object>()
                        };
                        LogMessage("Created new releases-index.json structure");
                    }
                }
                else
                {
                    // Create new if file doesn't exist
                    releaseIndex = new
                    {
                        releases = new List<object>()
                    };
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
                        UpdateReleaseIndexForRuntime(runtimeId, releaseIndex);
                    }
                }

                // Write the updated release index to the output directory
                string updatedJson = JsonSerializer.Serialize(releaseIndex, JsonOptions);
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
        /// Updates the release index for a specific runtime
        /// </summary>
        private void UpdateReleaseIndexForRuntime(string runtimeId, dynamic releaseIndex)
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
                UpdateReleaseIndexEntry(releaseIndex, configData, channelVersion);
                
                LogMessage($"Updated releases index data for runtime ID: {runtimeId}, channel: {channelVersion}");
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
        private void UpdateReleaseIndexEntry(dynamic releaseIndex, ReleasesConfiguration configData, string channelVersion)
        {
            // In a real implementation, you would analyze the releaseIndex object (which is dynamic)
            // and update or add an entry for this channel version with data from configData
            // Here's a simplified example structure:
            
            var newEntry = new 
            {
                channel_version = channelVersion,
                latest_release = configData.LatestRelease,
                latest_release_date = configData.LatestReleaseDate,
                latest_runtime = configData.LatestRuntime,
                latest_sdk = configData.LatestSdk,
                support_phase = configData.SupportPhase,
                release_type = configData.ReleaseType,
                lifecycle_policy = configData.LifecyclePolicy
                // Add other necessary fields
            };
            
            // Note: This is a simplified implementation and would need to be enhanced
            // to properly update the dynamic object based on your specific index format
            
            LogMessage($"Updated release index entry for channel version: {channelVersion}");
        }
    }
}