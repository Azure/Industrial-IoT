// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Additional unit tests for <see cref="SessionEx"/> covering
    /// single-node overloads and service-error branches not covered by
    /// <see cref="SessionExTests"/>.
    /// </summary>
    public sealed class SessionExMoreTests
    {
        // ── Shared helpers ────────────────────────────────────────────────────

        private static Mock<IOpcUaSession> CreateSessionWithReadResponse(ReadResponse response)
        {
            var services = new Mock<ISessionServices>(MockBehavior.Strict);
            services.Setup(s => s.ReadAsync(
                It.IsAny<RequestHeader>(),
                It.IsAny<double>(),
                It.IsAny<Opc.Ua.TimestampsToReturn>(),
                It.IsAny<List<ReadValueId>>(),
                It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult(response));

            var session = new Mock<IOpcUaSession>(MockBehavior.Loose);
            session.Setup(s => s.Services).Returns(services.Object);
            session.Setup(s => s.MessageContext).Returns(ServiceMessageContext.GlobalContext);
            return session;
        }

        private static Mock<IOpcUaSession> CreateSessionWithBrowseResponse(BrowseResponse response)
        {
            var services = new Mock<ISessionServices>(MockBehavior.Strict);
            services.Setup(s => s.BrowseAsync(
                It.IsAny<RequestHeader>(),
                It.IsAny<ViewDescription?>(),
                It.IsAny<uint>(),
                It.IsAny<List<BrowseDescription>>(),
                It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult(response));

            var session = new Mock<IOpcUaSession>(MockBehavior.Loose);
            session.Setup(s => s.Services).Returns(services.Object);
            session.Setup(s => s.MessageContext).Returns(ServiceMessageContext.GlobalContext);
            return session;
        }

        private static ReadResponse GoodReadResponse(DataValue dataValue)
        {
            return new ReadResponse
            {
                ResponseHeader = new ResponseHeader { ServiceResult = StatusCodes.Good },
                Results = new ArrayOf<DataValue>(new[] { dataValue })
            };
        }

        private static ReadResponse BadServiceReadResponse()
        {
            return new ReadResponse
            {
                ResponseHeader = new ResponseHeader { ServiceResult = StatusCodes.BadNodeIdInvalid }
            };
        }

        private static BrowseResponse BadServiceBrowseResponse()
        {
            return new BrowseResponse
            {
                ResponseHeader = new ResponseHeader { ServiceResult = StatusCodes.BadNodeIdInvalid }
            };
        }

        // ── ReadAttributeAsync<T> — single-nodeId overload ───────────────────

        [Fact]
        public async Task ReadAttributeAsync_SingleNodeId_SuccessResponse_ReturnsValueAsync()
        {
            var dataValue = new DataValue(
                new Variant((uint)Opc.Ua.NodeClass.Variable), StatusCodes.Good);
            var session = CreateSessionWithReadResponse(GoodReadResponse(dataValue));
            var nodeId = new NodeId(42u, 2);

            var (value, errorInfo) = await session.Object.ReadAttributeAsync<uint>(
                new RequestHeader(), nodeId, Attributes.NodeClass);

            Assert.Null(errorInfo);
        }

        [Fact]
        public async Task ReadAttributeAsync_SingleNodeId_ServiceError_ReturnsErrorAsync()
        {
            var session = CreateSessionWithReadResponse(BadServiceReadResponse());
            var nodeId = new NodeId(42u, 2);

            var (value, errorInfo) = await session.Object.ReadAttributeAsync<uint>(
                new RequestHeader(), nodeId, Attributes.NodeClass);

            Assert.NotNull(errorInfo);
        }

        // ── CollectVariableMetadataAsync — non-empty nodeIds, service error ───

        [Fact]
        public async Task CollectVariableMetadataAsync_ServiceError_ReturnsErrorInfoAsync()
        {
            var session = CreateSessionWithReadResponse(BadServiceReadResponse());
            var header = new RequestHeader();
            var metadata = new List<VariableMetadataModel>();
            var nodeIds = new[] { new NodeId(42u, 2) };

            var errorInfo = await session.Object.CollectVariableMetadataAsync(
                header, nodeIds, metadata, NamespaceFormat.Index, default);

            Assert.NotNull(errorInfo);
            Assert.Empty(metadata);
        }

        // ── GetVariableMetadataAsync (single-node overload) ───────────────────

        [Fact]
        public async Task GetVariableMetadataAsync_ServiceError_ReturnsNullAndErrorInfoAsync()
        {
            var session = CreateSessionWithReadResponse(BadServiceReadResponse());
            var header = new RequestHeader();

            var (variableMetadata, errorInfo) = await session.Object.GetVariableMetadataAsync(
                header, new NodeId(42u, 2), NamespaceFormat.Index, default);

            Assert.Null(variableMetadata);
            Assert.NotNull(errorInfo);
        }

        // ── CollectMethodMetadataAsync — non-empty nodeIds, service error ─────

        [Fact]
        public async Task CollectMethodMetadataAsync_ServiceError_ReturnsErrorInfoAsync()
        {
            var session = CreateSessionWithBrowseResponse(BadServiceBrowseResponse());
            var header = new RequestHeader();
            var metadata = new List<MethodMetadataModel>();
            var nodeIds = new[] { new NodeId(42u, 2) };

            var errorInfo = await session.Object.CollectMethodMetadataAsync(
                header, nodeIds, metadata, NamespaceFormat.Index, default);

            Assert.NotNull(errorInfo);
            Assert.Empty(metadata);
        }

        // ── GetMethodMetadataAsync (single-node overload) ─────────────────────

        [Fact]
        public async Task GetMethodMetadataAsync_ServiceError_ReturnsNullAndErrorInfoAsync()
        {
            var session = CreateSessionWithBrowseResponse(BadServiceBrowseResponse());
            var header = new RequestHeader();

            var (methodMetadata, errorInfo) = await session.Object.GetMethodMetadataAsync(
                header, new NodeId(42u, 2), NamespaceFormat.Index, default);

            Assert.Null(methodMetadata);
            Assert.NotNull(errorInfo);
        }

        // ── ReadNodeAsync rawMode=true — additional edge cases ────────────────

        [Fact]
        public async Task ReadNodeAsync_RawMode_ExpandedNodeId_ReturnsNodeIdStringAsync()
        {
            var messageContext = new ServiceMessageContext();
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            session.Setup(s => s.MessageContext).Returns(messageContext);

            var nodeId = new NodeId(Guid.NewGuid());
            var (nodeModel, errorInfo) = await session.Object.ReadNodeAsync(
                new RequestHeader(), nodeId,
                nodeClass: Opc.Ua.NodeClass.Object,
                skipValue: true,
                rawMode: true,
                namespaceFormat: NamespaceFormat.Index);

            Assert.Null(errorInfo);
            Assert.NotNull(nodeModel);
            Assert.Equal(Publisher.Models.NodeClass.Object, nodeModel.NodeClass);
        }

        [Fact]
        public async Task ReadNodeAsync_RawMode_Method_ReturnsMethodNodeClassAsync()
        {
            var messageContext = new ServiceMessageContext();
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            session.Setup(s => s.MessageContext).Returns(messageContext);

            var nodeId = new NodeId("method", 1);
            var (nodeModel, errorInfo) = await session.Object.ReadNodeAsync(
                new RequestHeader(), nodeId,
                nodeClass: Opc.Ua.NodeClass.Method,
                skipValue: false,
                rawMode: true,
                namespaceFormat: NamespaceFormat.Uri);

            Assert.Null(errorInfo);
            Assert.Equal(Publisher.Models.NodeClass.Method, nodeModel.NodeClass);
        }

        [Fact]
        public async Task ReadNodeAsync_RawMode_DataType_ReturnsDataTypeNodeClassAsync()
        {
            var messageContext = new ServiceMessageContext();
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            session.Setup(s => s.MessageContext).Returns(messageContext);

            var nodeId = new NodeId(12345u);
            var (nodeModel, errorInfo) = await session.Object.ReadNodeAsync(
                new RequestHeader(), nodeId,
                nodeClass: Opc.Ua.NodeClass.DataType,
                skipValue: false,
                rawMode: true,
                namespaceFormat: NamespaceFormat.Index);

            Assert.Null(errorInfo);
            Assert.Equal(Publisher.Models.NodeClass.DataType, nodeModel.NodeClass);
        }

        // ── ReadNodeAsync single-nodeId overload (dispatches to rawMode) ──────

        [Fact]
        public async Task ReadNodeAsync_SingleNodeId_RawMode_ReturnsNodeModelAsync()
        {
            var messageContext = new ServiceMessageContext();
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            session.Setup(s => s.MessageContext).Returns(messageContext);

            var nodeId = new NodeId("sensor", 2);

            var (nodeModel, errorInfo) = await session.Object.ReadNodeAsync(
                new RequestHeader(), nodeId,
                nodeClass: Opc.Ua.NodeClass.Variable,
                skipValue: true,
                rawMode: true,
                namespaceFormat: NamespaceFormat.Index);

            Assert.Null(errorInfo);
            Assert.NotNull(nodeModel);
            Assert.Equal(Publisher.Models.NodeClass.Variable, nodeModel.NodeClass);
        }

        // ── ReadAttributesAsync — service error path ──────────────────────────

        [Fact]
        public async Task ReadAttributesAsync_MultipleAttributes_ServiceError_ReturnsErrorAsync()
        {
            var session = CreateSessionWithReadResponse(BadServiceReadResponse());
            var header = new RequestHeader();
            var nodeIds = new[] { new NodeId(1u, 0), new NodeId(2u, 0) };
            var attributeIds = new uint[] { Attributes.BrowseName, Attributes.DisplayName };
            var results = new Dictionary<NodeId, Dictionary<uint, DataValue>>();

            var errorInfo = await session.Object.ReadAttributesAsync(
                header, nodeIds, attributeIds, results);

            Assert.NotNull(errorInfo);
            Assert.Empty(results);
        }
    }
}
