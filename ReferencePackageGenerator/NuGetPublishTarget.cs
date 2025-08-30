using Newtonsoft.Json;

namespace ReferencePackageGenerator
{
    [JsonObject]
    public class NuGetPublishTarget
    {
        public string ApiKey { get; set; } = string.Empty;
        public bool Publish { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}