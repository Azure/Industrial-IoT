// ------------------------------------------------------------
//  Copyright (c) Microsoft.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
        using Azure.IIoT.OpcUa.Core.Messaging;
        using Azure.IIoT.OpcUa.Core.Storage;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Event client that writes events to the filesystem
    /// </summary>
    public class FileSystemEventClient : IEventClient
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
        /// <param name="writers"></param>
        public FileSystemEventClient(IOptions<FileSystemEventClientOptions> options,
            IEnumerable<IFileWriter>? writers = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _rootFolder = Path.GetFullPath(_options.Value.OutputFolder ?? string.Empty);
            _writers = writers?.ToArray() ?? [];
        }

        /// <inheritdoc/>
        public IEvent CreateEvent()
        {
            return new FileSystemEvent(this);
        }

        /// <summary>
        /// Default writer
        /// </summary>
        private sealed class DefaultWriter : IFileWriter
        {
            /// <inheritdoc/>
            public bool SupportsContentType(string contentType)
            {
                return true;
            }

            /// <inheritdoc/>
            public async ValueTask WriteAsync(string fileName, DateTimeOffset timestamp,
                IEnumerable<ReadOnlySequence<byte>> buffers,
                IReadOnlyDictionary<string, string?> metadata,
                IEventSchema? schema, string contentType, CancellationToken ct)
            {
                var stream = new FileStream(fileName, FileMode.Append);
                await using (stream.ConfigureAwait(false))
                {
                    foreach (var buffer in buffers)
                    {
                        foreach (var memory in buffer)
                        {
                            await stream.WriteAsync(memory, ct).ConfigureAwait(false);
                        }
                    }
                }
                File.SetLastAccessTimeUtc(fileName, timestamp.DateTime);
            }
        }

        /// <summary>
        /// Get writer
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private IFileWriter Get(string contentType)
        {
            return _cache.GetOrAdd(contentType, c =>
            {
                return _writers.FirstOrDefault(w => w.SupportsContentType(c))
                    ?? new DefaultWriter();
            });
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
            public IEvent AsCloudEvent(CloudEventHeader header)
            {
                _metadata["ce:specversion"] = "1.0";
                _metadata["ce:id"] = header.Id;
                _metadata["ce:source"] = header.Source.ToString();
                _metadata["ce:type"] = header.Type;
                if (header.Time != null)
                {
                    _metadata["ce:time"] = header.Time.ToString();
                }
                if (header.DataContentType != null)
                {
                    _metadata["ce:datacontenttype"] = header.DataContentType;
                }
                if (header.Subject != null)
                {
                    _metadata["ce:subject"] = header.Subject;
                }
                return this;
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
            public IEvent SetTimestamp(DateTimeOffset value)
            {
                _timestamp = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetContentType(string? value)
            {
                _contentType = value;
                _metadata["ContentType"] = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetContentEncoding(string? value)
            {
                _metadata["ContentEncoding"] = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetSchema(IEventSchema schema)
            {
                _schema = schema;
                return this;
            }

            /// <inheritdoc/>
            public IEvent AddProperty(string name, string? value)
            {
                _metadata[name] = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetRetain(bool value)
            {
                _metadata["Retain"] = value ? "true" : "false";
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTtl(TimeSpan value)
            {
                _metadata["TTL"] = value.ToString();
                return this;
            }

            /// <inheritdoc/>
            public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
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
                _contentType ??= string.Empty;
                await _outer.Get(_contentType).WriteAsync(fileName, _timestamp,
                    _buffers, _metadata, _schema, _contentType, ct).ConfigureAwait(false);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _buffers.Clear();
            }

            private string? _topic;
            private DateTimeOffset _timestamp;
            private IEventSchema? _schema;
            private string? _contentType;
            private readonly Dictionary<string, string?> _metadata = [];
            private readonly List<ReadOnlySequence<byte>> _buffers = [];
            private readonly FileSystemEventClient _outer;
        }

        private readonly IOptions<FileSystemEventClientOptions> _options;
        private readonly string _rootFolder;
        private readonly IFileWriter[] _writers;
        private readonly ConcurrentDictionary<string, IFileWriter> _cache = new();
    }
}
