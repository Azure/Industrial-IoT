// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using FluentAssertions;
    using SimpleEvents;
    using System;
    using System.Collections.Generic;
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
                Query = "select * from BaseEventType"
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.EventFilter);
            result.EventFilter.Should().BeEquivalentTo(new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventId" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/EventId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventType" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/EventType.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceNode" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/SourceNode.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceName" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/SourceName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Time" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/Time.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ReceiveTime" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/ReceiveTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/LocalTime" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/LocalTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Message" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/Message.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Severity" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/Severity.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassId" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/ConditionClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassName" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/ConditionClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassId" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/ConditionSubClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassName" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/ConditionSubClassName.Value"
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
                    $"where oftype se:i={ObjectTypes.SystemCycleStartedEventType}"
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.EventFilter);
            result.EventFilter.Should().BeEquivalentTo(new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventId" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/EventId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventType" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/EventType.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceNode" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/SourceNode.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceName" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/SourceName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Time" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/Time.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ReceiveTime" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/ReceiveTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/LocalTime" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/LocalTime.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Message" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/Message.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Severity" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/Severity.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassId" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/ConditionClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionClassName" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/ConditionClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassId" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/ConditionSubClassId.Value"
                    },
                    new() {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ConditionSubClassName" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/ConditionSubClassName.Value"
                    },
                    new() {
                        TypeDefinitionId = "http://opcfoundation.org/SimpleEvents#i=235",
                        BrowsePath = new[] { "/http://opcfoundation.org/SimpleEvents#CycleId" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/CycleId.Value"
                    },
                    new() {
                        TypeDefinitionId = "http://opcfoundation.org/SimpleEvents#i=235",
                        BrowsePath = new[] { "/http://opcfoundation.org/SimpleEvents#CurrentStep" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/CurrentStep.Value"
                    },
                    new() {
                        TypeDefinitionId = "http://opcfoundation.org/SimpleEvents#i=184",
                        BrowsePath = new[] { "/http://opcfoundation.org/SimpleEvents#Steps" },
                        AttributeId = NodeAttribute.Value,
                        DataSetFieldName = "/Steps.Value"
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

        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;
    }
}
