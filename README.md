# ReferencePackageGenerator

A .NET tool for generating stripped reference assemblies and NuGet packages from compiled .NET assemblies. This fork is specifically configured for generating reference packages for the Resonite platform modding community.

Fork of [ReferencePackageGenerator](https://github.com/MonkeyModdingTroop/ReferencePackageGenerator)

## Features

- **Reference Assembly Generation**: Uses JetBrains.Refasmer to strip implementation details while preserving public API surface
- **Flexible Configuration**: JSON-based configuration for fine-tuned control over assembly selection
- **NuGet Package Creation**: Automatically packages reference assemblies with documentation and debug symbols
- **Batch Processing**: Process multiple assemblies at once or bundle them into a single package
- **Path Exclusion**: Exclude entire directory paths from processing
- **Force Include**: Override exclusion rules for specific critical assemblies

## Installation

### Prerequisites
- .NET 9.0 SDK or later
- Windows, Linux, or macOS

### Building from Source
```bash
git clone https://github.com/ResoniteModding/ReferencePackageGenerator.git
cd ReferencePackageGenerator
dotnet build
```

## Usage

### Basic Usage
```bash
dotnet run --project ReferencePackageGenerator -- config.json
```

Or if using the compiled executable:
```bash
ReferencePackageGenerator.exe config.json
```

You can specify multiple configuration files:
```bash
ReferencePackageGenerator.exe config1.json config2.json config3.json
```

### Configuration File

The tool uses JSON configuration files to control its behavior. If a config file doesn't exist, the tool will generate a template for you.

#### Example Configuration (Resonite.json)
```json
{
  "SourcePath": "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Resonite",
  "DllTargetPath": "./Public",
  "NupkgTargetPath": "./Packages",
  "PackageIdPrefix": "Resonite.",
  "SinglePackageMode": true,
  "SinglePackageName": "GameLibs",
  "SinglePackageVersion": "2025.8.27.954",
  "TargetFramework": "net9.0",
  "Recursive": true,
  "Authors": ["ResoniteModding"],
  "Tags": ["ResoniteModding", "Resonite", "GameModding", "Modding"],
  "IncludePatterns": [
    ".+\\.dll$",
    ".+\\.exe$"
  ],
  "ExcludePatterns": [
    "^Microsoft\\..+",
    "^System\\..+"
  ],
  "ExcludePathPatterns": [
    "Renderer",
    "Resonite_Data",
    "runtimes"
  ],
  "ForceIncludePatterns": [
    "^Critical\\.Assembly\\.dll$"
  ],
  "RefasmerOptions": {
    "FilterMode": "Auto",
    "LogLevel": "Warning"
  }
}
```

### Configuration Options

#### Core Paths
- **SourcePath** (required): Root directory to search for assemblies
- **DllTargetPath**: Output directory for stripped reference assemblies (default: `./Public`)
- **NupkgTargetPath**: Output directory for NuGet packages (default: `./Packages`)
- **DocumentationPath**: Directory to search for XML documentation files (defaults to SourcePath)
- **DebugSymbolsPath**: Directory to search for PDB files (defaults to SourcePath)

#### Package Configuration
- **PackageIdPrefix**: Prefix for generated NuGet package IDs
- **SinglePackageMode**: Bundle all assemblies into one package (true) or create individual packages (false)
- **SinglePackageName**: Name for the single package when SinglePackageMode is enabled
- **SinglePackageVersion**: Version for the single package (e.g., "1.0.0")
- **TargetFramework**: Target framework for the package (e.g., "net9.0")
- **Authors**: List of package authors
- **Tags**: List of NuGet package tags

#### File Selection
- **Recursive**: Search subdirectories for assemblies (true/false)
- **IncludePatterns**: Regex patterns for files to include (e.g., `".+\\.dll$"`)
- **ExcludePatterns**: Regex patterns for files to exclude (e.g., `"^System\\..+"`)
- **ExcludePathPatterns**: Directory names to exclude from processing
- **ForceIncludePatterns**: Regex patterns that override all exclusion rules

#### Refasmer Options
- **FilterMode**: Controls which members to include
  - `"Auto"`: Auto-detect based on InternalsVisibleTo attributes (default)
  - `"Public"`: Include only public types
  - `"Internals"`: Include public and internal types
  - `"All"`: Include all types regardless of visibility
- **OmitNonApiMembers**: Remove private members not in public API (true/false)
- **MakeMock**: Generate mock assemblies instead of reference assemblies
- **OmitReferenceAssemblyAttribute**: Skip adding ReferenceAssembly attribute
- **LogLevel**: Verbosity of output (`"Trace"`, `"Debug"`, `"Information"`, `"Warning"`, `"Error"`)

#### Publishing (Optional)
- **PublishTarget**: NuGet publishing configuration
  ```json
  "PublishTarget": {
    "Publish": true,
    "Source": "https://api.nuget.org/v3/index.json",
    "ApiKey": "your-api-key-here"
  }
  ```

## How It Works

1. **Discovery**: The tool searches the SourcePath for assemblies matching IncludePatterns
2. **Filtering**: Applies ExcludePatterns and ExcludePathPatterns to filter out unwanted files
3. **Force Include**: Overrides exclusions for files matching ForceIncludePatterns
4. **Stripping**: Uses JetBrains.Refasmer to create reference assemblies with no implementation
5. **Packaging**: Creates NuGet packages with the reference assemblies, documentation, and debug symbols
6. **Publishing**: Optionally publishes packages to a NuGet feed

## Pattern Matching

### Include/Exclude Patterns
Patterns use .NET regex syntax. In JSON, remember to escape backslashes:
- `"^System\\..+"` - Matches files starting with "System."
- `".+\\.dll$"` - Matches all DLL files
- `"^(Microsoft|System)\\."` - Matches Microsoft or System assemblies

### Path Exclusion
ExcludePathPatterns match directory names anywhere in the path:
- `"runtimes"` - Excludes anything in a "runtimes" folder
- `"Plugins"` - Excludes anything in a "Plugins" folder

### Force Include
ForceIncludePatterns override ALL exclusion rules. Use this for critical assemblies that would otherwise be excluded.

## Examples

### Generate Reference Package for Resonite
```bash
dotnet run --project ReferencePackageGenerator -- Resonite.json
```

### Create Individual Packages
Set `"SinglePackageMode": false` in your config to create separate NuGet packages for each assembly.

### Custom Framework Target
```json
{
  "TargetFramework": "net8.0",
  "SinglePackageMode": false,
  "PackageIdPrefix": "MyProject.Ref."
}
```

## Troubleshooting

### Assembly Not Valid
If you see "Not a valid .NET assembly" messages, the file is either:
- Not a .NET assembly
- Corrupted
- A native DLL
- Missing metadata

### Missing Documentation
The tool searches for XML documentation files in:
1. The specified DocumentationPath
2. The SourcePath (if DocumentationPath is not set)
3. The same directory as the assembly

### Version Issues
- Use `SinglePackageVersion` for single package mode
- Use `VersionOverrides` to set specific versions for individual assemblies
- Use `VersionBoost` to add to all versions (e.g., "0.1.0")

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

This project is licensed under the MIT License. See the LICENSE file for details.

## Credits

- Based on the original [ReferencePackageGenerator](https://github.com/MonkeyModdingTroop/ReferencePackageGenerator) by MonkeyModdingTroop
- Uses [JetBrains.Refasmer](https://github.com/JetBrains/Refasmer) for reference assembly generation