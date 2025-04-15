using System;
using System.IO;

namespace ReleaseNotesUpdater
{
    public abstract class FileUpdater
    {
        protected string TemplateDirectory;
        protected string LogFileLocation;

        protected FileUpdater(string templateDirectory, string logFileLocation)
        {
            TemplateDirectory = templateDirectory;
            LogFileLocation = logFileLocation;
        }

        protected void LogChanges(string message)
        {
            using (StreamWriter logFile = new StreamWriter(LogFileLocation, true))
            {
                logFile.WriteLine(message);
                logFile.WriteLine($"Timestamp: {DateTime.UtcNow}");
            }
        }

        protected void CreateDirectoryIfNotExists(string? path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public abstract void UpdateFiles();
    }
}