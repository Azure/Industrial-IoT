// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class ConsoleWriterTests
    {
        [Fact]
        public void PrettyPrintsJsonWithoutSerializerMetadata()
        {
            var result = ConsoleWriter.GetIndentedJson(
                new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("""{"value":[1,2]}""")));
            var json = Encoding.UTF8.GetString(result.Span);

            Assert.Contains("\n  \"value\"", json,
                StringComparison.Ordinal);
            using var document = JsonDocument.Parse(result);
            Assert.Equal(2, document.RootElement.GetProperty("value").GetArrayLength());
        }

        [Fact]
        public void InvalidJsonStillThrows()
        {
            Assert.ThrowsAny<JsonException>(() => ConsoleWriter.GetIndentedJson(
                new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{invalid}"))));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SupportsContentTypeReturnsConfiguredEnabledValue(bool enabled)
        {
            var writer = new ConsoleWriter(Options.Create(new ConsoleWriterOptions
            {
                Enabled = enabled
            }));

            var result = writer.SupportsContentType(ContentMimeType.Json);

            Assert.Equal(enabled, result);
        }

        [Fact]
        public async Task WriteAsyncPrettyPrintsJsonToInjectedStdoutAsync()
        {
            var writer = new ConsoleWriter(Options.Create(new ConsoleWriterOptions()));
            var stdout = new MemoryStream();
            SetStream(writer, "_stdout", stdout);
            var buffer = new ReadOnlySequence<byte>(
                Encoding.UTF8.GetBytes("""{"value":[1,2]}"""));

            await writer.WriteAsync("stdout", DateTimeOffset.UtcNow, [buffer],
                new Dictionary<string, string?>(), null, ContentMimeType.Json);

            var json = Encoding.UTF8.GetString(stdout.ToArray());
            Assert.Contains("\n  \"value\"", json, StringComparison.Ordinal);
            using var document = JsonDocument.Parse(json);
            Assert.Equal(2, document.RootElement.GetProperty("value").GetArrayLength());
        }

        [Fact]
        public async Task WriteAsyncWritesBinaryPayloadToInjectedStderrAsync()
        {
            var writer = new ConsoleWriter(Options.Create(new ConsoleWriterOptions()));
            var stderr = new MemoryStream();
            SetStream(writer, "_stderr", stderr);
            var payload = Encoding.UTF8.GetBytes("payload");

            await writer.WriteAsync("stderr", DateTimeOffset.UtcNow,
                [new ReadOnlySequence<byte>(payload)],
                new Dictionary<string, string?>(), null, "application/octet-stream");

            Assert.Equal(payload, stderr.ToArray());
        }

        [Fact]
        public async Task WriteAsyncIgnoresFilesOtherThanStdoutAndStderrAsync()
        {
            var writer = new ConsoleWriter(Options.Create(new ConsoleWriterOptions()));
            var stdout = new MemoryStream();
            SetStream(writer, "_stdout", stdout);

            await writer.WriteAsync("file.json", DateTimeOffset.UtcNow,
                [new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{}"))],
                new Dictionary<string, string?>(), null, ContentMimeType.Json);

            Assert.Empty(stdout.ToArray());
        }

        private static void SetStream(ConsoleWriter writer, string fieldName, Stream stream)
        {
            var field = typeof(ConsoleWriter).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(field);
            field!.SetValue(writer, stream);
        }
    }
}
