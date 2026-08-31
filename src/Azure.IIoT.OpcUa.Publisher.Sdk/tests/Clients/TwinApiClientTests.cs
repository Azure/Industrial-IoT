// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Tests.Clients
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Microsoft.Extensions.Options;
    using System;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// The twin client's contract with the module's method router: the
    /// method names it calls and the payloads it sends.
    /// </summary>
    /// <remarks>
    /// The method names carry a _V2 suffix that nothing in the client's own
    /// signature mentions. Getting one wrong is not a compile error and not a
    /// local failure - it is a call the module rejects at run time, which is
    /// why each one is named here explicitly.
    /// </remarks>
    public sealed class TwinApiClientTests : ApiClientTestBase
    {
        private TwinApiClient Client => new(MethodClient.Object, Target);

        [Fact]
        public async Task TestConnectionCallsTestConnectionV2Async()
        {
            Returns(new TestConnectionResponseModel());

            var result = await Client.TestConnectionAsync(Connection(),
                new TestConnectionRequestModel(), default);

            var payload = AssertCalled("TestConnection_V2");
            Assert.Equal("opc.tcp://server:4840", EndpointUrlOf(payload));
            Assert.NotNull(result);
        }

        [Fact]
        public async Task NodeBrowseFirstCallsBrowseV2Async()
        {
            Returns(new BrowseFirstResponseModel
            {
                Node = new NodeModel { NodeId = "i=84" },
                References = Array.Empty<NodeReferenceModel>()
            });

            var result = await Client.NodeBrowseFirstAsync(Connection(),
                new BrowseFirstRequestModel { NodeId = "i=84" }, default);

            var payload = AssertCalled("Browse_V2");
            Assert.Equal("i=84", RequestOf(payload).GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task NodeBrowseNextCallsBrowseNextV2Async()
        {
            Returns(new BrowseNextResponseModel { References = Array.Empty<NodeReferenceModel>() });

            var result = await Client.NodeBrowseNextAsync(Connection(),
                new BrowseNextRequestModel { ContinuationToken = "token" }, default);

            var payload = AssertCalled("BrowseNext_V2");
            Assert.Equal("token", RequestOf(payload).GetProperty("continuationToken").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task NodeBrowsePathCallsBrowsePathV2Async()
        {
            Returns(new BrowsePathResponseModel());

            var result = await Client.NodeBrowsePathAsync(Connection(),
                new BrowsePathRequestModel { BrowsePaths = new[] { new[] { "Objects", "Server" } } },
                default);

            var payload = AssertCalled("BrowsePath_V2");
            Assert.Equal("Objects", RequestOf(payload).GetProperty("browsePaths")[0][0].GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task NodeReadCallsNodeReadV2Async()
        {
            Returns(new ReadResponseModel { Results = Array.Empty<AttributeReadResponseModel>() });

            var result = await Client.NodeReadAsync(Connection(), new ReadRequestModel
            {
                Attributes = new[]
                {
                    new AttributeReadRequestModel
                    {
                        NodeId = "ns=2;s=Tag1",
                        Attribute = NodeAttribute.Value
                    }
                }
            }, default);

            var payload = AssertCalled("NodeRead_V2");
            Assert.Equal("ns=2;s=Tag1",
                RequestOf(payload).GetProperty("attributes")[0].GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task NodeWriteCallsNodeWriteV2Async()
        {
            Returns(new WriteResponseModel { Results = Array.Empty<AttributeWriteResponseModel>() });

            var result = await Client.NodeWriteAsync(Connection(), new WriteRequestModel
            {
                Attributes = new[]
                {
                    new AttributeWriteRequestModel
                    {
                        NodeId = "ns=2;s=Tag1",
                        Attribute = NodeAttribute.Value,
                        Value = JsonValue.Create(123)!
                    }
                }
            }, default);

            var payload = AssertCalled("NodeWrite_V2");
            Assert.Equal(123,
                RequestOf(payload).GetProperty("attributes")[0].GetProperty("value").GetInt32());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task NodeValueReadCallsValueReadV2Async()
        {
            Returns(new ValueReadResponseModel());

            var result = await Client.NodeValueReadAsync(Connection(),
                new ValueReadRequestModel { NodeId = "ns=2;s=Tag1" }, default);

            var payload = AssertCalled("ValueRead_V2");
            Assert.Equal("ns=2;s=Tag1", RequestOf(payload).GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task NodeValueWriteCallsValueWriteV2Async()
        {
            Returns(new ValueWriteResponseModel());

            var result = await Client.NodeValueWriteAsync(Connection(), new ValueWriteRequestModel
            {
                NodeId = "ns=2;s=Tag1",
                Value = JsonValue.Create(456)!
            }, default);

            var payload = AssertCalled("ValueWrite_V2");
            Assert.Equal(456, RequestOf(payload).GetProperty("value").GetInt32());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task NodeMethodGetMetadataCallsMethodMetadataV2Async()
        {
            Returns(new MethodMetadataResponseModel());

            var result = await Client.NodeMethodGetMetadataAsync(Connection(),
                new MethodMetadataRequestModel { MethodId = "ns=2;s=Method" }, default);

            var payload = AssertCalled("MethodMetadata_V2");
            Assert.Equal("ns=2;s=Method", RequestOf(payload).GetProperty("methodId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task NodeMethodCallCallsMethodCallV2Async()
        {
            Returns(new MethodCallResponseModel { Results = Array.Empty<MethodCallArgumentModel>() });

            var result = await Client.NodeMethodCallAsync(Connection(),
                new MethodCallRequestModel { MethodId = "ns=2;s=Method" }, default);

            var payload = AssertCalled("MethodCall_V2");
            Assert.Equal("ns=2;s=Method", RequestOf(payload).GetProperty("methodId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetServerCapabilitiesCallsGetServerCapabilitiesV2Async()
        {
            Returns(new ServerCapabilitiesModel { OperationLimits = new OperationLimitsModel() });

            var result = await Client.GetServerCapabilitiesAsync(Connection(), null, default);

            var payload = AssertCalled("GetServerCapabilities_V2");
            Assert.Equal("opc.tcp://server:4840", EndpointUrlOf(payload));
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetMetadataCallsGetMetadataV2Async()
        {
            Returns(new NodeMetadataResponseModel());

            var result = await Client.GetMetadataAsync(Connection(),
                new NodeMetadataRequestModel { NodeId = "ns=2;s=Tag1" }, default);

            var payload = AssertCalled("GetMetadata_V2");
            Assert.Equal("ns=2;s=Tag1", RequestOf(payload).GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CompileQueryCallsCompileQueryV2Async()
        {
            Returns(new QueryCompilationResponseModel());

            var result = await Client.CompileQueryAsync(Connection(),
                new QueryCompilationRequestModel { Query = "SELECT * FROM Objects" }, default);

            var payload = AssertCalled("CompileQuery_V2");
            Assert.Equal("SELECT * FROM Objects", RequestOf(payload).GetProperty("query").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryGetServerCapabilitiesCallsHistoryGetServerCapabilitiesV2Async()
        {
            Returns(new HistoryServerCapabilitiesModel());

            var result = await Client.HistoryGetServerCapabilitiesAsync(Connection(), null, default);

            var payload = AssertCalled("HistoryGetServerCapabilities_V2");
            Assert.Equal("opc.tcp://server:4840", EndpointUrlOf(payload));
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryGetConfigurationCallsHistoryGetConfigurationV2Async()
        {
            Returns(new HistoryConfigurationResponseModel());

            var result = await Client.HistoryGetConfigurationAsync(Connection(),
                new HistoryConfigurationRequestModel { NodeId = "ns=2;s=History" }, default);

            var payload = AssertCalled("HistoryGetConfiguration_V2");
            Assert.Equal("ns=2;s=History", RequestOf(payload).GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadCallsHistoryReadV2Async()
        {
            Returns(new HistoryReadResponseModel<JsonNode> { History = JsonNode.Parse("[]")! });

            var result = await Client.HistoryReadAsync(Connection(),
                new HistoryReadRequestModel<JsonNode>
                {
                    NodeId = "ns=2;s=History",
                    Details = JsonObject.Parse("{\"kind\":\"read\"}")!
                }, default);

            var payload = AssertCalled("HistoryRead_V2");
            Assert.Equal("ns=2;s=History", RequestOf(payload).GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadNextCallsHistoryReadNextV2Async()
        {
            Returns(new HistoryReadNextResponseModel<JsonNode> { History = JsonNode.Parse("[]")! });

            var result = await Client.HistoryReadNextAsync(Connection(),
                new HistoryReadNextRequestModel { ContinuationToken = "next" }, default);

            var payload = AssertCalled("HistoryReadNext_V2");
            Assert.Equal("next", RequestOf(payload).GetProperty("continuationToken").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryUpdateCallsHistoryUpdateV2Async()
        {
            Returns(new HistoryUpdateResponseModel());

            var result = await Client.HistoryUpdateAsync(Connection(),
                new HistoryUpdateRequestModel<JsonNode>
                {
                    NodeId = "ns=2;s=History",
                    Details = JsonObject.Parse("{\"kind\":\"update\"}")!
                }, default);

            var payload = AssertCalled("HistoryUpdate_V2");
            Assert.Equal("ns=2;s=History", RequestOf(payload).GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ARejectedArgumentNeverReachesTheTransportAsync()
        {
            //
            // Validation has to happen before the call, not after: a missing
            // endpoint URL or required request part that travelled would be a
            // remote error for something knowable locally.
            //
            await Assert.ThrowsAsync<ArgumentException>(() => Client.NodeBrowseFirstAsync(
                new ConnectionModel { Endpoint = new EndpointModel { Url = string.Empty } },
                new BrowseFirstRequestModel(), default));
            await Assert.ThrowsAsync<ArgumentException>(() => Client.NodeBrowseNextAsync(Connection(),
                new BrowseNextRequestModel { ContinuationToken = null! }, default));
            await Assert.ThrowsAsync<ArgumentException>(() => Client.NodeReadAsync(Connection(),
                new ReadRequestModel { Attributes = Array.Empty<AttributeReadRequestModel>() }, default));
            await Assert.ThrowsAsync<ArgumentException>(() => Client.HistoryReadNextAsync(Connection(),
                new HistoryReadNextRequestModel { ContinuationToken = string.Empty }, default));

            AssertNotCalled();
        }

        [Fact]
        public async Task AResponseThatDeserializesToNullIsAnErrorAsync()
        {
            //
            // The transport can only say the call succeeded. A body of "null"
            // means the module answered with nothing where the signature
            // promises a value, so the client must not hand back a null that
            // the caller's non-nullable return type says cannot happen.
            //
            ReturnsRaw("null");

            await Assert.ThrowsAsync<MethodCallException>(() =>
                Client.NodeValueReadAsync(Connection(), new ValueReadRequestModel(), default));
        }

        [Fact]
        public void TheClientRequiresATransportAndATarget()
        {
            Assert.Throws<ArgumentNullException>(() => new TwinApiClient(null!, Target));
            Assert.Throws<ArgumentNullException>(() => new TwinApiClient(MethodClient.Object, (string)null!));
            Assert.Throws<ArgumentNullException>(() => new TwinApiClient(MethodClient.Object, string.Empty));
        }

        [Fact]
        public async Task TheOptionsConstructorUsesTheConfiguredTargetAndTimeoutAsync()
        {
            var timeout = TimeSpan.FromSeconds(42);
            var client = new TwinApiClient(MethodClient.Object,
                Options.Create(new SdkOptions { Target = Target, Timeout = timeout }));
            Returns(new ValueReadResponseModel());

            await client.NodeValueReadAsync(Connection(), new ValueReadRequestModel(), default);

            AssertCalled("ValueRead_V2");
            Assert.Equal(timeout, LastCall!.Timeout);
        }

        private static ConnectionModel Connection()
        {
            return new ConnectionModel { Endpoint = new EndpointModel { Url = "opc.tcp://server:4840" } };
        }

        private static JsonElement RequestOf(JsonElement payload)
        {
            return payload.GetProperty("request");
        }

        private static string? EndpointUrlOf(JsonElement payload)
        {
            return payload.GetProperty("connection").GetProperty("endpoint").GetProperty("url").GetString();
        }
    }
}
