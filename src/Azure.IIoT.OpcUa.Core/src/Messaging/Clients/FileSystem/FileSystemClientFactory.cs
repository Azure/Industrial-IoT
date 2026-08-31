// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Storage;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Creates filesystem event clients.
    /// </summary>
    public sealed class FileSystemClientFactory : IEventClientFactory
    {
        /// <inheritdoc/>
        public string Name => "FileSystem";

        /// <summary>
        /// Create factory.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="writers"></param>
        public FileSystemClientFactory(ILogger<FileSystemClientFactory> logger,
            IEnumerable<IFileWriter>? writers = null)
        {
            _logger = logger;
            _writers = writers?.ToArray() ?? [];
        }

        /// <inheritdoc/>
        public IDisposable CreateEventClient(string connectionString, out IEventClient client)
        {
            lock (_clients)
            {
                var outputPath = Path.GetFullPath(connectionString);
                if (!_clients.TryGetValue(outputPath, out var refCountedClient))
                {
                    try
                    {
                        Directory.CreateDirectory(outputPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.CannotCreateOutputFolder(ex, outputPath);
                    }

                    refCountedClient = new RefCountedClient(this, outputPath);
                    _clients.Add(outputPath, refCountedClient);
                }
                refCountedClient.AddRef();
                client = refCountedClient.Client;
                return refCountedClient;
            }
        }

        /// <summary>
        /// Reference counted client wrapper.
        /// </summary>
        private sealed class RefCountedClient : IDisposable,
            IOptions<FileSystemEventClientOptions>
        {
            /// <summary>
            /// Client.
            /// </summary>
            public IEventClient Client { get; }

            /// <inheritdoc/>
            public FileSystemEventClientOptions Value => new()
            {
                OutputFolder = _connectionString
            };

            /// <summary>
            /// Create wrapper.
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="connectionString"></param>
            public RefCountedClient(FileSystemClientFactory outer, string connectionString)
            {
                _outer = outer;
                _connectionString = connectionString;
                Client = new FileSystemEventClient(this, outer._writers);
            }

            /// <summary>
            /// Add reference.
            /// </summary>
            public void AddRef()
            {
                Interlocked.Increment(ref _refCount);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (Interlocked.Decrement(ref _refCount) == 0)
                {
                    lock (_outer._clients)
                    {
                        _outer._clients.Remove(_connectionString);
                    }
                }
            }

            private readonly FileSystemClientFactory _outer;
            private readonly string _connectionString;
            private int _refCount;
        }

        private readonly ILogger<FileSystemClientFactory> _logger;
        private readonly IFileWriter[] _writers;
        private readonly Dictionary<string, RefCountedClient> _clients
            = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Source-generated logging.
    /// </summary>
    internal static partial class FileSystemClientFactoryLogging
    {
        private const int kEventClass = 30;

        [LoggerMessage(EventId = kEventClass + 0, Level = LogLevel.Debug,
            Message = "Cannot create output folder '{OutputPath}'")]
        public static partial void CannotCreateOutputFolder(this ILogger logger,
            Exception exception, string? outputPath);
    }
}
