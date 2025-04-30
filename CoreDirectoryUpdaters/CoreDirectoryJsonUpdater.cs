using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater.CoreDirectoryUpdaters
{
    /// <summary>
    /// Coordinator class that handles updating JSON files in the _coreDirectory.
    /// This class should be executed after AzurePipelineArtifactsDownloader 
    /// and before any updater classes.
    /// </summary>
    public class CoreDirectoryJsonUpdater
    {
        private readonly string _coreDirectory;
        private readonly string _outputDirectory;
        private readonly List<string> _runtimeIds;
        private readonly string _downloadPath;
        private readonly string _logFileLocation;
        private readonly JsonFileHandler _jsonFileHandler;
        private readonly JsonSerializerOptions _jsonOptions;

        public CoreDirectoryJsonUpdater(
            string coreDirectory,
            string outputDirectory,
            List<string> runtimeIds, 
            string downloadPath, 
            string logFileLocation,
            JsonFileHandler jsonFileHandler)
        {
            _coreDirectory = coreDirectory;
            _outputDirectory = outputDirectory;
            _runtimeIds = runtimeIds;
            _downloadPath = downloadPath;
            _logFileLocation = logFileLocation;
            _jsonFileHandler = jsonFileHandler;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Coordinates the update of all required JSON files in the core directory
        /// </summary>
        public void UpdateAllCoreDirectoryJsonFiles()
        {
            LogMessage("Starting Core Directory JSON file updates...");
            
            try
            {
                // Ensure the output directory exists
                CreateDirectoryIfNotExists(_outputDirectory);
                
                // Update release index file first
                UpdateReleaseIndexJson();
                
                // Then update channel version specific files for each runtime
                foreach (var runtimeId in _runtimeIds)
                {
                    UpdateChannelVersionReleaseJson(runtimeId);
                    UpdateRuntimeVersionReleaseJson(runtimeId);
                }
                
                LogMessage("Core Directory JSON file updates completed successfully.");
            }
            catch (Exception ex)
            {
                LogError($"Error updating Core Directory JSON files: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates the releases-index.json file in the output directory
        /// </summary>
        private void UpdateReleaseIndexJson()
        {
            // Implement this in a separate class
            var indexUpdater = new ReleasesIndexJsonUpdater(
                _coreDirectory,
                _outputDirectory,
                _downloadPath, 
                _logFileLocation, 
                _jsonFileHandler);
                
            indexUpdater.Update();
        }

        /// <summary>
        /// Updates the channel version (e.g. 8.0, 9.0) specific releases.json file
        /// </summary>
        private void UpdateChannelVersionReleaseJson(string runtimeId)
        {
            // Implement this in a separate class
            var channelUpdater = new ChannelVersionJsonUpdater(
                _coreDirectory,
                _outputDirectory,
                runtimeId, 
                _downloadPath, 
                _logFileLocation, 
                _jsonFileHandler);
                
            channelUpdater.Update();
        }

        /// <summary>
        /// Updates runtime version specific (e.g. 8.0.15, 9.0.4) release.json file
        /// </summary>
        private void UpdateRuntimeVersionReleaseJson(string runtimeId)
        {
            // Implement this in a separate class
            var runtimeUpdater = new RuntimeVersionJsonUpdater(
                _coreDirectory,
                _outputDirectory,
                runtimeId, 
                _downloadPath, 
                _logFileLocation, 
                _jsonFileHandler);
                
            runtimeUpdater.Update();
        }

        /// <summary>
        /// Creates directory if it doesn't exist
        /// </summary>
        private void CreateDirectoryIfNotExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                LogMessage($"Created directory: {directoryPath}");
            }
        }

        /// <summary>
        /// Logs a message to the log file
        /// </summary>
        private void LogMessage(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_logFileLocation, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
                Console.WriteLine($"[INFO] {message}");
            }
            catch
            {
                Console.WriteLine($"[WARNING] Failed to write to log file. Message: {message}");
            }
        }

        /// <summary>
        /// Logs an error to the log file
        /// </summary>
        private void LogError(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_logFileLocation, true))
                {
                    writer.WriteLine($"{DateTime.Now}: ERROR - {message}");
                }
                Console.WriteLine($"[ERROR] {message}");
            }
            catch
            {
                Console.WriteLine($"[ERROR] Failed to write error to log file. Error: {message}");
            }
        }
    }
}