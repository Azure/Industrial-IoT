// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Avro;
    using Avro.File;
    using Avro.Generic;
    using Avro.IO;
    using Furly;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Storage;
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Write avro files
    /// </summary>
    public sealed class AvroFileWriter : IFileWriter, IDisposable
    {
        /// <inheritdoc/>
        public string ContentType => Encoders.ContentType.Avro;

        /// <inheritdoc/>
        public ValueTask WriteAsync(string fileName, DateTime timestamp,
            IEnumerable<ReadOnlySequence<byte>> buffers,
            IReadOnlyDictionary<string, string?> metadata, IEventSchema? schema,
            CancellationToken ct = default)
        {
            if (schema?.Id == null)
            {
                return ValueTask.CompletedTask;
            }
            var file = _files.GetOrAdd(fileName + schema.Id,
                _ => new AvroFile(fileName, schema, metadata));
            return file.WriteAsync(buffers);
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
        private sealed class AvroFile : DatumWriter<ReadOnlySequence<byte>>,
            IDisposable
        {
            /// <inheritdoc/>
            public Schema Schema { get; }

            /// <summary>
            /// Generate avro file
            /// </summary>
            /// <param name="fileName"></param>
            /// <param name="schema"></param>
            /// <param name="metadata"></param>
            public AvroFile(string fileName, IEventSchema schema,
                IReadOnlyDictionary<string, string?> metadata)
            {
                Schema = Schema.Parse(schema.Schema);

                _writer = DataFileWriter<ReadOnlySequence<byte>>.OpenWriter(
                    this, fileName + ".avro");
                foreach (var item in metadata)
                {
                    _writer.SetMeta(item.Key, item.Value);
                }
            }

            /// <summary>
            /// Write to file
            /// </summary>
            /// <param name="buffers"></param>
            /// <returns></returns>
            public ValueTask WriteAsync(IEnumerable<ReadOnlySequence<byte>> buffers)
            {
                foreach (var buffer in buffers)
                {
                    _writer.Append(buffer);
                }
                _writer.Flush();
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc/>
            public void Write(ReadOnlySequence<byte> datum, Encoder encoder)
            {
                Debug.Assert(encoder is BinaryEncoder);
                encoder.WriteFixed(datum.ToArray());
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _writer.Dispose();
            }

            private readonly IFileWriter<ReadOnlySequence<byte>> _writer;
        }

        private readonly ConcurrentDictionary<string, AvroFile> _files = new();
    }
}
