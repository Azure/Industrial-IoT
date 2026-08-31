// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using SimpleEvents;
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Simple Events server node tests
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleEventsServerTests<T>
    {
        public SimpleEventsServerTests(Func<INodeServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task CompileSimpleBaseEventQueryTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = "select * from BaseEventType",
                QueryType = QueryType.Event
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.EventFilter);
            AssertEventFilter(result.EventFilter, new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventType" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceNode" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Time" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ReceiveTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/LocalTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Message" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Severity" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassName.Value"
                    }
                }
            });
        }

        public async Task CompileSimpleEventsQueryTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = "prefix se <http://opcfoundation.org/SimpleEvents> " +
                    $"select * from se:i={ObjectTypes.SystemCycleStartedEventType} " +
                    $"where oftype se:i={ObjectTypes.SystemCycleStartedEventType}",
                QueryType = QueryType.Event
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.EventFilter);
            AssertEventFilter(result.EventFilter, new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new() {
                        TypeDefinitionId = "i=2130",
                        BrowsePath = new[] { "/EventId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2130",
                        BrowsePath = new[] { "/EventType" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2130",
                        BrowsePath = new[] { "/SourceNode" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2130",
                        BrowsePath = new[] { "/SourceName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2130",
                        BrowsePath = new[] { "/Time" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2130",
                        BrowsePath = new[] { "/ReceiveTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/LocalTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2130",
                        BrowsePath = new[] { "/Message" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2130",
                        BrowsePath = new[] { "/Severity" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ConditionSubClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "http://opcfoundation.org/SimpleEvents#i=235",
                        BrowsePath = new[] { "/http://opcfoundation.org/SimpleEvents#CycleId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/CycleId.Value"
                    },
                    new() {
                        TypeDefinitionId = "http://opcfoundation.org/SimpleEvents#i=235",
                        BrowsePath = new[] { "/http://opcfoundation.org/SimpleEvents#CurrentStep" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/CurrentStep.Value"
                    },
                    new() {
                        TypeDefinitionId = "http://opcfoundation.org/SimpleEvents#i=184",
                        BrowsePath = new[] { "/http://opcfoundation.org/SimpleEvents#Steps" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Steps.Value"
                    }
                },
                WhereClause = new ContentFilterModel
                {
                    Elements = new List<ContentFilterElementModel>
                    {
                        new() {
                            FilterOperator = FilterOperatorType.OfType,
                            FilterOperands = new List<FilterOperandModel>
                            {
                                new() {
                                    Value = "http://opcfoundation.org/SimpleEvents#i=184",
                                    DataType = "NodeId"
                                }
                            }
                        }
                    }
                }
            });
        }

        private static void AssertEventFilter(EventFilterModel actual,
            EventFilterModel expected)
        {
            Assert.Equal(expected.SelectClauses!.Count, actual.SelectClauses!.Count);
            for (var index = 0; index < expected.SelectClauses.Count; index++)
            {
                var expectedClause = expected.SelectClauses[index];
                var actualClause = actual.SelectClauses[index];
                Assert.Equal(expectedClause.TypeDefinitionId, actualClause.TypeDefinitionId);
                Assert.Equal(expectedClause.BrowsePath, actualClause.BrowsePath);
                Assert.Equal(expectedClause.AttributeId, actualClause.AttributeId);
                Assert.Equal(expectedClause.DisplayName, actualClause.DisplayName);
            }

            if (expected.WhereClause is null)
            {
                Assert.Null(actual.WhereClause);
                return;
            }
            var expectedElements = expected.WhereClause.Elements!;
            var actualElements = Assert.IsType<ContentFilterModel>(actual.WhereClause).Elements!;
            Assert.Equal(expectedElements.Count, actualElements.Count);
            for (var index = 0; index < expectedElements.Count; index++)
            {
                var expectedElement = expectedElements[index];
                var actualElement = actualElements[index];
                Assert.Equal(expectedElement.FilterOperator, actualElement.FilterOperator);
                Assert.Equal(expectedElement.FilterOperands!.Count,
                    actualElement.FilterOperands!.Count);
                for (var operandIndex = 0; operandIndex < expectedElement.FilterOperands.Count;
                    operandIndex++)
                {
                    var expectedOperand = expectedElement.FilterOperands[operandIndex];
                    var actualOperand = actualElement.FilterOperands[operandIndex];
                    Assert.Equal(expectedOperand.Index, actualOperand.Index);
                    Assert.Equal(expectedOperand.NodeId, actualOperand.NodeId);
                    Assert.Equal(expectedOperand.BrowsePath, actualOperand.BrowsePath);
                    Assert.Equal(expectedOperand.AttributeId, actualOperand.AttributeId);
                    Assert.Equal(expectedOperand.DataType, actualOperand.DataType);
                    Assert.True(JsonNode.DeepEquals(expectedOperand.Value, actualOperand.Value),
                        $"{expectedOperand.Value} != {actualOperand.Value}");
                }
            }
        }

        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;
    }
}
