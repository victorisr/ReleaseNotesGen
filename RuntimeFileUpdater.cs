using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater
{
    public class RuntimeFileUpdater : FileUpdater
    {
        // List to store runtime IDs
        private readonly List<string> runtimeIds;

        // Path to the directory where the downloaded artifacts are stored
        private readonly string downloadPath;

        // Path to the directory where the new files will be created
        private readonly string outputPath;

        // Instance of JsonFileHandler for JSON file handling
        private readonly JsonFileHandler _jsonFileHandler;

        // Constructor to initialize the updater with relevant directories, log file location, runtime IDs, download path, output path, and new file name
        public RuntimeFileUpdater(string templateDirectory, string logFileLocation, List<string> runtimeIds, string downloadPath, string outputPath, JsonFileHandler jsonFileHandler)
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
                                    string runtimeTemplate = Path.Combine(TemplateDirectory, "runtime-template.md");
                                    string newRuntimeFile = Path.Combine(outputPath, $"{runtimeId}.md");

                                    // Ensure the directory for the new file exists
                                    CreateDirectoryIfNotExists(outputPath);
                                    // Modify the template file with data from the configuration and write to the new file
                                    ModifyTemplateFile(runtimeTemplate, newRuntimeFile, runtimeId, configData, release);
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
        private void ModifyTemplateFile(string templatePath, string outputPath, string version, ReleasesConfiguration configData, Release release)
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
            string blogPostDate = DateTime.Parse(latestReleaseDate).ToString("MMMM-yyyy", CultureInfo.InvariantCulture).ToLower();

            // Logging the extracted values
            Console.WriteLine($"Extracted runtime version: {runtimeVersion} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Extracted latest SDK: {latestSdk} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Extracted channel version: {channelVersion} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Extracted latest release date: {formattedDate} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Formatted blog post date: {blogPostDate} for runtime ID: {version}"); // Debug log

            // Replace placeholders in the template with actual data
            string modifiedContent = templateContent
                .Replace("{RUNTIME-VERSION}", runtimeVersion ?? "")
                .Replace("{LATEST-SDK}", latestSdk ?? "")
                .Replace("{ID-VERSION}", channelVersion ?? "")
                .Replace("{HEADER-DATE}", formattedDate ?? "")
                .Replace("{BLOGPOST-DATE}", blogPostDate ?? "");

            // Replace section placeholders with markdown-style tables
            modifiedContent = ReplaceSectionPlaceholders(modifiedContent, configData, release);

            // Write the modified content to the output path
            File.WriteAllText(outputPath, modifiedContent);

            // Confirm file creation
            Console.WriteLine($"New runtime file created at: {outputPath}");
        }

        // Method to replace section placeholders with markdown-style tables
        private string ReplaceSectionPlaceholders(string content, ReleasesConfiguration configData, Release release)
        {
            content = content.Replace("SECTION-ADDEDSDK", ReplaceAddedSdkSection(configData, release.Sdks, configData.LatestSdk));
            content = content.Replace("SECTION-SDKS", ReplaceSdksSection(configData, release.Sdks, configData.LatestSdk, configData.LatestRuntime));
            content = content.Replace("SECTION-RUNTIME", ReplaceRuntimeSection(configData.LatestRuntime, release.Runtime));
            content = content.Replace("SECTION-WINDOWSDESKTOP", ReplaceWindowsDesktopSection(configData.LatestRuntime, release.WindowsDesktop));
            content = content.Replace("SECTION-ASP", ReplaceAspSection(configData.LatestRuntime, release.AspNetCoreRuntime));
            content = content.Replace("SECTION-LATESTSDK", ReplaceLatestSdkSection(configData.LatestSdk, release.Sdk));
            content = content.Replace("SECTION-PACKAGES", ReplacePackagesSection(release.Packages));
            return content;
        }

        // Method to replace SECTION-ADDEDSDK placeholder
        private string ReplaceAddedSdkSection(ReleasesConfiguration configData, List<Sdk> sdks, string latestSdk)
        {
            if (sdks == null) return "";
            var markdownList = "\n";
            foreach (var sdk in sdks)
            {
                var version = sdk.Version;
                if (version != latestSdk)
                {
                    markdownList += $"* [{version}][{version}]\n";
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-SDKS placeholder
        private string ReplaceSdksSection(ReleasesConfiguration configData, List<Sdk> sdks, string latestSdk, string runtimeVersion)
        {
            if (sdks == null) return "";
            var markdownList = "\n";
            foreach (var sdk in sdks)
            {
                var version = sdk.Version;
                if (version == latestSdk)
                {
                    markdownList += $"[{latestSdk}]: {runtimeVersion}.md\n";
                }
                else
                {
                    markdownList += $"[{version}]: {version}.md\n";
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-RUNTIME placeholder
        private string ReplaceRuntimeSection(string latestRuntime, Runtime runtime)
        {
            if (runtime == null) return "";
            var markdownList = $"[//]: # ( Runtime {latestRuntime})\n";
            var files = runtime.Files;
            if (files != null)
            {
                foreach (var file in files)
                {
                    markdownList += $"[{file.Name}]: {file.Url}\n";
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-WINDOWSDESKTOP placeholder
        private string ReplaceWindowsDesktopSection(string latestRuntime, WindowsDesktop windowsDesktop)
        {
            if (windowsDesktop == null) return "";
            var markdownList = $"[//]: # ( WindowsDesktop {latestRuntime})\n";
            var files = windowsDesktop.Files;
            if (files != null)
            {
                foreach (var file in files)
                {
                    markdownList += $"[{file.Name}]: {file.Url}\n";
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-ASP placeholder
        private string ReplaceAspSection(string latestRuntime, AspNetCoreRuntime aspNetCoreRuntime)
        {
            if (aspNetCoreRuntime == null) return "";
            var markdownList = $"[//]: # ( ASP {latestRuntime})\n";
            var files = aspNetCoreRuntime.Files;
            if (files != null)
            {
                foreach (var file in files)
                {
                    markdownList += $"[{file.Name}]: {file.Url}\n";
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-LATESTSDK placeholder
        private string ReplaceLatestSdkSection(string latestSdk, Sdk sdk)
        {
            if (sdk == null) return "";
            var markdownList = $"[//]: # ( SDK {latestSdk})\n";
            var files = sdk.Files;
            if (files != null)
            {
                foreach (var file in files)
                {
                    markdownList += $"[{file.Name}]: {file.Url}\n";
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-PACKAGES placeholder
        private string ReplacePackagesSection(List<Package> packages)
        {
            if (packages == null) return "";
            var table = $"## Packages\n\n| Name | Version |\n| ---- | ------- |\n";
            foreach (var package in packages)
            {
                table += $"| {package.Name} | {package.Version} |\n";
            }
            return table;
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