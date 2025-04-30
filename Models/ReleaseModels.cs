using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ReleaseNotesUpdater.Models
{
    /// <summary>
    /// Represents the root object of the releases JSON-CDN file
    /// </summary>
    public class ReleasesConfiguration
    {
        [JsonProperty("channel-version")]
        public string ChannelVersion { get; set; }

        [JsonProperty("latest-runtime")]
        public string LatestRuntime { get; set; }

        [JsonProperty("latest-sdk")]
        public string LatestSdk { get; set; }

        [JsonProperty("latest-release-date")]
        public string LatestReleaseDate { get; set; }

        [JsonProperty("releases")]
        public List<Release> Releases { get; set; }
    }

    /// <summary>
    /// Represents a release in the releases array
    /// </summary>
    public class Release
    {
        [JsonProperty("runtime")]
        public Runtime Runtime { get; set; }
        
        [JsonProperty("sdk")]
        public Sdk Sdk { get; set; }
        
        [JsonProperty("aspnetcore-runtime")]
        public AspNetCoreRuntime AspNetCoreRuntime { get; set; }
        
        [JsonProperty("windowsdesktop")]
        public WindowsDesktop WindowsDesktop { get; set; }
        
        [JsonProperty("sdks")]
        public List<Sdk> Sdks { get; set; }
        
        [JsonProperty("packages")]
        public List<Package> Packages { get; set; }
    }

    /// <summary>
    /// Represents the runtime component of a release
    /// </summary>
    public class Runtime
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        
        [JsonProperty("files")]
        public List<FileInfo> Files { get; set; }
    }

    /// <summary>
    /// Represents the SDK component of a release
    /// </summary>
    public class Sdk
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        
        [JsonProperty("files")]
        public List<FileInfo> Files { get; set; }
    }

    /// <summary>
    /// Represents the ASP.NET Core runtime component of a release
    /// </summary>
    public class AspNetCoreRuntime
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        
        [JsonProperty("files")]
        public List<FileInfo> Files { get; set; }
    }

    /// <summary>
    /// Represents the Windows Desktop component of a release
    /// </summary>
    public class WindowsDesktop
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        
        [JsonProperty("files")]
        public List<FileInfo> Files { get; set; }
    }

    /// <summary>
    /// Represents a file in the files array
    /// </summary>
    public class FileInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("url")]
        public string Url { get; set; }
        
        [JsonProperty("hash")]
        public string Hash { get; set; }
    }

    /// <summary>
    /// Represents a package in the packages array
    /// </summary>
    public class Package
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    /// <summary>
    /// Represents the release notes JSON structure
    /// </summary>
    public class ReleaseNotes
    {
        [JsonProperty("latest-release")]
        public string LatestRelease { get; set; }
        
        [JsonProperty("support-phase")]
        public string SupportPhase { get; set; }
        
        [JsonProperty("release-type")]
        public string ReleaseType { get; set; }
        
        [JsonProperty("eol-date")]
        public string EolDate { get; set; }
        
        [JsonProperty("releases")]
        public List<ReleaseNote> Releases { get; set; }
    }

    /// <summary>
    /// Represents an individual release note in the release notes array
    /// </summary>
    public class ReleaseNote
    {
        [JsonProperty("release-date")]
        public string ReleaseDate { get; set; }
        
        [JsonProperty("release-version")]
        public string ReleaseVersion { get; set; }
        
        [JsonProperty("security")]
        public bool Security { get; set; }
        
        [JsonProperty("cve-list")]
        public List<string> CveList { get; set; }
    }
}