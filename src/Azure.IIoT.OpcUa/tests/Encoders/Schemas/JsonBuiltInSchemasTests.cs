// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Json
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public sealed class JsonBuiltInSchemasTests
    {
        [Theory]
        [MemberData(nameof(GetBuiltInTypes))]
        public void GetSchemaForBuiltInTypeCreatesDefinitionAndReturnsReference(
            BuiltInType builtInType)
        {
            var definitions = new Dictionary<string, JsonSchema>();
            var schemas = new JsonBuiltInSchemas(reversibleEncoding: true,
                useUriEncoding: false, definitions);

            var schema = schemas.GetSchemaForBuiltInType(builtInType);

            Assert.NotNull(schema.Reference);
            var definitionName = SchemaUtils.NamespaceZeroName + "." + builtInType;
            Assert.Equal(definitionName, schema.Reference.Fragment);
            Assert.Contains(definitionName, definitions.Keys);
            var definition = definitions[definitionName];
            Assert.Equal("OPC UA built in type " + builtInType, definition.Title);
        }

        [Theory]
        [InlineData(BuiltInType.Boolean, SchemaType.Boolean, null)]
        [InlineData(BuiltInType.SByte, SchemaType.Integer, "int8")]
        [InlineData(BuiltInType.Byte, SchemaType.Integer, "byte")]
        [InlineData(BuiltInType.Int16, SchemaType.Integer, "int16")]
        [InlineData(BuiltInType.UInt16, SchemaType.Integer, "uint16")]
        [InlineData(BuiltInType.Int32, SchemaType.Integer, "int32")]
        [InlineData(BuiltInType.UInt32, SchemaType.Integer, "uint32")]
        [InlineData(BuiltInType.Int64, SchemaType.String, "int64")]
        [InlineData(BuiltInType.UInt64, SchemaType.String, "uint64")]
        [InlineData(BuiltInType.Float, SchemaType.Number, "float")]
        [InlineData(BuiltInType.Double, SchemaType.Number, "double")]
        [InlineData(BuiltInType.String, SchemaType.String, null)]
        [InlineData(BuiltInType.DateTime, SchemaType.String, "date-time")]
        [InlineData(BuiltInType.Guid, SchemaType.String, "uuid")]
        [InlineData(BuiltInType.ByteString, SchemaType.String, "byte")]
        [InlineData(BuiltInType.XmlElement, SchemaType.String, "xmlelement")]
        public void PrimitiveBuiltInTypesHaveExpectedJsonSchema(BuiltInType builtInType,
            SchemaType expectedType, string? expectedFormat)
        {
            var definitions = new Dictionary<string, JsonSchema>();
            var schemas = new JsonBuiltInSchemas(reversibleEncoding: false,
                useUriEncoding: false, definitions);

            schemas.GetSchemaForBuiltInType(builtInType);

            var definition = definitions[SchemaUtils.NamespaceZeroName + "." + builtInType];
            Assert.Equal(expectedType, definition.Type);
            Assert.Equal(expectedFormat, definition.Format);
        }

        [Fact]
        public void ByteCollectionsUseByteStringSchema()
        {
            var definitions = new Dictionary<string, JsonSchema>();
            var schemas = new JsonBuiltInSchemas(reversibleEncoding: false,
                useUriEncoding: false, definitions);

            var schema = schemas.GetSchemaForBuiltInType(BuiltInType.Byte,
                SchemaRank.Collection);

            Assert.Equal(SchemaUtils.NamespaceZeroName + "." + BuiltInType.ByteString,
                schema.Reference?.Fragment);
            Assert.DoesNotContain(SchemaUtils.NamespaceZeroName + "." + BuiltInType.Byte,
                definitions.Keys);
        }

        [Fact]
        public void CollectionRankWrapsScalarSchemaAsArray()
        {
            var schemas = new JsonBuiltInSchemas(reversibleEncoding: false,
                useUriEncoding: false, definitions: null);

            var schema = schemas.GetSchemaForBuiltInType(BuiltInType.String,
                SchemaRank.Collection);

            Assert.Equal(SchemaType.Array, schema.Type);
            Assert.NotNull(schema.Items);
            var item = Assert.Single(schema.Items!);
            Assert.Equal(SchemaUtils.NamespaceZeroName + "." + BuiltInType.String,
                item.Reference?.Fragment);
        }

        [Fact]
        public void MatrixRankUsesInlineNonReversibleVariantSchema()
        {
            var schemas = new JsonBuiltInSchemas(reversibleEncoding: false,
                useUriEncoding: false, definitions: null);

            var schema = schemas.GetSchemaForBuiltInType(BuiltInType.String,
                SchemaRank.Matrix);

            Assert.Null(schema.Reference);
            Assert.Contains(SchemaType.Array, schema.Types);
            Assert.Contains(SchemaType.Object, schema.Types);
            Assert.Contains(SchemaType.Null, schema.Types);
        }

        [Theory]
        [InlineData(false, false, BuiltInType.NodeId, SchemaType.Object, null)]
        [InlineData(false, true, BuiltInType.NodeId, SchemaType.String, "opcuaNodeId")]
        [InlineData(false, false, BuiltInType.ExpandedNodeId, SchemaType.Object, null)]
        [InlineData(false, true, BuiltInType.ExpandedNodeId, SchemaType.String, "opcuaExpandedNodeId")]
        [InlineData(false, false, BuiltInType.QualifiedName, SchemaType.Object, null)]
        [InlineData(false, true, BuiltInType.QualifiedName, SchemaType.String, "opcuaQualifiedName")]
        [InlineData(false, false, BuiltInType.LocalizedText, SchemaType.String, null)]
        [InlineData(true, false, BuiltInType.LocalizedText, SchemaType.Object, null)]
        [InlineData(false, false, BuiltInType.StatusCode, SchemaType.Object, null)]
        [InlineData(true, false, BuiltInType.StatusCode, SchemaType.Integer, "uint32")]
        [InlineData(false, false, BuiltInType.Enumeration, SchemaType.String, null)]
        [InlineData(true, false, BuiltInType.Enumeration, SchemaType.Integer, "int32")]
        public void EncodingOptionsChangeSpecialBuiltInSchemas(bool reversible,
            bool useUriEncoding, BuiltInType builtInType, SchemaType expectedType,
            string? expectedFormat)
        {
            var definitions = new Dictionary<string, JsonSchema>();
            var schemas = new JsonBuiltInSchemas(reversible, useUriEncoding, definitions);

            schemas.GetSchemaForBuiltInType(builtInType);

            var definition = definitions[SchemaUtils.NamespaceZeroName + "." + builtInType];
            Assert.Equal(expectedType, definition.Type);
            Assert.Equal(expectedFormat, definition.Format);
        }

        [Fact]
        public void ReversibleVariantSchemaIncludesTypeBodyAndDimensions()
        {
            var definitions = new Dictionary<string, JsonSchema>();
            var schemas = new JsonBuiltInSchemas(reversibleEncoding: true,
                useUriEncoding: false, definitions);

            schemas.GetSchemaForBuiltInType(BuiltInType.Variant);

            var definition = definitions[SchemaUtils.NamespaceZeroName + "." +
                BuiltInType.Variant];
            Assert.Equal(SchemaType.Object, definition.Type);
            Assert.NotNull(definition.Properties);
            var properties = definition.Properties!;
            Assert.Contains("Type", properties.Keys);
            Assert.Contains("Body", properties.Keys);
            Assert.Contains("Dimensions", properties.Keys);
            Assert.Equal(false, definition.AdditionalProperties?.Allowed);
        }

        [Fact]
        public void NonReversibleVariantSchemaAllowsAnyJsonValue()
        {
            var definitions = new Dictionary<string, JsonSchema>();
            var schemas = new JsonBuiltInSchemas(reversibleEncoding: false,
                useUriEncoding: false, definitions);

            schemas.GetSchemaForBuiltInType(BuiltInType.Variant);

            var definition = definitions[SchemaUtils.NamespaceZeroName + "." +
                BuiltInType.Variant];
            Assert.Contains(SchemaType.Number, definition.Types);
            Assert.Contains(SchemaType.Null, definition.Types);
            Assert.Contains(SchemaType.Object, definition.Types);
            Assert.Contains(SchemaType.Array, definition.Types);
            Assert.Contains(SchemaType.String, definition.Types);
            Assert.Contains(SchemaType.Integer, definition.Types);
            Assert.Contains(SchemaType.Boolean, definition.Types);
        }

        [Theory]
        [InlineData(DataSetFieldContentFlags.RawData, false)]
        [InlineData((DataSetFieldContentFlags)0, true)]
        [InlineData(DataSetFieldContentFlags.StatusCode, false)]
        public void FieldContentFlagsSelectVariantEncoding(
            DataSetFieldContentFlags flags, bool expectedTypedVariant)
        {
            var definitions = new Dictionary<string, JsonSchema>();
            var schemas = new JsonBuiltInSchemas(flags, definitions);
            var valueSchema = schemas.GetSchemaForBuiltInType(BuiltInType.Int32);

            var field = schemas.GetSchemaForDataSetField("ns", asDataValue: false,
                valueSchema, BuiltInType.Int32);

            Assert.Equal(expectedTypedVariant, field.Reference?.Fragment == "ns.Int32Variant");
        }

        [Fact]
        public void DataValueFieldSchemaWrapsValueAndStatusMembers()
        {
            var definitions = new Dictionary<string, JsonSchema>();
            var schemas = new JsonBuiltInSchemas(reversibleEncoding: false,
                useUriEncoding: false, definitions);
            var valueSchema = schemas.GetSchemaForBuiltInType(BuiltInType.Double);

            var schema = schemas.GetSchemaForDataSetField("ns", asDataValue: true,
                valueSchema, BuiltInType.Double);

            Assert.Equal("ns.DoubleDataValue", schema.Reference?.Fragment);
            var definition = definitions["ns.DoubleDataValue"];
            Assert.Equal(SchemaType.Object, definition.Type);
            Assert.NotNull(definition.Properties);
            var properties = definition.Properties!;
            Assert.Contains("Value", properties.Keys);
            Assert.Contains("Status", properties.Keys);
            Assert.Contains("SourceTimestamp", properties.Keys);
            Assert.Equal(false, definition.AdditionalProperties?.Allowed);
        }

        [Fact]
        public void UnknownBuiltInTypeThrowsArgumentException()
        {
            var schemas = new JsonBuiltInSchemas(reversibleEncoding: false,
                useUriEncoding: false, definitions: null);

            var exception = Assert.Throws<ArgumentException>(() =>
                schemas.GetSchemaForBuiltInType((BuiltInType)255));

            Assert.Contains("255", exception.Message, StringComparison.Ordinal);
        }

        public static TheoryData<BuiltInType> GetBuiltInTypes()
        {
            var data = new TheoryData<BuiltInType>();
            for (var type = (int)BuiltInType.Boolean;
                type <= (int)BuiltInType.Enumeration; type++)
            {
                data.Add((BuiltInType)type);
            }
            return data;
        }
    }
}
