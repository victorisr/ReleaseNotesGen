using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater.CoreDirJsonUpdaters
{
    /// <summary>
    /// Updates the release-notes/{channelVersion}/releases.json file in the core directory
    /// and generates a {channelVersion}-releases.json file in the output directory
    /// </summary>
    public class ChannelVersionJsonUpdater : CoreDirectoryBaseUpdater
    {
        private readonly string _runtimeId;

        public ChannelVersionJsonUpdater(
            string coreDirectory,
            string outputDirectory,
            string runtimeId,
            string downloadPath,
            string logFileLocation,
            JsonFileHandler jsonFileHandler)
            : base(coreDirectory, outputDirectory, downloadPath, logFileLocation, jsonFileHandler)
        {
            _runtimeId = runtimeId;
        }

        /// <summary>
        /// Updates the channel version releases.json file with information from the JSON-CDN file
        /// </summary>
        public override void Update()
        {
            try
            {
                // Extract the channel version (e.g., "8.0" from "8.0.15")
                string channelVersion = ExtractChannelVersion(_runtimeId);

                // Find the JSON-CDN file for the runtime ID
                string jsonFilePath = JsonFileHandler.FindJsonFile(_runtimeId, $"releases-json-CDN-{_runtimeId}.json");
                if (jsonFilePath == null)
                {
                    LogError($"JSON-CDN file not found for runtime ID: {_runtimeId}");
                    return;
                }

                // Load the JSON-CDN file
                var configData = JsonFileHandler.DeserializeReleasesConfiguration(jsonFilePath);
                if (configData == null)
                {
                    LogError($"Failed to deserialize JSON-CDN file for runtime ID: {_runtimeId}");
                    return;
                }

                // Create the output path for the channel version releases file
                string outputFileName = $"{channelVersion}-releases.json";
                string outputFilePath = Path.Combine(OutputDirectory, outputFileName);
                
                // Ensure the output directory exists
                CreateDirectoryIfNotExists(OutputDirectory);

                // Load existing file from core directory for reference if needed
                CoreReleasesConfiguration channelReleases;
                
                // Get the path to the existing file in the core directory for reference
                string coreReleasesPath = Path.Combine(CoreDirectory, "core", "release-notes", channelVersion, "releases.json");
                
                if (File.Exists(coreReleasesPath))
                {
                    try
                    {
                        // Load existing file for reference
                        string json = File.ReadAllText(coreReleasesPath);
                        channelReleases = JsonSerializer.Deserialize<CoreReleasesConfiguration>(json, JsonOptions);
                        LogMessage($"Successfully loaded existing releases.json from core directory for channel {channelVersion} as reference");
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error loading existing releases.json from core directory for channel {channelVersion}: {ex.Message}");
                        // Create new configuration if loading fails
                        channelReleases = CreateNewChannelReleases(configData, channelVersion);
                    }
                }
                else
                {
                    // Create new configuration if file doesn't exist in core directory
                    channelReleases = CreateNewChannelReleases(configData, channelVersion);
                    LogMessage($"Created new releases.json structure for channel {channelVersion}");
                }

                // Update the channel releases with the latest release information
                UpdateChannelReleasesWithLatest(channelReleases, configData);

                // Write the updated channel releases to the output file
                WriteJsonFile(outputFilePath, channelReleases);
                LogMessage($"Successfully created {outputFileName} in output directory");
            }
            catch (Exception ex)
            {
                LogError($"Error updating channel version releases.json for runtime {_runtimeId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a new channel releases configuration
        /// </summary>
        private CoreReleasesConfiguration CreateNewChannelReleases(ReleasesConfiguration configData, string channelVersion)
        {
            return new CoreReleasesConfiguration
            {
                ChannelVersion = channelVersion,
                LatestRelease = configData.LatestRelease,
                LatestReleaseDate = configData.LatestReleaseDate,
                LatestRuntime = configData.LatestRuntime,
                LatestSdk = configData.LatestSdk,
                SupportPhase = configData.SupportPhase,
                ReleaseType = configData.ReleaseType,
                LifecyclePolicy = configData.LifecyclePolicy,
                EolDate = null, // This needs to be set based on the support phase
                Releases = new List<CoreRelease>()
            };
        }

        /// <summary>
        /// Updates the channel releases with the latest release information from the JSON-CDN file
        /// </summary>
        private void UpdateChannelReleasesWithLatest(CoreReleasesConfiguration channelReleases, ReleasesConfiguration configData)
        {
            // Update the top-level properties
            channelReleases.LatestRelease = configData.LatestRelease;
            channelReleases.LatestReleaseDate = configData.LatestReleaseDate;
            channelReleases.LatestRuntime = configData.LatestRuntime;
            channelReleases.LatestSdk = configData.LatestSdk;
            channelReleases.SupportPhase = configData.SupportPhase;
            channelReleases.ReleaseType = configData.ReleaseType;
            channelReleases.LifecyclePolicy = configData.LifecyclePolicy;

            // Get the latest release from the JSON-CDN file
            var latestRelease = configData.Releases?.FirstOrDefault();
            if (latestRelease == null)
            {
                LogError("No releases found in the JSON-CDN file");
                return;
            }

            // Check if this release already exists in the channel releases
            var existingRelease = channelReleases.Releases?
                .FirstOrDefault(r => r.ReleaseVersion == latestRelease.ReleaseVersion);

            if (existingRelease != null)
            {
                // Update existing release
                existingRelease.ReleaseDate = latestRelease.ReleaseDate;
                existingRelease.Security = latestRelease.Security;
                existingRelease.ReleaseNotes = latestRelease.ReleaseNotes;
                
                // Update CVEs
                existingRelease.CveList = latestRelease.CveList?
                    .Select(c => new CoreCveItem { CveId = ExtractCveId(c.CveUrl), CveUrl = c.CveUrl })
                    .ToList() ?? new List<CoreCveItem>();
                
                LogMessage($"Updated existing release {latestRelease.ReleaseVersion} in channel releases");
            }
            else
            {
                // Create new release
                var newRelease = new CoreRelease
                {
                    ReleaseDate = latestRelease.ReleaseDate,
                    ReleaseVersion = latestRelease.ReleaseVersion,
                    Security = latestRelease.Security,
                    ReleaseNotes = latestRelease.ReleaseNotes,
                    CveList = latestRelease.CveList?
                        .Select(c => new CoreCveItem { CveId = ExtractCveId(c.CveUrl), CveUrl = c.CveUrl })
                        .ToList() ?? new List<CoreCveItem>()
                };

                // Add the new release at the beginning of the list
                if (channelReleases.Releases == null)
                {
                    channelReleases.Releases = new List<CoreRelease>();
                }
                channelReleases.Releases.Insert(0, newRelease);
                
                LogMessage($"Added new release {latestRelease.ReleaseVersion} to channel releases");
            }
        }

        /// <summary>
        /// Extracts the CVE ID from a CVE URL
        /// </summary>
        private string ExtractCveId(string cveUrl)
        {
            if (string.IsNullOrEmpty(cveUrl))
                return string.Empty;

            // Extract the CVE ID from the URL (e.g., "CVE-2023-1234" from "https://portal.msrc.microsoft.com/en-us/security-guidance/advisory/CVE-2023-1234")
            string[] parts = cveUrl.Split('/');
            string lastPart = parts.LastOrDefault() ?? "";
            
            // Check if it's already a CVE ID
            if (lastPart.StartsWith("CVE-", StringComparison.OrdinalIgnoreCase))
                return lastPart;
            
            return string.Empty;
        }
    }
}