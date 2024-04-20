// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Avro;
    using Furly;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Storage;
    using Microsoft.Extensions.Logging;
    using Microsoft.IO;
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Write json and binary messages as zip files
    /// </summary>
    public sealed class ZipFileWriter : IFileWriter, IDisposable
    {
        /// <inheritdoc/>
        public bool SupportsContentType(string contentType)
        {
            return GetContentType(contentType) != ContentType.None;
        }

        /// <summary>
        /// Create writer
        /// </summary>
        /// <param name="logger"></param>
        public ZipFileWriter(ILogger<ZipFileWriter> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(string fileName, DateTime timestamp,
            IEnumerable<ReadOnlySequence<byte>> buffers,
            IReadOnlyDictionary<string, string?> metadata, IEventSchema? schema,
            string contentType, CancellationToken ct = default)
        {
            var file = _files.GetOrAdd(fileName + schema?.Id + contentType,
                _ => ZipFile.Create(fileName, schema?.Schema,
                    GetContentType(contentType)));
            file.Write(timestamp, buffers);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var file in _files.Values)
            {
                file.Dispose();
            }
        }

        /// <summary>
        /// The zip file being written
        /// </summary>
        internal sealed class ZipFile : IDisposable
        {
            /// <summary>
            /// Create zip file
            /// </summary>
            /// <param name="fileName"></param>
            /// <param name="stream"></param>
            /// <param name="schema"></param>
            /// <param name="leaveOpen"></param>
            /// <param name="contentType"></param>
            private ZipFile(string fileName, Stream stream, string? schema,
                bool leaveOpen, ContentType contentType)
            {
                _zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen);
                _contentType = contentType;
                _fileName = fileName;
                WriteSchema(schema);
            }

            /// <summary>
            /// Create file from file name
            /// </summary>
            /// <param name="fileName"></param>
            /// <param name="schema"></param>
            /// <param name="contentType"></param>
            /// <returns></returns>
            public static ZipFile Create(string fileName, string? schema,
                ContentType contentType)
            {
                var fs = new FileStream(fileName + ".zip", FileMode.OpenOrCreate);
                return new ZipFile(fileName, fs, schema, false, contentType);
            }

            /// <summary>
            /// Create file from stream
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="schema"></param>
            /// <param name="contentType"></param>
            /// <returns></returns>
            public static ZipFile CreateFromStream(Stream stream, string? schema,
                ContentType contentType)
            {
                return new ZipFile(string.Empty, stream, schema, true, contentType);
            }

            /// <summary>
            /// Write to file
            /// </summary>
            /// <param name="timestamp"></param>
            /// <param name="buffers"></param>
            /// <returns></returns>
            public void Write(DateTime timestamp, IEnumerable<ReadOnlySequence<byte>> buffers)
            {
                var entry = _zip.CreateEntry(timestamp +
                    (_contentType == ContentType.Binary ? ".bin" : ".json"));
                using var stream = entry.Open();
                foreach (var buffer in buffers)
                {
                    foreach (var memory in _contentType == ContentType.JsonGzip ?
                        GzipDecompressData(buffer) : buffer)
                    {
                        stream.Write(memory.Span);
                    }
                }
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _zip.Dispose();
            }

            /// <summary>
            /// Writes the schema.
            /// </summary>
            /// <param name="schema"></param>
            private void WriteSchema(string? schema)
            {
                if (schema is not null)
                {
                    var entry = _zip.CreateEntry("schema.json");
                    using var stream = entry.Open();
                    stream.Write(Encoding.UTF8.GetBytes(schema));
                }
            }

            private readonly string _fileName;
            private readonly ZipArchive _zip;
            private readonly ContentType _contentType;
        }

        internal enum ContentType
        {
            None,
            Json,
            JsonGzip,
            Binary
        }

        /// <summary>
        /// Return type
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        internal static ContentType GetContentType(string contentType)
        {
#pragma warning disable CA1308 // Normalize strings to uppercase
            switch (contentType.ToLowerInvariant())
            {
                case Encoders.ContentType.JsonGzip:
                    return ContentType.JsonGzip;
                case ContentMimeType.Json:
                case Encoders.ContentType.UaJson:
                case Encoders.ContentType.UaLegacyPublisher:
                case Encoders.ContentType.UaNonReversibleJson:
                    return ContentType.Json;
                case ContentMimeType.Binary:
                case Encoders.ContentType.Uadp:
                    return ContentType.Binary;
            }
#pragma warning restore CA1308 // Normalize strings to uppercase
            return ContentType.None;
        }




        /// <summary>
        /// Decompress
        /// </summary>
        /// <param name="compressedData"></param>
        /// <returns></returns>
        private static ReadOnlySequence<byte> GzipDecompressData(ReadOnlySequence<byte> compressedData)
        {
            using var compressedStream = new MemoryStream(compressedData.ToArray());
            using var deflateStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            return new ReadOnlySequence<byte>(deflateStream.ReadAsBuffer());
        }

        internal static readonly RecyclableMemoryStreamManager kStreams = new();
        private readonly ConcurrentDictionary<string, ZipFile> _files = new();
        private readonly ILogger _logger;
    }
}
