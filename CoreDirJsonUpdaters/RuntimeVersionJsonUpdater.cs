using System;
using System.IO;
using System.Text.Json;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater.CoreDirJsonUpdaters
{
    /// <summary>
    /// Generates {runtimeId}release.json files in the output directory based on information
    /// from the JSON-CDN files
    /// </summary>
    public class RuntimeVersionJsonUpdater : CoreDirectoryBaseUpdater
    {
        private readonly string _runtimeId;

        public RuntimeVersionJsonUpdater(
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
        /// Creates the runtime version release.json file with the information from the JSON-CDN file
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

                // Define the output path for the runtime version release.json
                string outputFileName = $"{_runtimeId}release.json";
                string outputFilePath = Path.Combine(OutputDirectory, outputFileName);

                // Ensure the output directory exists
                CreateDirectoryIfNotExists(OutputDirectory);

                // Get the release data from the config
                Release release = null;
                foreach (var rel in configData.Releases)
                {
                    if (rel.Runtime?.Version == _runtimeId)
                    {
                        release = rel;
                        break;
                    }
                }

                if (release == null)
                {
                    LogError($"No release found for runtime ID: {_runtimeId}");
                    return;
                }

                // Create a copy of the release that we can modify and write to the file
                var releaseCopy = CloneRelease(release, configData);

                // Write the release to the file
                WriteJsonFile(outputFilePath, releaseCopy);
                LogMessage($"Successfully created {outputFileName} in output directory");
            }
            catch (Exception ex)
            {
                LogError($"Error creating runtime version release.json for runtime {_runtimeId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a copy of the release with the necessary modifications
        /// </summary>
        private object CloneRelease(Release release, ReleasesConfiguration configData)
        {
            // Create a new anonymous object with the properties needed
            // This approach allows us to customize the structure of the output JSON
            return new
            {
                // Include key metadata from the release configuration
                configData.ChannelVersion,
                runtime_version = _runtimeId,
                release.ReleaseDate,
                release.ReleaseVersion,
                release.Security,
                // Include release notes URL
                release.ReleaseNotes,
                // Include CVEs if any
                cve_list = release.CveList,
                // Include the runtime component
                runtime = release.Runtime,
                // Include the SDK component
                sdk = release.Sdk,
                // Include the ASP.NET Core runtime component
                aspnetcore_runtime = release.AspNetCoreRuntime,
                // Include the Windows Desktop component
                windowsdesktop = release.WindowsDesktop,
                // Include SDKs
                sdks = release.Sdks,
                // Include packages
                packages = release.Packages
            };
        }
    }
}