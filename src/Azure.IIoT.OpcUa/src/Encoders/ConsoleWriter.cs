// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Storage;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Debug output writer
    /// </summary>
    public sealed class ConsoleWriter : IFileWriter, IDisposable
    {
        /// <inheritdoc/>
        public bool SupportsContentType(string contentType)
        {
            return _options.Value.Enabled;
        }

        /// <summary>
        /// Create writer
        /// </summary>
        /// <param name="options"></param>
        public ConsoleWriter(IOptions<ConsoleWriterOptions> options)
        {
            _options = options;
        }

        /// <inheritdoc/>
        public async ValueTask WriteAsync(string fileName, DateTimeOffset timestamp,
            IEnumerable<ReadOnlySequence<byte>> buffers,
            IReadOnlyDictionary<string, string?> metadata, IEventSchema? schema,
            string contentType, CancellationToken ct = default)
        {
            Stream fs;
            if (fileName.Contains("stdout", StringComparison.InvariantCulture))
            {
                fs = _stdout ??= Console.OpenStandardOutput();
            }
            else if (fileName.Contains("stderr", StringComparison.InvariantCulture))
            {
                fs = _stderr ??= Console.OpenStandardError();
            }
            else
            {
                return;
            }

            switch (contentType)
            {
                case ContentMimeType.Json:
                    foreach (var buffer in buffers)
                    {
                        await fs.WriteAsync(GetIndentedJson(buffer), ct).ConfigureAwait(false);
                    }
                    break;
                default:
                    foreach (var buffer in buffers)
                    {
                        foreach (var memory in buffer)
                        {
                            await fs.WriteAsync(memory, ct).ConfigureAwait(false);
                        }
                    }
                    break;
            }

        }

        internal static ReadOnlyMemory<byte> GetIndentedJson(ReadOnlySequence<byte> buffer)
        {
            using var document = JsonDocument.Parse(buffer);
            var output = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(output,
                new JsonWriterOptions { Indented = true }))
            {
                document.RootElement.WriteTo(writer);
            }
            return output.WrittenMemory.ToArray();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _stdout?.Dispose();
            _stderr?.Dispose();
        }

        private readonly IOptions<ConsoleWriterOptions> _options;
        private Stream? _stdout;
        private Stream? _stderr;
    }
}
