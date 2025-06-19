using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater.InstallersMarkdownUpdaters
{
    public class InstallWindowsUpdater : FileUpdater
    {
        // List to store runtime IDs
        private readonly List<string> runtimeIds;

        // Path to the directory where the downloaded artifacts are stored
        private readonly string downloadPath;

        // Path to the directory where the new files will be created
        private readonly string outputPath;

        // Name of the new file to be created (declared within the class)
        private readonly string newFileName = "3install-windows";

        // Instance of JsonFileHandler for JSON file handling
        private readonly JsonFileHandler _jsonFileHandler;

        // Constructor to initialize the updater with relevant directories, log file location, runtime IDs, download path, output path, and JsonFileHandler
        public InstallWindowsUpdater(string templateDirectory, string logFileLocation, List<string> runtimeIds, string downloadPath, string outputPath, JsonFileHandler jsonFileHandler)
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
                                    string channelVersion = configData.ChannelVersion ?? "unknown";
                                    string installWindowsTemplate = Path.Combine(TemplateDirectory, "install-windows-template.md");
                                    string outputDir = Path.Combine(outputPath, channelVersion);
                                    string newInstallWindowsFile = Path.Combine(outputDir, $"{newFileName}-{runtimeId.Replace(".", "")}.md");

                                    // Ensure the directory for the new file exists
                                    CreateDirectoryIfNotExists(outputDir);

                                    // Check if the file already exists to avoid duplication
                                    if (!File.Exists(newInstallWindowsFile))
                                    {
                                        // Modify the template file with data from the configuration and write to the new file
                                        ModifyTemplateFile(installWindowsTemplate, newInstallWindowsFile, runtimeId, configData.ChannelVersion, release, configData.LatestSdk);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"File already exists: {newInstallWindowsFile}. Skipping creation.");
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
        private void ModifyTemplateFile(string templatePath, string outputPath, string version, string? channelVersion, Release release, string? latestSdk)
        {
            // Read the content of the template file
            string templateContent = File.ReadAllText(templatePath);

            // Logging the extracted channel version and latest SDK
            Console.WriteLine($"Extracted channel version: {channelVersion} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Extracted latest SDK: {latestSdk} for runtime ID: {version}"); // Debug log

            // Find the URL for "dotnet-sdk-win-x64.exe"
            string sdkUrl = "";
            if (release.Sdk != null && release.Sdk.Files != null)
            {
                foreach (var sdkFile in release.Sdk.Files)
                {
                    if (sdkFile.Name == "dotnet-sdk-win-x64.exe")
                    {
                        sdkUrl = sdkFile.Url;
                        break;
                    }
                }
            }
            Console.WriteLine($"Extracted SDK URL: {sdkUrl} for runtime ID: {version}"); // Debug log

            // Replace placeholders in the template with actual data
            string modifiedContent = templateContent
                .Replace("{ID-VERSION}", channelVersion ?? "")
                .Replace("{WIN-SDK-URL}", sdkUrl)
                .Replace("{LATEST-SDK}", latestSdk ?? "");

            // Write the modified content to the output path
            File.WriteAllText(outputPath, modifiedContent);

            // Confirm file creation
            Console.WriteLine($"New install-windows file created at: {outputPath}");
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