using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReleaseNotesUpdater.Models
{
    public class MsrcConfig
    {
        [JsonPropertyName("RuntimeId")]
        public string? RuntimeId { get; set; }

        [JsonPropertyName("Cves")]
        public List<MsrcCveInfo>? Cves { get; set; }
    }

    public class MsrcCveInfo
    {
        [JsonPropertyName("CveId")]
        public string? CveId { get; set; }

        [JsonPropertyName("CveTitle")]
        public string? CveTitle { get; set; }

        [JsonPropertyName("CveDescription")]
        public string? CveDescription { get; set; }
    }
}
