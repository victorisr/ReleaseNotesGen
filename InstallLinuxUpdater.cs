using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ReleaseNotesUpdater
{
    public class InstallLinuxUpdater : FileUpdater
    {
        // List to store runtime IDs
        private readonly List<string> runtimeIds;

        // Path to the directory where the downloaded artifacts are stored
        private readonly string downloadPath;

        // Path to the directory where the new files will be created
        private readonly string outputPath;

        // Name of the new file to be created
        private readonly string newFileName;

        // Constructor to initialize the updater with relevant directories, log file location, runtime IDs, download path, output path, and new file name
        public InstallLinuxUpdater(string templateDirectory, string logFileLocation, List<string> runtimeIds, string downloadPath, string outputPath, string newFileName)
            : base(templateDirectory, logFileLocation)
        {
            this.runtimeIds = runtimeIds;
            this.downloadPath = downloadPath;
            this.outputPath = outputPath;
            this.newFileName = newFileName;
        }

        // Method to find the JSON file in the runtime ID folders
        private string? FindJsonFile(string runtimeId)
        {
            string jsonFileName = $"releases-json-CDN-{runtimeId}.json";
            string runtimeFolderPath = Path.Combine(downloadPath, $"release-manifests_{runtimeId}", "release-manifests");

            if (Directory.Exists(runtimeFolderPath))
            {
                string[] files = Directory.GetFiles(runtimeFolderPath, jsonFileName, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    return files[0]; // Return the first matching file
                }
            }

            return null; // Return null if no matching file is found
        }

        // Method to load configuration data from the JSON file
        private dynamic LoadConfigData(string jsonFilePath)
        {
            using (StreamReader r = new StreamReader(jsonFilePath))
            {
                string json = r.ReadToEnd();
                // Deserialize JSON data into dynamic object
                return JsonConvert.DeserializeObject<dynamic>(json);
            }
        }

        // Method to update files based on the loaded configuration data
        public override void UpdateFiles()
        {
            foreach (var runtimeId in runtimeIds)
            {
                string? jsonFilePath = FindJsonFile(runtimeId);

                if (jsonFilePath != null)
                {
                    var configData = LoadConfigData(jsonFilePath);

                    // Check if the releases section exists in the configuration data
                    if (configData.releases != null)
                    {
                        foreach (var release in configData.releases)
                        {
                            // Check if release and necessary properties are not null
                            if (release != null && release.runtime != null && release.runtime.version != null)
                            {
                                // Find the release that matches the current version
                                if (release.runtime.version == runtimeId)
                                {
                                    // Determine the appropriate template based on the channel version
                                    string? channelVersion = configData["channel-version"]?.ToString();
                                    string installLinuxTemplate;
                                    if (double.TryParse(channelVersion, out double versionNumber) && versionNumber <= 8.0)
                                    {
                                        installLinuxTemplate = Path.Combine(TemplateDirectory, "install-linux-template8.md");
                                    }
                                    else
                                    {
                                        installLinuxTemplate = Path.Combine(TemplateDirectory, "install-linux-template.md");
                                    }

                                    string newInstallLinuxFile = Path.Combine(outputPath, $"{newFileName}-{runtimeId.Replace(".", "")}.md");

                                    // Ensure the directory for the new file exists
                                    CreateDirectoryIfNotExists(outputPath);
                                    // Modify the template file with data from the configuration and write to the new file
                                    ModifyTemplateFile(installLinuxTemplate, newInstallLinuxFile, runtimeId, channelVersion, release, configData["latest-sdk"]?.ToString());
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
        private void ModifyTemplateFile(string templatePath, string outputPath, string version, string? channelVersion, dynamic release, string? latestSdk)
        {
            // Read the content of the template file
            string templateContent = File.ReadAllText(templatePath);

            // Logging the extracted channel version and latest SDK
            Console.WriteLine($"Extracted channel version: {channelVersion} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Extracted latest SDK: {latestSdk} for runtime ID: {version}"); // Debug log

            // Find the URL for "dotnet-sdk-linux-x64.tar.gz"
            string sdkUrl = "";
            foreach (var sdkFile in release.sdk.files)
            {
                if (sdkFile.name == "dotnet-sdk-linux-x64.tar.gz")
                {
                    sdkUrl = sdkFile.url;
                    break;
                }
            }
            Console.WriteLine($"Extracted SDK URL: {sdkUrl} for runtime ID: {version}"); // Debug log

            // Replace placeholders in the template with actual data
            string modifiedContent = templateContent
                .Replace("{ID-VERSION}", channelVersion ?? "")
                .Replace("{LINUX-SDK-URL}", sdkUrl)
                .Replace("{LATEST-SDK}", latestSdk ?? "");

            // Write the modified content to the output path
            File.WriteAllText(outputPath, modifiedContent);

            // Confirm file creation
            Console.WriteLine($"New install-Linux file created at: {outputPath}");
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