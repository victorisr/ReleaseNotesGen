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
                string downloadPath = @"C:\Users\victorisr\OneDrive - Microsoft\Desktop\ReleaseNotesArtifacts"; // Path to the downloaded artifacts
                string outputDirectory = @"C:\Users\victorisr\OneDrive - Microsoft\Desktop"; // Directory where the modified file will be saved by CveFileUpdater
                string linuxFileName = "install-linux"; // Name of the new file to be created for Linux
                string macosFileName = "install-macos"; // Name of the new file to be created for macOS
                string windowsFileName = "install-windows"; // Name of the new file to be created for Windows

                // Define Azure Pipeline details
                string organization = "dnceng";
                string project = "internal";
                string personalAccessToken = "Insert_Your_Personal_Azure_Token_Here"; // Replace with your PAT
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
                    var artifactsDownloader = new AzurePipelineArtifactsDownloader(organization, project, buildId, personalAccessToken, artifactName, downloadPath, runtimeId);
                    await artifactsDownloader.DownloadArtifactsAsync();

                    // Create instances of the updater classes
                    /* 
                    var readMeUpdater = new ReadMeUpdater(templateDirectory, logFileLocation);
                    var releasesUpdater = new ReleasesUpdater(templateDirectory, logFileLocation);
                    var rnReadMeUpdater = new RNReadMeUpdater(templateDirectory, logFileLocation);
                    var cveFileUpdater = new CveFileUpdater(templateDirectory, logFileLocation);
                    var versionReadMeUpdater  = new VersionReadMeUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, linuxFileName);
                     */
                   
                    var installLinuxUpdater = new InstallLinuxUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, linuxFileName);
                    var installMacosUpdater = new InstallMacosUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, macosFileName);
                    var installWindowsUpdater = new InstallWindowsUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory, windowsFileName);
                    var runtimeFileUpdater = new RuntimeFileUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory);
                    var sdkFileUpdater = new SdkFileUpdater(templateDirectory, logFileLocation, runtimeIds, downloadPath, outputDirectory);

                    // Update the files
                    /* 
                    readMeUpdater.UpdateFiles();
                    releasesUpdater.UpdateFiles();
                    rnReadMeUpdater.UpdateFiles();
                    cveFileUpdater.UpdateFiles();
                    versionReadMeUpdater.UpdateFiles();
                    */
                    installLinuxUpdater.UpdateFiles();
                    installMacosUpdater.UpdateFiles();
                    installWindowsUpdater.UpdateFiles();
                    runtimeFileUpdater.UpdateFiles();
                    sdkFileUpdater.UpdateFiles();

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