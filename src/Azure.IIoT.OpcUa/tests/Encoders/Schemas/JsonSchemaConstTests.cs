// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Json
{
    using System;
    using System.Collections.Generic;
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

        [Fact]
        public void WritesDraft7ObjectArrayAndNumericConstraints()
        {
            var schema = new JsonSchema
            {
                SchemaVersion = JsonSchemaVersion.Draft7,
                Id = new UriOrFragment("root"),
                Title = "Sample",
                Description = "A sample schema",
                Comment = "comment",
                Examples = ["one", "two"],
                Types = [SchemaType.Object, SchemaType.Null],
                Properties = new Dictionary<string, JsonSchema>
                {
                    ["name"] = new() { Type = SchemaType.String },
                    ["age"] = new() { Type = SchemaType.Integer }
                },
                Required = ["name"],
                MinProperties = 1,
                MaxProperties = 3,
                PropertyNames = new JsonSchema { Title = "property-name" },
                AdditionalProperties = new JsonSchema { Allowed = false },
                Dependencies = new Dictionary<string, Dependency>
                {
                    ["name"] = new(new List<string> { "age", "kind" }),
                    ["kind"] = new(new JsonSchema { Type = SchemaType.String })
                },
                AllOf = [new JsonSchema { Type = SchemaType.Object }],
                AnyOf = [new JsonSchema { Type = SchemaType.String }],
                OneOf = [new JsonSchema { Type = SchemaType.Integer }],
                Not = new JsonSchema { Allowed = true },
                Items = [new JsonSchema { Type = SchemaType.String }],
                AdditionalItems = new JsonSchema { Allowed = false },
                Contains = ["item"],
                MinItems = 1,
                MaxItems = 5,
                UniqueItems = true,
                Minimum = Limit.From(1, exclusive: true),
                Maximum = Limit.From(10, exclusive: true),
                MultipleOf = Const.From(2),
                Format = "int32",
                Default = Const.From(4),
                Const = Const.From(6),
                ReadOnly = true,
                MinLength = 2,
                MaxLength = 8,
                Pattern = "^[a-z]+$",
                Definitions = new Dictionary<string, JsonSchema>
                {
                    ["child"] = new() { Reference = new UriOrFragment("name") }
                }
            };

            using var document = JsonDocument.Parse(JsonSchemaWriter.SerializeAsString(schema));
            var root = document.RootElement;

            Assert.Equal(JsonSchemaVersion.Draft7, root.GetProperty("$schema").GetString());
            Assert.Equal("#/definitions/root", root.GetProperty("$id").GetString());
            Assert.Equal("Sample", root.GetProperty("title").GetString());
            Assert.Equal("A sample schema", root.GetProperty("description").GetString());
            Assert.Equal("comment", root.GetProperty("$comment").GetString());
            Assert.Equal("one", root.GetProperty("examples")[0].GetString());
            Assert.Equal("object", root.GetProperty("type")[0].GetString());
            Assert.Equal("null", root.GetProperty("type")[1].GetString());
            Assert.Equal("string", root.GetProperty("properties")
                .GetProperty("name").GetProperty("type").GetString());
            Assert.Equal("name", root.GetProperty("required")[0].GetString());
            Assert.Equal(1, root.GetProperty("minProperties").GetInt32());
            Assert.Equal(3, root.GetProperty("maxProperties").GetInt32());
            Assert.Equal("property-name", root.GetProperty("propertyNames")
                .GetProperty("title").GetString());
            Assert.False(root.GetProperty("additionalProperties").GetBoolean());
            Assert.Equal("age", root.GetProperty("dependencies")
                .GetProperty("name")[0].GetString());
            Assert.Equal("string", root.GetProperty("dependencies")
                .GetProperty("kind").GetProperty("type").GetString());
            Assert.Equal("object", root.GetProperty("allOf")[0]
                .GetProperty("type").GetString());
            Assert.Equal("string", root.GetProperty("anyOf")[0]
                .GetProperty("type").GetString());
            Assert.Equal("integer", root.GetProperty("oneOf")[0]
                .GetProperty("type").GetString());
            Assert.Equal(true, root.GetProperty("not").GetBoolean());
            Assert.Equal("string", root.GetProperty("items")
                .GetProperty("type").GetString());
            Assert.False(root.GetProperty("additionalItems").GetBoolean());
            Assert.Equal("item", root.GetProperty("contains")[0].GetString());
            Assert.Equal(true, root.GetProperty("uniqueItems").GetBoolean());
            Assert.Equal(1, root.GetProperty("exclusiveMinimum").GetInt32());
            Assert.Equal(10, root.GetProperty("exclusiveMaximum").GetInt32());
            Assert.Equal(2, root.GetProperty("multipleOf").GetInt32());
            Assert.Equal("int32", root.GetProperty("format").GetString());
            Assert.Equal(4, root.GetProperty("default").GetInt32());
            Assert.Equal(6, root.GetProperty("const").GetInt32());
            Assert.Equal(true, root.GetProperty("readOnly").GetBoolean());
            Assert.Equal("^[a-z]+$", root.GetProperty("pattern").GetString());
            Assert.Equal("#/definitions/name", root.GetProperty("definitions")
                .GetProperty("child").GetProperty("$ref").GetString());
        }

        [Fact]
        public void WritesDraft4SpecificKeywords()
        {
            var schema = new JsonSchema
            {
                SchemaVersion = JsonSchemaVersion.Draft4,
                Id = new UriOrFragment("root"),
                Minimum = Limit.From(1, exclusive: true),
                Maximum = Limit.From(10, exclusive: true),
                Not = new JsonSchema { Allowed = false },
                AdditionalProperties = new JsonSchema { Allowed = false }
            };

            using var document = JsonDocument.Parse(JsonSchemaWriter.SerializeAsString(schema));
            var root = document.RootElement;

            Assert.Equal("#/definitions/root", root.GetProperty("id").GetString());
            Assert.Equal(1, root.GetProperty("minimum").GetInt32());
            Assert.Equal(true, root.GetProperty("exclusiveMinimum").GetBoolean());
            Assert.Equal(10, root.GetProperty("maximum").GetInt32());
            Assert.Equal(true, root.GetProperty("exclusiveMaximum").GetBoolean());
            Assert.Equal(JsonValueKind.Object, root.GetProperty("not").ValueKind);
            Assert.False(root.GetProperty("additionalProperties").GetBoolean());
        }

        [Fact]
        public void WritesDraft202012DefsAndReferences()
        {
            var schema = new JsonSchema
            {
                SchemaVersion = JsonSchemaVersion.Draft202012,
                Reference = UriOrFragment.Self,
                Definitions = new Dictionary<string, JsonSchema>
                {
                    ["child"] = new() { Type = SchemaType.String }
                }
            };

            using var document = JsonDocument.Parse(JsonSchemaWriter.SerializeAsString(schema));
            var root = document.RootElement;

            Assert.Equal("#", root.GetProperty("$ref").GetString());
            Assert.Equal("string", root.GetProperty("$defs")
                .GetProperty("child").GetProperty("type").GetString());
        }

        [Fact]
        public void WritesNamespaceReferencesVerbatim()
        {
            var schema = new JsonSchema
            {
                Reference = new UriOrFragment("field name", "https://example.test/schema")
            };

            using var document = JsonDocument.Parse(JsonSchemaWriter.SerializeAsString(schema));

            Assert.Equal("https://example.test/schema#field%20name",
                document.RootElement.GetProperty("$ref").GetString());
        }
    }
}
