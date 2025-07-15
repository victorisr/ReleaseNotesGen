using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ReleaseNotesUpdater
{
    public class CoreDirectorySync : FileUpdater
    {
        private readonly string _outputDirectory;
        private readonly string _coreDirectory;
        private readonly List<string> _runtimeIds;
        private readonly string _backupDirectory;

        public CoreDirectorySync(
            string templateDirectory,
            string logFileLocation,
            string outputDirectory,
            string coreDirectory,
            List<string> runtimeIds)
            : base(templateDirectory, logFileLocation)
        {
            _outputDirectory = outputDirectory;
            _coreDirectory = coreDirectory;
            _runtimeIds = runtimeIds;
            _backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "backups", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        }

        public override void UpdateFiles()
        {
            Console.WriteLine("Starting CoreDirectorySync process...");
            
            try
            {
                // Ensure core directory exists
                string coreDestination = Path.Combine(_coreDirectory, "core");
                CreateDirectoryIfNotExists(coreDestination);

                // Create backup directory
                CreateDirectoryIfNotExists(_backupDirectory);
                LogChanges($"Created backup directory: {_backupDirectory}");

                // Sync root level files
                SyncRootLevelFiles(coreDestination);

                // Sync release-notes directory
                SyncReleaseNotesDirectory(coreDestination);

                LogChanges("CoreDirectorySync completed successfully");
                Console.WriteLine("CoreDirectorySync completed successfully");
                Console.WriteLine($"Backup files saved to: {_backupDirectory}");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error during CoreDirectorySync: {ex.Message}";
                LogChanges(errorMessage);
                Console.WriteLine(errorMessage);
                throw;
            }
        }

        private void SyncRootLevelFiles(string coreDestination)
        {
            // Files to sync at root level
            string[] rootFiles = { "README.md", "releases.md" };

            foreach (string fileName in rootFiles)
            {
                string sourceFile = Path.Combine(_outputDirectory, fileName);
                string destFile = Path.Combine(coreDestination, fileName);

                if (File.Exists(sourceFile))
                {
                    CopyFileWithBackup(sourceFile, destFile, "root");
                }
                else
                {
                    Console.WriteLine($"Warning: Source file not found: {sourceFile}");
                }
            }
        }

        private void SyncReleaseNotesDirectory(string coreDestination)
        {
            string sourceReleaseNotesDir = Path.Combine(_outputDirectory, "release-notes");
            string destReleaseNotesDir = Path.Combine(coreDestination, "release-notes");

            if (!Directory.Exists(sourceReleaseNotesDir))
            {
                Console.WriteLine($"Warning: Source release-notes directory not found: {sourceReleaseNotesDir}");
                return;
            }

            CreateDirectoryIfNotExists(destReleaseNotesDir);

            // Sync release-notes/README.md
            string sourceReadme = Path.Combine(sourceReleaseNotesDir, "README.md");
            string destReadme = Path.Combine(destReleaseNotesDir, "README.md");

            if (File.Exists(sourceReadme))
            {
                CopyFileWithBackup(sourceReadme, destReadme, "release-notes");
            }

            // Sync version-specific directories
            SyncVersionDirectories(sourceReleaseNotesDir, destReleaseNotesDir);
        }

        private void SyncVersionDirectories(string sourceReleaseNotesDir, string destReleaseNotesDir)
        {
            // Get all version directories from runtime IDs (e.g., "9.0.7" -> "9.0", "8.0.18" -> "8.0")
            var versionDirectories = _runtimeIds
                .Select(ExtractChannelVersion)
                .Distinct()
                .ToList();

            foreach (string versionDir in versionDirectories)
            {
                string sourceVersionDir = Path.Combine(sourceReleaseNotesDir, versionDir);
                string destVersionDir = Path.Combine(destReleaseNotesDir, versionDir);

                if (Directory.Exists(sourceVersionDir))
                {
                    SyncVersionDirectory(sourceVersionDir, destVersionDir, versionDir);
                }
            }
        }

        private void SyncVersionDirectory(string sourceVersionDir, string destVersionDir, string versionDir)
        {
            CreateDirectoryIfNotExists(destVersionDir);

            // Files to sync directly in version directory
            string[] versionFiles = { "README.md", "cve.md", "install-linux.md", "install-macos.md", "install-windows.md" };

            foreach (string fileName in versionFiles)
            {
                string sourceFile = Path.Combine(sourceVersionDir, fileName);
                string destFile = Path.Combine(destVersionDir, fileName);

                if (File.Exists(sourceFile))
                {
                    CopyFileWithBackup(sourceFile, destFile, $"release-notes/{versionDir}");
                }
            }

            // Handle runtime-specific directories (e.g., 9.0.7, 8.0.18)
            SyncRuntimeDirectories(sourceVersionDir, destVersionDir, versionDir);
        }

        private void SyncRuntimeDirectories(string sourceVersionDir, string destVersionDir, string versionDir)
        {
            // Get runtime IDs that match this version
            var matchingRuntimeIds = _runtimeIds
                .Where(id => ExtractChannelVersion(id) == versionDir)
                .ToList();

            foreach (string runtimeId in matchingRuntimeIds)
            {
                string sourceRuntimeDir = Path.Combine(sourceVersionDir, runtimeId);
                string destRuntimeDir = Path.Combine(destVersionDir, runtimeId);

                if (Directory.Exists(sourceRuntimeDir))
                {
                    SyncRuntimeDirectory(sourceRuntimeDir, destRuntimeDir, runtimeId, versionDir);
                }
            }
        }

        private void SyncRuntimeDirectory(string sourceRuntimeDir, string destRuntimeDir, string runtimeId, string versionDir)
        {
            bool destExists = Directory.Exists(destRuntimeDir);
            CreateDirectoryIfNotExists(destRuntimeDir);

            if (destExists)
            {
                // SDK-only release scenario: only add new files that don't exist
                Console.WriteLine($"Destination directory exists for {runtimeId}, performing selective sync (SDK-only release)");
                SyncRuntimeDirectorySelective(sourceRuntimeDir, destRuntimeDir, runtimeId, versionDir);
            }
            else
            {
                // Full sync: copy all files
                Console.WriteLine($"Full sync for new runtime directory: {runtimeId}");
                SyncRuntimeDirectoryFull(sourceRuntimeDir, destRuntimeDir, runtimeId, versionDir);
            }
        }

        private void SyncRuntimeDirectoryFull(string sourceRuntimeDir, string destRuntimeDir, string runtimeId, string versionDir)
        {
            // Copy all files and subdirectories
            CopyDirectoryRecursively(sourceRuntimeDir, destRuntimeDir, $"release-notes/{versionDir}/{runtimeId}");
        }

        private void SyncRuntimeDirectorySelective(string sourceRuntimeDir, string destRuntimeDir, string runtimeId, string versionDir)
        {
            // Only copy files that don't already exist in destination
            string[] files = Directory.GetFiles(sourceRuntimeDir, "*", SearchOption.AllDirectories);

            foreach (string sourceFile in files)
            {
                string relativePath = Path.GetRelativePath(sourceRuntimeDir, sourceFile);
                string destFile = Path.Combine(destRuntimeDir, relativePath);

                if (!File.Exists(destFile))
                {
                    CreateDirectoryIfNotExists(Path.GetDirectoryName(destFile));
                    CopyFileWithLogging(sourceFile, destFile, $"release-notes/{versionDir}/{runtimeId}");
                    Console.WriteLine($"Added new SDK file: {relativePath}");
                }
                else
                {
                    Console.WriteLine($"Skipped existing file: {relativePath}");
                }
            }
        }

        private void CopyDirectoryRecursively(string sourceDir, string destDir, string backupContext)
        {
            // Copy all files
            string[] files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

            foreach (string sourceFile in files)
            {
                string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                string destFile = Path.Combine(destDir, relativePath);

                CreateDirectoryIfNotExists(Path.GetDirectoryName(destFile));
                CopyFileWithBackup(sourceFile, destFile, backupContext);
            }
        }

        private void CopyFileWithBackup(string sourceFile, string destFile, string backupContext)
        {
            try
            {
                // Create backup if destination file exists
                if (File.Exists(destFile))
                {
                    CreateBackup(destFile, backupContext);
                }

                // Copy the new file
                CreateDirectoryIfNotExists(Path.GetDirectoryName(destFile));
                File.Copy(sourceFile, destFile, overwrite: true);
                
                string logMessage = $"Synced file: {sourceFile} -> {destFile}";
                LogChanges(logMessage);
                Console.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error copying file {sourceFile} to {destFile}: {ex.Message}";
                LogChanges(errorMessage);
                Console.WriteLine(errorMessage);
                throw;
            }
        }

        private void CopyFileWithLogging(string sourceFile, string destFile, string backupContext)
        {
            try
            {
                CreateDirectoryIfNotExists(Path.GetDirectoryName(destFile));
                File.Copy(sourceFile, destFile, overwrite: true);
                
                string logMessage = $"Added new file: {sourceFile} -> {destFile}";
                LogChanges(logMessage);
                Console.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error copying file {sourceFile} to {destFile}: {ex.Message}";
                LogChanges(errorMessage);
                Console.WriteLine(errorMessage);
                throw;
            }
        }

        private void CreateBackup(string originalFile, string backupContext)
        {
            try
            {
                // Create backup directory structure
                string backupContextDir = Path.Combine(_backupDirectory, backupContext);
                CreateDirectoryIfNotExists(backupContextDir);

                // Create backup file path
                string fileName = Path.GetFileName(originalFile);
                string backupFile = Path.Combine(backupContextDir, fileName);

                // Handle duplicate backup files by adding timestamp
                if (File.Exists(backupFile))
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    string timestamp = DateTime.Now.ToString("HHmmss");
                    fileName = $"{fileNameWithoutExt}_{timestamp}{extension}";
                    backupFile = Path.Combine(backupContextDir, fileName);
                }

                // Copy original file to backup location
                File.Copy(originalFile, backupFile, overwrite: false);
                
                string logMessage = $"Created backup: {originalFile} -> {backupFile}";
                LogChanges(logMessage);
                Console.WriteLine($"Backup created: {backupFile}");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error creating backup for {originalFile}: {ex.Message}";
                LogChanges(errorMessage);
                Console.WriteLine(errorMessage);
                // Don't throw here - backup failure shouldn't stop the sync process
            }
        }

        private string ExtractChannelVersion(string runtimeId)
        {
            // Extract channel version (e.g., "9.0" from "9.0.7")
            string[] parts = runtimeId.Split('.');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}.{parts[1]}";
            }
            
            Console.WriteLine($"Warning: Unable to extract channel version from runtime ID: {runtimeId}");
            return runtimeId;
        }
    }
}
