// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using System;
    using System.Buffers;
    using System.Text;
    using System.Text.Json;
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
    }
}
