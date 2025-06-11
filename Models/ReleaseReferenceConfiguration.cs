using System.Collections.Generic;

namespace ReleaseNotesUpdater.Models
{
    /// <summary>
    /// Configuration class for storing reference data loaded from external JSON files
    /// </summary>
    public class ReleaseReferenceConfiguration
    {
        /// <summary>
        /// Dictionary mapping .NET version to launch date
        /// </summary>
        public Dictionary<string, string> LaunchDates { get; set; } = new();

        /// <summary>
        /// Dictionary mapping .NET version to announcement blog post URLs
        /// </summary>
        public Dictionary<string, string> AnnouncementLinks { get; set; } = new();

        /// <summary>
        /// Dictionary mapping .NET version to end-of-life announcement URLs
        /// </summary>
        public Dictionary<string, string> EolAnnouncementLinks { get; set; } = new();
    }
}
