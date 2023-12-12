// ------------------------------------------------------------
//  Copyright (c) Microsoft.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Messaging.Runtime;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Event client that writes events to the filesystem
    /// </summary>
    public sealed class FileSystemEventClient : IEventClient
    {
        /// <inheritdoc/>
        public string Name => "FileSystem";

        /// <inheritdoc/>
        public int MaxEventPayloadSizeInBytes
            => _options.Value.MessageMaxBytes ?? 512 * 1024 * 1024;

        /// <inheritdoc/>
        public string Identity => Guid.NewGuid().ToString();

        /// <summary>
        /// Create dapr client
        /// </summary>
        /// <param name="options"></param>
        public FileSystemEventClient(IOptions<FileSystemOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _rootFolder = Path.GetFullPath(_options.Value.OutputFolder ?? string.Empty);
        }

        /// <inheritdoc/>
        public IEvent CreateEvent()
        {
            return new FileSystemEvent(this);
        }

        /// <summary>
        /// Event wrapper
        /// </summary>
        private sealed class FileSystemEvent : IEvent
        {
            /// <summary>
            /// Create event
            /// </summary>
            /// <param name="outer"></param>
            public FileSystemEvent(FileSystemEventClient outer)
            {
                _outer = outer;
            }

            /// <inheritdoc/>
            public IEvent SetTopic(string? value)
            {
                _topic = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetQoS(QoS value)
            {
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTimestamp(DateTime value)
            {
                _timestamp = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetContentType(string? value)
            {
                _metadata.AddOrUpdate("ContentType", value);
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetContentEncoding(string? value)
            {
                _metadata.AddOrUpdate("ContentEncoding", value);
                return this;
            }

            /// <inheritdoc/>
            public IEvent AddProperty(string name, string? value)
            {
                _metadata.AddOrUpdate(name, value);
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetRetain(bool value)
            {
                _metadata.AddOrUpdate("Retain", value ? "true" : "false");
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTtl(TimeSpan value)
            {
                _metadata.AddOrUpdate("TTL", value.ToString());
                return this;
            }

            /// <inheritdoc/>
            public IEvent AddBuffers(IEnumerable<ReadOnlyMemory<byte>> value)
            {
                _buffers.AddRange(value);
                return this;
            }

            /// <inheritdoc/>
            public async ValueTask SendAsync(CancellationToken ct)
            {
                if (_buffers.Count == 0)
                {
                    return;
                }
                if (string.IsNullOrEmpty(_topic))
                {
                    throw new InvalidOperationException("Need topic");
                }
                var fileName = string.Join("_", _topic.Split(Path.GetInvalidFileNameChars()))
                    .Trim('/');
                fileName = _outer._rootFolder + "/" + fileName;
                if (Path.DirectorySeparatorChar != '/')
                {
                    fileName = fileName.Replace('/', Path.DirectorySeparatorChar);
                }
                using (var stream = new FileStream(fileName, FileMode.Append))
                {
                    foreach (var buffer in _buffers)
                    {
                        await stream.WriteAsync(buffer, ct).ConfigureAwait(false);
                    }
                }
                File.SetLastAccessTimeUtc(fileName, _timestamp);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _buffers.Clear();
            }

            private string? _topic;
            private DateTime _timestamp;
            private readonly Dictionary<string, string?> _metadata = new();
            private readonly List<ReadOnlyMemory<byte>> _buffers = new();
            private readonly FileSystemEventClient _outer;
        }

        private readonly IOptions<FileSystemOptions> _options;
        private readonly string _rootFolder;
    }
}
