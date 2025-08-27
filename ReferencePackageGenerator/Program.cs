﻿using Newtonsoft.Json;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Packaging;
using NuGet.Versioning;
using Mono.Cecil;
using NuGet.Common;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ReferencePackageGenerator
{
    internal class Program
    {
        private static readonly JsonSerializer _jsonSerializer = new();

        private static string ChangeFileDirectory(string file, string newDirectory)
            => Path.Combine(newDirectory, Path.GetFileName(file));

        private static string ChangeFileDirectoryAndExtension(string file, string newDirectory, string newExtension)
            => Path.Combine(newDirectory, $"{Path.GetFileNameWithoutExtension(file)}{(newExtension.StartsWith('.') ? "" : ".")}{newExtension}");

        private static string ChangeFileExtension(string file, string newExtension)
            => Path.Combine(Path.GetDirectoryName(file)!, $"{Path.GetFileNameWithoutExtension(file)}{(newExtension.StartsWith('.') ? "" : ".")}{newExtension}");

        private static Version CombineVersions(Version primary, Version boost)
        {
            var primaries = new[] { primary.Major, primary.Minor, primary.Build, primary.Revision };
            var boosts = new[] { boost.Major, boost.Minor, boost.Build, boost.Revision };

            var merged = primaries.Zip(boosts, CombineVersionSegments).TakeWhile(segment => segment > -1).ToArray();

            return merged.Length switch
            {
                2 => new Version(merged[0], merged[1]),
                3 => new Version(merged[0], merged[1], merged[2]),
                4 => new Version(merged[0], merged[1], merged[2], merged[3]),
                _ => throw new InvalidOperationException("Need at least two segments in version!")
            };
        }

        private static int CombineVersionSegments(int primary, int boost)
        {
            if (boost == -1)
                return primary;

            if (primary == -1)
                return boost;

            return primary + boost;
        }

        private static int GetPathDepth(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || relativePath == ".")
                return 0;
            
            return relativePath.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar);
        }


        private static async Task GenerateSingleNuGetPackageAsync(Config config, IEnumerable<(string target, AssemblyDefinition assembly)> assemblies)
        {
            var builder = new PackageBuilder
            {
                Id = $"{config.PackageIdPrefix}{config.SinglePackageName}",
                Version = new NuGetVersion(config.SinglePackageVersion ?? new Version(1, 0, 0), config.VersionReleaseLabel),

                Title = $"Stripped All References Package",
                Description = $"Stripped reference package containing all assemblies.",

                IconUrl = string.IsNullOrWhiteSpace(config.IconUrl) ? null : new Uri(config.IconUrl),
                ProjectUrl = string.IsNullOrWhiteSpace(config.ProjectUrl) ? null : new Uri(config.ProjectUrl),
                Repository = new RepositoryMetadata(Path.GetExtension(config.RepositoryUrl)?.TrimStart('.'), config.RepositoryUrl, null, null)
            };

            builder.Authors.AddRange(config.Authors);
            builder.Tags.AddRange(config.Tags);

            var destinationPath = $"ref/{config.TargetFramework}/";
            var addedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (target, assembly) in assemblies)
            {
                var fileName = Path.GetFileName(target);
                
                // Skip if we've already added a file with this name
                if (!addedFiles.Add(fileName))
                {
                    Console.WriteLine($"Skipping duplicate assembly: {fileName}");
                    continue;
                }
                
                builder.AddFiles("", target, destinationPath);

                // Check for XML documentation
                var docFileName = Path.GetFileNameWithoutExtension(fileName) + ".xml";
                if (!addedFiles.Contains(docFileName))
                {
                    var docFile = ChangeFileDirectoryAndExtension(target, config.DocumentationPath, ".xml");
                    if (File.Exists(docFile))
                    {
                        builder.AddFiles("", docFile, destinationPath);
                        addedFiles.Add(docFileName);
                        Console.WriteLine($"Added documentation: {docFileName}");
                    }
                    else
                    {
                        docFile = ChangeFileDirectoryAndExtension(target, config.SourcePath, ".xml");
                        if (File.Exists(docFile))
                        {
                            builder.AddFiles("", docFile, destinationPath);
                            addedFiles.Add(docFileName);
                            Console.WriteLine($"Added documentation: {docFileName}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Skipping duplicate documentation: {docFileName}");
                }
                
                // Check for pdb DebugSymbols
                var pdbFileName = Path.GetFileNameWithoutExtension(fileName) + ".pdb";
                if (!addedFiles.Contains(pdbFileName))
                {
                    var pdbFile = ChangeFileDirectoryAndExtension(target, config.DebugSymbolsPath, ".pdb");
                    if (File.Exists(pdbFile))
                    {
                        builder.AddFiles("", pdbFile, destinationPath);
                        addedFiles.Add(pdbFileName);
                        Console.WriteLine($"Added DebugSymbols: {pdbFileName}");
                    }
                    else
                    {
                        pdbFile = ChangeFileDirectoryAndExtension(target, config.SourcePath, ".pdb");
                        if (File.Exists(pdbFile))
                        {
                            builder.AddFiles("", pdbFile, destinationPath);
                            addedFiles.Add(pdbFileName);
                            Console.WriteLine($"Added DebugSymbols: {pdbFileName}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Skipping duplicate DebugSymbols: {pdbFileName}");
                }
            }

            if (File.Exists(config.IconPath))
            {
                var iconName = Path.GetFileName(config.IconPath);
                builder.AddFiles("", config.IconPath, iconName);
                builder.Icon = iconName;
            }

            if (File.Exists(config.ReadmePath))
            {
                var readmeName = Path.GetFileName(config.ReadmePath);
                builder.AddFiles("", config.ReadmePath, readmeName);
                builder.Readme = readmeName;
            }


            var packagePath = Path.Combine(config.NupkgTargetPath, $"{config.PackageIdPrefix}{config.SinglePackageName}.nupkg");
            using (var outputStream = new FileStream(packagePath, FileMode.Create))
                builder.Save(outputStream);

            Console.WriteLine($"Saved single package to {packagePath}");

            if (config.PublishTarget is null || !config.PublishTarget.Publish)
            {
                Console.WriteLine("No PublishTarget defined or publishing disabled, skipping package upload.");
                return;
            }

            Console.WriteLine($"Publishing package to {config.PublishTarget.Source}");

            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(config.PublishTarget.Source);
            var resource = await repository.GetResourceAsync<PackageUpdateResource>();

            try
            {
                await resource.Push(new List<string>() { packagePath }, null, 20, false, source => config.PublishTarget.ApiKey, source => null, false, true, null, ConsoleLogger.Instance);
                Console.WriteLine("Finished publishing package!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to publish package!");
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task GenerateNuGetPackageAsync(Config config, string target, AssemblyDefinition assembly)
        {
            var version = config.VersionOverrides.TryGetValue(Path.GetFileName(target), out var versionOverride)
            ? versionOverride
            : assembly.Name.Version;

            version = CombineVersions(version, config.VersionBoost);

            var builder = new PackageBuilder
            {
                Id = $"{config.PackageIdPrefix}{Path.GetFileNameWithoutExtension(target)}",
                Version = new NuGetVersion(version, config.VersionReleaseLabel),

                Title = $"Stripped {Path.GetFileNameWithoutExtension(target)} Reference",
                Description = $"Stripped reference package for {Path.GetFileName(target)}.",

                IconUrl = string.IsNullOrWhiteSpace(config.IconUrl) ? null : new Uri(config.IconUrl),
                ProjectUrl = string.IsNullOrWhiteSpace(config.ProjectUrl) ? null : new Uri(config.ProjectUrl),
                Repository = new RepositoryMetadata(Path.GetExtension(config.RepositoryUrl)?.TrimStart('.'), config.RepositoryUrl, null, null)
            };

            builder.Authors.AddRange(config.Authors);

            builder.Tags.AddRange(config.Tags);

            //builder.DependencyGroups.Add(new PackageDependencyGroup(
            //    targetFramework: NuGetFramework.Parse("netstandard1.4"),
            //    packages: new[]
            //    {
            //        new PackageDependency("Newtonsoft.Json", VersionRange.Parse("10.0.1"))
            //    }));

            var destinationPath = $"ref/{config.TargetFramework}/";
            builder.AddFiles("", target, destinationPath);

            var docFile = ChangeFileDirectoryAndExtension(target, config.DocumentationPath, ".xml");
            if (File.Exists(docFile))
            {
                builder.AddFiles("", docFile, destinationPath);
            }
            else
            {
                docFile = ChangeFileDirectoryAndExtension(target, config.SourcePath, ".xml");
                if (File.Exists(docFile))
                    builder.AddFiles("", docFile, destinationPath);
            }

            // Check for pdb DebugSymbols
            var pdbFile = ChangeFileDirectoryAndExtension(target, config.DebugSymbolsPath, ".pdb");
            if (File.Exists(pdbFile))
            {
                builder.AddFiles("", pdbFile, destinationPath);
                Console.WriteLine($"Added DebugSymbols: {Path.GetFileName(pdbFile)}");
            }
            else
            {
                pdbFile = ChangeFileDirectoryAndExtension(target, config.SourcePath, ".pdb");
                if (File.Exists(pdbFile))
                {
                    builder.AddFiles("", pdbFile, destinationPath);
                    Console.WriteLine($"Added DebugSymbols: {Path.GetFileName(pdbFile)}");
                }
            }

            if (File.Exists(config.IconPath))
            {
                var iconName = Path.GetFileName(config.IconPath);
                builder.AddFiles("", config.IconPath, iconName);
                builder.Icon = iconName;
            }

            if (File.Exists(config.ReadmePath))
            {
                var readmeName = Path.GetFileName(config.ReadmePath);
                builder.AddFiles("", config.ReadmePath, readmeName);
                builder.Readme = readmeName;
            }


            var packagePath = Path.Combine(config.NupkgTargetPath, $"{config.PackageIdPrefix}{Path.GetFileNameWithoutExtension(target)}.nupkg");
            using (var outputStream = new FileStream(packagePath, FileMode.Create))
                builder.Save(outputStream);

            Console.WriteLine($"Saved package to {packagePath}");

            if (config.PublishTarget is null || !config.PublishTarget.Publish)
            {
                Console.WriteLine("No PublishTarget defined or publishing disabled, skipping package upload.");
                return;
            }

            Console.WriteLine($"Publishing package to {config.PublishTarget.Source}");

            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(config.PublishTarget.Source);
            var resource = await repository.GetResourceAsync<PackageUpdateResource>();

            try
            {
                await resource.Push(new List<string>() { packagePath }, null, 20, false, source => config.PublishTarget.ApiKey, source => null, false, true, null, ConsoleLogger.Instance);
                Console.WriteLine("Finished publishing package!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to publish package!");
                Console.WriteLine(ex.ToString());
            }
        }

        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: ReferencePackageGenerator.exe configPaths...");
                Console.WriteLine("There can be any number of config paths, which will be handled one by one.");
                Console.WriteLine("Missing config files will be generated.");
                return;
            }

            foreach (var configPath in args)
            {
                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"Generating missing config file: {configPath}");

                    try
                    {
                        using var file = File.OpenWrite(configPath);
                        using var streamWriter = new StreamWriter(file);
                        using var jsonTextWriter = new JsonTextWriter(streamWriter);
                        jsonTextWriter.Formatting = Formatting.Indented;

                        _jsonSerializer.Serialize(jsonTextWriter, new Config());

                        file.SetLength(file.Position);
                        jsonTextWriter.Flush();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to generate config file!");
                        Console.WriteLine(ex.ToString());
                    }

                    continue;
                }

                Config config;

                try
                {
                    using var file = File.OpenRead(configPath);
                    using var streamReader = new StreamReader(file);
                    using var jsonTextReader = new JsonTextReader(streamReader);

                    config = _jsonSerializer.Deserialize<Config>(jsonTextReader)!;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to read config file: {configPath}");
                    Console.WriteLine(ex.ToString());
                    continue;
                }

                if (config is null)
                {
                    Console.WriteLine($"Failed to read config file: {configPath}");
                    continue;
                }

                var codeStripper = new CodeStripper();
                codeStripper.Resolver.AddSearchDirectory(RuntimeEnvironment.GetRuntimeDirectory());
                codeStripper.Resolver.AddSearchDirectory(config.SourcePath);

                try
                {
                    Directory.CreateDirectory(config.DllTargetPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create DLL target directory: {config.DllTargetPath}");
                    Console.WriteLine(ex.ToString());
                    continue;
                }

                try
                {
                    Directory.CreateDirectory(config.NupkgTargetPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create nupkg target directory: {config.NupkgTargetPath}");
                    Console.WriteLine(ex.ToString());
                    continue;
                }

                Console.WriteLine($"Stripping matching assembly files from: {config.SourcePath}");

                if (config.SinglePackageMode)
                {
                    var assemblies = new List<(string source, string target, AssemblyDefinition assembly)>();

                    foreach (var source in config.Search())
                    {
                        var target = ChangeFileDirectory(source, config.DllTargetPath);

                        try
                        {
                            var assembly = codeStripper.CreateReferenceAssembly(source, target);
                            Console.WriteLine($"Stripped {Path.GetFileName(source)} to {Path.GetFileName(target)}");
                            assemblies.Add((source, target, assembly));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to strip assembly: {Path.GetFileName(source)}");
                            Console.WriteLine(ex.ToString());
                            continue;
                        }
                    }

                    if (assemblies.Count > 0)
                    {
                        // Sort assemblies by distance from SourcePath (closest first)
                        var sortedAssemblies = assemblies
                            .OrderBy(a => GetPathDepth(Path.GetRelativePath(config.SourcePath, a.source)))
                            .Select(a => (a.target, a.assembly));
                        
                        GenerateSingleNuGetPackageAsync(config, sortedAssemblies).GetAwaiter().GetResult();
                    }
                }
                else
                {
                    foreach (var source in config.Search())
                    {
                        var target = ChangeFileDirectory(source, config.DllTargetPath);

                        try
                        {
                            var assembly = codeStripper.CreateReferenceAssembly(source, target);
                            Console.WriteLine($"Stripped {Path.GetFileName(source)} to {Path.GetFileName(target)}");

                            GenerateNuGetPackageAsync(config, target, assembly).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to strip assembly: {Path.GetFileName(source)}");
                            Console.WriteLine(ex.ToString());
                            continue;
                        }
                    }
                }
            }
        }
    }
}
