using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReleaseNotesUpdater.Models
{
    /// <summary>
    /// Represents the root object of the releases JSON file in the _coreDirectory
    /// </summary>
    public class CoreReleasesConfiguration
    {
        [JsonPropertyName("channel-version")]
        public string ChannelVersion { get; set; }

        [JsonPropertyName("latest-release")]
        public string LatestRelease { get; set; }

        [JsonPropertyName("latest-release-date")]
        public string LatestReleaseDate { get; set; }

        [JsonPropertyName("latest-runtime")]
        public string LatestRuntime { get; set; }

        [JsonPropertyName("latest-sdk")]
        public string LatestSdk { get; set; }

        [JsonPropertyName("support-phase")]
        public string SupportPhase { get; set; }

        [JsonPropertyName("release-type")]
        public string ReleaseType { get; set; }

        [JsonPropertyName("eol-date")]
        public string EolDate { get; set; }

        [JsonPropertyName("lifecycle-policy")]
        public string LifecyclePolicy { get; set; }

        [JsonPropertyName("releases")]
        public List<CoreRelease> Releases { get; set; }
    }

    /// <summary>
    /// Represents a release in the releases array for the _coreDirectory JSON
    /// </summary>
    public class CoreRelease
    {
        [JsonPropertyName("release-date")]
        public string ReleaseDate { get; set; }

        [JsonPropertyName("release-version")]
        public string ReleaseVersion { get; set; }

        [JsonPropertyName("security")]
        public bool Security { get; set; }

        [JsonPropertyName("cve-list")]
        public List<CoreCveItem> CveList { get; set; }

        [JsonPropertyName("release-notes")]
        public string ReleaseNotes { get; set; }
    }

    /// <summary>
    /// Represents a CVE item in the _coreDirectory JSON
    /// </summary>
    public class CoreCveItem
    {
        [JsonPropertyName("cve-id")]
        public string CveId { get; set; }

        [JsonPropertyName("cve-url")]
        public string CveUrl { get; set; }
    }
}