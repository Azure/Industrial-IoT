// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Avro.File;
    using Avro.IO;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Storage;
    using Microsoft.Extensions.Logging;
    using Microsoft.IO;
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Write avro files
    /// </summary>
    public sealed class AvroFileWriter : IFileWriter, IDisposable
    {
        /// <inheritdoc/>
        public bool SupportsContentType(string contentType)
        {
            return
                contentType.Equals(ContentType.Avro,
                    StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals(ContentType.AvroGzip,
                    StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Create writer
        /// </summary>
        /// <param name="logger"></param>
        public AvroFileWriter(ILogger<AvroFileWriter> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(string fileName, DateTime timestamp,
            IEnumerable<ReadOnlySequence<byte>> buffers,
            IReadOnlyDictionary<string, string?>? metadata, IEventSchema? schema,
            string contentType, CancellationToken ct = default)
        {
            if (schema?.Id != null)
            {
                fileName = fileName.SanitizeFileName();
                var file = _files.GetOrAdd(fileName + schema.Id + contentType,
                    _ => AvroFile.Create(fileName, schema.Schema, metadata, _logger,
                        contentType.Equals(ContentType.AvroGzip,
                            StringComparison.OrdinalIgnoreCase)));
                file.Write(buffers);
            }
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
        /// The avro file being written
        /// </summary>
        internal sealed class AvroFile : IDisposable
        {
            /// <summary>
            /// Create avro file
            /// </summary>
            /// <param name="fileName"></param>
            /// <param name="stream"></param>
            /// <param name="schema"></param>
            /// <param name="metadata"></param>
            /// <param name="logger"></param>
            /// <param name="leaveOpen"></param>
            /// <param name="isGzip"></param>
            private AvroFile(string fileName, Stream stream, string schema,
                IReadOnlyDictionary<string, string?>? metadata, ILogger logger,
                bool leaveOpen, bool isGzip)
            {
                _logger = logger;
                _leaveOpen = leaveOpen;
                _isGzip = isGzip;
                _codec = Codec.CreateCodec(Codec.Type.Deflate);
                _fileName = fileName;
                _stream = stream;
                _encoder = new BinaryEncoder(_stream);
                _blockStream = kStreams.GetStream();
                _compressedBlockStream = kStreams.GetStream();
                _syncMarker = new byte[16];
#pragma warning disable CA5394 // Do not use insecure randomness
                Random.Shared.NextBytes(_syncMarker);
#pragma warning restore CA5394 // Do not use insecure randomness

                // Write header
                _encoder.WriteFixed(DataFileConstants.Magic);
                WriteMetaData(schema, metadata?
                    .Where(kv => !IsReservedMeta(kv.Key) && kv.Value != null)
                    .ToDictionary(kv => kv.Key, kv => Encoding.UTF8.GetBytes(kv.Value!)));
                _encoder.WriteFixed(_syncMarker);
            }

            /// <summary>
            /// Create file from file name
            /// </summary>
            /// <param name="fileName"></param>
            /// <param name="schema"></param>
            /// <param name="metadata"></param>
            /// <param name="logger"></param>
            /// <param name="isGzip"></param>
            /// <returns></returns>
            public static AvroFile Create(string fileName, string schema,
                IReadOnlyDictionary<string, string?>? metadata, ILogger logger,
                bool isGzip = false)
            {
                fileName = fileName.ReplaceLineEndings();
                var fs = new FileStream(fileName + ".avro", FileMode.OpenOrCreate);
                return new AvroFile(fileName, fs, schema, metadata, logger, false, isGzip);
            }

            /// <summary>
            /// Create file from stream
            /// </summary>
            /// <param name="stream"></param>
            /// <param name="schema"></param>
            /// <param name="metadata"></param>
            /// <param name="logger"></param>
            /// <param name="isGzip"></param>
            /// <returns></returns>
            public static AvroFile CreateFromStream(Stream stream, string schema,
                IReadOnlyDictionary<string, string?>? metadata, ILogger logger,
                bool isGzip = false)
            {
                return new AvroFile(string.Empty, stream, schema, metadata,
                    logger, true, isGzip);
            }

            /// <summary>
            /// Write to file
            /// </summary>
            /// <param name="buffers"></param>
            /// <returns></returns>
            public void Write(IEnumerable<ReadOnlySequence<byte>> buffers)
            {
                foreach (var buffer in buffers)
                {
                    foreach (var memory in _isGzip ? buffer.GzipDecompress() : buffer)
                    {
                        _blockStream.Write(memory.Span);
                    }
                    _blockCount++;

                    if (_blockStream.Position >= kBlockSize)
                    {
                        WriteBlocks();
                    }
                }
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                WriteBlocks();

                _stream.Flush();
                _blockStream.Dispose();

                _compressedBlockStream.Dispose();

                if (!_leaveOpen)
                {
                    _stream.Dispose();
                }
            }

            /// <summary>
            /// Writes the blocks.
            /// </summary>
            private void WriteBlocks()
            {
                if (_blockCount <= 0)
                {
                    return;
                }

                // write count
                _encoder.WriteLong(_blockCount);

                // write data
                _codec.Compress(_blockStream, _compressedBlockStream);
                _encoder.WriteBytes(_compressedBlockStream.GetBuffer(),
                    0, (int)_compressedBlockStream.Length);

                // write sync marker
                _encoder.WriteFixed(_syncMarker);
                _encoder.Flush();

                _logger.LogInformation(
                    "{BlockCount} blocks ({Size} bytes) written to {FileName}...",
                    _blockCount, _compressedBlockStream.Length, _fileName);

                // reset / re-init block
                _blockCount = 0;
                _blockStream.SetLength(0);
            }

            /// <summary>
            /// Writes the meta data.
            /// </summary>
            /// <param name="schema"></param>
            /// <param name="metadata"></param>
            private void WriteMetaData(string schema,
                Dictionary<string, byte[]>? metadata)
            {
                metadata ??= new Dictionary<string, byte[]>();
                metadata.Add(DataFileConstants.MetaDataCodec,
                    Encoding.UTF8.GetBytes(_codec.GetName()));
                metadata.Add(DataFileConstants.MetaDataSchema,
                    Encoding.UTF8.GetBytes(schema));

                // write metadata
                var size = metadata.Count;

                _encoder.WriteMapStart();
                _encoder.SetItemCount(size);
                foreach (var metaPair in metadata)
                {
                    _encoder.WriteString(metaPair.Key);
                    _encoder.WriteBytes(metaPair.Value);
                }
                _encoder.WriteMapEnd();
            }

            /// <inheritdoc/>
            private static bool IsReservedMeta(string key)
            {
                return key.StartsWith(
                    DataFileConstants.MetaDataReserved, StringComparison.Ordinal);
            }

            private int _blockCount;
            private readonly Codec _codec;
            private readonly string _fileName;
            private readonly Stream _stream;
            private readonly MemoryStream _blockStream;
            private readonly MemoryStream _compressedBlockStream;
            private readonly byte[] _syncMarker;
            private readonly BinaryEncoder _encoder;
            private readonly ILogger _logger;
            private readonly bool _leaveOpen;
            private readonly bool _isGzip;
        }

        private const int kBlockSize = 16000;
        internal static readonly RecyclableMemoryStreamManager kStreams = new();
        private readonly ConcurrentDictionary<string, AvroFile> _files = new();
        private readonly ILogger _logger;
    }
}
