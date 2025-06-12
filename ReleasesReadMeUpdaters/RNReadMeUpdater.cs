using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;
using System.Globalization;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater.ReleasesReadMeUpdaters
{
    public class RNReadMeUpdater
    {
        private readonly string _templateDirectory;
        private readonly string _logFileLocation;
        private readonly string _outputDirectory;
        private readonly string _coreDirectory;
        private readonly Dictionary<string, string> _launchDates;
        private readonly Dictionary<string, string> _announcementLinks;
        private readonly JsonFileHandler _jsonFileHandler;
        private readonly List<string> _runtimeIds;
        private readonly Dictionary<string, string> _eolDates;
        public RNReadMeUpdater(string templateDirectory, string logFileLocation, string outputDirectory, string coreDirectory, JsonFileHandler jsonFileHandler, string configDirectory, List<string> runtimeIds)
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
            // Load EOL dates
            var eolDatesPath = Path.Combine(configDirectory, "eol-dates.json");
            _eolDates = _jsonFileHandler.DeserializeJsonFile<Dictionary<string, string>>(eolDatesPath) ?? new();
        }

        public void UpdateFiles()
        {
            try
            {
                Console.WriteLine($"[DEBUG] Core Directory: {_coreDirectory}");
                Console.WriteLine($"[DEBUG] Output Directory: {_outputDirectory}");

                string templatePath = Path.Combine(_templateDirectory, "rn-readme-template.md");

                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template file '{templatePath}' not found.");
                }

                string templateContent = File.ReadAllText(templatePath);

                // Generate the markdown table, dynamic links, and list of markdown files
                string markdownTable = GenerateMarkdownTable(out string dynamicLinks, out string markdownFilesList);

                // Add the "[policies]" link at the end of the dynamic links
                dynamicLinks += "\n[policies]: ../release-policies.md";

                // Replace the placeholders in the template
                string updatedContent = templateContent
                    .Replace("SECTION-RELEASE", $"{markdownTable}\n{dynamicLinks}")
                    .Replace("SECTION-MARKDOWNFILES", markdownFilesList);

                // Define the output path for the updated file
                string outputFilePath = Path.Combine(_outputDirectory, "2README.md");

                // Ensure the output directory exists
                Directory.CreateDirectory(_outputDirectory);

                // Write the updated content to the output file
                File.WriteAllText(outputFilePath, updatedContent, Encoding.UTF8);

                Console.WriteLine($"README file has been successfully updated and saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                LogError($"An error occurred while updating the README file: {ex.Message}");
            }
        }

        private string GenerateMarkdownTable(out string dynamicLinks, out string markdownFilesList)
        {
            // Define the table structure
            StringBuilder tableBuilder = new StringBuilder();
            StringBuilder linksBuilder = new StringBuilder();
            StringBuilder markdownFilesBuilder = new StringBuilder();
            var versionRows = new List<(string Version, string Row, string LinkEntry, string MarkdownFileEntry)>();

            tableBuilder.AppendLine("|  Version  | Release Date | Release type | Support phase | Latest Patch Version | End of Support |");
            tableBuilder.AppendLine("| :-- | :-- | :-- | :-- | :-- | :-- |");

            try
            {
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
                    string latestRelease = configData.LatestRelease ?? "TBA";
                    string supportPhase = ToTitleCase(configData.SupportPhase ?? "TBA");
                    string releaseType = (configData.ReleaseType ?? "TBA").ToUpper();
                    // Use EOL date from _eolDates, try both version and version + '.0' as keys
                    string eolDate = _eolDates.TryGetValue(version, out var eol) ? eol :
                        (_eolDates.TryGetValue(version + ".0", out var eolDot) ? eolDot : "TBD");
                    if (supportPhase.Equals("EOL", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (latestRelease.Contains("preview", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    string launchDate = GetLaunchDate(version);
                    string announcementLink = GetAnnouncementLink(version);
                    string releaseDateColumn = string.IsNullOrEmpty(announcementLink) ? launchDate : $"[{launchDate}]({announcementLink})";
                    string versionDisplay = $"[.NET {version}](./{version}/README.md)";
                    string row = $"| {versionDisplay} | {releaseDateColumn} | [{releaseType}][policies] | {supportPhase} | [{latestRelease}][{latestRelease}] | {eolDate} |";
                    string linkPath;
                    if (latestRelease.Contains("preview"))
                    {
                        string[] previewParts = latestRelease.Split('-');
                        if (previewParts.Length == 2 && previewParts[1].StartsWith("preview"))
                        {
                            string previewNumber = previewParts[1].Replace("preview.", "preview");
                            linkPath = $"release-notes/{version}/preview/{previewNumber}/{latestRelease}.md";
                        }
                        else
                        {
                            LogError($"Unexpected preview release format: {latestRelease}");
                            continue;
                        }
                    }
                    else if (version.Contains("."))
                    {
                        linkPath = $"./{version}/{latestRelease}/{latestRelease}.md";
                    }
                    else
                    {
                        linkPath = $"./{version}/{latestRelease}/{latestRelease}.md";
                    }
                    string linkEntry = string.IsNullOrEmpty(latestRelease) ? "" : $"[{latestRelease}]: {linkPath}";
                    string markdownFileEntry = $"* [{version}/{latestRelease}/{latestRelease}.md](./{version}/{latestRelease}/{latestRelease}.md)";
                    versionRows.Add((version, row, linkEntry, markdownFileEntry));
                }
            }
            catch (Exception ex)
            {
                LogError($"An error occurred while generating the markdown table: {ex.Message}");
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
            foreach (var versionRow in versionRows)
            {
                markdownFilesBuilder.AppendLine(versionRow.MarkdownFileEntry);
            }
            dynamicLinks = linksBuilder.ToString();
            markdownFilesList = markdownFilesBuilder.ToString();
            return tableBuilder.ToString();
        }
        // Helper method to convert version string to a numeric value for sorting
        private double GetVersionSortValue(string version)
        {
            if (double.TryParse(version, out double result))
            {
                return result;
            }
            var match = System.Text.RegularExpressions.Regex.Match(version, @"(\d+(\.\d+)?)");
            if (match.Success && double.TryParse(match.Groups[1].Value, out double value))
            {
                return value;
            }
            return 0;
        }

        private string GetLaunchDate(string channelVersion)
        {
            return _launchDates.TryGetValue(channelVersion, out string? launchDate) ? launchDate : "TBD";
        }

        private string GetAnnouncementLink(string channelVersion)
        {
            return _announcementLinks.TryGetValue(channelVersion, out string? link) ? link : string.Empty;
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