// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Json
{
    using System;
    using System.Text.Json;
    using Xunit;

    public sealed class JsonSchemaConstTests
    {
        [Fact]
        public void WritesConstAndLimitsWithoutSerializerMetadata()
        {
            var schema = new JsonSchema
            {
                Const = Const.From("value"),
                Minimum = Limit.From(-1),
                Maximum = Limit.From(12.5m),
                Default = Const.From(Guid.Parse("9f5f0f43-4dfd-4b3e-8eb5-b642cabd29a4"))
            };

            var json = JsonSchemaWriter.SerializeAsString(schema);
            using var document = JsonDocument.Parse(json);

            Assert.Equal("value", document.RootElement.GetProperty("const").GetString());
            Assert.Equal(-1, document.RootElement.GetProperty("minimum").GetInt32());
            Assert.Equal(12.5m, document.RootElement.GetProperty("maximum").GetDecimal());
            Assert.Equal("9f5f0f43-4dfd-4b3e-8eb5-b642cabd29a4",
                document.RootElement.GetProperty("default").GetString());
        }

        [Fact]
        public void RejectsUnsupportedConstType()
        {
            var schema = new JsonSchema { Const = Const.From(new Version(1, 0)) };

            Assert.Throws<NotSupportedException>(() => JsonSchemaWriter.SerializeAsString(schema));
        }
    }
}
