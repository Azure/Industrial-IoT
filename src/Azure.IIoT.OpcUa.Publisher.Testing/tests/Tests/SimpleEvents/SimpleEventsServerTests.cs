// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher;
    using System;

    /// <summary>
    /// Simple Events server node tests
    /// </summary>
    public class SimpleEventsServerTests<T>
    {
        public SimpleEventsServerTests(Func<INodeServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

#if UNUSED

        public async Task CompileSimpleBaseEventQueryTestAsync()
        {
            var services = _services();
            var connection = await _connection().ConfigureAwait(false);

            var result = await services.CompileQueryAsync(connection, new QueryCompilationRequestModel
            {
                Query = "select * from BaseEventType"
            }).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            result.EventFilter.Should().BeEquivalentTo(new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/EventId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/EventType",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/SourceNode",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/SourceName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/Time",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/ReceiveTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/LocalTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/Message",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/Severity",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    }
                }
            });
        }

        public async Task CompileSimpleEventsQueryTestAsync()
        {
            var services = _services();
            var connection = await _connection().ConfigureAwait(false);

            var result = await services.CompileQueryAsync(connection, new QueryCompilationRequestModel
            {
                Query = "prefix se <http://opcfoundation.org/SimpleEvents> " +
                    $"select * from se:i={ObjectTypes.SystemCycleStartedEventType} " +
                    $"where oftype se:i={ObjectTypes.SystemCycleStartedEventType}"
            }).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            result.EventFilter.Should().BeEquivalentTo(new EventFilterModel
            {
                SelectClauses = new List<SimpleAttributeOperandModel>
                {
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/EventId",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/EventType",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/EventType.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/SourceNode",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceNode.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/SourceName",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/SourceName.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/Time",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Time.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/ReceiveTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/ReceiveTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/LocalTime",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/LocalTime.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/Message",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Message.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "i=2041",
                        BrowsePath = "/Severity",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/Severity.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "http://opcfoundation.org/SimpleEvents#i=235",
                        BrowsePath = "/[http://opcfoundation.org/SimpleEvents#CycleId]",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/CycleId.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "http://opcfoundation.org/SimpleEvents#i=235",
                        BrowsePath = "/[http://opcfoundation.org/SimpleEvents#CurrentStep]",
                        AttributeId = NodeAttribute.Value,
                        DisplayName = "/CurrentStep.Value"
                    },
                    new SimpleAttributeOperandModel
                    {
                        TypeDefinitionId = = "http://opcfoundation.org/SimpleEvents#i=184",
                        BrowsePath = "/[http://opcfoundation.org/SimpleEvents#Steps]",
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
                                    DataType = "TypeDefinitionId ="
                                }
                            }
                        }
                    }
                }
            });
        }
#endif

        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;
    }
}
