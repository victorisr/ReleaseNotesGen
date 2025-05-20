using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater
{    public class CveFileUpdater : FileUpdater
    {
        private readonly string _coreDirectory;
        private readonly string _outputDirectory;
        private readonly List<string> _runtimeIds;
        private readonly JsonFileHandler _jsonFileHandler;
        private readonly List<MsrcConfig> _msrcConfigs;

        public CveFileUpdater(
            string templateDirectory, 
            string logFileLocation,
            string coreDirectory,
            string outputDirectory,
            List<string> runtimeIds,
            JsonFileHandler jsonFileHandler,
            List<MsrcConfig> msrcConfigs)
            : base(templateDirectory, logFileLocation)
        {
            _coreDirectory = coreDirectory;
            _outputDirectory = outputDirectory;
            _runtimeIds = runtimeIds;
            _jsonFileHandler = jsonFileHandler;
            _msrcConfigs = msrcConfigs;
        }

        public override void UpdateFiles()
        {
            Console.WriteLine("Starting CveFileUpdater process...");

            foreach (var runtimeId in _runtimeIds)
            {
                try
                {
                    // Extract the channel version (e.g., "8.0" from "8.0.15")
                    string channelVersion = ExtractChannelVersion(runtimeId);
                    Console.WriteLine($"Processing runtime ID: {runtimeId} for channel version: {channelVersion}");

                    // Find the source cve.md in the core directory
                    string sourceCvePath = Path.Combine(_coreDirectory, "core", "release-notes", channelVersion, "cve.md");

                    if (!File.Exists(sourceCvePath))
                    {
                        Console.WriteLine($"WARNING: Source cve.md file not found at: {sourceCvePath}");
                        continue;
                    }

                    Console.WriteLine($"Found source cve.md file at: {sourceCvePath}");

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
                    }

                    Console.WriteLine($"Successfully loaded JSON data for runtime ID: {runtimeId}");

                    // Create the output directory if it doesn't exist
                    CreateDirectoryIfNotExists(_outputDirectory);

                    // Define the output path
                    string outputFileName = $"{channelVersion}-cve.md";
                    string outputFilePath = Path.Combine(_outputDirectory, outputFileName);

                    // Read the content of the source cve file
                    string cveContent = File.ReadAllText(sourceCvePath);

                    // Modify the CVE content
                    string modifiedContent = UpdateCveList(cveContent, configData, runtimeId);

                    // Write the modified content to the output path
                    File.WriteAllText(outputFilePath, modifiedContent);

                    Console.WriteLine($"Successfully created {outputFileName} in output directory");
                    LogChanges($"Created CVE file: {outputFilePath} for version: {channelVersion}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: An error occurred while processing runtime ID {runtimeId}: {ex.Message}");
                    LogChanges($"Error processing CVE file for runtime ID {runtimeId}: {ex.Message}");
                }
            }
        }

        private string UpdateCveList(string content, ReleasesConfiguration configData, string runtimeId)
        {
            // Look for the CVE list section header and the content that follows
            string pattern = @"(## Which CVEs apply to my app\?\s*\n\s*Your app may be vulnerable to the following published security \[CVEs\]\(https://www\.cve\.org/\) if you are using an older version\.\s*\n)";
            
            Regex sectionRegex = new Regex(pattern);
            Match sectionMatch = sectionRegex.Match(content);
            
            if (!sectionMatch.Success)
            {
                Console.WriteLine("WARNING: Could not find the CVE list section in the cve.md file");
                return content;
            }

            string sectionHeader = sectionMatch.Groups[1].Value;
            
            // Format the release version and date for the new entry
            string formattedDate = FormatReleaseDate(configData.LatestReleaseDate);
            string releaseVersion = configData.LatestRelease;
              // Check if there are any CVEs in the latest release
            var cveItems = new List<string>();
            
            if (configData.Releases != null && configData.Releases.Count > 0)
            {
                var latestRelease = configData.Releases.FirstOrDefault(r => r.ReleaseVersion == releaseVersion);
                if (latestRelease != null && latestRelease.CveList != null && latestRelease.CveList.Count > 0)
                {                    foreach (var cve in latestRelease.CveList)
                    {                        // Format each CVE entry in the standardized format                        // Extract CVE ID from the URL if possible, otherwise use the URL as the ID
                        string cveId = ExtractCveIdFromUrl(cve.CveUrl);
                        
                        // Check if we have matching MSRC information for this CVE
                        var msrcConfig = _msrcConfigs.FirstOrDefault(m => m.RuntimeId == runtimeId);
                        var msrcCveInfo = msrcConfig?.Cves?.FirstOrDefault(c => c.CveId == cveId);
                        
                        string cveTitle = msrcCveInfo?.CveTitle ?? cveId; // Use MSRC title if available, otherwise use ID
                          // Always use the placeholder URL with "xxx" as requested
                        string cveUrl = "https://github.com/dotnet/announcements/issues/xxx";
                            
                        string cveEntry = $"  - [{cveId} | {cveTitle}]({cveUrl})";
                        cveItems.Add(cveEntry);
                    }
                }
            }            // Add MSRC information from config if available
            // This section adds CVEs from MSRC config that weren't already added from the release data
            var currentMsrcConfig = _msrcConfigs.FirstOrDefault(m => m.RuntimeId == runtimeId);
            if (currentMsrcConfig != null && currentMsrcConfig.Cves != null && currentMsrcConfig.Cves.Count > 0)
            {
                foreach (var cve in currentMsrcConfig.Cves)
                {// Check if this is a duplicate with an existing CVE entry we've already added
                    bool isDuplicate = cveItems.Any(item => item.Contains(cve.CveId));
                    if (!isDuplicate)
                    {
                        string cveUrl = GetAnnouncementUrlForCve(cve.CveId);
                        string cveEntry = $"  - [{cve.CveId} | {cve.CveTitle}]({cveUrl})";
                        cveItems.Add(cveEntry);
                    }
                }
            }

            // If no CVEs found, add a "No new CVEs" entry
            if (cveItems.Count == 0)
            {
                cveItems.Add("  - No new CVEs.");
            }            // Build the new entry with the release version, date, and CVE items
            // Format exactly as requested without the security advisories link
            string releaseHeader = $"- {releaseVersion} ({formattedDate})";
            string newEntry = $"{releaseHeader}\n{string.Join("\n", cveItems)}\n";            // Insert the new entry after the section header
            string updatedContent = sectionRegex.Replace(content, sectionHeader + newEntry);
            
            Console.WriteLine($"Added new CVE entry for release {releaseVersion} dated {formattedDate}");
            
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
        }        private string FormatReleaseDate(string releaseDate)
        {
            // Try to parse the date from the JSON
            if (DateTime.TryParse(releaseDate, out DateTime parsedDate))
            {
                // Format as "MMMM yyyy" (e.g., "April 2025")
                return parsedDate.ToString("MMMM yyyy");
            }
            
            // Return the original string if parsing fails
            Console.WriteLine($"WARNING: Unable to parse release date: {releaseDate}");
            return releaseDate;
        }

        private string ExtractCveIdFromUrl(string cveUrl)
        {
            // Try to extract a CVE ID (e.g., "CVE-2025-12345") from the URL
            // Common patterns in URLs: 
            // - Contains "CVE-YYYY-NNNNN"
            // - Contains "cve=CVE-YYYY-NNNNN"
            // - The last path segment may be the CVE ID
            
            if (string.IsNullOrEmpty(cveUrl))
                return "CVE-Unknown";

            // Try to find a CVE pattern in the URL
            var cveRegex = new Regex(@"CVE-\d{4}-\d{4,}");
            var match = cveRegex.Match(cveUrl);
            
            if (match.Success)
                return match.Value;
            
            // If no CVE ID found, try to get the last path segment
            var uri = new Uri(cveUrl);
            string lastSegment = uri.Segments.LastOrDefault() ?? string.Empty;
            
            // Clean up the segment (remove trailing slash, .html, etc.)
            lastSegment = lastSegment.TrimEnd('/').Replace(".html", string.Empty);
            
            if (!string.IsNullOrEmpty(lastSegment))
                return lastSegment;
                
            // If all else fails, return the domain as an identifier
            return uri.Host;
        }        private string GetAnnouncementUrlForCve(string cveId)
        {
            // Always use "xxx" as the issue number placeholder as requested in the example
            return "https://github.com/dotnet/announcements/issues/xxx";
        }
    }
}