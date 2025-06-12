using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;
using System.Globalization;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater.ReleasesReadMeUpdaters
{
    public class ReleasesUpdater
    {
        private readonly string _templateDirectory;
        private readonly string _logFileLocation;
        private readonly string _outputDirectory;
        private readonly string _coreDirectory;
        private readonly Dictionary<string, string> _launchDates;
        private readonly Dictionary<string, string> _announcementLinks;
        private readonly Dictionary<string, string> _eolAnnouncementLinks;
        private readonly JsonFileHandler _jsonFileHandler;
        private readonly List<string> _runtimeIds;
        private readonly Dictionary<string, string> _eolDates;
        private readonly Dictionary<string, (string LatestRelease, string LatestReleaseDate)> _unsupportedVersions;

        public ReleasesUpdater(string templateDirectory, string logFileLocation, string outputDirectory, string coreDirectory, JsonFileHandler jsonFileHandler, string configDirectory, List<string> runtimeIds)
        {
            _templateDirectory = templateDirectory;
            _logFileLocation = logFileLocation;
            _outputDirectory = outputDirectory;
            _coreDirectory = coreDirectory;
            _jsonFileHandler = jsonFileHandler;
            _runtimeIds = runtimeIds;

            // Load configuration from external JSON files
            var config = _jsonFileHandler.LoadReleaseReferenceConfiguration(configDirectory);
            _launchDates = config.LaunchDates;
            _announcementLinks = config.AnnouncementLinks;
            _eolAnnouncementLinks = config.EolAnnouncementLinks;

            // Load EOL dates
            var eolDatesPath = Path.Combine(configDirectory, "eol-dates.json");
            _eolDates = _jsonFileHandler.DeserializeJsonFile<Dictionary<string, string>>(eolDatesPath) ?? new();

            // Load unsupported versions
            var unsupportedPath = Path.Combine(configDirectory, "unsupported-versions.json");
            var unsupportedRaw = _jsonFileHandler.DeserializeJsonFile<Dictionary<string, Dictionary<string, string>>>(unsupportedPath) ?? new();
            _unsupportedVersions = new();
            foreach (var kvp in unsupportedRaw)
            {
                var v = kvp.Value;
                _unsupportedVersions[kvp.Key] = (v.GetValueOrDefault("LatestRelease", "TBA"), v.GetValueOrDefault("LatestReleaseDate", "TBD"));
            }
        }

        public void UpdateFiles()
        {
            try
            {
                Console.WriteLine($"[DEBUG] Core Directory: {_coreDirectory}");
                Console.WriteLine($"[DEBUG] Output Directory: {_outputDirectory}");

                string templatePath = Path.Combine(_templateDirectory, "releases-template.md");

                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template file '{templatePath}' not found.");
                }

                string templateContent = File.ReadAllText(templatePath);

                // Generate the markdown tables
                string supportedTable = GenerateMarkdownTable(supported: true, out string supportedLinks);
                string unsupportedTable = GenerateMarkdownTable(supported: false, out string unsupportedLinks);

                // Replace the placeholders in the template
                string updatedContent = templateContent
                    .Replace("SECTION-SUPPORTED", supportedTable + "\n" + supportedLinks)
                    .Replace("SECTION-UNSUPPORTED", unsupportedTable + "\n" + unsupportedLinks);

                // Define the output path for the updated file
                string outputFilePath = Path.Combine(_outputDirectory, "1releases.md");

                // Ensure the output directory exists
                Directory.CreateDirectory(_outputDirectory);

                // Write the updated content to the output file
                File.WriteAllText(outputFilePath, updatedContent, Encoding.UTF8);

                Console.WriteLine($"Releases file has been successfully updated and saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                LogError($"An error occurred while updating the releases file: {ex.Message}");
            }
        }

        private string GenerateMarkdownTable(bool supported, out string dynamicLinks)
        {
            StringBuilder tableBuilder = new StringBuilder();
            StringBuilder linksBuilder = new StringBuilder();
            var versionRows = new List<(string Version, string Row, string LinkEntry)>();

            if (supported)
            {
                tableBuilder.AppendLine("|  Version  | Release Date | Release type | Support phase | Latest Patch Version | End of Support |");
                tableBuilder.AppendLine("| :-- | :-- | :-- | :-- | :-- | :-- |");
                foreach (var runtimeId in _runtimeIds)
                {
                    string? jsonFilePath = _jsonFileHandler.FindJsonFile(runtimeId, $"releases-json-CDN-{runtimeId}.json");
                    if (jsonFilePath == null)
                    {
                        LogError($"CDN JSON not found for runtimeId: {runtimeId}");
                        continue;
                    }
                    var configData = _jsonFileHandler.DeserializeReleasesConfiguration(jsonFilePath);
                    if (configData == null)
                    {
                        LogError($"Could not parse CDN JSON for runtimeId: {runtimeId}");
                        continue;
                    }
                    string version = configData.ChannelVersion ?? runtimeId;
                    string releaseType = (configData.ReleaseType ?? "TBA").ToUpper();
                    string supportPhase = ToTitleCase(configData.SupportPhase ?? "TBA");
                    string latestRelease = configData.LatestRelease ?? "TBA";
                    string eolDate = _eolDates.TryGetValue(version, out var eol) ? eol : "TBD";
                    string launchDate = GetLaunchDate(version);
                    string announcementLink = GetAnnouncementLink(version);
                    string releaseDateColumn = string.IsNullOrEmpty(announcementLink) ? launchDate : $"[{launchDate}]({announcementLink})";
                    string eolAnnouncementLink = GetEolAnnouncementLink(version);
                    string eolDateColumn = string.IsNullOrEmpty(eolAnnouncementLink) ? eolDate : $"[{eolDate}]({eolAnnouncementLink})";
                    string versionDisplay = $"[.NET {version}](release-notes/{version}/README.md)";
                    string row = $"| {versionDisplay} | {releaseDateColumn} | [{releaseType}][policies] | {supportPhase} | [{latestRelease}][{latestRelease}] | {eolDateColumn} |";
                    string linkEntry = string.IsNullOrEmpty(latestRelease) ? "" : $"[{latestRelease}]: release-notes/{version}/{latestRelease}/{latestRelease}.md";
                    versionRows.Add((version, row, linkEntry));
                }
            }
            else
            {
                tableBuilder.AppendLine("|  Version  | Release Date | Release type | Latest Patch Version | End of Support |");
                tableBuilder.AppendLine("| :-- | :-- | :-- | :-- | :-- |");
                foreach (var kvp in _unsupportedVersions)
                {
                    string version = kvp.Key;
                    var (latestRelease, latestReleaseDate) = kvp.Value;
                    string eolDate = _eolDates.TryGetValue(version, out var eol) ? eol : "TBD";
                    // Get release type from unsupported-versions.json if available
                    string releaseType = "TBA";
                    if (_unsupportedVersions.TryGetValue(version, out var tuple))
                    {
                        // Try to get ReleaseType from the raw JSON (deserialize as Dictionary<string, object> if needed)
                        var unsupportedRaw = _jsonFileHandler.DeserializeJsonFile<Dictionary<string, Dictionary<string, string>>>(Path.Combine(Directory.GetCurrentDirectory(), "configuration", "unsupported-versions.json"));
                        if (unsupportedRaw != null && unsupportedRaw.TryGetValue(version, out var dict) && dict.TryGetValue("ReleaseType", out var rtype))
                        {
                            releaseType = rtype.ToUpper();
                        }
                    }
                    string versionDisplay = IsLegacyNetCoreVersion(version) ? $"[.NET Core {version}](release-notes/{version}/README.md)" : $"[.NET {version}](release-notes/{version}/README.md)";
                    string announcementKey = version.Contains(".") ? version : version + ".0";
                    string announcementLink = GetAnnouncementLink(announcementKey);
                    string releaseDateColumn = string.IsNullOrEmpty(announcementLink) ? latestReleaseDate : $"[{latestReleaseDate}]({announcementLink})";
                    string row = $"| {versionDisplay} | {releaseDateColumn} | [{releaseType}][policies] | [{latestRelease}][{latestRelease}] | {eolDate} |";
                    string linkEntry = string.IsNullOrEmpty(latestRelease) ? "" : $"[{latestRelease}]: release-notes/{version}/{latestRelease}/{latestRelease}.md";
                    versionRows.Add((version, row, linkEntry));
                }
            }
            // Sort versions by numeric value (descending)
            versionRows = versionRows.OrderByDescending(v => GetVersionSortValue(v.Version)).ToList();
            foreach (var versionRow in versionRows)
            {
                tableBuilder.AppendLine(versionRow.Row);
            }
            foreach (var versionRow in versionRows)
            {
                if (!string.IsNullOrEmpty(versionRow.LinkEntry))
                {
                    linksBuilder.AppendLine(versionRow.LinkEntry);
                }
            }
            dynamicLinks = "\n" + linksBuilder.ToString().TrimEnd();
            return tableBuilder.ToString().TrimEnd();
        }

        // Helper method to convert version string to a numeric value for sorting
        private double GetVersionSortValue(string version)
        {
            // Try to parse the version as a double
            if (double.TryParse(version, out double result))
            {
                return result;
            }
            
            // For complex version strings, extract first numeric part
            var match = System.Text.RegularExpressions.Regex.Match(version, @"(\d+(\.\d+)?)");
            if (match.Success && double.TryParse(match.Groups[1].Value, out double value))
            {
                return value;
            }
            
            return 0; // Default value for non-numeric versions
        }

        // Helper method to determine if a version is a legacy .NET Core version (1.0-3.1)
        private bool IsLegacyNetCoreVersion(string version)
        {
            string[] legacyVersions = { "1.0", "1.1", "2.0", "2.1", "2.2", "3.0", "3.1" };
            return Array.Exists(legacyVersions, v => v == version);
        }

        private string GenerateLinkPath(string channelVersion, string latestRelease)
        {
            if (latestRelease.Contains("preview"))
            {
                // Handle preview releases
                string[] previewParts = latestRelease.Split('-');
                if (previewParts.Length == 2 && previewParts[1].StartsWith("preview"))
                {
                    string previewNumber = previewParts[1].Replace("preview.", "preview");
                    return $"release-notes/{channelVersion}/preview/{previewNumber}/{latestRelease}.md";
                }
                else
                {
                    LogError($"Unexpected preview release format: {latestRelease}");
                    return string.Empty;
                }
            }
            else if (channelVersion.Contains("."))
            {
                // Handle final releases with no preview
                return $"release-notes/{channelVersion}/{latestRelease}/{latestRelease}.md";
            }
            else
            {
                // Handle other versions (default logic)
                return $"release-notes/{channelVersion}/{latestRelease}/{latestRelease}.md";
            }
        }

        private string GetLaunchDate(string channelVersion)
        {
            return _launchDates.TryGetValue(channelVersion, out string? launchDate) ? launchDate : "TBD";
        }

        private string GetAnnouncementLink(string channelVersion)
        {
            return _announcementLinks.TryGetValue(channelVersion, out string? link) ? link : string.Empty;
        }

        private string GetEolAnnouncementLink(string channelVersion)
        {
            return _eolAnnouncementLinks.TryGetValue(channelVersion, out string? link) ? link : string.Empty;
        }

        private string FormatDate(string? date)
        {
            if (DateTime.TryParse(date, out DateTime parsedDate))
            {
                // Format the date as "Month Day, Year"
                return parsedDate.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);
            }

            return "TBD";
        }

        private string ToTitleCase(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return "TBA";

            // Convert to Title Case
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(input.ToLowerInvariant());
        }

        private void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {message}");
            try
            {
                using (StreamWriter writer = new StreamWriter(_logFileLocation, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch
            {
                Console.WriteLine("Failed to write to log file.");
            }
        }
    }
}