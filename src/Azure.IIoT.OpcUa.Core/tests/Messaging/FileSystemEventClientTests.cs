// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
    using Azure.IIoT.OpcUa.Core.Storage;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class FileSystemEventClientTests
    {
        [Fact]
        public async Task SendPreservesFileWriterContractAsync()
        {
            var writer = new CapturingWriter();
            var root = Path.Combine(AppContext.BaseDirectory, "events");
            var client = new FileSystemEventClient(Options.Create(
                new FileSystemEventClientOptions { OutputFolder = root }), [writer]);
            using var @event = client.CreateEvent();
            using var cancellation = new CancellationTokenSource();
            var timestamp = new DateTimeOffset(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);
            var schema = new TestEventSchema();

            await @event
                .SetTopic("factory/line1")
                .SetTimestamp(timestamp)
                .SetContentType("application/json")
                .SetContentEncoding("utf-8")
                .SetRetain(true)
                .SetTtl(TimeSpan.FromMinutes(1))
                .AddProperty("tenant", "factory-a")
                .AsCloudEvent(new CloudEventHeader
                {
                    Id = "event-id",
                    Source = new Uri("urn:test"),
                    Type = "test.event",
                    Subject = "subject",
                    DataContentType = "application/json"
                })
                .SetSchema(schema)
                .AddBuffers([new ReadOnlySequence<byte>(
                    Encoding.UTF8.GetBytes("payload"))])
                .SendAsync(cancellation.Token);

            Assert.Equal(Path.Combine(Path.GetFullPath(root), "factory_line1"),
                writer.FileName);
            Assert.Equal(timestamp, writer.Timestamp);
            Assert.Equal("payload", Encoding.UTF8.GetString(writer.Payload));
            Assert.Equal("application/json", writer.ContentType);
            Assert.Same(schema, writer.Schema);
            Assert.Equal(cancellation.Token, writer.CancellationToken);
            Assert.Equal("application/json", writer.Metadata["ContentType"]);
            Assert.Equal("utf-8", writer.Metadata["ContentEncoding"]);
            Assert.Equal("true", writer.Metadata["Retain"]);
            Assert.Equal(TimeSpan.FromMinutes(1).ToString(), writer.Metadata["TTL"]);
            Assert.Equal("factory-a", writer.Metadata["tenant"]);
            Assert.Equal("1.0", writer.Metadata["ce:specversion"]);
            Assert.Equal("event-id", writer.Metadata["ce:id"]);
            Assert.Equal("urn:test", writer.Metadata["ce:source"]);
            Assert.Equal("test.event", writer.Metadata["ce:type"]);
            Assert.Equal("subject", writer.Metadata["ce:subject"]);
            Assert.Equal("application/json", writer.Metadata["ce:datacontenttype"]);
        }

        private sealed class CapturingWriter : IFileWriter
        {
            public string? FileName { get; private set; }
            public DateTimeOffset Timestamp { get; private set; }
            public byte[] Payload { get; private set; } = [];
            public Dictionary<string, string?> Metadata { get; private set; } = [];
            public IEventSchema? Schema { get; private set; }
            public string? ContentType { get; private set; }
            public CancellationToken CancellationToken { get; private set; }

            public bool SupportsContentType(string contentType)
            {
                return true;
            }

            public ValueTask WriteAsync(string fileName, DateTimeOffset timestamp,
                IEnumerable<ReadOnlySequence<byte>> buffers,
                IReadOnlyDictionary<string, string?> metadata, IEventSchema? schema,
                string contentType, CancellationToken ct = default)
            {
                FileName = fileName;
                Timestamp = timestamp;
                Payload = buffers.SelectMany(buffer => buffer.ToArray()).ToArray();
                Metadata = new Dictionary<string, string?>(metadata);
                Schema = schema;
                ContentType = contentType;
                CancellationToken = ct;
                return ValueTask.CompletedTask;
            }
        }

        private sealed class TestEventSchema : IEventSchema
        {
            public string Type => "application/schema+json";
            public string Name => "test";
            public ulong Version => 1;
            public string Schema => "{}";
            public string Id => "urn:test:schema";
        }
    }
}
