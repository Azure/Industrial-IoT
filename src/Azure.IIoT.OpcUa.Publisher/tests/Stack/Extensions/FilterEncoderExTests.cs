// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public sealed class FilterEncoderExTests
    {
        private static JsonVariantEncoder CreateEncoder()
        {
            return new JsonVariantEncoder(new ServiceMessageContext());
        }

        [Fact]
        public void StringLiteralOperandShouldDecodeAsStringNotNodeId()
        {
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Value = "Error"
            };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.String, literal.Value.TypeInfo.BuiltInType);
            Assert.Equal("Error", literal.Value.Value);
        }

        [Fact]
        public void StringLiteralOperandWithWildcardShouldDecodeAsString()
        {
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Value = "Main%"
            };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.String, literal.Value.TypeInfo.BuiltInType);
            Assert.Equal("Main%", literal.Value.Value);
        }

        [Fact]
        public void IntegerLiteralOperandShouldDecodeAsInteger()
        {
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Value = 42
            };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.NotEqual(BuiltInType.NodeId, literal.Value.TypeInfo.BuiltInType);
            Assert.Equal(42L, System.Convert.ToInt64(literal.Value.Value,
                System.Globalization.CultureInfo.InvariantCulture));
        }

        [Fact]
        public void NodeIdLiteralOperandWithDataTypeHintShouldDecodeAsNodeId()
        {
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Value = "i=10751",
                DataType = "NodeId"
            };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.NodeId, literal.Value.TypeInfo.BuiltInType);
        }

        [Fact]
        public void NodeIdShapedStringWithoutDataTypeShouldStillDecodeAsNodeId()
        {
            // Backwards-compat: when the value is a string that follows the
            // OPC UA NodeId textual format, it should still be promoted to a
            // NodeId literal even without an explicit DataType hint, so that
            // existing usages (e.g. OfType operator with "ns=2;i=235") keep
            // working.
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Value = "ns=2;i=235"
            };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.NodeId, literal.Value.TypeInfo.BuiltInType);
            Assert.Equal(new NodeId("ns=2;i=235"), literal.Value.Value);
        }

        // ── GetDefaultEventFilter ────────────────────────────────────────

        [Fact]
        public void GetDefaultEventFilterReturnsNineSelectClauses()
        {
            var filter = FilterEncoderEx.GetDefaultEventFilter();

            Assert.NotNull(filter);
            Assert.Equal(9, filter.SelectClauses.Count);
        }

        [Fact]
        public void GetDefaultEventFilterClausesHaveBaseEventTypeNodeId()
        {
            var filter = FilterEncoderEx.GetDefaultEventFilter();

            foreach (var clause in filter.SelectClauses.ToArray())
            {
                Assert.Equal(ObjectTypeIds.BaseEventType, clause.TypeDefinitionId);
            }
        }

        // ── Decode(EventFilterModel?) ────────────────────────────────────

        [Fact]
        public void DecodeNullEventFilterModelReturnsDefaultFilter()
        {
            var encoder = CreateEncoder();

            var filter = encoder.Decode((EventFilterModel?)null);

            Assert.NotNull(filter);
            Assert.Equal(9, filter.SelectClauses.Count);
        }

        [Fact]
        public void DecodeNullEventFilterModelWithNoDefaultReturnsNull()
        {
            var encoder = CreateEncoder();

            var filter = encoder.Decode((EventFilterModel?)null, noDefaultFilter: true);

            Assert.Null(filter);
        }

        [Fact]
        public void DecodeEventFilterModelWithEmptySelectClausesReturnsDefaultFilter()
        {
            var encoder = CreateEncoder();
            var model = new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>()
            };

            var filter = encoder.Decode(model);

            Assert.NotNull(filter);
            Assert.Equal(9, filter.SelectClauses.Count);
        }

        [Fact]
        public void DecodeEventFilterModelWithEmptySelectClausesNoDefaultThrows()
        {
            var encoder = CreateEncoder();
            var model = new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>()
            };

            Assert.Throws<ArgumentException>(() => encoder.Decode(model, noDefaultFilter: true));
        }

        [Fact]
        public void DecodeEventFilterModelWithSelectClausesReturnsPopulatedFilter()
        {
            var encoder = CreateEncoder();
            var model = new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "EventId" }
                    }
                }
            };

            var filter = encoder.Decode(model);

            Assert.NotNull(filter);
            Assert.Equal(1, filter.SelectClauses.Count);
        }

        [Fact]
        public void DecodeEventFilterModelWithWhereClausePopulatesWhereClause()
        {
            var encoder = CreateEncoder();
            var model = new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "Severity" }
                    }
                },
                WhereClause = new ContentFilterModel
                {
                    Elements = new List<ContentFilterElementModel>
                    {
                        new ContentFilterElementModel
                        {
                            FilterOperator = FilterOperatorType.GreaterThan,
                            FilterOperands = new List<FilterOperandModel>
                            {
                                new FilterOperandModel { Value = 300 }
                            }
                        }
                    }
                }
            };

            var filter = encoder.Decode(model);

            Assert.NotNull(filter);
            Assert.Equal(1, filter.WhereClause.Elements.Count);
        }

        // ── Encode(EventFilter?) ─────────────────────────────────────────

        [Fact]
        public void EncodeNullEventFilterReturnsNull()
        {
            var encoder = CreateEncoder();

            var result = encoder.Encode((EventFilter?)null, NamespaceFormat.Index);

            Assert.Null(result);
        }

        [Fact]
        public void EncodeDefaultEventFilterReturnsModelWithSelectClauses()
        {
            var encoder = CreateEncoder();
            var filter = FilterEncoderEx.GetDefaultEventFilter();

            var result = encoder.Encode(filter, NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.NotNull(result.SelectClauses);
            Assert.Equal(9, result.SelectClauses!.Count);
        }

        [Fact]
        public void EncodeEventFilterRoundTripsSelectClauseCount()
        {
            var encoder = CreateEncoder();
            var model = new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "Message" }
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "Severity" }
                    }
                }
            };
            var filter = encoder.Decode(model);

            var result = encoder.Encode(filter, NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.Equal(2, result.SelectClauses!.Count);
        }

        // ── Decode(ContentFilterModel?) ──────────────────────────────────

        [Fact]
        public void DecodeNullContentFilterModelReturnsEmptyContentFilter()
        {
            var encoder = CreateEncoder();

            var result = encoder.Decode((ContentFilterModel?)null);

            Assert.NotNull(result);
            Assert.Equal(0, result.Elements.Count);
        }

        [Fact]
        public void DecodeContentFilterModelWithNullElementsReturnsEmpty()
        {
            var encoder = CreateEncoder();
            var model = new ContentFilterModel { Elements = null };

            var result = encoder.Decode(model);

            Assert.NotNull(result);
            Assert.Equal(0, result.Elements.Count);
        }

        [Fact]
        public void DecodeContentFilterModelWithElementsDecodesThemAll()
        {
            var encoder = CreateEncoder();
            var model = new ContentFilterModel
            {
                Elements = new List<ContentFilterElementModel>
                {
                    new ContentFilterElementModel
                    {
                        FilterOperator = FilterOperatorType.Equals,
                        FilterOperands = new List<FilterOperandModel>
                        {
                            new FilterOperandModel { Index = 0u },
                            new FilterOperandModel { Value = 500 }
                        }
                    },
                    new ContentFilterElementModel
                    {
                        FilterOperator = FilterOperatorType.IsNull
                    }
                }
            };

            var result = encoder.Decode(model);

            Assert.Equal(2, result.Elements.Count);
        }

        // ── Encode(ContentFilter?) ───────────────────────────────────────

        [Fact]
        public void EncodeNullContentFilterReturnsNull()
        {
            var encoder = CreateEncoder();

            var result = encoder.Encode((ContentFilter?)null, NamespaceFormat.Index);

            Assert.Null(result);
        }

        [Fact]
        public void EncodeEmptyContentFilterReturnsEmptyModel()
        {
            var encoder = CreateEncoder();
            var filter = new ContentFilter();

            var result = encoder.Encode(filter, NamespaceFormat.Index);

            Assert.NotNull(result);
            // Empty Elements collection encodes to empty list (not null)
            var elements = result.Elements?.ToList() ?? new List<ContentFilterElementModel>();
            Assert.Empty(elements);
        }

        // ── Decode(ContentFilterElementModel?) ──────────────────────────

        [Fact]
        public void DecodeNullContentFilterElementModelReturnsNull()
        {
            var encoder = CreateEncoder();

            var result = encoder.Decode((ContentFilterElementModel?)null);

            Assert.Null(result);
        }

        [Fact]
        public void DecodeContentFilterElementModelWithIndexOperandDecodesAsElementOperand()
        {
            var encoder = CreateEncoder();
            var model = new ContentFilterElementModel
            {
                FilterOperator = FilterOperatorType.Equals,
                FilterOperands = new List<FilterOperandModel>
                {
                    new FilterOperandModel { Index = 3u }
                }
            };

            var result = encoder.Decode(model);

            Assert.NotNull(result);
            Assert.Equal(FilterOperator.Equals, result.FilterOperator);
            Assert.Equal(1, result.FilterOperands.Count);
            var body = Assert.IsType<ElementOperand>(result.FilterOperands[0].Body);
            Assert.Equal(3u, body.Index);
        }

        [Fact]
        public void DecodeContentFilterElementModelWithNoOperandsDecodesEmptyList()
        {
            var encoder = CreateEncoder();
            var model = new ContentFilterElementModel
            {
                FilterOperator = FilterOperatorType.IsNull,
                FilterOperands = null
            };

            var result = encoder.Decode(model);

            Assert.NotNull(result);
            Assert.Equal(FilterOperator.IsNull, result.FilterOperator);
            Assert.Equal(0, result.FilterOperands.Count);
        }

        // ── Encode(ContentFilterElement?) ───────────────────────────────

        [Fact]
        public void EncodeNullContentFilterElementReturnsNull()
        {
            var encoder = CreateEncoder();

            var result = encoder.Encode((ContentFilterElement?)null, NamespaceFormat.Index);

            Assert.Null(result);
        }

        [Fact]
        public void EncodeContentFilterElementRoundTripsOperatorAndOperands()
        {
            var encoder = CreateEncoder();
            // Build element: GreaterThan(Index=0, Literal=100)
            var element = new ContentFilterElement
            {
                FilterOperator = FilterOperator.GreaterThan,
                FilterOperands = new List<ExtensionObject>
                {
                    new ExtensionObject(new ElementOperand { Index = 0u }),
                    new ExtensionObject(new LiteralOperand(new Variant(100)))
                }
            };

            var result = encoder.Encode(element, NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.Equal(FilterOperatorType.GreaterThan, result.FilterOperator);
            Assert.NotNull(result.FilterOperands);
            Assert.Equal(2, result.FilterOperands!.Count);
        }

        // ── Decode(FilterOperandModel?) overload — Index / Alias paths ──

        [Fact]
        public void DecodeFilterOperandModelWithIndexReturnsElementOperand()
        {
            var encoder = CreateEncoder();
            var model = new FilterOperandModel { Index = 7u };

            var operand = encoder.Decode(model);

            var elem = Assert.IsType<ElementOperand>(operand);
            Assert.Equal(7u, elem.Index);
        }

        [Fact]
        public void DecodeNullFilterOperandModelReturnsNull()
        {
            var encoder = CreateEncoder();

            var result = encoder.Decode((FilterOperandModel?)null);

            Assert.Null(result);
        }

        [Fact]
        public void DecodeFilterOperandModelWithAliasReturnsAttributeOperand()
        {
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Alias = "myAlias",
                NodeId = "i=2041",
                AttributeId = NodeAttribute.Value
            };

            var operand = encoder.Decode(model);

            var attr = Assert.IsType<AttributeOperand>(operand);
            Assert.Equal("myAlias", attr.Alias);
        }

        [Fact]
        public void DecodeFilterOperandModelWithAliasAndOnlySimpleReturnsSimpleAttributeOperand()
        {
            var encoder = CreateEncoder();
            var model = new FilterOperandModel
            {
                Alias = "myAlias",
                NodeId = "i=2041",
                AttributeId = NodeAttribute.Value
            };

            // When onlySimpleAttributeOperands=true the Alias is ignored and a
            // SimpleAttributeOperand is returned instead of AttributeOperand.
            var operand = encoder.Decode(model, onlySimpleAttributeOperands: true);

            Assert.IsType<SimpleAttributeOperand>(operand);
        }

        // ── Encode(FilterOperand?) ────────────────────────────────────────

        [Fact]
        public void EncodeNullFilterOperandReturnsNull()
        {
            var encoder = CreateEncoder();

            var result = encoder.Encode((FilterOperand?)null, NamespaceFormat.Index);

            Assert.Null(result);
        }

        [Fact]
        public void EncodeElementOperandProducesModelWithIndex()
        {
            var encoder = CreateEncoder();
            var operand = new ElementOperand { Index = 5u };

            var result = encoder.Encode(operand, NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.Equal(5u, result.Index);
            Assert.Null(result.Value);
            Assert.Null(result.Alias);
        }

        [Fact]
        public void EncodeLiteralOperandProducesModelWithValue()
        {
            var encoder = CreateEncoder();
            var operand = new LiteralOperand(new Variant(42));

            var result = encoder.Encode(operand, NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.Null(result.Index);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void EncodeAttributeOperandProducesModelWithAlias()
        {
            var encoder = CreateEncoder();
            var operand = new AttributeOperand
            {
                Alias = "testAlias",
                NodeId = new NodeId(2041u),
                AttributeId = (uint)NodeAttribute.Value,
                IndexRange = null
            };

            var result = encoder.Encode(operand, NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.Equal("testAlias", result.Alias);
            Assert.Null(result.Index);
        }

        [Fact]
        public void EncodeSimpleAttributeOperandProducesModelWithoutAlias()
        {
            var encoder = CreateEncoder();
            var operand = new SimpleAttributeOperand
            {
                TypeDefinitionId = new NodeId(2041u),
                AttributeId = (uint)NodeAttribute.Value
            };

            var result = encoder.Encode(operand, NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.Null(result.Alias);
            Assert.Null(result.Index);
            Assert.NotNull(result.NodeId);
        }

        // ── Round-trip (Decode ↔ Encode) ─────────────────────────────────

        [Fact]
        public void ContentFilterElementRoundTripWithLiteralAndElementOperands()
        {
            var encoder = CreateEncoder();
            var original = new ContentFilterElementModel
            {
                FilterOperator = FilterOperatorType.LessThan,
                FilterOperands = new List<FilterOperandModel>
                {
                    new FilterOperandModel { Value = 999 },
                    new FilterOperandModel { Index = 1u }
                }
            };

            var stackElement = encoder.Decode(original);
            var roundTripped = encoder.Encode(stackElement, NamespaceFormat.Index);

            Assert.NotNull(roundTripped);
            Assert.Equal(FilterOperatorType.LessThan, roundTripped.FilterOperator);
            Assert.Equal(2, roundTripped.FilterOperands!.Count);
        }

        // ── LooksLikeNodeId backward-compat promotion — i=, s=, g=, b=, nsu= ──

        [Fact]
        public void LiteralWithIEqualsPrefix_PromotedToNodeId()
        {
            // "i=123" starts with "i=" → LooksLikeNodeId=true → promoted
            var encoder = CreateEncoder();
            var model = new FilterOperandModel { Value = "i=123" };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.NodeId, literal.Value.TypeInfo.BuiltInType);
        }

        [Fact]
        public void LiteralWithSEqualsPrefix_PromotedToNodeId()
        {
            // "s=hello" starts with "s=" → LooksLikeNodeId=true → promoted to string NodeId
            var encoder = CreateEncoder();
            var model = new FilterOperandModel { Value = "s=hello" };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.NodeId, literal.Value.TypeInfo.BuiltInType);
        }

        [Fact]
        public void LiteralWithGEqualsPrefix_PromotedToNodeId()
        {
            // "g=..." starts with "g=" → LooksLikeNodeId=true → promoted to Guid NodeId
            var encoder = CreateEncoder();
            var model = new FilterOperandModel { Value = "g=00000000-0000-0000-0000-000000000001" };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.NodeId, literal.Value.TypeInfo.BuiltInType);
        }

        [Fact]
        public void LiteralWithBEqualsPrefix_PromotedToNodeId()
        {
            // "b=AAAA" starts with "b=" → LooksLikeNodeId=true → promoted to opaque NodeId
            var encoder = CreateEncoder();
            var model = new FilterOperandModel { Value = "b=AAAA" };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.NodeId, literal.Value.TypeInfo.BuiltInType);
        }

        [Fact]
        public void LiteralWithNsuEqualsPrefix_LooksLikeNodeId()
        {
            // "nsu=...;i=N" triggers the nsu= branch in LooksLikeNodeId.
            // Register the namespace URI so ToNodeId can resolve it.
            var context = new ServiceMessageContext();
            context.NamespaceUris.Append("http://test.example.com");
            var encoder = new JsonVariantEncoder(context);
            var model = new FilterOperandModel { Value = "nsu=http://test.example.com;i=50" };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.NodeId, literal.Value.TypeInfo.BuiltInType);
        }

        [Fact]
        public void LiteralWithPlainString_NotPromotedToNodeId()
        {
            // "hello" does not look like NodeId → stays as String
            var encoder = CreateEncoder();
            var model = new FilterOperandModel { Value = "hello" };

            var operand = encoder.Decode(model);

            var literal = Assert.IsType<LiteralOperand>(operand);
            Assert.Equal(BuiltInType.String, literal.Value.TypeInfo.BuiltInType);
        }

        // ── Encode throws for unknown FilterOperand subtype ────────────────

        [Fact]
        public void EncodeUnknownFilterOperand_ThrowsNotSupportedException()
        {
            var encoder = CreateEncoder();
            var operand = new CustomFilterOperand();

            Assert.Throws<NotSupportedException>(() =>
                encoder.Encode(operand, NamespaceFormat.Index));
        }

        // ── AttributeOperand with browse path ─────────────────────────────

        [Fact]
        public void EncodeAttributeOperandWithBrowsePath_IncludesBrowsePath()
        {
            var encoder = CreateEncoder();
            var operand = new AttributeOperand
            {
                Alias = "browsedAlias",
                NodeId = new NodeId(2041u),
                AttributeId = (uint)NodeAttribute.Value,
                BrowsePath = new RelativePath
                {
                    Elements = new[]
                    {
                        new RelativePathElement { TargetName = new QualifiedName("Child") }
                    }
                }
            };

            var result = encoder.Encode(operand, NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.NotNull(result.BrowsePath);
            Assert.Equal("browsedAlias", result.Alias);
        }

        // ── SimpleAttributeOperand with browse path ────────────────────────

        [Fact]
        public void EncodeSimpleAttributeOperandWithBrowsePath_IncludesBrowsePath()
        {
            var encoder = CreateEncoder();
            var operand = new SimpleAttributeOperand
            {
                TypeDefinitionId = new NodeId(2041u),
                AttributeId = (uint)NodeAttribute.Value,
                BrowsePath = new List<QualifiedName>
                {
                    new QualifiedName("Child1"),
                    new QualifiedName("Child2")
                }
            };

            var result = encoder.Encode(operand, NamespaceFormat.Index);

            Assert.NotNull(result);
            Assert.NotNull(result.BrowsePath);
            Assert.Equal(2, result.BrowsePath!.Count);
        }

        [Fact]
        public void DecodeEventFilterModelWithNullSelectClauses_ReturnsDefaultFilter()
        {
            var encoder = CreateEncoder();
            var model = new EventFilterModel
            {
                SelectClauses = null
            };

            var filter = encoder.Decode(model);

            Assert.NotNull(filter);
            Assert.Equal(9, filter.SelectClauses.Count);
        }

        private sealed class CustomFilterOperand : FilterOperand
        {
        }
    }
}
