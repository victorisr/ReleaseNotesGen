using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ReleaseNotesUpdater.InstallersMarkdownUpdaters;
using ReleaseNotesUpdater.VersionsMarkdownUpdater;
using ReleaseNotesUpdater.ReleasesReadMeUpdaters;

namespace ReleaseNotesUpdater
{    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Load configuration from appsettings.json
                IConfiguration config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();                // Get path settings
                string? templateDirectory = config["Paths:TemplateDirectory"];
                string? logFileLocation = config["Paths:LogFileLocation"];
                string? downloadPath = config["Paths:DownloadPath"];
                string? outputDirectory = config["Paths:OutputDirectory"];
                string? coreDirectory = config["Paths:CoreDirectory"];
                string? configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                string configDirectory = Path.Combine(Directory.GetCurrentDirectory(), "configuration");

                // Get Azure Pipeline details
                string? organization = config["AzurePipeline:Organization"];
                string? project = config["AzurePipeline:Project"];
                string? personalAccessToken = config["AzurePipeline:PersonalAccessToken"];
                string? artifactName = config["AzurePipeline:ArtifactName"];

                // Get runtime and build IDs
                var runtimeBuildPairs = new List<(string runtimeId, int buildId)>();
                var runtimeSection = config.GetSection("RuntimeBuildPairs");
                foreach (var child in runtimeSection.GetChildren())
                {
                    string? runtimeId = child["RuntimeId"];
                    string? buildIdStr = child["BuildId"];
                    if (!string.IsNullOrEmpty(runtimeId) && int.TryParse(buildIdStr, out int buildId))
                    {
                        runtimeBuildPairs.Add((runtimeId, buildId));
                    }
                }
                // Extract runtime IDs
                var runtimeIds = new List<string>();
                foreach (var pair in runtimeBuildPairs)
                {
                    runtimeIds.Add(pair.runtimeId);
                }

                // Null checks for required config values
                if (templateDirectory == null || logFileLocation == null || downloadPath == null || outputDirectory == null || coreDirectory == null || organization == null || project == null || personalAccessToken == null || artifactName == null)
                {
                    throw new Exception("One or more required configuration values are missing in appsettings.json.");
                }                // Create an instance of JsonFileHandler
                var jsonFileHandler = new JsonFileHandler(downloadPath);
                
                // Load MSRC information from the config file
                var msrcConfigs = jsonFileHandler.LoadMsrcInformation(configFilePath);
                Console.WriteLine($"Loaded {msrcConfigs.Count} MSRC configurations from config file.");                // Process each runtime separately for downloading
                foreach (var pair in runtimeBuildPairs)
                {
                    string runtimeId = pair.runtimeId;
                    int buildId = pair.buildId;

                    try
                    {
                        // Create an instance of AzurePipelineArtifactsDownloader and download artifacts
                        var artifactsDownloader = new AzurePipelineArtifactsDownloader(organization, project, buildId, personalAccessToken, artifactName, downloadPath, runtimeId, logFileLocation);
                        await artifactsDownloader.DownloadArtifactsAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to download artifacts for runtime {runtimeId}: {ex.Message}");
                        // Continue processing other runtimes even if one fails
                        continue;
                    }                }

                // REMOVED: Core directory JSON file generation 
                // The application now works directly with downloaded artifacts and existing core directory files
                Console.WriteLine("Starting markdown file generation...");                // Create instances of the updater classes
                var readMeUpdater = new ReadMeUpdater(templateDirectory, logFileLocation, outputDirectory, coreDirectory, jsonFileHandler, configDirectory);
                var releasesUpdater = new ReleasesUpdater(templateDirectory, logFileLocation, outputDirectory, coreDirectory, jsonFileHandler, configDirectory, runtimeIds);
                var rnReadMeUpdater = new RNReadMeUpdater(templateDirectory, logFileLocation, outputDirectory, coreDirectory, jsonFileHandler, configDirectory);
                var installLinuxUpdater = new InstallLinuxUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, jsonFileHandler);
                var installMacosUpdater = new InstallMacosUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, jsonFileHandler);
                var installWindowsUpdater = new InstallWindowsUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, jsonFileHandler);
                var runtimeFileUpdater = new RuntimeFileUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, jsonFileHandler, msrcConfigs);
                var sdkFileUpdater = new SdkFileUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, jsonFileHandler);
                var versionReadMeUpdater = new VersionReadMeUpdater(templateDirectory, logFileLocation, coreDirectory, outputDirectory, runtimeIds, jsonFileHandler);
                var cveFileUpdater = new CveFileUpdater(templateDirectory, logFileLocation, coreDirectory, outputDirectory, runtimeIds, jsonFileHandler, msrcConfigs);

                // Update the files
                readMeUpdater.UpdateFiles();
                releasesUpdater.UpdateFiles();
                rnReadMeUpdater.UpdateFiles();
                installLinuxUpdater.UpdateFiles();
                installMacosUpdater.UpdateFiles();
                installWindowsUpdater.UpdateFiles();
                runtimeFileUpdater.UpdateFiles();
                sdkFileUpdater.UpdateFiles();
                versionReadMeUpdater.UpdateFiles();
                cveFileUpdater.UpdateFiles();

                Console.WriteLine("All markdown files updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}