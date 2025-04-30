using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReleaseNotesUpdater
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Define the template directory and log file location
                string templateDirectory = "templates/";
                string logFileLocation = "logfile.log";
                string downloadPath = @"C:\Users\victorisr\OneDrive - Microsoft\Desktop\ReleaseNotesArtifacts"; // Path to the downloaded Pipeline artifacts to
                string outputDirectory = @"C:\Users\victorisr\OneDrive - Microsoft\Desktop"; // Directory where the modified file will be saved to
                string coreDirectory = @"C:\ReleaseNoteGeneratorCore"; // User-specified path to the Core Local Repo directory

                // Define Azure Pipeline details
                string organization = "dnceng";
                string project = "internal";
                string personalAccessToken = "insert_Your_Personal_Access_Token_Here"; // Replace with your PAT
                string artifactName = "release-manifests"; // Replace with your artifact name

                // Define a list of runtime IDs with their corresponding build IDs
                var runtimeBuildPairs = new List<(string runtimeId, int buildId)>
                {
                    ("9.0.4", 2664589), // Replace with your runtime IDs and build IDs
                    ("8.0.15", 2667669)
                };

                // Extract runtime IDs
                var runtimeIds = new List<string>();
                foreach (var pair in runtimeBuildPairs)
                {
                    runtimeIds.Add(pair.runtimeId);
                }

                foreach (var pair in runtimeBuildPairs)
                {
                    string runtimeId = pair.runtimeId;
                    int buildId = pair.buildId;

                    // Create an instance of AzurePipelineArtifactsDownloader and download artifacts
                /* 
                    var artifactsDownloader = new AzurePipelineArtifactsDownloader(organization, project, buildId, personalAccessToken, artifactName, downloadPath, runtimeId);
                    await artifactsDownloader.DownloadArtifactsAsync();
                */

                    // Create an instance of JsonFileHandler
                    var jsonFileHandler = new JsonFileHandler(downloadPath);

                    // Create instances of the updater classes
                    var readMeUpdater = new ReadMeUpdater(templateDirectory, logFileLocation, outputDirectory, coreDirectory, jsonFileHandler);
                    var releasesUpdater = new ReleasesUpdater(templateDirectory, logFileLocation, outputDirectory, coreDirectory, jsonFileHandler);
                    var rnReadMeUpdater = new RNReadMeUpdater(templateDirectory, logFileLocation, outputDirectory, coreDirectory, jsonFileHandler);
                    var installLinuxUpdater = new InstallLinuxUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, jsonFileHandler);
                    var installMacosUpdater = new InstallMacosUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, jsonFileHandler);
                    var installWindowsUpdater = new InstallWindowsUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, jsonFileHandler);
                    var runtimeFileUpdater = new RuntimeFileUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, jsonFileHandler);
                    var sdkFileUpdater = new SdkFileUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, jsonFileHandler);  
                
                /*
                    var cveFileUpdater = new CveFileUpdater(templateDirectory, logFileLocation, outputDirectory, coreDirectory);
                */

                    // Update the files
                    readMeUpdater.UpdateFiles();
                    releasesUpdater.UpdateFiles();
                    rnReadMeUpdater.UpdateFiles();
                    installLinuxUpdater.UpdateFiles();
                    installMacosUpdater.UpdateFiles();
                    installWindowsUpdater.UpdateFiles();
                    runtimeFileUpdater.UpdateFiles();
                    sdkFileUpdater.UpdateFiles();
                /*       
                    cveFileUpdater.UpdateFiles();
                */

                    Console.WriteLine($"Successfully updated all files for runtime ID {runtimeId}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}