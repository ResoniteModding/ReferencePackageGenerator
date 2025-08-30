using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using JetBrains.Refasmer;
using JetBrains.Refasmer.Filters;

namespace ReferencePackageGenerator
{
    public class SimpleLogger : ILogger
    {
        private readonly LogLevel _minLevel;

        public SimpleLogger(LogLevel minLevel = LogLevel.Information)
        {
            _minLevel = minLevel;
        }

        public void Log(LogLevel logLevel, string message)
        {
            if (IsEnabled(logLevel))
            {
                var prefix = logLevel switch
                {
                    LogLevel.Error or LogLevel.Critical => "[ERROR]",
                    LogLevel.Warning => "[WARN]",
                    LogLevel.Information => "[INFO]",
                    LogLevel.Debug => "[DEBUG]",
                    LogLevel.Trace => "[TRACE]",
                    _ => "[LOG]"
                };
                Console.WriteLine($"{prefix} {message}");
            }
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;
    }

    public class RefasmerStripper
    {
        private readonly LoggerBase _logger;
        private readonly RefasmerOptions _options;

        public RefasmerStripper(RefasmerOptions? options = null)
        {
            _options = options ?? new RefasmerOptions();
            _logger = new LoggerBase(new SimpleLogger(_options.LogLevel));
        }

        public static bool IsValidAssembly(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var peReader = new PEReader(stream);

                if (!peReader.HasMetadata)
                    return false;

                var metadataReader = peReader.GetMetadataReader();

                return metadataReader.IsAssembly;
            }
            catch
            {
                return false;
            }
        }

        public void CreateReferenceAssembly(string sourcePath, string targetPath)
        {
            try
            {
                using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
                using var peReader = new PEReader(sourceStream);

                if (!peReader.HasMetadata)
                {
                    throw new InvalidOperationException($"File {Path.GetFileName(sourcePath)} is not a .NET assembly (no metadata)");
                }

                var metadataReader = peReader.GetMetadataReader();

                IImportFilter? filter = null;

                switch (_options.FilterMode)
                {
                    case FilterMode.Public:
                        filter = new AllowPublic(_options.OmitNonApiMembers ?? false);
                        break;
                    case FilterMode.Internals:
                        filter = new AllowPublicAndInternals(_options.OmitNonApiMembers ?? false);
                        break;
                    case FilterMode.All:
                        filter = new AllowAll();
                        break;
                    case FilterMode.Auto:
                        // Let Refasmer auto-detect based on InternalsVisibleTo
                        filter = null;
                        break;
                }

                var refasmBytes = MetadataImporter.MakeRefasm(
                    metadataReader,
                    peReader,
                    _logger,
                    filter,
                    _options.OmitNonApiMembers,
                    makeMock: _options.MakeMock,
                    omitReferenceAssemblyAttr: _options.OmitReferenceAssemblyAttribute
                );

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.WriteAllBytes(targetPath, refasmBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create reference assembly for {Path.GetFileName(sourcePath)}: {ex.Message}", ex);
            }
        }
    }
}