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
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventType" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceNode" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Time" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ReceiveTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/LocalTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Message" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Severity" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
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
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/EventType" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceNode" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/SourceName" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Time" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/ReceiveTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/LocalTime" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Message" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "i=2041",
                        BrowsePath = new[] { "/Severity" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "http://opcfoundation.org/SimpleEvents#i=235",
                        BrowsePath = new[] { "/http://opcfoundation.org/SimpleEvents#CycleId" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/CycleId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = "http://opcfoundation.org/SimpleEvents#i=235",
                        BrowsePath = new[] { "/http://opcfoundation.org/SimpleEvents#CurrentStep" },
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/CurrentStep.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
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
                        new ContentFilterElementModel
                        {
                            FilterOperator = FilterOperatorType.OfType,
                            FilterOperands = new List<FilterOperandModel>
                            {
                                new FilterOperandModel
                                {
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
