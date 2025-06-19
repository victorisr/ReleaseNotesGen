using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Linq;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater.VersionsMarkdownUpdater
{    public class RuntimeFileUpdater : FileUpdater
    {
        // List to store runtime IDs
        private readonly List<string> runtimeIds;

        // Path to the directory where the downloaded artifacts are stored
        private readonly string downloadPath;

        // Path to the directory where the new files will be created
        private readonly string outputPath;

        // Instance of JsonFileHandler for JSON file handling
        private readonly JsonFileHandler _jsonFileHandler;
        
        // MSRC security information
        private readonly List<MsrcConfig> _msrcConfigs;

        // Constructor to initialize the updater with relevant directories, log file location, runtime IDs, download path, output path, and new file name
        public RuntimeFileUpdater(string templateDirectory, string logFileLocation, List<string> runtimeIds, string downloadPath, string outputPath, JsonFileHandler jsonFileHandler, List<MsrcConfig>? msrcConfigs = null)
            : base(templateDirectory, logFileLocation)
        {
            this.runtimeIds = runtimeIds;
            this.downloadPath = downloadPath;
            this.outputPath = outputPath;
            _jsonFileHandler = jsonFileHandler;
            _msrcConfigs = msrcConfigs ?? new List<MsrcConfig>();
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
                                    string runtimeDir = Path.Combine(outputPath, runtimeId);
                                    CreateDirectoryIfNotExists(runtimeDir);
                                    string newRuntimeFile = Path.Combine(runtimeDir, $"{runtimeId}.md");
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
            string templateContent = File.ReadAllText(templatePath);            // Extract key values from the config data
            string runtimeVersion = configData.LatestRuntime;
            string latestSdk = configData.LatestSdk;
            string channelVersion = configData.ChannelVersion;
            string latestReleaseDate = configData.LatestReleaseDate;            // Format the latest release date
            string formattedDate = DateTime.Parse(latestReleaseDate).ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);
            string blogPostDate = DateTime.Parse(latestReleaseDate).ToString("MMMM-yyyy", CultureInfo.InvariantCulture).ToLower();
            string blogDate = DateTime.Parse(latestReleaseDate).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
              // Extract the minimum VS version required from the release
            string vsVersion = GetMinimumVisualStudioVersion(release);
            
            // Extract the C# version
            string csharpVersion = GetCSharpVersion(release, latestSdk);

            // Logging the extracted values
            Console.WriteLine($"Extracted runtime version: {runtimeVersion} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Extracted latest SDK: {latestSdk} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Extracted channel version: {channelVersion} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Extracted latest release date: {formattedDate} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Formatted blog post date: {blogPostDate} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Formatted blog date: {blogDate} for runtime ID: {version}"); // Debug log            
            Console.WriteLine($"Extracted VS version: {vsVersion} for runtime ID: {version}"); // Debug log
            Console.WriteLine($"Extracted C# version: {csharpVersion} for runtime ID: {version}"); // Debug log
              // Replace placeholders in the template with actual data
            string modifiedContent = templateContent
                .Replace("{RUNTIME-VERSION}", runtimeVersion ?? "")
                .Replace("{LATEST-SDK}", latestSdk ?? "")
                .Replace("{ID-VERSION}", channelVersion ?? "")
                .Replace("{HEADER-DATE}", formattedDate ?? "")
                .Replace("{BLOGPOST-DATE}", blogPostDate ?? "")
                .Replace("{BLOG-DATE}", blogDate ?? "")
                .Replace("{VS-VERSION}", vsVersion)
                .Replace("{CSHARPSDK-VERSION}", csharpVersion);

            // Replace section placeholders with markdown-style tables
            modifiedContent = ReplaceSectionPlaceholders(modifiedContent, configData, release);

            // Write the modified content to the output path
            File.WriteAllText(outputPath, modifiedContent);

            // Confirm file creation
            Console.WriteLine($"New runtime file created at: {outputPath}");
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
        private string ReplaceSectionPlaceholders(string content, ReleasesConfiguration configData, Release release)
        {
            // First replace the sections that might add link references to the content
            content = content.Replace("SECTION-ADDEDSDK", ReplaceAddedSdkSection(configData, release.Sdks, configData.LatestSdk));
            content = content.Replace("SECTION-PACKAGES", ReplacePackagesSection(release.Packages));
            content = content.Replace("SECTION-MSRC", ReplaceMsrcSecuritySection(release));
            
            // Find all referenced links in the content after initial replacements
            var referencedLinks = FindReferencedLinks(content);
            
            // Now replace the sections that provide link definitions, filtering to only include used links
            content = content.Replace("SECTION-SDKS", ReplaceSdksSection(configData, release.Sdks, configData.LatestSdk, configData.LatestRuntime, referencedLinks));
            content = content.Replace("SECTION-RUNTIME", ReplaceRuntimeSection(configData.LatestRuntime, release.Runtime, referencedLinks));
            content = content.Replace("SECTION-WINDOWSDESKTOP", ReplaceWindowsDesktopSection(configData.LatestRuntime, release.WindowsDesktop, referencedLinks));
            content = content.Replace("SECTION-ASP", ReplaceAspSection(configData.LatestRuntime, release.AspNetCoreRuntime, referencedLinks));
            content = content.Replace("SECTION-LATESTSDK", ReplaceLatestSdkSection(configData.LatestSdk, release.Sdk, referencedLinks));
            
            return content;
        }// Method to replace SECTION-ADDEDSDK placeholder
        private string ReplaceAddedSdkSection(ReleasesConfiguration configData, List<Sdk> sdks, string latestSdk)
        {
            if (sdks == null) return "";
            
            // Start with a newline
            var markdownList = "\n";
            
            // Add the latest SDK as the first item
            markdownList += $"* [{latestSdk}][{latestSdk}]";
            
            // Add all other SDKs
            foreach (var sdk in sdks)
            {
                var version = sdk.Version;
                if (version != latestSdk)
                {
                    markdownList += $"\n* [{version}][{version}]";
                }
            }
            
            return markdownList;
        }        // Method to replace SECTION-SDKS placeholder
        private string ReplaceSdksSection(ReleasesConfiguration configData, List<Sdk> sdks, string latestSdk, string runtimeVersion, HashSet<string> referencedLinks)
        {
            if (sdks == null) return "";
            var markdownList = "\n";
            foreach (var sdk in sdks)
            {
                var version = sdk.Version;
                if (version == latestSdk)
                {
                    // Only add if referenced
                    if (referencedLinks.Contains(latestSdk))
                    {
                        markdownList += $"[{latestSdk}]: {runtimeVersion}.md";
                    }
                }
                else
                {
                    // Only add if referenced
                    if (referencedLinks.Contains(version))
                    {
                        markdownList += $"\n[{version}]: {version}.md";
                    }
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-RUNTIME placeholder
        private string ReplaceRuntimeSection(string latestRuntime, Runtime runtime, HashSet<string> referencedLinks)
        {
            if (runtime == null) return "";
            var markdownList = $"[//]: # ( Runtime {latestRuntime})";
            var files = runtime.Files;
            if (files != null)
            {
                foreach (var file in files)
                {
                    // Only add if the link name is actually referenced in the content
                    if (referencedLinks.Contains(file.Name))
                    {
                        markdownList += $"\n[{file.Name}]: {file.Url}";
                    }
                }
            }
            return markdownList;
        }        // Method to replace SECTION-WINDOWSDESKTOP placeholder
        private string ReplaceWindowsDesktopSection(string latestRuntime, WindowsDesktop windowsDesktop, HashSet<string> referencedLinks)
        {
            if (windowsDesktop == null) return "";
            var markdownList = $"[//]: # ( WindowsDesktop {latestRuntime})";
            var files = windowsDesktop.Files;
            if (files != null)
            {
                foreach (var file in files)
                {
                    // Only add if the link name is actually referenced in the content
                    if (referencedLinks.Contains(file.Name))
                    {
                        markdownList += $"\n[{file.Name}]: {file.Url}";
                    }
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-ASP placeholder
        private string ReplaceAspSection(string latestRuntime, AspNetCoreRuntime aspNetCoreRuntime, HashSet<string> referencedLinks)
        {
            if (aspNetCoreRuntime == null) return "";
            var markdownList = $"[//]: # ( ASP {latestRuntime})";
            var files = aspNetCoreRuntime.Files;
            if (files != null)
            {
                foreach (var file in files)
                {
                    // Only add if the link name is actually referenced in the content
                    if (referencedLinks.Contains(file.Name))
                    {
                        markdownList += $"\n[{file.Name}]: {file.Url}";
                    }
                }
            }
            return markdownList;
        }

        // Method to replace SECTION-LATESTSDK placeholder
        private string ReplaceLatestSdkSection(string latestSdk, Sdk sdk, HashSet<string> referencedLinks)
        {
            if (sdk == null) return "";
            var markdownList = $"[//]: # ( SDK {latestSdk})";
            var files = sdk.Files;
            if (files != null)
            {
                foreach (var file in files)
                {
                    // Only add if the link name is actually referenced in the content
                    if (referencedLinks.Contains(file.Name))
                    {
                        markdownList += $"\n[{file.Name}]: {file.Url}";
                    }
                }
            }
            return markdownList;
        }// Method to replace SECTION-PACKAGES placeholder
        private string ReplacePackagesSection(List<Package> packages)
        {
            if (packages == null) return "";
            var table = $"## Packages\n| Name | Version |\n| ---- | ------- |\n";
            foreach (var package in packages)
            {
                table += $"| {package.Name} | {package.Version} |";
            }
            return table;
        }        // Method to replace SECTION-MSRC placeholder with security information
        private string ReplaceMsrcSecuritySection(Release release)
        {
            // If the release has the Security flag set to true, display security notice
            if (release.Security)
            {
                string securityText = "";
                
                // Check if we have MSRC information for this runtime
                var runtimeId = release.Runtime?.Version;
                var msrcConfig = runtimeId != null ? _msrcConfigs.FirstOrDefault(m => m.RuntimeId == runtimeId) : null;
                
                // Use MSRC CVE information if available in the requested format
                if (msrcConfig != null && msrcConfig.Cves != null && msrcConfig.Cves.Count > 0)
                {
                    // First part - add the introduction text only once
                    securityText = "This release includes security and non-sercurity fixes. Details on security fixes below can be found in the [Microsoft Security Advisory](https://github.com/dotnet/announcements/issues?q=is%3Aissue%20state%3Aopen%20%20Microsoft%20Security%20Advisory):\n\n";
                    
                    // Second part - add each CVE entry
                    foreach (var cve in msrcConfig.Cves)
                    {
                        securityText += $"### Microsoft Security Advisory {cve.CveId} | {cve.CveTitle}";
                        securityText += $"\n\n{cve.CveDescription}";
                    }
                }
                // Fall back to generic security notice if MSRC information is not available
                else if (release.CveList != null && release.CveList.Count > 0)
                {
                    securityText = "### Security\n\n";
                    securityText += "This release includes security fixes. Details on security fixes below can be found in the [Microsoft Security Advisory](https://github.com/dotnet/announcements/issues?q=is%3Aissue%20state%3Aopen%20%20Microsoft%20Security%20Advisory):\n\n";
                    
                    foreach (var cve in release.CveList)
                    {
                        if (!string.IsNullOrEmpty(cve.CveUrl))
                        {
                            securityText += $"* [{cve.CveUrl}]({cve.CveUrl})\n";
                        }
                    }
                }
                else
                {
                    securityText = "### Security\n\n";
                    securityText += "This release includes security fixes. Details can be found in the [Microsoft Security Advisory](https://github.com/dotnet/announcements/issues?q=is%3Aissue%20state%3Aopen%20%20Microsoft%20Security%20Advisory).";
                }
                
                return securityText;
            }
            
            // If no security issues, return an empty string
            return "";
        }// Helper method to create directory if it does not exist
        private new void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        // Helper method to get minimum VS version from the release data
        private string GetMinimumVisualStudioVersion(Release release)
        {
            if (release?.Runtime?.VsVersion == null)
            {
                return "17.0"; // Default fallback if no version is specified
            }
            
            string vsVersionFull = release.Runtime.VsVersion;
            
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
          // Method to get the C# version from the SDK in the release (major version only)
        private string GetCSharpVersion(Release release, string? latestSdk = null)
        {
            // Default value if we can't find a C# version
            string defaultVersion = "12";
            
            // First check if we have a specific SDK to look for
            if (!string.IsNullOrEmpty(latestSdk) && release?.Sdks != null)
            {
                // Try to find the latest SDK in the SDKs list
                var latestSdkObj = release.Sdks.FirstOrDefault(s => s.Version == latestSdk);
                if (latestSdkObj != null && !string.IsNullOrWhiteSpace(latestSdkObj.CsharpVersion))
                {
                    return ExtractMajorVersion(latestSdkObj.CsharpVersion);
                }
            }
            
            // If not found or no latestSdk specified, try the main SDK
            if (release?.Sdk != null && !string.IsNullOrWhiteSpace(release.Sdk.CsharpVersion))
            {
                return ExtractMajorVersion(release.Sdk.CsharpVersion);
            }
            
            // If still not found, try to find any SDK with a C# version
            if (release?.Sdks != null)
            {
                foreach (var sdk in release.Sdks)
                {
                    if (!string.IsNullOrWhiteSpace(sdk.CsharpVersion))
                    {
                        return ExtractMajorVersion(sdk.CsharpVersion);
                    }
                }
            }
            
            // If no C# version found, return the default
            return defaultVersion;
        }
        
        // Helper method to extract major version from a version string
        private string ExtractMajorVersion(string fullVersion)
        {
            if (string.IsNullOrEmpty(fullVersion))
                return "12"; // Default fallback
                
            // Extract just the major part (e.g., "12" from "12.0")
            int dotIndex = fullVersion.IndexOf('.');
            
            if (dotIndex > 0)
            {
                // There's a dot, so take just the first part
                return fullVersion.Substring(0, dotIndex);
            }
            
            // No dot, return as is
            return fullVersion;
        }
    }
}