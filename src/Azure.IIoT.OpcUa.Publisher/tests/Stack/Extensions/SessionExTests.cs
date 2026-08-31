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
    /// Unit tests for early-exit and trivial paths in <see cref="SessionEx"/>.
    /// All tests here run without a live OPC UA session.
    /// </summary>
    public sealed class SessionExTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a mock IOpcUaSession whose Services.ReadAsync returns the given response.
        /// </summary>
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
        // ── ReadAttributeAsync<T> — empty nodeIds ────────────────────────────

        [Fact]
        public async Task ReadAttributeAsync_EmptyNodeIds_ReturnsEmptyEnumerableAsync()
        {
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            var header = new RequestHeader();

            var result = await session.Object.ReadAttributeAsync<string>(
                header, Enumerable.Empty<NodeId>(), Attributes.NodeClass);

            Assert.Empty(result);
        }

        // ── ReadAttributesAsync — empty nodeIds ──────────────────────────────

        [Fact]
        public async Task ReadAttributesAsync_EmptyNodeIds_ReturnsNullAsync()
        {
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            var header = new RequestHeader();
            var results = new Dictionary<NodeId, Dictionary<uint, DataValue>>();
            var attributeIds = new uint[] { Attributes.NodeClass, Attributes.BrowseName };

            var errorInfo = await session.Object.ReadAttributesAsync(
                header, Enumerable.Empty<NodeId>(), attributeIds, results);

            Assert.Null(errorInfo);
            Assert.Empty(results);
        }

        [Fact]
        public async Task ReadAttributesAsync_EmptyAttributeIds_ReturnsNullAsync()
        {
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            var header = new RequestHeader();
            var results = new Dictionary<NodeId, Dictionary<uint, DataValue>>();
            var nodeIds = new[] { new NodeId("ns=2;s=test") };

            var errorInfo = await session.Object.ReadAttributesAsync(
                header, nodeIds, Enumerable.Empty<uint>(), results);

            Assert.Null(errorInfo);
            Assert.Empty(results);
        }

        // ── GetBrowsePathsFromRootAsync — empty nodes ────────────────────────

        [Fact]
        public async Task GetBrowsePathsFromRootAsync_EmptyNodes_ReturnsEmptyListAsync()
        {
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            var header = new RequestHeader();

            var result = await session.Object.GetBrowsePathsFromRootAsync(
                header, Enumerable.Empty<NodeId>());

            Assert.Empty(result);
        }

        // ── CollectVariableMetadataAsync — empty nodeIds ─────────────────────

        [Fact]
        public async Task CollectVariableMetadataAsync_EmptyNodeIds_ReturnsNullAsync()
        {
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            var header = new RequestHeader();
            var metadata = new List<VariableMetadataModel>();

            var errorInfo = await session.Object.CollectVariableMetadataAsync(
                header, Enumerable.Empty<NodeId>(), metadata, NamespaceFormat.Index,
                default);

            Assert.Null(errorInfo);
            Assert.Empty(metadata);
        }

        // ── CollectMethodMetadataAsync — empty nodeIds ───────────────────────

        [Fact]
        public async Task CollectMethodMetadataAsync_EmptyNodeIds_ReturnsNullAsync()
        {
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            var header = new RequestHeader();
            var metadata = new List<MethodMetadataModel>();

            var errorInfo = await session.Object.CollectMethodMetadataAsync(
                header, Enumerable.Empty<NodeId>(), metadata, NamespaceFormat.Index,
                default);

            Assert.Null(errorInfo);
            Assert.Empty(metadata);
        }

        // ── ReadNodeAsync — rawMode = true ───────────────────────────────────

        [Fact]
        public async Task ReadNodeAsync_RawMode_ReturnsNodeModelWithStringIdAsync()
        {
            var messageContext = new ServiceMessageContext();
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            session.Setup(s => s.MessageContext).Returns(messageContext);

            var nodeId = new NodeId("MyTag", 2);
            var (nodeModel, errorInfo) = await session.Object.ReadNodeAsync(
                new RequestHeader(), nodeId,
                nodeClass: null,
                skipValue: false,
                rawMode: true,
                namespaceFormat: NamespaceFormat.Index,
                children: null);

            Assert.Null(errorInfo);
            Assert.NotNull(nodeModel);
            Assert.Contains("MyTag", nodeModel.NodeId, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ReadNodeAsync_RawMode_WithKnownNodeClass_SetsNodeClassAsync()
        {
            var messageContext = new ServiceMessageContext();
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            session.Setup(s => s.MessageContext).Returns(messageContext);

            var nodeId = new NodeId(2258); // Server/ServerStatus
            var (nodeModel, errorInfo) = await session.Object.ReadNodeAsync(
                new RequestHeader(), nodeId,
                nodeClass: Opc.Ua.NodeClass.Variable,
                skipValue: false,
                rawMode: true,
                namespaceFormat: NamespaceFormat.Index,
                children: null);

            Assert.Null(errorInfo);
            Assert.Equal(Azure.IIoT.OpcUa.Publisher.Models.NodeClass.Variable, nodeModel.NodeClass);
        }

        [Fact]
        public async Task ReadNodeAsync_RawMode_NullNodeClass_NodeClassIsNullAsync()
        {
            var messageContext = new ServiceMessageContext();
            var session = new Mock<IOpcUaSession>(MockBehavior.Strict);
            session.Setup(s => s.MessageContext).Returns(messageContext);

            var nodeId = new NodeId(2253); // Server
            var (nodeModel, errorInfo) = await session.Object.ReadNodeAsync(
                new RequestHeader(), nodeId,
                nodeClass: null,
                skipValue: true,
                rawMode: true,
                namespaceFormat: NamespaceFormat.Index);

            Assert.Null(errorInfo);
            Assert.Null(nodeModel.NodeClass);
        }

        // ── ReadValueAsync — mocked ReadAsync ────────────────────────────────

        [Fact]
        public async Task ReadValueAsync_SuccessResponse_ReturnsValueAndNullErrorInfoAsync()
        {
            var dataValue = new DataValue(new Variant(42u), StatusCodes.Good);
            var session = CreateSessionWithReadResponse(GoodReadResponse(dataValue));

            var (value, errorInfo) = await session.Object.ReadValueAsync(
                new RequestHeader(), new NodeId(42u, 2));

            Assert.Null(errorInfo);
            Assert.NotNull(value);
        }

        [Fact]
        public async Task ReadValueAsync_ServiceLevelError_ReturnsNullValueAndErrorInfoAsync()
        {
            var session = CreateSessionWithReadResponse(BadServiceReadResponse());

            var (value, errorInfo) = await session.Object.ReadValueAsync(
                new RequestHeader(), new NodeId(42u, 2));

            Assert.NotNull(errorInfo);
            Assert.Null(value);
        }

        [Fact]
        public async Task ReadValueAsync_ItemLevelError_ReturnsNullValueAndItemErrorAsync()
        {
            var dataValue = new DataValue(StatusCodes.BadNodeIdInvalid);
            var session = CreateSessionWithReadResponse(GoodReadResponse(dataValue));

            var (value, errorInfo) = await session.Object.ReadValueAsync(
                new RequestHeader(), new NodeId(42u, 2));

            Assert.NotNull(errorInfo);
        }

        // ── ReadAttributeAsync<T> — mocked ReadAsync ─────────────────────────

        [Fact]
        public async Task ReadAttributeAsync_SingleNodeId_SuccessResponse_ReturnsValueAsync()
        {
            var dataValue = new DataValue(new Variant((uint)Opc.Ua.NodeClass.Variable), StatusCodes.Good);
            var session = CreateSessionWithReadResponse(GoodReadResponse(dataValue));
            var nodeIds = new[] { new NodeId(42u, 2) };

            var results = (await session.Object.ReadAttributeAsync<uint>(
                new RequestHeader(), nodeIds, Attributes.NodeClass)).ToList();

            Assert.Single(results);
            Assert.Null(results[0].Item2);
        }

        [Fact]
        public async Task ReadAttributeAsync_SingleNodeId_ServiceError_ReturnsErrorAsync()
        {
            var session = CreateSessionWithReadResponse(BadServiceReadResponse());
            var nodeIds = new[] { new NodeId(42u, 2) };

            var results = (await session.Object.ReadAttributeAsync<uint>(
                new RequestHeader(), nodeIds, Attributes.NodeClass)).ToList();

            Assert.Single(results);
            Assert.NotNull(results[0].Item2);
        }

        // ── ReadAttributesAsync — mocked ReadAsync ────────────────────────────

        [Fact]
        public async Task ReadAttributesAsync_SingleNodeAndAttribute_SuccessResponse_PopulatesResultsAsync()
        {
            var dataValue = new DataValue(new Variant((uint)Opc.Ua.NodeClass.Variable), StatusCodes.Good);
            var session = CreateSessionWithReadResponse(GoodReadResponse(dataValue));
            var nodeIds = new[] { new NodeId(42u, 2) };
            var attributeIds = new uint[] { Attributes.NodeClass };
            var results = new Dictionary<NodeId, Dictionary<uint, DataValue>>();

            var errorInfo = await session.Object.ReadAttributesAsync(
                new RequestHeader(), nodeIds, attributeIds, results);

            Assert.Null(errorInfo);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task ReadAttributesAsync_ServiceError_ReturnsErrorAndEmptyResultsAsync()
        {
            var session = CreateSessionWithReadResponse(BadServiceReadResponse());
            var nodeIds = new[] { new NodeId(42u, 2) };
            var attributeIds = new uint[] { Attributes.NodeClass };
            var results = new Dictionary<NodeId, Dictionary<uint, DataValue>>();

            var errorInfo = await session.Object.ReadAttributesAsync(
                new RequestHeader(), nodeIds, attributeIds, results);

            Assert.NotNull(errorInfo);
            Assert.Empty(results);
        }
    }
}
