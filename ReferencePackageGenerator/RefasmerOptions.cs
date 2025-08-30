using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ReferencePackageGenerator
{
    /// <summary>
    /// Configuration options for JetBrains.Refasmer
    /// </summary>
    public class RefasmerOptions
    {
        /// <summary>
        /// Filter mode for determining which types to include.
        /// Options: "auto" (default), "public", "internals", "all"
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public FilterMode FilterMode { get; set; } = FilterMode.Auto;

        /// <summary>
        /// Omit private members and types not participating in the public API.
        /// This preserves empty vs non-empty struct semantics but might affect unmanaged struct constraint.
        /// Only used when FilterMode is not "all".
        /// </summary>
        public bool? OmitNonApiMembers { get; set; }

        /// <summary>
        /// Make mock assembly instead of reference assembly.
        /// Mock assemblies throw NotImplementedException in each imported method.
        /// </summary>
        public bool MakeMock { get; set; } = false;

        /// <summary>
        /// Omit the ReferenceAssembly attribute from the generated assembly.
        /// </summary>
        public bool OmitReferenceAssemblyAttribute { get; set; } = false;

        /// <summary>
        /// Log level for Refasmer operations.
        /// Options: "Trace", "Debug", "Information", "Warning", "Error", "Critical", "None"
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public JetBrains.Refasmer.LogLevel LogLevel { get; set; } = JetBrains.Refasmer.LogLevel.Warning;
    }

    /// <summary>
    /// Filter mode for determining which types to include in the reference assembly
    /// </summary>
    public enum FilterMode
    {
        /// <summary>
        /// Auto-detect based on InternalsVisibleTo attributes (default)
        /// </summary>
        Auto,
        
        /// <summary>
        /// Include only public types (drop non-public even with InternalsVisibleTo)
        /// </summary>
        Public,
        
        /// <summary>
        /// Include public and internal types
        /// </summary>
        Internals,
        
        /// <summary>
        /// Include all types regardless of visibility
        /// </summary>
        All
    }
}