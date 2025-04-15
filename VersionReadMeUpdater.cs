using System.IO;

namespace ReleaseNotesUpdater
{
    public class VersionReadMeUpdater : FileUpdater
    {
        private readonly string[] versions = { "NET6", "NET8", "NET9" };

        public VersionReadMeUpdater(string templateDirectory, string logFileLocation)
            : base(templateDirectory, logFileLocation)
        {
        }

        public override void UpdateFiles()
        {
            foreach (var version in versions)
            {
                string versionReadMeTemplate = Path.Combine(TemplateDirectory, "version-readme-template.md");
                string newVersionReadMeFile = Path.Combine($"core/release-notes/{version}.0", "README.md");

                CreateDirectoryIfNotExists(Path.GetDirectoryName(newVersionReadMeFile));
                File.Copy(versionReadMeTemplate, newVersionReadMeFile, true);

                LogChanges($"Created version README file: {newVersionReadMeFile} for version: {version}");
            }
        }
    }
}