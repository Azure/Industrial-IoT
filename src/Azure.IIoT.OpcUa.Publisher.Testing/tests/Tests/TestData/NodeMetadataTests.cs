// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class NodeMetadataTests<T>
    {
        /// <summary>
        /// Create metadata tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public NodeMetadataTests(Func<INodeServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task GetServerCapabilitiesTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var results = await services.GetServerCapabilitiesAsync(_connection,
                null, ct).ConfigureAwait(false);

            Assert.NotNull(results);
            Assert.NotNull(results.AggregateFunctions);
            Assert.Equal(37, results.AggregateFunctions.Count);
            Assert.Single(results.SupportedLocales!);
            Assert.Equal("en-US", results.SupportedLocales![0]);
            Assert.NotEmpty(results.ServerProfiles!);
            Assert.Equal(3, results.ServerProfiles!.Count);
            Assert.NotNull(results.OperationLimits);
            Assert.Null(results.OperationLimits.MinSupportedSampleRate);
            Assert.Equal(1000u, results.OperationLimits.MaxBrowseContinuationPoints!.Value);
            Assert.Equal(1000u, results.OperationLimits.MaxQueryContinuationPoints!.Value);
            Assert.Equal(1000u, results.OperationLimits.MaxHistoryContinuationPoints!.Value);
            Assert.Null(results.OperationLimits.MaxNodesPerBrowse);
            Assert.Null(results.OperationLimits.MaxNodesPerRegisterNodes);
            Assert.Null(results.OperationLimits.MaxNodesPerWrite);
            Assert.Null(results.OperationLimits.MaxNodesPerMethodCall);
            Assert.Null(results.OperationLimits.MaxNodesPerNodeManagement);
            Assert.Null(results.OperationLimits.MaxMonitoredItemsPerCall);
            Assert.Null(results.OperationLimits.MaxNodesPerHistoryReadData);
            Assert.Null(results.OperationLimits.MaxNodesPerHistoryReadEvents);
            Assert.Null(results.OperationLimits.MaxNodesPerHistoryUpdateData);
            Assert.Null(results.OperationLimits.MaxNodesPerHistoryUpdateEvents);
            Assert.Equal(65535u, results.OperationLimits.MaxArrayLength);
            Assert.Equal(130816u, results.OperationLimits.MaxStringLength);
            Assert.Equal(1048576u, results.OperationLimits.MaxByteStringLength);
            Assert.Null(results.ModellingRules);
        }

        public async Task HistoryGetServerCapabilitiesTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var results = await services.HistoryGetServerCapabilitiesAsync(_connection,
                null, ct).ConfigureAwait(false);

            Assert.NotNull(results);
            Assert.Null(results.AggregateFunctions);
            Assert.False(results.AccessHistoryDataCapability);
            Assert.False(results.AccessHistoryEventsCapability);
            Assert.False(results.InsertDataCapability);
            Assert.False(results.InsertEventCapability);
            Assert.False(results.InsertAnnotationCapability);
            Assert.False(results.ReplaceDataCapability);
            Assert.False(results.ReplaceEventCapability);
            Assert.False(results.DeleteRawCapability);
            Assert.False(results.DeleteAtTimeCapability);
            Assert.False(results.DeleteEventCapability);
            Assert.False(results.ServerTimestampSupported);
            Assert.False(results.UpdateDataCapability);
            Assert.False(results.UpdateEventCapability);
            Assert.Null(results.MaxReturnDataValues);
            Assert.Null(results.MaxReturnEventValues);
        }

        public async Task NodeGetMetadataForFolderTypeTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var result = await browser.GetMetadataAsync(_connection, new NodeMetadataRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        Level = DiagnosticsLevel.Verbose
                    }
                },
                NodeId = Opc.Ua.ObjectTypeIds.FolderType.ToString()
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.TypeDefinition);
            Assert.Equal(NodeType.Object, result.TypeDefinition.NodeType);
            Assert.NotNull(result.TypeDefinition.Declarations);
            Assert.Empty(result.TypeDefinition.Declarations);
        }

        public async Task NodeGetMetadataForServerObjectTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var result = await browser.GetMetadataAsync(_connection, new NodeMetadataRequestModel
            {
                NodeId = Opc.Ua.ObjectIds.Server.ToString()
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.TypeDefinition);
            Assert.Equal("ServerType", result.TypeDefinition.BrowseName);
            Assert.Equal(Opc.Ua.ObjectTypeIds.ServerType.ToString(),
                result.TypeDefinition.TypeDefinitionId);
            Assert.Equal(NodeType.Object, result.TypeDefinition.NodeType);
            var baseType = Assert.Single(result.TypeDefinition.TypeHierarchy!);
            Assert.Equal("i=58", baseType.NodeId);
            Assert.Equal("BaseObjectType", baseType.BrowseName);
            Assert.NotNull(result.TypeDefinition.Declarations);
            Assert.NotEmpty(result.TypeDefinition.Declarations);
            Assert.Equal(63, result.TypeDefinition.Declarations.Count);
            Assert.Collection(result.TypeDefinition.Declarations.Select(d => d.BrowseName),
                arg => Assert.Equal("ServerArray", arg),
                arg => Assert.Equal("NamespaceArray", arg),
                arg => Assert.Equal("UrisVersion", arg),
                arg => Assert.Equal("ServerStatus", arg),
                arg => Assert.Equal("StartTime", arg),
                arg => Assert.Equal("CurrentTime", arg),
                arg => Assert.Equal("State", arg),
                arg => Assert.Equal("BuildInfo", arg),
                arg => Assert.Equal("ProductUri", arg),
                arg => Assert.Equal("ManufacturerName", arg),
                arg => Assert.Equal("ProductName", arg),
                arg => Assert.Equal("SoftwareVersion", arg),
                arg => Assert.Equal("BuildNumber", arg),
                arg => Assert.Equal("BuildDate", arg),
                arg => Assert.Equal("SecondsTillShutdown", arg),
                arg => Assert.Equal("ShutdownReason", arg),
                arg => Assert.Equal("ServiceLevel", arg),
                arg => Assert.Equal("Auditing", arg),
                arg => Assert.Equal("EstimatedReturnTime", arg),
                arg => Assert.Equal("LocalTime", arg),
                arg => Assert.Equal("ServerCapabilities", arg),
                arg => Assert.Equal("ServerProfileArray", arg),
                arg => Assert.Equal("LocaleIdArray", arg),
                arg => Assert.Equal("MinSupportedSampleRate", arg),
                arg => Assert.Equal("MaxBrowseContinuationPoints", arg),
                arg => Assert.Equal("MaxQueryContinuationPoints", arg),
                arg => Assert.Equal("MaxHistoryContinuationPoints", arg),
                arg => Assert.Equal("SoftwareCertificates", arg),
                arg => Assert.Equal("ModellingRules", arg),
                arg => Assert.Equal("AggregateFunctions", arg),
                arg => Assert.Equal("ServerDiagnostics", arg),
                arg => Assert.Equal("ServerDiagnosticsSummary", arg),
                arg => Assert.Equal("ServerViewCount", arg),
                arg => Assert.Equal("CurrentSessionCount", arg),
                arg => Assert.Equal("CumulatedSessionCount", arg),
                arg => Assert.Equal("SecurityRejectedSessionCount", arg),
                arg => Assert.Equal("RejectedSessionCount", arg),
                arg => Assert.Equal("SessionTimeoutCount", arg),
                arg => Assert.Equal("SessionAbortCount", arg),
                arg => Assert.Equal("PublishingIntervalCount", arg),
                arg => Assert.Equal("CurrentSubscriptionCount", arg),
                arg => Assert.Equal("CumulatedSubscriptionCount", arg),
                arg => Assert.Equal("SecurityRejectedRequestsCount", arg),
                arg => Assert.Equal("RejectedRequestsCount", arg),
                arg => Assert.Equal("SubscriptionDiagnosticsArray", arg),
                arg => Assert.Equal("SessionsDiagnosticsSummary", arg),
                arg => Assert.Equal("SessionDiagnosticsArray", arg),
                arg => Assert.Equal("SessionSecurityDiagnosticsArray", arg),
                arg => Assert.Equal("EnabledFlag", arg),
                arg => Assert.Equal("VendorServerInfo", arg),
                arg => Assert.Equal("ServerRedundancy", arg),
                arg => Assert.Equal("RedundancySupport", arg),
                arg => Assert.Equal("Namespaces", arg),
                arg => Assert.Equal("GetMonitoredItems", arg),
                arg => Assert.Equal("InputArguments", arg),
                arg => Assert.Equal("OutputArguments", arg),
                arg => Assert.Equal("ResendData", arg),
                arg => Assert.Equal("InputArguments", arg),
                arg => Assert.Equal("SetSubscriptionDurable", arg),
                arg => Assert.Equal("InputArguments", arg),
                arg => Assert.Equal("OutputArguments", arg),
                arg => Assert.Equal("RequestServerStateChange", arg),
                arg => Assert.Equal("InputArguments", arg));
        }

        public async Task NodeGetMetadataForConditionTypeTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var result = await browser.GetMetadataAsync(_connection, new NodeMetadataRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        Level = DiagnosticsLevel.Verbose
                    }
                },
                NodeId = Opc.Ua.ObjectTypeIds.ConditionType.ToString()
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.TypeDefinition);
            Assert.Equal(NodeType.Event, result.TypeDefinition.NodeType);
            Assert.NotEmpty(result.TypeDefinition.TypeHierarchy!);
            Assert.Equal(2, result.TypeDefinition.TypeHierarchy!.Count);
            Assert.NotNull(result.TypeDefinition.Declarations);
            Assert.NotEmpty(result.TypeDefinition.Declarations);
            Assert.Equal(35, result.TypeDefinition.Declarations.Count);
        }

        public async Task NodeGetMetadataForServerStatusVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var result = await browser.GetMetadataAsync(_connection, new NodeMetadataRequestModel
            {
                NodeId = Opc.Ua.VariableIds.Server_ServerStatus.ToString()
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.TypeDefinition);
            Assert.Equal("ServerStatusType", result.TypeDefinition.BrowseName);
            Assert.Equal(Opc.Ua.VariableTypeIds.ServerStatusType.ToString(),
                result.TypeDefinition.TypeDefinitionId);
            Assert.Equal(NodeType.DataVariable, result.TypeDefinition.NodeType);
            Assert.Equal(2, result.TypeDefinition.TypeHierarchy!.Count);
            Assert.Equal("i=62", result.TypeDefinition.TypeHierarchy[0].NodeId);
            Assert.Equal("BaseVariableType", result.TypeDefinition.TypeHierarchy[0].BrowseName);
            Assert.NotNull(result.TypeDefinition.Declarations);
            Assert.NotEmpty(result.TypeDefinition.Declarations);
            Assert.Equal(12, result.TypeDefinition.Declarations.Count);
            Assert.Collection(result.TypeDefinition.Declarations.Select(d => d.BrowseName),
                arg => Assert.Equal("StartTime", arg),
                arg => Assert.Equal("CurrentTime", arg),
                arg => Assert.Equal("State", arg),
                arg => Assert.Equal("BuildInfo", arg),
                arg => Assert.Equal("ProductUri", arg),
                arg => Assert.Equal("ManufacturerName", arg),
                arg => Assert.Equal("ProductName", arg),
                arg => Assert.Equal("SoftwareVersion", arg),
                arg => Assert.Equal("BuildNumber", arg),
                arg => Assert.Equal("BuildDate", arg),
                arg => Assert.Equal("SecondsTillShutdown", arg),
                arg => Assert.Equal("ShutdownReason", arg));
        }

        public async Task NodeGetMetadataForRedundancySupportPropertyTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var result = await browser.GetMetadataAsync(_connection, new NodeMetadataRequestModel
            {
                NodeId = Opc.Ua.VariableIds.Server_ServerRedundancy_RedundancySupport.ToString()
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.TypeDefinition);
            Assert.Equal("PropertyType", result.TypeDefinition.BrowseName);
            Assert.Equal(Opc.Ua.VariableTypeIds.PropertyType.ToString(),
                result.TypeDefinition.TypeDefinitionId);
            Assert.Equal(NodeType.Property, result.TypeDefinition.NodeType);
            var baseType = Assert.Single(result.TypeDefinition.TypeHierarchy!);
            Assert.Equal("i=62", baseType.NodeId);
            Assert.Equal("BaseVariableType", baseType.BrowseName);
            Assert.NotNull(result.TypeDefinition.Declarations);
            Assert.Empty(result.TypeDefinition.Declarations);
        }

        public async Task NodeGetMetadataForBaseInterfaceTypeTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var result = await browser.GetMetadataAsync(_connection, new NodeMetadataRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        Level = DiagnosticsLevel.Verbose
                    }
                },
                NodeId = Opc.Ua.ObjectTypeIds.BaseInterfaceType.ToString()
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.TypeDefinition);
            Assert.NotEmpty(result.TypeDefinition.TypeHierarchy!);
            var baseType = Assert.Single(result.TypeDefinition.TypeHierarchy!);
            Assert.Equal("i=58", baseType.NodeId);
            Assert.Equal("BaseObjectType", baseType.BrowseName);
            Assert.Equal(NodeType.Interface, result.TypeDefinition.NodeType);
            Assert.NotNull(result.TypeDefinition.Declarations);
            Assert.Empty(result.TypeDefinition.Declarations);
        }

        public async Task NodeGetMetadataTestForBaseEventTypeTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var result = await browser.GetMetadataAsync(_connection, new NodeMetadataRequestModel
            {
                Header = new RequestHeaderModel
                {
                    Diagnostics = new DiagnosticsModel
                    {
                        Level = DiagnosticsLevel.Verbose
                    }
                },
                NodeId = Opc.Ua.ObjectTypeIds.BaseEventType.ToString()
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.TypeDefinition);
            Assert.NotNull(result.TypeDefinition.Declarations);
            Assert.NotEmpty(result.TypeDefinition.Declarations);
            Assert.NotEmpty(result.TypeDefinition.TypeHierarchy!);
            var baseType = Assert.Single(result.TypeDefinition!.TypeHierarchy!);
            Assert.Equal("i=58", baseType.NodeId);
            Assert.Equal("BaseObjectType", baseType.BrowseName);
            Assert.Equal(NodeType.Event, result.TypeDefinition.NodeType);
            Assert.All(result.TypeDefinition.Declarations, arg =>
            {
                Assert.Equal("i=2041", arg.RootTypeId);
                Assert.Equal(NodeClass.Variable, arg.NodeClass);
                Assert.NotNull(arg.VariableMetadata);
                Assert.NotNull(arg.VariableMetadata.DataType);
                Assert.Null(arg.Description);
                Assert.Null(arg.OverriddenDeclaration);
            });
            Assert.Collection(result.TypeDefinition.Declarations,
                arg =>
                {
                    Assert.Equal("/EventId", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("EventId", arg.DisplayName);
                    Assert.Equal("ByteString", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Mandatory", arg.ModellingRule);
                    Assert.Equal("i=2042", arg.NodeId);
                    Assert.Null(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/EventType", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("EventType", arg.DisplayName);
                    Assert.Equal("NodeId", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Mandatory", arg.ModellingRule);
                    Assert.Equal("i=2043", arg.NodeId);
                    Assert.Null(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/SourceNode", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("SourceNode", arg.DisplayName);
                    Assert.Equal("NodeId", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Mandatory", arg.ModellingRule);
                    Assert.Equal("i=2044", arg.NodeId);
                    Assert.Null(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/SourceName", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("SourceName", arg.DisplayName);
                    Assert.Equal("String", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Mandatory", arg.ModellingRule);
                    Assert.Equal("i=2045", arg.NodeId);
                    Assert.Null(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/Time", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("Time", arg.DisplayName);
                    Assert.Equal("UtcTime", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Mandatory", arg.ModellingRule);
                    Assert.Equal("i=2046", arg.NodeId);
                    Assert.Null(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/ReceiveTime", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("ReceiveTime", arg.DisplayName);
                    Assert.Equal("UtcTime", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Mandatory", arg.ModellingRule);
                    Assert.Equal("i=2047", arg.NodeId);
                    Assert.Null(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/LocalTime", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("LocalTime", arg.DisplayName);
                    Assert.Equal("TimeZoneDataType", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Optional", arg.ModellingRule);
                    Assert.Equal("i=3190", arg.NodeId);
                    Assert.Null(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/Message", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("Message", arg.DisplayName);
                    Assert.Equal("LocalizedText", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Mandatory", arg.ModellingRule);
                    Assert.Equal("i=2050", arg.NodeId);
                    Assert.Null(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/Severity", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("Severity", arg.DisplayName);
                    Assert.Equal("UInt16", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Mandatory", arg.ModellingRule);
                    Assert.Equal("i=2051", arg.NodeId);
                    Assert.Null(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/ConditionClassId", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("ConditionClassId", arg.DisplayName);
                    Assert.Equal("NodeId", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Optional", arg.ModellingRule);
                    Assert.Equal("i=31771", arg.NodeId);
                    Assert.Null(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/ConditionClassName", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("ConditionClassName", arg.DisplayName);
                    Assert.Equal("LocalizedText", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Optional", arg.ModellingRule);
                    Assert.Equal("i=31772", arg.NodeId);
                    Assert.Null(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/ConditionSubClassId", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("ConditionSubClassId", arg.DisplayName);
                    Assert.Equal("NodeId", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Optional", arg.ModellingRule);
                    Assert.Equal("i=31773", arg.NodeId);
                    Assert.NotNull(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(0u, Assert.Single(arg.VariableMetadata.ArrayDimensions));
                    Assert.Equal(NodeValueRank.OneDimension, arg.VariableMetadata.ValueRank!.Value);
                },
                arg =>
                {
                    Assert.Equal("/ConditionSubClassName", Assert.Single(arg.BrowsePath!));
                    Assert.Equal("ConditionSubClassName", arg.DisplayName);
                    Assert.Equal("LocalizedText", arg.VariableMetadata!.DataType!.DataType);
                    Assert.Equal("Optional", arg.ModellingRule);
                    Assert.Equal("i=31774", arg.NodeId);
                    Assert.NotNull(arg.VariableMetadata.ArrayDimensions);
                    Assert.Equal(0u, Assert.Single(arg.VariableMetadata.ArrayDimensions));
                    Assert.Equal(NodeValueRank.OneDimension, arg.VariableMetadata.ValueRank!.Value);
                });
        }

        public async Task NodeGetMetadataForPropertyTypeTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var result = await browser.GetMetadataAsync(_connection, new NodeMetadataRequestModel
            {
                NodeId = Opc.Ua.VariableTypeIds.PropertyType.ToString()
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.TypeDefinition);
            Assert.NotEmpty(result.TypeDefinition.TypeHierarchy!);
            var baseType = Assert.Single(result.TypeDefinition.TypeHierarchy!);
            Assert.Equal("i=62", baseType.NodeId);
            Assert.Equal("BaseVariableType", baseType.BrowseName);
            Assert.Equal(NodeType.Property, result.TypeDefinition.NodeType);
            Assert.NotNull(result.TypeDefinition.Declarations);
            Assert.Empty(result.TypeDefinition.Declarations);
        }

        public async Task NodeGetMetadataForBaseDataVariableTypeTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var result = await browser.GetMetadataAsync(_connection, new NodeMetadataRequestModel
            {
                NodeId = Opc.Ua.VariableTypeIds.BaseDataVariableType.ToString()
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.TypeDefinition);
            Assert.NotEmpty(result.TypeDefinition.TypeHierarchy!);
            var baseType = Assert.Single(result.TypeDefinition.TypeHierarchy!);
            Assert.Equal("i=62", baseType.NodeId);
            Assert.Equal("BaseVariableType", baseType.BrowseName);
            Assert.Equal(NodeType.DataVariable, result.TypeDefinition.NodeType);
            Assert.NotNull(result.TypeDefinition.Declarations);
            Assert.Empty(result.TypeDefinition.Declarations);
        }

        public async Task NodeGetMetadataForAudioVariableTypeTestAsync(CancellationToken ct = default)
        {
            var browser = _services();

            // Act
            var result = await browser.GetMetadataAsync(_connection, new NodeMetadataRequestModel
            {
                NodeId = Opc.Ua.VariableTypeIds.AudioVariableType.ToString()
            }, ct).ConfigureAwait(false);

            // Assert
            Assert.Null(result.ErrorInfo);
            Assert.NotNull(result.TypeDefinition);
            Assert.NotEmpty(result.TypeDefinition.TypeHierarchy!);
            Assert.Equal(2, result.TypeDefinition.TypeHierarchy!.Count);
            Assert.Equal(NodeType.DataVariable, result.TypeDefinition.NodeType);
            Assert.NotNull(result.TypeDefinition.Declarations);
            Assert.Equal(3, result.TypeDefinition.Declarations.Count);
            Assert.All(result.TypeDefinition.Declarations, arg =>
            {
                Assert.Equal(Opc.Ua.VariableTypeIds.AudioVariableType.ToString(), arg.RootTypeId);
                Assert.Equal(NodeClass.Variable, arg.NodeClass);
                Assert.NotNull(arg.VariableMetadata);
                Assert.Null(arg.VariableMetadata.ArrayDimensions);
                Assert.Null(arg.MethodMetadata);
                Assert.Null(arg.Description);
                Assert.Null(arg.OverriddenDeclaration);
                Assert.NotNull(arg.VariableMetadata.DataType);
                Assert.Equal("String", arg.VariableMetadata!.DataType!.DataType);
                Assert.Equal(NodeValueRank.Scalar, arg.VariableMetadata!.ValueRank!.Value);
            });
            Assert.Collection(result.TypeDefinition.Declarations,
                arg =>
                {
                    Assert.Equal("Optional", arg.ModellingRule);
                    Assert.Equal("ListId", arg.DisplayName);
                    Assert.Equal("i=17988", arg.NodeId);
                },
                arg =>
                {
                    Assert.Equal("Optional", arg.ModellingRule);
                    Assert.Equal("AgencyId", arg.DisplayName);
                    Assert.Equal("i=17989", arg.NodeId);
                },
                arg =>
                {
                    Assert.Equal("Optional", arg.ModellingRule);
                    Assert.Equal("VersionId", arg.DisplayName);
                    Assert.Equal("i=17990", arg.NodeId);
                });
        }

        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;
    }
}
