// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Tests.Clients
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// The publisher client's contract with the module's method router: the
    /// method names it calls and the payloads it sends.
    /// </summary>
    public sealed class PublisherApiClientTests : ApiClientTestBase
    {
        private PublisherApiClient Client => new(MethodClient.Object, Target);

        [Fact]
        public async Task CreateOrUpdateDataSetWriterEntryCallsCreateOrUpdateDataSetWriterEntryAsync()
        {
            await Client.CreateOrUpdateDataSetWriterEntryAsync(Entry(), default);

            var payload = AssertCalled("CreateOrUpdateDataSetWriterEntry");
            Assert.Equal("opc.tcp://server:4840", StringOf(payload, "EndpointUrl"));
        }

        [Fact]
        public async Task GetDataSetWriterEntryCallsGetDataSetWriterEntryAsync()
        {
            Returns(Entry());

            var result = await Client.GetDataSetWriterEntryAsync("group", "writer", default);

            var payload = AssertCalled("GetDataSetWriterEntry");
            Assert.Equal("group", StringOf(payload, "dataSetWriterGroup"));
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AddOrUpdateNodesCallsAddOrUpdateNodesAsync()
        {
            await Client.AddOrUpdateNodesAsync("group", "writer",
                new[] { new OpcNodeModel { Id = "ns=2;s=Tag1" } }, "field0", default);

            var payload = AssertCalled("AddOrUpdateNodes");
            Assert.Equal("field0", StringOf(payload, "insertAfterFieldId"));
        }

        [Fact]
        public async Task RemoveNodesCallsRemoveNodesAsync()
        {
            await Client.RemoveNodesAsync("group", "writer", new[] { "field1" }, default);

            var payload = AssertCalled("RemoveNodes");
            Assert.Equal("field1", payload.GetProperty("dataSetFieldIds")[0].GetString());
        }

        [Fact]
        public async Task GetNodesCallsGetNodesAsync()
        {
            Returns(new List<OpcNodeModel> { new() { Id = "ns=2;s=Tag1" } });

            var result = await Client.GetNodesAsync("group", "writer", "last", 10, default);

            var payload = AssertCalled("GetNodes");
            Assert.Equal(10, payload.GetProperty("pageSize").GetInt32());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task RemoveDataSetWriterEntryCallsRemoveDataSetWriterEntryAsync()
        {
            await Client.RemoveDataSetWriterEntryAsync("group", "writer", default);

            var payload = AssertCalled("RemoveDataSetWriterEntry");
            Assert.Equal("writer", StringOf(payload, "dataSetWriterId"));
        }

        [Fact]
        public async Task PublishStartCallsPublishStartAsync()
        {
            Returns(new PublishStartResponseModel());

            var result = await Client.PublishStartAsync(Connection(), new PublishStartRequestModel
            {
                Item = new PublishedItemModel { NodeId = "ns=2;s=Tag1" }
            }, default);

            var payload = AssertCalled("PublishStart");
            Assert.Equal("ns=2;s=Tag1",
                RequestOf(payload).GetProperty("item").GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task PublishStopCallsPublishStopAsync()
        {
            Returns(new PublishStopResponseModel());

            var result = await Client.PublishStopAsync(Connection(),
                new PublishStopRequestModel { NodeId = "ns=2;s=Tag1" }, default);

            var payload = AssertCalled("PublishStop");
            Assert.Equal("ns=2;s=Tag1", RequestOf(payload).GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task PublishBulkCallsPublishBulkAsync()
        {
            Returns(new PublishBulkResponseModel());

            var result = await Client.PublishBulkAsync(Connection(), new PublishBulkRequestModel
            {
                NodesToRemove = new[] { "ns=2;s=Tag1" }
            }, default);

            var payload = AssertCalled("PublishBulk");
            Assert.Equal("ns=2;s=Tag1",
                RequestOf(payload).GetProperty("nodesToRemove")[0].GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task PublishListCallsPublishListAsync()
        {
            Returns(new PublishedItemListResponseModel());

            var result = await Client.PublishListAsync(Connection(),
                new PublishedItemListRequestModel { ContinuationToken = "list-next" }, default);

            var payload = AssertCalled("PublishList");
            Assert.Equal("list-next", RequestOf(payload).GetProperty("continuationToken").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task PublishNodesCallsPublishNodesAsync()
        {
            Returns(new PublishedNodesResponseModel());

            var result = await Client.PublishNodesAsync(Entry(), default);

            var payload = AssertCalled("PublishNodes");
            Assert.Equal("opc.tcp://server:4840", StringOf(payload, "EndpointUrl"));
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UnpublishNodesCallsUnpublishNodesAsync()
        {
            Returns(new PublishedNodesResponseModel());

            var result = await Client.UnpublishNodesAsync(Entry(), default);

            var payload = AssertCalled("UnpublishNodes");
            Assert.Equal("writer", StringOf(payload, "DataSetWriterId"));
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UnpublishAllNodesCallsUnpublishAllNodesAsync()
        {
            Returns(new PublishedNodesResponseModel());

            var result = await Client.UnpublishAllNodesAsync(Entry(), default);

            var payload = AssertCalled("UnpublishAllNodes");
            Assert.Equal("group", StringOf(payload, "DataSetWriterGroup"));
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AddOrUpdateEndpointsCallsAddOrUpdateEndpointsAsync()
        {
            Returns(new PublishedNodesResponseModel());

            var result = await Client.AddOrUpdateEndpointsAsync(new[] { Entry() }, default);

            var payload = AssertCalled("AddOrUpdateEndpoints");
            Assert.Equal("opc.tcp://server:4840", payload[0].GetProperty("EndpointUrl").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetConfiguredEndpointsCallsGetConfiguredEndpointsAsync()
        {
            Returns(new GetConfiguredEndpointsResponseModel());

            var result = await Client.GetConfiguredEndpointsAsync(
                new GetConfiguredEndpointsRequestModel { IncludeNodes = true }, default);

            var payload = AssertCalled("GetConfiguredEndpoints");
            Assert.Equal(JsonValueKind.True, payload.GetProperty("includeNodes").ValueKind);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SetConfiguredEndpointsCallsSetConfiguredEndpointsAsync()
        {
            await Client.SetConfiguredEndpointsAsync(new SetConfiguredEndpointsRequestModel
            {
                Endpoints = new[] { Entry() }
            }, default);

            var payload = AssertCalled("SetConfiguredEndpoints");
            Assert.Equal(JsonValueKind.Array, payload.GetProperty("endpoints").ValueKind);
        }

        [Fact]
        public async Task GetConfiguredNodesOnEndpointCallsGetConfiguredNodesOnEndpointAsync()
        {
            Returns(new GetConfiguredNodesOnEndpointResponseModel());

            var result = await Client.GetConfiguredNodesOnEndpointAsync(Entry(), default);

            var payload = AssertCalled("GetConfiguredNodesOnEndpoint");
            Assert.Equal("opc.tcp://server:4840", StringOf(payload, "EndpointUrl"));
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetDiagnosticInfoCallsGetDiagnosticInfoAsync()
        {
            Returns(new List<PublishDiagnosticInfoModel> { new() });

            var result = await Client.GetDiagnosticInfoAsync(default);

            AssertCalledWithoutPayload("GetDiagnosticInfo");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShutdownCallsShutdownAsync()
        {
            await Client.ShutdownAsync(true, default);

            var payload = AssertCalled("Shutdown");
            Assert.Equal(JsonValueKind.True, payload.ValueKind);
        }

        [Fact]
        public async Task GetServerCertificateCallsGetServerCertificateAsync()
        {
            Returns("certificate");

            var result = await Client.GetServerCertificateAsync(default);

            AssertCalledWithoutPayload("GetServerCertificate");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetApiKeyCallsGetApiKeyAsync()
        {
            Returns("api-key");

            var result = await Client.GetApiKeyAsync(default);

            AssertCalledWithoutPayload("GetApiKey");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ARejectedArgumentNeverReachesTheTransportAsync()
        {
            //
            // Validation has to happen before the call, not after: a missing
            // writer id, endpoint URL, or empty remove list that travelled
            // would be a remote error for something knowable locally.
            //
            await Assert.ThrowsAsync<ArgumentException>(() =>
                Client.RemoveNodesAsync("group", "writer", Array.Empty<string>(), default));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                Client.GetNodesAsync(string.Empty, "writer", null, null, default));
            await Assert.ThrowsAsync<ArgumentException>(() => Client.PublishStartAsync(
                new ConnectionModel { Endpoint = new EndpointModel { Url = string.Empty } },
                new PublishStartRequestModel { Item = new PublishedItemModel { NodeId = "ns=2;s=Tag1" } },
                default));

            AssertNotCalled();
        }

        [Fact]
        public void TheClientRequiresATransportAndATarget()
        {
            Assert.Throws<ArgumentNullException>(() => new PublisherApiClient(null!, Target));
            Assert.Throws<ArgumentNullException>(() => new PublisherApiClient(MethodClient.Object, (string)null!));
            Assert.Throws<ArgumentNullException>(() => new PublisherApiClient(MethodClient.Object, string.Empty));
        }

        [Fact]
        public async Task TheOptionsConstructorUsesTheConfiguredTargetAndTimeoutAsync()
        {
            var timeout = TimeSpan.FromSeconds(42);
            var client = new PublisherApiClient(MethodClient.Object,
                Options.Create(new SdkOptions { Target = Target, Timeout = timeout }));

            await client.ShutdownAsync(false, default);

            AssertCalled("Shutdown");
            Assert.Equal(timeout, LastCall!.Timeout);
        }

        private static ConnectionModel Connection()
        {
            return new ConnectionModel { Endpoint = new EndpointModel { Url = "opc.tcp://server:4840" } };
        }

        private static PublishedNodesEntryModel Entry()
        {
            return new PublishedNodesEntryModel
            {
                EndpointUrl = "opc.tcp://server:4840",
                DataSetWriterGroup = "group",
                DataSetWriterId = "writer",
                OpcNodes = new List<OpcNodeModel> { new() { Id = "ns=2;s=Tag1" } }
            };
        }

        private static JsonElement RequestOf(JsonElement payload)
        {
            return payload.GetProperty("request");
        }

        private void AssertCalledWithoutPayload(string method)
        {
            var call = LastCall;
            Assert.NotNull(call);
            Assert.Equal(Target, call!.Target);
            Assert.Equal(method, call.Method);
            Assert.Equal(ContentMimeType.Json, call.ContentType);
            Assert.Empty(call.Payload.ToArray());
            MethodClient.Verify(c => c.CallMethodAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
