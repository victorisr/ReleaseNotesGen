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

        public RNReadMeUpdater(string templateDirectory, string logFileLocation, string outputDirectory, string coreDirectory, JsonFileHandler jsonFileHandler)
        {
            _templateDirectory = templateDirectory;
            _logFileLocation = logFileLocation;
            _outputDirectory = outputDirectory;
            _coreDirectory = coreDirectory;
            _jsonFileHandler = jsonFileHandler;

            // Initialize dictionaries
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

            tableBuilder.AppendLine("|  Version  | Release Date | Release type | Support phase | Latest Patch Version | End of Support |");
            tableBuilder.AppendLine("| :-- | :-- | :-- | :-- | :-- | :-- |");

            try
            {
                string releaseNotesPath = Path.Combine(_coreDirectory, "core", "release-notes");

                if (!Directory.Exists(releaseNotesPath))
                {
                    throw new DirectoryNotFoundException($"Release Notes folder not found at '{releaseNotesPath}'.");
                }

                var channelFolders = Directory.GetDirectories(releaseNotesPath);

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
                        // Deserialize safely
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

                            // Skip channel versions that are in EOL phase
                            if (supportPhase.Equals("EOL", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // Skip preview versions for markdown file list
                            if (latestRelease.Contains("preview", StringComparison.OrdinalIgnoreCase))
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

                            tableBuilder.AppendLine($"| [.NET {channelVersion}](./{channelVersion}/README.md) | {releaseDateColumn} | [{releaseType}][policies] | {supportPhase} | [{latestRelease}][{latestRelease}] | {eolDate} |");

                            if (!string.IsNullOrEmpty(latestRelease))
                            {
                                // Build the markdown file list
                                markdownFilesBuilder.AppendLine($"* [{channelVersion}/{latestRelease}/{latestRelease}.md](./{channelVersion}/{latestRelease}/{latestRelease}.md)");

                                // Add the dynamic link for the latest release
                                string linkPath;
                                if (latestRelease.Contains("preview"))
                                {
                                    // Handle preview releases
                                    string[] previewParts = latestRelease.Split('-');
                                    if (previewParts.Length == 2 && previewParts[1].StartsWith("preview"))
                                    {
                                        string previewNumber = previewParts[1].Replace("preview.", "preview");
                                        linkPath = $"release-notes/{channelVersion}/preview/{previewNumber}/{latestRelease}.md";
                                    }
                                    else
                                    {
                                        LogError($"Unexpected preview release format: {latestRelease}");
                                        continue;
                                    }
                                }
                                else if (channelVersion.Contains("."))
                                {
                                    // Handle final releases with no preview
                                    linkPath = $"./{channelVersion}/{latestRelease}/{latestRelease}.md";
                                }
                                else
                                {
                                    // Handle other versions (default logic)
                                    linkPath = $"./{channelVersion}/{latestRelease}/{latestRelease}.md";
                                }

                                linksBuilder.AppendLine($"[{latestRelease}]: {linkPath}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"An error occurred while generating the markdown table: {ex.Message}");
            }

            markdownFilesList = markdownFilesBuilder.ToString();
            dynamicLinks = linksBuilder.ToString();
            return tableBuilder.ToString();
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