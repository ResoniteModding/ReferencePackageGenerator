using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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