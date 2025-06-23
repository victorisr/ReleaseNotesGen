using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater.ReleasesReadMeUpdaters
{
    public class VersionReadMeUpdater : FileUpdater
    {
        private readonly string _coreDirectory;
        private readonly string _outputDirectory;
        private readonly List<string> _runtimeIds;
        private readonly JsonFileHandler _jsonFileHandler;

        public VersionReadMeUpdater(
            string templateDirectory, 
            string logFileLocation,
            string coreDirectory,
            string outputDirectory,
            List<string> runtimeIds,
            JsonFileHandler jsonFileHandler)
            : base(templateDirectory, logFileLocation)
        {
            _coreDirectory = coreDirectory;
            _outputDirectory = outputDirectory;
            _runtimeIds = runtimeIds;
            _jsonFileHandler = jsonFileHandler;
        }

        public override void UpdateFiles()
        {
            Console.WriteLine("Starting VersionReadMeUpdater process...");

            foreach (var runtimeId in _runtimeIds)
            {
                try
                {
                    // Extract the channel version (e.g., "8.0" from "8.0.15")
                    string channelVersion = ExtractChannelVersion(runtimeId);
                    Console.WriteLine($"Processing runtime ID: {runtimeId} for channel version: {channelVersion}");

                    // Find the source README.md in the core directory
                    string sourceReadmePath = Path.Combine(_coreDirectory, "core", "release-notes", channelVersion, "README.md");

                    if (!File.Exists(sourceReadmePath))
                    {
                        Console.WriteLine($"WARNING: Source README.md file not found at: {sourceReadmePath}");
                        continue;
                    }

                    Console.WriteLine($"Found source README.md file at: {sourceReadmePath}");

                    // Find the JSON-CDN file for the runtime ID
                    string? jsonFilePath = _jsonFileHandler.FindJsonFile(runtimeId, $"releases-json-CDN-{runtimeId}.json");
                    if (jsonFilePath == null)
                    {
                        Console.WriteLine($"WARNING: JSON-CDN file not found for runtime ID: {runtimeId}");
                        continue;
                    }

                    // Load the JSON-CDN file
                    var configData = _jsonFileHandler.DeserializeReleasesConfiguration(jsonFilePath);
                    if (configData == null)
                    {
                        Console.WriteLine($"WARNING: Failed to deserialize JSON-CDN file for runtime ID: {runtimeId}");
                        continue;
                    }                    Console.WriteLine($"Successfully loaded JSON data for runtime ID: {runtimeId}");                    // Create the channel-specific output directory if it doesn't exist - now under release-notes
                    string releaseNotesDir = Path.Combine(_outputDirectory, "release-notes");
                    string channelOutputDirectory = Path.Combine(releaseNotesDir, channelVersion);
                    CreateDirectoryIfNotExists(channelOutputDirectory);

                    // Define the output path - now using the channel-specific directory
                    string outputFileName = $"{channelVersion}-README.md";
                    string outputFilePath = Path.Combine(channelOutputDirectory, outputFileName);

                    // Read the content of the source README file
                    string readmeContent = File.ReadAllText(sourceReadmePath);

                    // Modify the README content
                    string modifiedContent = UpdateReleaseNotesTable(readmeContent, configData, runtimeId);

                    // Write the modified content to the output path
                    File.WriteAllText(outputFilePath, modifiedContent);

                    Console.WriteLine($"Successfully created {outputFileName} in output directory");
                    LogChanges($"Created version README file: {outputFilePath} for version: {channelVersion}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: An error occurred while processing runtime ID {runtimeId}: {ex.Message}");
                    LogChanges($"Error processing README.md for runtime ID {runtimeId}: {ex.Message}");
                }
            }
        }        private string UpdateReleaseNotesTable(string content, ReleasesConfiguration configData, string runtimeId)
        {
            // Look for either "## Release notes" or "## Releases" section and the table that follows
            // This uses an alternation pattern (Release notes|Releases) to match either header format
            Regex tableRegex = new Regex(@"(## (Release notes|Releases)\s*\n\s*\|\s*Date\s*\|\s*Release\s*\|\s*SDK\s*\|\s*\n\s*\|\s*:--\s*\|\s*:--\s*\|\s*:--\s*\|\s*\n)");
            
            Match match = tableRegex.Match(content);
            if (!match.Success)
            {
                // Try alternative regex without SDK column for older README files
                tableRegex = new Regex(@"(## (Release notes|Releases)\s*\n\s*\|\s*Date\s*\|\s*Release\s*\|\s*\n\s*\|\s*:--\s*\|\s*:--\s*\|\s*\n)");
                match = tableRegex.Match(content);
                
                if (!match.Success)
                {
                    Console.WriteLine("WARNING: Could not find the Release notes/Releases table in the README file");
                    return content;
                }
            }

            string tableHeader = match.Groups[1].Value;
            string headerType = match.Groups[2].Value;
            
            Console.WriteLine($"Found table with header '## {headerType}'");// Format the release date
            string releaseDate = FormatDate(configData.LatestReleaseDate);
            
            // Get the latest release and extract file path for linking
            string latestRelease = configData.LatestRelease;
            string latestSdk = configData.LatestSdk;
            
            // Generate SDK column content with all SDK versions for this runtime
            string sdkColumn = GenerateSdkColumn(configData, latestRelease, latestSdk);

            // Create the new row to insert
            string newRow = $"| {releaseDate} | [{latestRelease}](./{latestRelease}/{latestRelease}.md) | {sdkColumn} |\n";
            
            // Insert the new row right after the table header
            string updatedContent = tableRegex.Replace(content, tableHeader + newRow);
            
            Console.WriteLine($"Added new row for release {latestRelease} dated {releaseDate} to the table under '## {headerType}'");
            
            return updatedContent;
        }

        private string ExtractChannelVersion(string runtimeId)
        {
            // Split by dot and take the first two segments (e.g., "8.0" from "8.0.15")
            string[] parts = runtimeId.Split('.');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}.{parts[1]}";
            }
            
            Console.WriteLine($"WARNING: Unable to extract channel version from runtime ID: {runtimeId}");
            return runtimeId; // Return original if extraction fails
        }

        private string FormatDate(string releaseDate)
        {
            // Try to parse the date from the JSON
            if (DateTime.TryParse(releaseDate, out DateTime parsedDate))
            {
                // Format as yyyy/MM/dd (e.g., "2025/04/08")
                return parsedDate.ToString("yyyy/MM/dd");
            }
            
            // Return the original string if parsing fails
            Console.WriteLine($"WARNING: Unable to parse release date: {releaseDate}");
            return releaseDate;
        }

        private string GenerateSdkColumn(ReleasesConfiguration configData, string latestRelease, string latestSdk)
        {
            // Initialize an empty list to store SDK links
            List<string> sdkLinks = new List<string>();
            
            // Find the runtime release that matches the latest release
            foreach (var release in configData.Releases)
            {
                if (release.Runtime?.Version == latestRelease)
                {
                    // Process all SDKs for this runtime
                    if (release.Sdks != null)
                    {
                        foreach (var sdk in release.Sdks)
                        {
                            string version = sdk.Version;
                            string link;
                            
                            // If the SDK version is the same as the latest SDK, use the latestRelease folder
                            if (version == latestSdk)
                            {
                                link = $"[{version}](./{latestRelease}/{latestRelease}.md)";
                            }
                            else
                            {
                                link = $"[{version}](./{latestRelease}/{version}.md)";
                            }
                            
                            sdkLinks.Add(link);
                        }
                    }
                    
                    // Break once we've found the matching release
                    break;
                }
            }
            
            // If no SDKs were found, return an empty string
            if (sdkLinks.Count == 0)
            {
                Console.WriteLine($"WARNING: No SDK versions found for runtime {latestRelease}");
                return "";
            }
            
            // Join the SDK links with commas
            return string.Join(", ", sdkLinks);
        }
    }
}