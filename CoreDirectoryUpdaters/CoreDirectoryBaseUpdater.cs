using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReleaseNotesUpdater.Models;

namespace ReleaseNotesUpdater.CoreDirectoryUpdaters
{
    /// <summary>
    /// Base class for all core directory JSON file updaters
    /// </summary>
    public abstract class CoreDirectoryBaseUpdater
    {
        protected readonly string CoreDirectory;
        protected readonly string OutputDirectory;
        protected readonly string DownloadPath;
        protected readonly string LogFileLocation;
        protected readonly JsonFileHandler JsonFileHandler;
        protected readonly JsonSerializerOptions JsonOptions;

        protected CoreDirectoryBaseUpdater(
            string coreDirectory,
            string outputDirectory,
            string downloadPath, 
            string logFileLocation,
            JsonFileHandler jsonFileHandler)
        {
            CoreDirectory = coreDirectory;
            OutputDirectory = outputDirectory;
            DownloadPath = downloadPath;
            LogFileLocation = logFileLocation;
            JsonFileHandler = jsonFileHandler;
            JsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = null // Keep original property names
            };
        }

        /// <summary>
        /// Updates the JSON file
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Creates directory if it doesn't exist
        /// </summary>
        protected void CreateDirectoryIfNotExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                LogMessage($"Created directory: {directoryPath}");
            }
        }

        /// <summary>
        /// Extracts the channel version from a runtime ID (e.g., "8.0.15" returns "8.0")
        /// </summary>
        protected string ExtractChannelVersion(string runtimeId)
        {
            // Split by dot and take the first two segments (e.g., "8.0" from "8.0.15")
            string[] parts = runtimeId.Split('.');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}.{parts[1]}";
            }
            
            LogError($"Unable to extract channel version from runtime ID: {runtimeId}");
            return runtimeId; // Return original if extraction fails
        }

        /// <summary>
        /// Logs a message to the log file
        /// </summary>
        protected void LogMessage(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(LogFileLocation, true))
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
        protected void LogError(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(LogFileLocation, true))
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
        
        /// <summary>
        /// Writes JSON data to a file
        /// </summary>
        protected void WriteJsonFile<T>(string filePath, T data)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(filePath, jsonContent);
                LogMessage($"Created JSON file: {filePath}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to write JSON file {filePath}: {ex.Message}");
                throw;
            }
        }
    }
}