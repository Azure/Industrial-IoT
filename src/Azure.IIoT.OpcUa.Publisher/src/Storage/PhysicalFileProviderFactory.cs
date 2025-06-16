// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Storage
{
    using Azure.IIoT.OpcUa.Publisher;
    using Furly.Extensions.Storage;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Physical file provider factory
    /// </summary>
    public sealed class PhysicalFileProviderFactory : IFileProviderFactory, IDisposable
    {
        /// <inheritdoc/>
        public PhysicalFileProviderFactory(IOptions<PublisherOptions> options,
            ILogger<PhysicalFileProviderFactory> logger)
        {
            _options = options;
            _logger = logger;
            _providers = new ConcurrentDictionary<string, PhysicalFileProvider>();
        }

        /// <inheritdoc/>
        public IFileProvider Create(string root)
        {
            root = Path.GetFullPath(string.IsNullOrWhiteSpace(root) ?
                Environment.CurrentDirectory : root);
            return _providers.GetOrAdd(root, directory =>
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var provider = new PhysicalFileProvider(directory);

                if (_options.Value.UseFileChangePolling == true)
                {
                    provider.UseActivePolling = true;
                    provider.UsePollingFileWatcher = true;
                }

                _logger.MappingDirectory(directory);
                return provider;
            });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _providers.Values.ToList().ForEach(p => p.Dispose());
        }

        private readonly IOptions<PublisherOptions> _options;
        private readonly ILogger<PhysicalFileProviderFactory> _logger;
        private readonly ConcurrentDictionary<string, PhysicalFileProvider> _providers;
    }

    /// <summary>
    /// Source-generated logging extensions for PhysicalFileProviderFactory
    /// </summary>
    internal static partial class PhysicalFileProviderFactoryLogging
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Mapping directory {Directory} via physical file provider.")]
        public static partial void MappingDirectory(this ILogger logger, string directory);
    }
}
