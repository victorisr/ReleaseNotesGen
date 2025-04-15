using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        // Constructor to initialize the updater with relevant directories, log file location, runtime IDs, download path, output path, and new file name
        public RuntimeFileUpdater(string templateDirectory, string logFileLocation, List<string> runtimeIds, string downloadPath, string outputPath)
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
        private void ModifyTemplateFile(string templatePath, string outputPath, string version, dynamic configData, dynamic release)
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
        private string ReplaceSectionPlaceholders(string content, dynamic configData, dynamic release)
        {
            content = content.Replace("SECTION-ADDEDSDK", ReplaceAddedSdkSection(configData, release["sdks"] as JArray, configData["latest-sdk"]?.ToString()));
            content = content.Replace("SECTION-SDKS", ReplaceSdksSection(configData, release["sdks"] as JArray, configData["latest-sdk"]?.ToString(), configData["latest-runtime"]?.ToString()));
            content = content.Replace("SECTION-RUNTIME", ReplaceRuntimeSection(configData["latest-runtime"]?.ToString(), release["runtime"] as JObject));
            content = content.Replace("SECTION-WINDOWSDESKTOP", ReplaceWindowsDesktopSection(configData["latest-runtime"]?.ToString(), release["windowsdesktop"] as JObject));
            content = content.Replace("SECTION-ASP", ReplaceAspSection(configData["latest-runtime"]?.ToString(), release["aspnetcore-runtime"] as JObject));
            content = content.Replace("SECTION-LATESTSDK", ReplaceLatestSdkSection(configData["latest-sdk"]?.ToString(), release["sdk"] as JObject));
            content = content.Replace("SECTION-PACKAGES", ReplacePackagesSection(release["packages"] as JArray));
            return content;
        }

        // Method to replace SECTION-ADDEDSDK placeholder
        private string ReplaceAddedSdkSection(dynamic configData, JArray? sectionData, string latestSdk)
        {
            if (sectionData == null) return "";
            var markdownList = "\n";
            foreach (var item in sectionData)
            {
                var version = item["version"]?.ToString() ?? "";
                if (version != latestSdk)
                {
                    markdownList += $"* [{version}][{version}]\n";
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-SDKS placeholder
        private string ReplaceSdksSection(dynamic configData, JArray? sectionData, string latestSdk, string runtimeVersion)
        {
            if (sectionData == null) return "";
            var markdownList = "\n";
            foreach (var item in sectionData)
            {
                var version = item["version"]?.ToString() ?? "";
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
        private string ReplaceRuntimeSection(string latestRuntime, JObject? sectionData)
        {
            if (sectionData == null) return "";
            var markdownList = $"[//]: # ( Runtime {latestRuntime})\n";
            var files = sectionData["files"] as JArray;
            if (files != null)
            {
                foreach (var item in files)
                {
                    var name = item["name"]?.ToString() ?? "";
                    var url = item["url"]?.ToString() ?? "";
                    markdownList += $"[{name}]: {url}\n";
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-WINDOWSDESKTOP placeholder
        private string ReplaceWindowsDesktopSection(string latestRuntime, JObject? sectionData)
        {
            if (sectionData == null) return "";
            var markdownList = $"[//]: # ( WindowsDesktop {latestRuntime})\n";
            var files = sectionData["files"] as JArray;
            if (files != null)
            {
                foreach (var item in files)
                {
                    var name = item["name"]?.ToString() ?? "";
                    var url = item["url"]?.ToString() ?? "";
                    markdownList += $"[{name}]: {url}\n";
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-ASP placeholder
        private string ReplaceAspSection(string latestRuntime, JObject? sectionData)
        {
            if (sectionData == null) return "";
            var markdownList = $"[//]: # ( ASP {latestRuntime})\n";
            var files = sectionData["files"] as JArray;
            if (files != null)
            {
                foreach (var item in files)
                {
                    var name = item["name"]?.ToString() ?? "";
                    var url = item["url"]?.ToString() ?? "";
                    markdownList += $"[{name}]: {url}\n";
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-LATESTSDK placeholder
        private string ReplaceLatestSdkSection(string latestSdk, JObject? sectionData)
        {
            if (sectionData == null) return "";
            var markdownList = $"[//]: # ( SDK {latestSdk})\n";
            var files = sectionData["files"] as JArray;
            if (files != null)
            {
                foreach (var item in files)
                {
                    var name = item["name"]?.ToString() ?? "";
                    var url = item["url"]?.ToString() ?? "";
                    markdownList += $"[{name}]: {url}\n";
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-PACKAGES placeholder
        private string ReplacePackagesSection(JArray? sectionData)
        {
            if (sectionData == null) return "";
            var table = $"## Packages\n\n| Name | URL |\n| ---- | --- |\n";
            foreach (var item in sectionData)
            {
                table += $"| {item["name"]?.ToString() ?? ""} | {item["url"]?.ToString() ?? ""} |\n";
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