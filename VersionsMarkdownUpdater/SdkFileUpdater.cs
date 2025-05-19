using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
        }        // Method to modify the template file with actual data and write to the output path
        private void ModifyTemplateFile(string templatePath, string outputPath, string sdkVersion, ReleasesConfiguration configData, Sdk sdk)
        {
            // Read the content of the template file
            string templateContent = File.ReadAllText(templatePath);            
            
            // Extract key values from the config data
            string runtimeVersion = configData.LatestRuntime;
            string latestSdk = configData.LatestSdk;
            string channelVersion = configData.ChannelVersion;
            string latestReleaseDate = configData.LatestReleaseDate;
            
            // Extract the VS versions
            string sdkVsVersion = GetSdkVisualStudioVersion(sdk);
            string minVsVersion = GetMinimumRuntimeVsVersion(configData);

            // Format the latest release date
            string formattedDate = DateTime.Parse(latestReleaseDate).ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);

            // Logging the extracted values
            Console.WriteLine($"Extracted runtime version: {runtimeVersion} for SDK version: {sdkVersion}"); // Debug log
            Console.WriteLine($"Extracted latest SDK: {latestSdk} for SDK version: {sdkVersion}"); // Debug log
            Console.WriteLine($"Extracted channel version: {channelVersion} for SDK version: {sdkVersion}"); // Debug log
            Console.WriteLine($"Extracted latest release date: {formattedDate} for SDK version: {sdkVersion}"); // Debug log
            Console.WriteLine($"Extracted SDK VS version: {sdkVsVersion} for SDK version: {sdkVersion}"); // Debug log
            Console.WriteLine($"Extracted minimum VS version: {minVsVersion} for SDK version: {sdkVersion}"); // Debug log
            
            // Replace placeholders in the template with actual data
            string modifiedContent = templateContent
                .Replace("{RUNTIME-VERSION}", runtimeVersion ?? "")
                .Replace("{LATEST-SDK}", latestSdk ?? "")
                .Replace("{ID-VERSION}", channelVersion ?? "")
                .Replace("{SDK-VERSION}", sdkVersion ?? "")
                .Replace("{HEADER-DATE}", formattedDate ?? "")
                .Replace("{SDKVS-VERSION}", sdkVsVersion ?? "")
                .Replace("{VS-VERSION}", minVsVersion ?? "");

            // Replace section placeholders with markdown-style tables
            modifiedContent = ReplaceSectionPlaceholders(modifiedContent, configData, sdk);

            // Write the modified content to the output path
            File.WriteAllText(outputPath, modifiedContent);

            // Confirm file creation
            Console.WriteLine($"New SDK file created at: {outputPath}");
        }        // Method to get all referenced link names from the content
        private HashSet<string> FindReferencedLinks(string content)
        {
            var referencedLinks = new HashSet<string>();
            
            // Match markdown link references like [linkName] or [linkText][linkName]
            // This regex looks for square brackets not preceded by an exclamation mark (to avoid images)
            var regex = new Regex(@"(?<!\!)\[([^\]]+)\](?:\[([^\]]+)\]|\(.*?\)|)?");
            var matches = regex.Matches(content);
            
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    // If it's a reference style link with text and reference [text][reference]
                    if (match.Groups.Count > 2 && !string.IsNullOrEmpty(match.Groups[2].Value))
                    {
                        referencedLinks.Add(match.Groups[2].Value);
                    }
                    // If it's a basic reference style link [reference]
                    else if (!match.Value.Contains("]("))
                    {
                        referencedLinks.Add(match.Groups[1].Value);
                    }
                }
            }
            
            return referencedLinks;
        }

        // Method to replace section placeholders with markdown-style tables
        private string ReplaceSectionPlaceholders(string content, ReleasesConfiguration configData, Sdk sdk)
        {
            // Find all referenced links in the content before adding link definitions
            var referencedLinks = FindReferencedLinks(content);
            
            // Replace sections with link definitions, filtering to only include used links
            content = content.Replace("SECTION-RUNTIME", ReplaceRuntimeSection(configData, sdk, referencedLinks));
            content = content.Replace("SECTION-WINDOWSDESKTOP", ReplaceWindowsDesktopSection(configData, sdk, referencedLinks));
            content = content.Replace("SECTION-ASP", ReplaceAspSection(configData, sdk, referencedLinks));
            content = content.Replace("SECTION-VERSIONSDK", ReplaceVersionSdkSection(sdk.Version, sdk.Files, referencedLinks));
            
            return content;
        }        // Method to replace SECTION-RUNTIME placeholder
        private string ReplaceRuntimeSection(ReleasesConfiguration configData, Sdk sdk, HashSet<string> referencedLinks)
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
                    // Only add if the link name is actually referenced in the content
                    if (referencedLinks.Contains(file.Name))
                    {
                        markdownList += $"[{file.Name}]: {file.Url}\n";
                    }
                }
            }
            else
            {
                Console.WriteLine("SECTION-RUNTIME: No files found.");
            }
            return markdownList;
        }

        // Method to replace SECTION-WINDOWSDESKTOP placeholder
        private string ReplaceWindowsDesktopSection(ReleasesConfiguration configData, Sdk sdk, HashSet<string> referencedLinks)
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
                    // Only add if the link name is actually referenced in the content
                    if (referencedLinks.Contains(file.Name))
                    {
                        markdownList += $"[{file.Name}]: {file.Url}\n";
                    }
                }
            }
            else
            {
                Console.WriteLine("SECTION-WINDOWSDESKTOP: No files found.");
            }
            return markdownList;
        }        // Method to replace SECTION-ASP placeholder
        private string ReplaceAspSection(ReleasesConfiguration configData, Sdk sdk, HashSet<string> referencedLinks)
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
                    // Only add if the link name is actually referenced in the content
                    if (referencedLinks.Contains(file.Name))
                    {
                        markdownList += $"[{file.Name}]: {file.Url}\n";
                    }
                }
            }
            else
            {
                Console.WriteLine("SECTION-ASP: No files found.");
            }
            return markdownList;
        }

        // Method to replace SECTION-VERSIONSDK placeholder
        private string ReplaceVersionSdkSection(string sdkVersion, List<Models.FileInfo> files, HashSet<string> referencedLinks)
        {
            if (files == null) return "";
            var markdownList = $"[//]: # ( SDK {sdkVersion})\n";
            foreach (var file in files)
            {
                // Only add if the link name is actually referenced in the content
                if (referencedLinks.Contains(file.Name))
                {
                    markdownList += $"[{file.Name}]: {file.Url}\n";
                }
            }
            return markdownList;
        }

        // Extract the Visual Studio version (major.minor) from the SDK
        private string GetSdkVisualStudioVersion(Sdk sdk)
        {
            if (sdk?.VsVersion == null)
            {
                return "17.0"; // Default fallback if no version is specified
            }
            
            string vsVersionFull = sdk.VsVersion;
            
            // Extract just the major.minor part (e.g., "17.8" from "17.8.21")
            int secondDotIndex = vsVersionFull.IndexOf('.', vsVersionFull.IndexOf('.') + 1);
            
            if (secondDotIndex > 0)
            {
                // There's at least a second dot, so trim after it
                return vsVersionFull.Substring(0, secondDotIndex);
            }
            else
            {
                // No second dot, check if there's at least one dot
                int firstDotIndex = vsVersionFull.IndexOf('.');
                if (firstDotIndex > 0)
                {
                    // Return as is since it's already in major.minor format
                    return vsVersionFull;
                }
                
                // Just return whatever is there if format is unexpected
                return vsVersionFull;
            }
        }

        // Method to get the minimum VS version from all runtime objects in the releases array
        private string GetMinimumRuntimeVsVersion(ReleasesConfiguration configData)
        {
            if (configData?.Releases == null || configData.Releases.Count == 0)
            {
                return "17.0"; // Default fallback if no releases data
            }

            string minVersion = "999.999"; // Start with a high version to ensure any real version is lower
            bool foundVersion = false;

            foreach (var release in configData.Releases)
            {
                if (release?.Runtime?.VsVersion != null && !string.IsNullOrWhiteSpace(release.Runtime.VsVersion))
                {
                    // The vs-version field might contain multiple versions separated by commas
                    string[] versions = release.Runtime.VsVersion.Split(',');
                    
                    foreach (string version in versions)
                    {
                        string trimmedVersion = version.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmedVersion))
                        {
                            // Extract just the major.minor part
                            string majorMinorVersion = ExtractMajorMinorVersion(trimmedVersion);
                            
                            // Compare versions to find the minimum
                            if (CompareVersions(majorMinorVersion, minVersion) < 0)
                            {
                                minVersion = majorMinorVersion;
                                foundVersion = true;
                            }
                        }
                    }
                }
            }

            // If we didn't find any versions, return the default
            return foundVersion ? minVersion : "17.0";
        }

        // Helper method to extract major.minor from a version string
        private string ExtractMajorMinorVersion(string fullVersion)
        {
            // Split by dot and take the first two segments (e.g., "17.8" from "17.8.21")
            string[] parts = fullVersion.Split('.');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}.{parts[1]}";
            }
            
            // If there's only one part or the format is unexpected, return as is
            return fullVersion;
        }        // Helper method to compare version strings
        private int CompareVersions(string version1, string version2)
        {
            // Try to parse both versions as Version objects
            if (string.IsNullOrEmpty(version1) || !Version.TryParse(version1, out Version? v1))
                v1 = new Version(0, 0);
            
            if (string.IsNullOrEmpty(version2) || !Version.TryParse(version2, out Version? v2))
                v2 = new Version(0, 0);
            
            return v1.CompareTo(v2);
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