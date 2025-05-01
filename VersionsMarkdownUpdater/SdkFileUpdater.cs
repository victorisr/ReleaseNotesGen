using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater.VersionsMarkdownUpdater
{
    public class SdkFileUpdater : FileUpdater
    {
        // List to store runtime IDs
        private readonly List<string> runtimeIds;

        // Path to the directory where the downloaded artifacts are stored
        private readonly string downloadPath;

        // Path to the directory where the new files will be created
        private readonly string outputPath;

        // Instance of JsonFileHandler for JSON file operations
        private readonly JsonFileHandler _jsonFileHandler;

        // Constructor to initialize the updater with relevant directories, log file location, runtime IDs, download path, output path, and JsonFileHandler
        public SdkFileUpdater(string templateDirectory, string logFileLocation, List<string> runtimeIds, string downloadPath, string outputPath, JsonFileHandler jsonFileHandler)
            : base(templateDirectory, logFileLocation)
        {
            this.runtimeIds = runtimeIds;
            this.downloadPath = downloadPath;
            this.outputPath = outputPath;
            _jsonFileHandler = jsonFileHandler;
        }

        // Method to update files based on the loaded configuration data
        public override void UpdateFiles()
        {
            foreach (var runtimeId in runtimeIds)
            {
                string? jsonFilePath = _jsonFileHandler.FindJsonFile(runtimeId, $"releases-json-CDN-{runtimeId}.json");

                if (jsonFilePath != null)
                {
                    var configData = _jsonFileHandler.DeserializeReleasesConfiguration(jsonFilePath);

                    // Check if the releases section exists in the configuration data
                    if (configData?.Releases != null)
                    {
                        foreach (var release in configData.Releases)
                        {
                            // Check if release and necessary properties are not null
                            if (release != null && release.Runtime != null && release.Runtime.Version != null)
                            {
                                // Find the release that matches the current version
                                if (release.Runtime.Version == runtimeId)
                                {
                                    // For each version in the sdks section, create a new file if it's not the latest-sdk
                                    foreach (var sdk in release.Sdks)
                                    {
                                        string sdkVersion = sdk.Version;
                                        if (sdkVersion != configData.LatestSdk)
                                        {
                                            string sdkTemplate = Path.Combine(TemplateDirectory, "sdk-template.md");
                                            string newSdkFile = Path.Combine(outputPath, $"{sdkVersion}.md");

                                            // Ensure the directory for the new file exists
                                            CreateDirectoryIfNotExists(outputPath);
                                            // Modify the template file with data from the configuration and write to the new file
                                            ModifyTemplateFile(sdkTemplate, newSdkFile, sdkVersion, configData, sdk);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Log if release or necessary properties are null
                                Console.WriteLine($"Invalid release data for runtime ID: {runtimeId}");
                            }
                        }
                    }
                    else
                    {
                        // Log if no configuration found for the version
                        Console.WriteLine($"No configuration found for version: {runtimeId}");
                    }
                }
                else
                {
                    // Log if JSON file is not found
                    Console.WriteLine($"JSON file not found for runtime ID: {runtimeId}");
                }
            }
        }

        // Method to modify the template file with actual data and write to the output path
        private void ModifyTemplateFile(string templatePath, string outputPath, string sdkVersion, ReleasesConfiguration configData, Sdk sdk)
        {
            // Read the content of the template file
            string templateContent = File.ReadAllText(templatePath);

            // Extract key values from the config data
            string runtimeVersion = configData.LatestRuntime;
            string latestSdk = configData.LatestSdk;
            string channelVersion = configData.ChannelVersion;
            string latestReleaseDate = configData.LatestReleaseDate;

            // Format the latest release date
            string formattedDate = DateTime.Parse(latestReleaseDate).ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);

            // Logging the extracted values
            Console.WriteLine($"Extracted runtime version: {runtimeVersion} for SDK version: {sdkVersion}"); // Debug log
            Console.WriteLine($"Extracted latest SDK: {latestSdk} for SDK version: {sdkVersion}"); // Debug log
            Console.WriteLine($"Extracted channel version: {channelVersion} for SDK version: {sdkVersion}"); // Debug log
            Console.WriteLine($"Extracted latest release date: {formattedDate} for SDK version: {sdkVersion}"); // Debug log

            // Replace placeholders in the template with actual data
            string modifiedContent = templateContent
                .Replace("{RUNTIME-VERSION}", runtimeVersion ?? "")
                .Replace("{LATEST-SDK}", latestSdk ?? "")
                .Replace("{ID-VERSION}", channelVersion ?? "")
                .Replace("{SDK-VERSION}", sdkVersion ?? "")
                .Replace("{HEADER-DATE}", formattedDate ?? "");

            // Replace section placeholders with markdown-style tables
            modifiedContent = ReplaceSectionPlaceholders(modifiedContent, configData, sdk);

            // Write the modified content to the output path
            File.WriteAllText(outputPath, modifiedContent);

            // Confirm file creation
            Console.WriteLine($"New SDK file created at: {outputPath}");
        }

        // Method to replace section placeholders with markdown-style tables
        private string ReplaceSectionPlaceholders(string content, ReleasesConfiguration configData, Sdk sdk)
        {
            content = content.Replace("SECTION-RUNTIME", ReplaceRuntimeSection(configData, sdk));
            content = content.Replace("SECTION-WINDOWSDESKTOP", ReplaceWindowsDesktopSection(configData, sdk));
            content = content.Replace("SECTION-ASP", ReplaceAspSection(configData, sdk));
            content = content.Replace("SECTION-VERSIONSDK", ReplaceVersionSdkSection(sdk.Version, sdk.Files));
            return content;
        }

        // Method to replace SECTION-RUNTIME placeholder
        private string ReplaceRuntimeSection(ReleasesConfiguration configData, Sdk sdk)
        {
            var runtimeSection = configData.Releases?[0].Runtime;
            if (runtimeSection == null)
            {
                Console.WriteLine("SECTION-RUNTIME: No data found.");
                return "";
            }
            var markdownList = $"[//]: # ( Runtime {configData.LatestRuntime})\n";
            var files = runtimeSection.Files;
            if (files != null)
            {
                foreach (var file in files)
                {
                    markdownList += $"[{file.Name}]: {file.Url}\n";
                }
            }
            else
            {
                Console.WriteLine("SECTION-RUNTIME: No files found.");
            }
            return markdownList;
        }

        // Method to replace SECTION-WINDOWSDESKTOP placeholder
        private string ReplaceWindowsDesktopSection(ReleasesConfiguration configData, Sdk sdk)
        {
            var windowsDesktopSection = configData.Releases?[0].WindowsDesktop;
            if (windowsDesktopSection == null)
            {
                Console.WriteLine("SECTION-WINDOWSDESKTOP: No data found.");
                return "";
            }
            var markdownList = $"[//]: # ( WindowsDesktop {configData.LatestRuntime})\n";
            var files = windowsDesktopSection.Files;
            if (files != null)
            {
                foreach (var file in files)
                {
                    markdownList += $"[{file.Name}]: {file.Url}\n";
                }
            }
            else
            {
                Console.WriteLine("SECTION-WINDOWSDESKTOP: No files found.");
            }
            return markdownList;
        }

        // Method to replace SECTION-ASP placeholder
        private string ReplaceAspSection(ReleasesConfiguration configData, Sdk sdk)
        {
            var aspSection = configData.Releases?[0].AspNetCoreRuntime;
            if (aspSection == null)
            {
                Console.WriteLine("SECTION-ASP: No data found.");
                return "";
            }
            var markdownList = $"[//]: # ( ASP {configData.LatestRuntime})\n";
            var files = aspSection.Files;
            if (files != null)
            {
                foreach (var file in files)
                {
                    markdownList += $"[{file.Name}]: {file.Url}\n";
                }
            }
            else
            {
                Console.WriteLine("SECTION-ASP: No files found.");
            }
            return markdownList;
        }

        // Method to replace SECTION-VERSIONSDK placeholder
        private string ReplaceVersionSdkSection(string sdkVersion, List<Models.FileInfo> files)
        {
            if (files == null) return "";
            var markdownList = $"[//]: # ( SDK {sdkVersion})\n";
            foreach (var file in files)
            {
                markdownList += $"[{file.Name}]: {file.Url}\n";
            }
            return markdownList;
        }

        // Helper method to create directory if it does not exist
        private new void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}