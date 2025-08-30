﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReferencePackageGenerator
{
    [JsonObject]
    public class Config
    {
        [JsonProperty(nameof(DocumentationPath))]
        private string? _documentationPath = null;
        [JsonProperty(nameof(DebugSymbolsPath))]
        private string? _debugSymbolsPath = null;

        public string[] Authors { get; set; } = [];

        public string DllTargetPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "Public");

        [JsonIgnore]
        public string DocumentationPath => _documentationPath ?? SourcePath;
        [JsonIgnore]
        public string DebugSymbolsPath => _debugSymbolsPath ?? SourcePath;

        public string[] ExcludePatterns
        {
            get => Excludes.Select(regex => regex.ToString()).ToArray();

            [MemberNotNull(nameof(Excludes))]
            set => Excludes = value?.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).ToArray() ?? [];
        }

        [JsonIgnore]
        public Regex[] Excludes { get; private set; } = [];

        public string IconPath { get; set; } = string.Empty;

        public string IconUrl { get; set; } = string.Empty;

        public string ReadmePath { get; set; } = string.Empty;

        public string[] IncludePatterns
        {
            get => Includes.Select(regex => regex.ToString()).ToArray();

            [MemberNotNull(nameof(Includes))]
            set => Includes = value?.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).ToArray() ?? [];
        }

        [JsonIgnore]
        public Regex[] Includes { get; private set; }

        public string NupkgTargetPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "Packages");

        public string PackageIdPrefix { get; set; } = string.Empty;

        public string? ProjectUrl { get; set; }
        public NuGetPublishTarget? PublishTarget { get; set; }

        public bool Recursive { get; set; } = false;

        public string? RepositoryUrl { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string SourcePath { get; set; } = Environment.CurrentDirectory;

        public string[] Tags { get; set; } = [];

        public string TargetFramework { get; set; } = string.Empty;

        public bool SinglePackageMode { get; set; } = false;

        public string SinglePackageName { get; set; } = "AllReferences";

        [JsonIgnore]
        public Version? SinglePackageVersion { get; set; }

        [JsonIgnore]
        public Version VersionBoost { get; set; } = new();

        [JsonIgnore]
        public Dictionary<string, Version> VersionOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public string VersionReleaseLabel { get; set; } = string.Empty;

        public RefasmerOptions RefasmerOptions { get; set; } = new RefasmerOptions();

        [JsonProperty(nameof(SinglePackageVersion))]
        private string? SinglePackageVersionString
        {
            get => SinglePackageVersion?.ToString();
            set => SinglePackageVersion = value is null ? null : new Version(value);
        }

        [JsonProperty(nameof(VersionBoost))]
        private string? VersionBoostString
        {
            get => VersionBoost.ToString();
            set => VersionBoost = value is null ? new() : new(value);
        }

        [JsonProperty(nameof(VersionOverrides))]
        private Dictionary<string, string> VersionOverrideStrings
        {
            get => VersionOverrides?.ToDictionary(entry => entry.Key, entry => entry.Value.ToString(), StringComparer.OrdinalIgnoreCase)!;
            set => VersionOverrides = value?.ToDictionary(entry => entry.Key, entry => new Version(entry.Value), StringComparer.OrdinalIgnoreCase) ?? new(StringComparer.OrdinalIgnoreCase);
        }

        public string[] ExcludePathPatterns { get; set; } = [];

        public string[] ForceIncludePatterns
        {
            get => ForceIncludes.Select(regex => regex.ToString()).ToArray();

            [MemberNotNull(nameof(ForceIncludes))]
            set => ForceIncludes = value?.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).ToArray() ?? [];
        }

        [JsonIgnore]
        public Regex[] ForceIncludes { get; private set; } = [];

        public Config()
        {
            IncludePatterns = [@".+\.dll$", @".+\.exe$"];
            ExcludePatterns = [@"^Microsoft\..+", @"^System\..+", @"^Mono\..+", @"^UnityEngine\..+"];
            ExcludePathPatterns = [@"Renderer", @"Resonite_Data", @"runtimes", @"BepInEx", @"rml_", @"SRAnipal", @"Tools", @"Migrations", @"Locale", @"Plugins"];
        }

        public IEnumerable<string> Search()
        {
            foreach (var path in Directory.EnumerateFiles(SourcePath, "*", Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(path);

                // If file matches ForceIncludePatterns, always include it (override all exclusions)
                if (ForceIncludes.Length != 0 && ForceIncludes.Any(regex => regex.IsMatch(fileName)))
                {
                    yield return path;
                    continue;
                }

                // Check if path should be excluded (folder and all contents)
                if (ExcludePathPatterns.Length != 0)
                {
                    var relativePath = Path.GetRelativePath(SourcePath, path);
                    var pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    // Check if any folder in the path matches an exclude pattern
                    foreach (var part in pathParts.Take(pathParts.Length - 1)) // Exclude the file name itself
                    {
                        if (ExcludePathPatterns.Any(pattern =>
                            part.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                            part.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)))
                        {
                            goto NextFile; // Skip this file
                        }
                    }
                }

                // Check if file matches include patterns (if specified)
                if (Includes.Length != 0 && !Includes.Any(regex => regex.IsMatch(fileName)))
                    continue;

                // Check file name exclusions
                if (Excludes.Length != 0 && Excludes.Any(regex => regex.IsMatch(fileName)))
                    continue;

                yield return path;

            NextFile:;
            }
        }
    }
}