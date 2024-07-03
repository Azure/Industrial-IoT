// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Furly;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Storage;
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Write messages as zip files
    /// </summary>
    public sealed class ZipFileWriter : IFileWriter, IDisposable
    {
        /// <inheritdoc/>
        public bool SupportsContentType(string contentType)
        {
            return GetContentType(contentType) != ContentType.None;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(string fileName, DateTimeOffset timestamp,
            IEnumerable<ReadOnlySequence<byte>> buffers,
            IReadOnlyDictionary<string, string?> metadata, IEventSchema? schema,
            string contentType, CancellationToken ct = default)
        {
            fileName = fileName.SanitizeFileName();
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
            /// <param name="stream"></param>
            /// <param name="schema"></param>
            /// <param name="leaveOpen"></param>
            /// <param name="contentType"></param>
            private ZipFile(Stream stream, string? schema, bool leaveOpen,
                ContentType contentType)
            {
                _zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen);
                _contentType = contentType;
                WriteHeader(schema, contentType);
                _suffix = Suffix(contentType);
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
                fileName = fileName.ReplaceLineEndings();
                var fs = new FileStream(fileName + FileSuffix, FileMode.OpenOrCreate);
                return new ZipFile(fs, schema, false, contentType);
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
                return new ZipFile(stream, schema, true, contentType);
            }

            /// <summary>
            /// Write to file
            /// </summary>
            /// <param name="timestamp"></param>
            /// <param name="buffers"></param>
            /// <returns></returns>
            public void Write(DateTimeOffset timestamp, IEnumerable<ReadOnlySequence<byte>> buffers)
            {
                foreach (var buffer in buffers)
                {
                    var entry = _zip.CreateEntry(Interlocked.Increment(ref _sequenceNumber) +
                        _suffix, CompressionLevel.Optimal);
                    entry.LastWriteTime = timestamp;
                    using var stream = entry.Open();
                    foreach (var memory in _contentType == ContentType.JsonGzip ?
                        buffer.GzipDecompress() : buffer)
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
            /// <param name="contentType"></param>
            private void WriteHeader(string? schema, ContentType contentType)
            {
                if (schema is not null)
                {
                    var entry = _zip.CreateEntry(MessageSchemaFile, CompressionLevel.Optimal);
                    using var stream = entry.Open();
                    stream.Write(Encoding.UTF8.GetBytes(schema));
                }
                {
                    var entry = _zip.CreateEntry(ContentTypeFile, CompressionLevel.NoCompression);
                    using var stream = entry.Open();
                    stream.WriteByte((byte)contentType);
                }
            }

            private int _sequenceNumber;
            private readonly string _suffix;
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
        /// Get suffix for content type
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        internal static string Suffix(ContentType contentType)
        {
            return contentType switch
            {
                ContentType.Json or ContentType.JsonGzip => ".json",
                ContentType.Binary => ".bin",
                _ => throw new FormatException("Invalid content type")
            };
        }

        internal const string FileSuffix = ".zip";
        internal const string MessageSchemaFile = "schema.json";
        internal const string ContentTypeFile = "content-type";
        private readonly ConcurrentDictionary<string, ZipFile> _files = new();
    }
}
