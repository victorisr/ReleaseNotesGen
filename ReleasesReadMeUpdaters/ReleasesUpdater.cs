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

        public ReleasesUpdater(string templateDirectory, string logFileLocation, string outputDirectory, string coreDirectory, JsonFileHandler jsonFileHandler)
        {
            _templateDirectory = templateDirectory;
            _logFileLocation = logFileLocation;
            _outputDirectory = outputDirectory;
            _coreDirectory = coreDirectory;
            _jsonFileHandler = jsonFileHandler;

            // Initialize launch dates for channel versions
            _launchDates = new Dictionary<string, string>
            {
                { "10.0", "November 11, 2025" },
                { "9.0", "November 12, 2024" },
                { "8.0", "November 14, 2023" },
                { "7.0", "November 8, 2022" },
                { "6.0", "November 8, 2021" },
                { "5.0", "November 10, 2020" },
                { "3.1", "December 3, 2019" },
                { "3.0", "September 23, 2019" },
                { "2.2", "December 4th, 2018" },
                { "2.1", "May 30, 2018" },
                { "2.0", "August 14th, 2017" },
                { "1.1", "November 16th, 2016" },
                { "1.0", "June 27th, 2016" }
            };

            // Initialize announcement links for channel versions
            _announcementLinks = new Dictionary<string, string>
            {
                { "9.0", "https://devblogs.microsoft.com/dotnet/announcing-dotnet-9/" },
                { "8.0", "https://devblogs.microsoft.com/dotnet/announcing-dotnet-8/" },
                { "7.0", "https://devblogs.microsoft.com/dotnet/announcing-dotnet-7/" },
                { "6.0", "https://devblogs.microsoft.com/dotnet/announcing-net-6/" },
                { "5.0", "https://devblogs.microsoft.com/dotnet/announcing-net-5-0/" },
                { "3.1", "https://devblogs.microsoft.com/dotnet/announcing-net-core-3-1/" },
                { "3.0", "https://devblogs.microsoft.com/dotnet/announcing-net-core-3-0/" },
                { "2.2", "https://devblogs.microsoft.com/dotnet/announcing-net-core-2-2/" },
                { "2.1", "https://devblogs.microsoft.com/dotnet/announcing-net-core-2-1/" },
                { "2.0", "https://devblogs.microsoft.com/dotnet/announcing-net-core-2-0/" },
                { "1.1", "https://devblogs.microsoft.com/dotnet/announcing-net-core-1-1/" },
                { "1.0", "https://devblogs.microsoft.com/dotnet/announcing-net-core-1-0/" }
            };

            // Initialize EOL announcement links for channel versions
            _eolAnnouncementLinks = new Dictionary<string, string>
            {
                { "7.0", "https://devblogs.microsoft.com/dotnet/dotnet-7-end-of-support/" },
                { "6.0", "https://devblogs.microsoft.com/dotnet/dotnet-6-end-of-support/" },
                { "5.0", "https://devblogs.microsoft.com/dotnet/dotnet-5-end-of-support-update/" },
                { "3.1", "https://devblogs.microsoft.com/dotnet/net-core-3-1-will-reach-end-of-support-on-december-13-2022/" },
                { "3.0", "https://devblogs.microsoft.com/dotnet/net-core-3-0-end-of-life/" },
                { "2.2", "https://devblogs.microsoft.com/dotnet/net-core-2-2-will-reach-end-of-life-on-december-23-2019/" },
                { "2.1", "https://devblogs.microsoft.com/dotnet/net-core-2-1-will-reach-end-of-support-on-august-21-2021/" },
                { "2.0", "https://devblogs.microsoft.com/dotnet/net-core-2-0-will-reach-end-of-life-on-september-1-2018/" },
                { "1.1", "https://devblogs.microsoft.com/dotnet/net-core-1-0-and-1-1-will-reach-end-of-life-on-june-27-2019/" },
                { "1.0", "https://devblogs.microsoft.com/dotnet/net-core-1-0-and-1-1-will-reach-end-of-life-on-june-27-2019/" }
            };
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
            // Define the table structure
            StringBuilder tableBuilder = new StringBuilder();
            StringBuilder linksBuilder = new StringBuilder();

            if (supported)
            {
                tableBuilder.AppendLine("|  Version  | Release Date | Release type | Support phase | Latest Patch Version | End of Support |");
                tableBuilder.AppendLine("| :-- | :-- | :-- | :-- | :-- | :-- |");
            }
            else
            {
                tableBuilder.AppendLine("|  Version  | Release Date | Release type | Latest Patch Version | End of Support |");
                tableBuilder.AppendLine("| :-- | :-- | :-- | :-- | :-- |");
            }

            try
            {
                string releaseNotesPath = Path.Combine(_coreDirectory, "core", "release-notes");

                if (!Directory.Exists(releaseNotesPath))
                {
                    throw new DirectoryNotFoundException($"Release Notes folder not found at '{releaseNotesPath}'.");
                }

                var channelFolders = Directory.GetDirectories(releaseNotesPath);

                // Create a list to collect all rows with their version for sorting
                var versionRows = new List<(string Version, string Row, string LinkEntry)>();

                // Sort the channel folders in descending order
                Array.Sort(channelFolders, (x, y) => string.Compare(y, x, StringComparison.OrdinalIgnoreCase));

                foreach (var channelFolder in channelFolders)
                {
                    string channelVersion = new DirectoryInfo(channelFolder).Name;

                    // Skip irrelevant folders
                    if (channelVersion == "download-archives" || channelVersion == "schemas" || channelVersion == "templates")
                    {
                        continue;
                    }

                    string releasesFilePath = Path.Combine(channelFolder, "releases.json");
                    if (File.Exists(releasesFilePath))
                    {
                        CoreReleasesConfiguration? coreReleaseNotes = null;

                        try
                        {
                            coreReleaseNotes = _jsonFileHandler.DeserializeCoreReleasesConfiguration(releasesFilePath);
                        }
                        catch (JsonException ex)
                        {
                            LogError($"Failed to parse JSON in '{releasesFilePath}': {ex.Message}");
                            continue;
                        }

                        if (coreReleaseNotes != null)
                        {
                            string latestRelease = coreReleaseNotes.LatestRelease ?? "TBA";
                            string supportPhase = ToTitleCase(coreReleaseNotes.SupportPhase ?? "TBA");
                            string releaseType = (coreReleaseNotes.ReleaseType ?? "TBA").ToUpper();
                            string eolDate = FormatDate(coreReleaseNotes.EolDate);

                            // Check support phase against the "supported" parameter
                            bool isSupported = !supportPhase.Equals("EOL", StringComparison.OrdinalIgnoreCase);
                            if (supported != isSupported)
                            {
                                continue;
                            }

                            // Get the launch date for the channel version
                            string launchDate = GetLaunchDate(channelVersion);

                            // Generate the release date column with the announcement link if available
                            string announcementLink = GetAnnouncementLink(channelVersion);
                            string releaseDateColumn = string.IsNullOrEmpty(announcementLink)
                                ? launchDate
                                : $"[{launchDate}]({announcementLink})";

                            // Generate the EOL date column with the EOL announcement link if available
                            string eolAnnouncementLink = GetEolAnnouncementLink(channelVersion);
                            string eolDateColumn = string.IsNullOrEmpty(eolAnnouncementLink)
                                ? eolDate
                                : $"[{eolDate}]({eolAnnouncementLink})";

                            // Determine if this is a legacy .NET Core version (1.0-3.1)
                            bool isLegacyNetCore = IsLegacyNetCoreVersion(channelVersion);
                            
                            // Format the version display differently based on supported status and version
                            string versionDisplay;
                            if (!supported && isLegacyNetCore)
                            {
                                versionDisplay = $"[.NET Core {channelVersion}](release-notes/{channelVersion}/README.md)";
                            }
                            else
                            {
                                versionDisplay = $"[.NET {channelVersion}](release-notes/{channelVersion}/README.md)";
                            }

                            string row;
                            if (supported)
                            {
                                row = $"| {versionDisplay} | {releaseDateColumn} | [{releaseType}][policies] | {supportPhase} | [{latestRelease}][{latestRelease}] | {eolDateColumn} |";
                            }
                            else
                            {
                                row = $"| {versionDisplay} | {releaseDateColumn} | [{releaseType}][policies] | [{latestRelease}][{latestRelease}] | {eolDateColumn} |";
                            }

                            // Add the dynamic link for the latest release
                            string linkEntry = "";
                            if (!string.IsNullOrEmpty(latestRelease))
                            {
                                string linkPath = GenerateLinkPath(channelVersion, latestRelease);
                                linkEntry = $"[{latestRelease}]: {linkPath}";
                            }

                            // Add to collection for sorting
                            versionRows.Add((channelVersion, row, linkEntry));
                        }
                    }
                }

                // Sort versions by numeric value (descending)
                versionRows = versionRows.OrderByDescending(v => GetVersionSortValue(v.Version)).ToList();

                // Add rows to table in sorted order
                foreach (var versionRow in versionRows)
                {
                    tableBuilder.AppendLine(versionRow.Row);
                }

                // Add links in the same order
                foreach (var versionRow in versionRows)
                {
                    if (!string.IsNullOrEmpty(versionRow.LinkEntry))
                    {
                        linksBuilder.AppendLine(versionRow.LinkEntry);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"An error occurred while generating the markdown table: {ex.Message}");
            }

            dynamicLinks = "\n" + linksBuilder.ToString().TrimEnd(); // Add a single newline after the table and trim trailing empty lines in links
            return tableBuilder.ToString().TrimEnd(); // Remove trailing empty lines in the table
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