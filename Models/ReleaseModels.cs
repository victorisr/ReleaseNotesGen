using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReleaseNotesUpdater.Models
{
    /// <summary>
    /// Represents the root object of the releases JSON-CDN file
    /// </summary>
    public class ReleasesConfiguration
    {
        [JsonPropertyName("channel-version")]
        public string? ChannelVersion { get; set; }

        [JsonPropertyName("latest-release")]
        public string? LatestRelease { get; set; }

        [JsonPropertyName("latest-release-date")]
        public string? LatestReleaseDate { get; set; }

        [JsonPropertyName("latest-runtime")]
        public string? LatestRuntime { get; set; }

        [JsonPropertyName("latest-sdk")]
        public string? LatestSdk { get; set; }

        [JsonPropertyName("support-phase")]
        public string? SupportPhase { get; set; }

        [JsonPropertyName("release-type")]
        public string? ReleaseType { get; set; }

        [JsonPropertyName("lifecycle-policy")]
        public string? LifecyclePolicy { get; set; }

        [JsonPropertyName("releases")]
        public List<Release>? Releases { get; set; }
    }

    /// <summary>
    /// Represents a release in the releases array
    /// </summary>
    public class Release
    {
        [JsonPropertyName("release-date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("release-version")]
        public string? ReleaseVersion { get; set; }

        [JsonPropertyName("security")]
        public bool Security { get; set; }

        [JsonPropertyName("cve-list")]
        public List<CveItem>? CveList { get; set; }

        [JsonPropertyName("release-notes")]
        public string? ReleaseNotes { get; set; }

        [JsonPropertyName("runtime")]
        public Runtime? Runtime { get; set; }
        
        [JsonPropertyName("sdk")]
        public Sdk? Sdk { get; set; }
        
        [JsonPropertyName("aspnetcore-runtime")]
        public AspNetCoreRuntime? AspNetCoreRuntime { get; set; }
        
        [JsonPropertyName("windowsdesktop")]
        public WindowsDesktop? WindowsDesktop { get; set; }
        
        [JsonPropertyName("sdks")]
        public List<Sdk>? Sdks { get; set; }
        
        [JsonPropertyName("packages")]
        public List<Package>? Packages { get; set; }
    }

    /// <summary>
    /// Represents a CVE item
    /// </summary>
    public class CveItem
    {
        [JsonPropertyName("cve-id")]
        public string? CveId { get; set; }

        [JsonPropertyName("cve-url")]
        public string? CveUrl { get; set; }
    }

    /// <summary>
    /// Represents the runtime component of a release
    /// </summary>
    public class Runtime
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("version-display")]
        public string? VersionDisplay { get; set; }

        [JsonPropertyName("vs-version")]
        public string? VsVersion { get; set; }

        [JsonPropertyName("vs-mac-version")]
        public string? VsMacVersion { get; set; }
        
        [JsonPropertyName("files")]
        public List<FileInfo>? Files { get; set; }
    }

    /// <summary>
    /// Represents the SDK component of a release
    /// </summary>
    public class Sdk
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("version-display")]
        public string? VersionDisplay { get; set; }

        [JsonPropertyName("runtime-version")]
        public string? RuntimeVersion { get; set; }

        [JsonPropertyName("vs-version")]
        public string? VsVersion { get; set; }

        [JsonPropertyName("vs-mac-version")]
        public string? VsMacVersion { get; set; }

        [JsonPropertyName("vs-support")]
        public string? VsSupport { get; set; }

        [JsonPropertyName("vs-mac-support")]
        public string? VsMacSupport { get; set; }

        [JsonPropertyName("csharp-version")]
        public string? CsharpVersion { get; set; }

        [JsonPropertyName("fsharp-version")]
        public string? FsharpVersion { get; set; }

        [JsonPropertyName("vb-version")]
        public string? VbVersion { get; set; }
        
        [JsonPropertyName("files")]
        public List<FileInfo>? Files { get; set; }
    }

    /// <summary>
    /// Represents the ASP.NET Core runtime component of a release
    /// </summary>
    public class AspNetCoreRuntime
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("version-display")]
        public string? VersionDisplay { get; set; }

        [JsonPropertyName("version-aspnetcoremodule")]
        public List<string>? VersionAspNetCoreModule { get; set; }

        [JsonPropertyName("vs-version")]
        public string? VsVersion { get; set; }
        
        [JsonPropertyName("files")]
        public List<FileInfo>? Files { get; set; }
    }

    /// <summary>
    /// Represents the Windows Desktop component of a release
    /// </summary>
    public class WindowsDesktop
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("version-display")]
        public string? VersionDisplay { get; set; }
        
        [JsonPropertyName("files")]
        public List<FileInfo>? Files { get; set; }
    }

    /// <summary>
    /// Represents a file in the files array
    /// </summary>
    public class FileInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("rid")]
        public string? Rid { get; set; }
        
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }

        [JsonPropertyName("akams")]
        public string? Akams { get; set; }
    }

    /// <summary>
    /// Represents a package in the packages array
    /// </summary>
    public class Package
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }

    /// <summary>
    /// Represents the release notes JSON structure
    /// </summary>
    public class ReleaseNotes
    {
        [JsonPropertyName("channel-version")]
        public string? ChannelVersion { get; set; }

        [JsonPropertyName("latest-release")]
        public string? LatestRelease { get; set; }
        
        [JsonPropertyName("support-phase")]
        public string? SupportPhase { get; set; }
        
        [JsonPropertyName("release-type")]
        public string? ReleaseType { get; set; }
        
        [JsonPropertyName("eol-date")]
        public string? EolDate { get; set; }
        
        [JsonPropertyName("releases")]
        public List<ReleaseNote>? Releases { get; set; }
    }

    /// <summary>
    /// Represents an individual release note in the release notes array
    /// </summary>
    public class ReleaseNote
    {
        [JsonPropertyName("release-date")]
        public string? ReleaseDate { get; set; }
        
        [JsonPropertyName("release-version")]
        public string? ReleaseVersion { get; set; }
        
        [JsonPropertyName("security")]
        public bool Security { get; set; }
        
        [JsonPropertyName("cve-list")]
        public List<string>? CveList { get; set; }
    }
}