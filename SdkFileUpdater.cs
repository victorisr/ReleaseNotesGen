using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ReleaseNotesUpdater
{
    public class SdkFileUpdater : FileUpdater
    {
        // List to store runtime IDs
        private readonly List<string> runtimeIds;

        // Path to the directory where the downloaded artifacts are stored
        private readonly string downloadPath;

        // Path to the directory where the new files will be created
        private readonly string outputPath;

        // Constructor to initialize the updater with relevant directories, log file location, runtime IDs, download path, output path, and new file name
        public SdkFileUpdater(string templateDirectory, string logFileLocation, List<string> runtimeIds, string downloadPath, string outputPath)
            : base(templateDirectory, logFileLocation)
        {
            this.runtimeIds = runtimeIds;
            this.downloadPath = downloadPath;
            this.outputPath = outputPath;
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
                                    // For each version in the sdks section, create a new file if it's not the latest-sdk
                                    foreach (var sdk in release.sdks)
                                    {
                                        string sdkVersion = sdk.version.ToString();
                                        if (sdkVersion != configData["latest-sdk"].ToString())
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
        private void ModifyTemplateFile(string templatePath, string outputPath, string sdkVersion, dynamic configData, dynamic sdk)
        {
            // Read the content of the template file
            string templateContent = File.ReadAllText(templatePath);

            // Extract key values from the config data
            string runtimeVersion = configData["latest-runtime"]?.ToString();
            string latestSdk = configData["latest-sdk"]?.ToString();
            string channelVersion = configData["channel-version"]?.ToString();
            string latestReleaseDate = configData["latest-release-date"]?.ToString();

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
        private string ReplaceSectionPlaceholders(string content, dynamic configData, dynamic sdk)
        {
            content = content.Replace("SECTION-RUNTIME", ReplaceRuntimeSection(configData, sdk));
            content = content.Replace("SECTION-WINDOWSDESKTOP", ReplaceWindowsDesktopSection(configData, sdk));
            content = content.Replace("SECTION-ASP", ReplaceAspSection(configData, sdk));
            content = content.Replace("SECTION-VERSIONSDK", ReplaceVersionSdkSection(sdk["version"]?.ToString(), sdk["files"] as JArray));
            return content;
        }

        // Method to replace SECTION-RUNTIME placeholder
        private string ReplaceRuntimeSection(dynamic configData, dynamic sdk)
        {
            var runtimeSection = configData["releases"][0]["runtime"];
            if (runtimeSection == null)
            {
                Console.WriteLine("SECTION-RUNTIME: No data found.");
                return "";
            }
            var markdownList = $"[//]: # ( Runtime {configData["latest-runtime"]})\n";
            var files = runtimeSection["files"] as JArray;
            if (files != null)
            {
                foreach (var item in files)
                {
                    var name = item["name"]?.ToString() ?? "";
                    var url = item["url"]?.ToString() ?? "";
                    markdownList += $"[{name}]: {url}\n";
                }
            }
            else
            {
                Console.WriteLine("SECTION-RUNTIME: No files found.");
            }
            return markdownList;
        }

        // Method to replace SECTION-WINDOWSDESKTOP placeholder
        private string ReplaceWindowsDesktopSection(dynamic configData, dynamic sdk)
        {
            var windowsDesktopSection = configData["releases"][0]["windowsdesktop"];
            if (windowsDesktopSection == null)
            {
                Console.WriteLine("SECTION-WINDOWSDESKTOP: No data found.");
                return "";
            }
            var markdownList = $"[//]: # ( WindowsDesktop {configData["latest-runtime"]})\n";
            var files = windowsDesktopSection["files"] as JArray;
            if (files != null)
            {
                foreach (var item in files)
                {
                    var name = item["name"]?.ToString() ?? "";
                    var url = item["url"]?.ToString() ?? "";
                    markdownList += $"[{name}]: {url}\n";
                }
            }
            else
            {
                Console.WriteLine("SECTION-WINDOWSDESKTOP: No files found.");
            }
            return markdownList;
        }

        // Method to replace SECTION-ASP placeholder
        private string ReplaceAspSection(dynamic configData, dynamic sdk)
        {
            var aspSection = configData["releases"][0]["aspnetcore-runtime"];
            if (aspSection == null)
            {
                Console.WriteLine("SECTION-ASP: No data found.");
                return "";
            }
            var markdownList = $"[//]: # ( ASP {configData["latest-runtime"]})\n";
            var files = aspSection["files"] as JArray;
            if (files != null)
            {
                foreach (var item in files)
                {
                    var name = item["name"]?.ToString() ?? "";
                    var url = item["url"]?.ToString() ?? "";
                    markdownList += $"[{name}]: {url}\n";
                }
            }
            else
            {
                Console.WriteLine("SECTION-ASP: No files found.");
            }
            return markdownList;
        }

        // Method to replace SECTION-VERSIONSDK placeholder
        private string ReplaceVersionSdkSection(string sdkVersion, JArray? sectionData)
        {
            if (sectionData == null) return "";
            var markdownList = $"[//]: # ( SDK {sdkVersion})\n";
            if (sectionData != null)
            {
                foreach (var item in sectionData)
                {
                    var name = item["name"]?.ToString() ?? "";
                    var url = item["url"]?.ToString() ?? "";
                    markdownList += $"[{name}]: {url}\n";
                }
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