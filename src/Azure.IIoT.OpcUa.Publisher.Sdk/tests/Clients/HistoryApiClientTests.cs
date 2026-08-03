// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Tests.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Microsoft.Extensions.Options;
    using System;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// The history client's contract with the module's method router: the
    /// method names it calls and the payloads it sends.
    /// </summary>
    /// <remarks>
    /// The method names carry a _V2 suffix that nothing in the client's own
    /// signature mentions. Getting one wrong is not a compile error and not a
    /// local failure - it is a call the module rejects at run time, which is
    /// why each one is named here explicitly.
    /// </remarks>
    public sealed class HistoryApiClientTests : ApiClientTestBase
    {
        private HistoryApiClient Client => new(MethodClient.Object, Target);

        [Fact]
        public async Task HistoryReadValuesCallsHistoryReadValuesV2Async()
        {
            Returns(new HistoryReadResponseModel<HistoricValueModel[]> { History = Array.Empty<HistoricValueModel>() });

            var result = await Client.HistoryReadValuesAsync(Connection(),
                ReadRequest(new ReadValuesDetailsModel { NumValues = 2 }), default);

            var payload = AssertCalled("HistoryReadValues_V2");
            Assert.Equal(2u, RequestOf(payload).GetProperty("details").GetProperty("numValues").GetUInt32());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadModifiedValuesCallsHistoryReadModifiedValuesV2Async()
        {
            Returns(new HistoryReadResponseModel<HistoricValueModel[]> { History = Array.Empty<HistoricValueModel>() });

            var result = await Client.HistoryReadModifiedValuesAsync(Connection(),
                ReadRequest(new ReadModifiedValuesDetailsModel { NumValues = 3 }), default);

            var payload = AssertCalled("HistoryReadModifiedValues_V2");
            Assert.Equal(3u, RequestOf(payload).GetProperty("details").GetProperty("numValues").GetUInt32());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadValuesAtTimesCallsHistoryReadValuesAtTimesV2Async()
        {
            Returns(new HistoryReadResponseModel<HistoricValueModel[]> { History = Array.Empty<HistoricValueModel>() });

            var result = await Client.HistoryReadValuesAtTimesAsync(Connection(),
                ReadRequest(new ReadValuesAtTimesDetailsModel
                {
                    ReqTimes = new[] { new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc) }
                }), default);

            var payload = AssertCalled("HistoryReadValuesAtTimes_V2");
            Assert.Equal(JsonValueKind.Array,
                RequestOf(payload).GetProperty("details").GetProperty("reqTimes").ValueKind);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadProcessedValuesCallsHistoryReadProcessedValuesV2Async()
        {
            Returns(new HistoryReadResponseModel<HistoricValueModel[]> { History = Array.Empty<HistoricValueModel>() });

            var result = await Client.HistoryReadProcessedValuesAsync(Connection(),
                ReadRequest(new ReadProcessedValuesDetailsModel { AggregateType = "Average" }), default);

            var payload = AssertCalled("HistoryReadProcessedValues_V2");
            Assert.Equal("Average",
                RequestOf(payload).GetProperty("details").GetProperty("aggregateType").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadValuesNextCallsHistoryReadValuesNextV2Async()
        {
            Returns(new HistoryReadNextResponseModel<HistoricValueModel[]> { History = Array.Empty<HistoricValueModel>() });

            var result = await Client.HistoryReadValuesNextAsync(Connection(),
                new HistoryReadNextRequestModel { ContinuationToken = "values-next" }, default);

            var payload = AssertCalled("HistoryReadValuesNext_V2");
            Assert.Equal("values-next", RequestOf(payload).GetProperty("continuationToken").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReplaceValuesCallsHistoryReplaceValuesV2Async()
        {
            Returns(new HistoryUpdateResponseModel());

            var result = await Client.HistoryReplaceValuesAsync(Connection(), UpdateValuesRequest(), default);

            var payload = AssertCalled("HistoryReplaceValues_V2");
            Assert.Equal("ns=2;s=History", RequestOf(payload).GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryInsertValuesCallsHistoryInsertValuesV2Async()
        {
            Returns(new HistoryUpdateResponseModel());

            var result = await Client.HistoryInsertValuesAsync(Connection(), UpdateValuesRequest(), default);

            var payload = AssertCalled("HistoryInsertValues_V2");
            Assert.Equal(JsonValueKind.Array,
                RequestOf(payload).GetProperty("details").GetProperty("values").ValueKind);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryUpsertValuesCallsHistoryUpsertValuesV2Async()
        {
            Returns(new HistoryUpdateResponseModel());

            var result = await Client.HistoryUpsertValuesAsync(Connection(), UpdateValuesRequest(), default);

            var payload = AssertCalled("HistoryUpsertValues_V2");
            Assert.Equal("ns=2;s=History", RequestOf(payload).GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryDeleteValuesCallsHistoryDeleteValuesV2Async()
        {
            Returns(new HistoryUpdateResponseModel());

            var result = await Client.HistoryDeleteValuesAsync(Connection(),
                UpdateRequest(new DeleteValuesDetailsModel { StartTime = DateTime.UtcNow }), default);

            var payload = AssertCalled("HistoryDeleteValues_V2");
            Assert.Equal(JsonValueKind.String,
                RequestOf(payload).GetProperty("details").GetProperty("startTime").ValueKind);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryDeleteModifiedValuesCallsHistoryDeleteModifiedValuesV2Async()
        {
            Returns(new HistoryUpdateResponseModel());

            var result = await Client.HistoryDeleteModifiedValuesAsync(Connection(),
                UpdateRequest(new DeleteValuesDetailsModel { EndTime = DateTime.UtcNow }), default);

            var payload = AssertCalled("HistoryDeleteModifiedValues_V2");
            Assert.Equal(JsonValueKind.String,
                RequestOf(payload).GetProperty("details").GetProperty("endTime").ValueKind);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryDeleteValuesAtTimesCallsHistoryDeleteValuesAtTimesV2Async()
        {
            Returns(new HistoryUpdateResponseModel());

            var result = await Client.HistoryDeleteValuesAtTimesAsync(Connection(),
                UpdateRequest(new DeleteValuesAtTimesDetailsModel { ReqTimes = new[] { DateTime.UtcNow } }), default);

            var payload = AssertCalled("HistoryDeleteValuesAtTimes_V2");
            Assert.Equal(JsonValueKind.Array,
                RequestOf(payload).GetProperty("details").GetProperty("reqTimes").ValueKind);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadEventsCallsHistoryReadEventsV2Async()
        {
            Returns(new HistoryReadResponseModel<HistoricEventModel[]> { History = Array.Empty<HistoricEventModel>() });

            var result = await Client.HistoryReadEventsAsync(Connection(),
                ReadRequest(new ReadEventsDetailsModel { NumEvents = 4 }), default);

            var payload = AssertCalled("HistoryReadEvents_V2");
            Assert.Equal(4u, RequestOf(payload).GetProperty("details").GetProperty("numEvents").GetUInt32());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadEventsNextCallsHistoryReadEventsNextV2Async()
        {
            Returns(new HistoryReadNextResponseModel<HistoricEventModel[]> { History = Array.Empty<HistoricEventModel>() });

            var result = await Client.HistoryReadEventsNextAsync(Connection(),
                new HistoryReadNextRequestModel { ContinuationToken = "events-next" }, default);

            var payload = AssertCalled("HistoryReadEventsNext_V2");
            Assert.Equal("events-next", RequestOf(payload).GetProperty("continuationToken").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReplaceEventsCallsHistoryReplaceEventsV2Async()
        {
            Returns(new HistoryUpdateResponseModel());

            var result = await Client.HistoryReplaceEventsAsync(Connection(), UpdateEventsRequest(), default);

            var payload = AssertCalled("HistoryReplaceEvents_V2");
            Assert.Equal(JsonValueKind.Array,
                RequestOf(payload).GetProperty("details").GetProperty("events").ValueKind);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryInsertEventsCallsHistoryInsertEventsV2Async()
        {
            Returns(new HistoryUpdateResponseModel());

            var result = await Client.HistoryInsertEventsAsync(Connection(), UpdateEventsRequest(), default);

            var payload = AssertCalled("HistoryInsertEvents_V2");
            Assert.Equal("ns=2;s=History", RequestOf(payload).GetProperty("nodeId").GetString());
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryUpsertEventsCallsHistoryUpsertEventsV2Async()
        {
            Returns(new HistoryUpdateResponseModel());

            var result = await Client.HistoryUpsertEventsAsync(Connection(), UpdateEventsRequest(), default);

            var payload = AssertCalled("HistoryUpsertEvents_V2");
            Assert.Equal(JsonValueKind.Array,
                RequestOf(payload).GetProperty("details").GetProperty("events").ValueKind);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryDeleteEventsCallsHistoryDeleteEventsV2Async()
        {
            Returns(new HistoryUpdateResponseModel());

            var result = await Client.HistoryDeleteEventsAsync(Connection(),
                UpdateRequest(new DeleteEventsDetailsModel { EventIds = new[] { new byte[] { 1, 2, 3 } } }), default);

            var payload = AssertCalled("HistoryDeleteEvents_V2");
            Assert.Equal(JsonValueKind.Array,
                RequestOf(payload).GetProperty("details").GetProperty("eventIds").ValueKind);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ARejectedArgumentNeverReachesTheTransportAsync()
        {
            //
            // Validation has to happen before the call, not after: a missing
            // endpoint URL, continuation, or details object that travelled
            // would be a remote error for something knowable locally.
            //
            await Assert.ThrowsAsync<ArgumentException>(() => Client.HistoryReadValuesAsync(
                new ConnectionModel { Endpoint = new EndpointModel { Url = string.Empty } },
                ReadRequest(new ReadValuesDetailsModel()), default));
            await Assert.ThrowsAsync<ArgumentException>(() => Client.HistoryReadValuesNextAsync(Connection(),
                new HistoryReadNextRequestModel { ContinuationToken = null! }, default));
            await Assert.ThrowsAsync<ArgumentException>(() => Client.HistoryDeleteValuesAsync(Connection(),
                new HistoryUpdateRequestModel<DeleteValuesDetailsModel> { Details = null! }, default));

            AssertNotCalled();
        }

        [Fact]
        public void TheClientRequiresATransportAndATarget()
        {
            Assert.Throws<ArgumentNullException>(() => new HistoryApiClient(null!, Target));
            Assert.Throws<ArgumentNullException>(() => new HistoryApiClient(MethodClient.Object, (string)null!));
            Assert.Throws<ArgumentNullException>(() => new HistoryApiClient(MethodClient.Object, string.Empty));
        }

        [Fact]
        public async Task TheOptionsConstructorUsesTheConfiguredTargetAndTimeoutAsync()
        {
            var timeout = TimeSpan.FromSeconds(42);
            var client = new HistoryApiClient(MethodClient.Object,
                Options.Create(new SdkOptions { Target = Target, Timeout = timeout }));
            Returns(new HistoryUpdateResponseModel());

            await client.HistoryDeleteValuesAsync(Connection(),
                UpdateRequest(new DeleteValuesDetailsModel()), default);

            AssertCalled("HistoryDeleteValues_V2");
            Assert.Equal(timeout, LastCall!.Timeout);
        }

        private static ConnectionModel Connection()
        {
            return new ConnectionModel { Endpoint = new EndpointModel { Url = "opc.tcp://server:4840" } };
        }

        private static HistoryReadRequestModel<T> ReadRequest<T>(T details) where T : class
        {
            return new HistoryReadRequestModel<T> { NodeId = "ns=2;s=History", Details = details };
        }

        private static HistoryUpdateRequestModel<T> UpdateRequest<T>(T details) where T : class
        {
            return new HistoryUpdateRequestModel<T> { NodeId = "ns=2;s=History", Details = details };
        }

        private static HistoryUpdateRequestModel<UpdateValuesDetailsModel> UpdateValuesRequest()
        {
            return UpdateRequest(new UpdateValuesDetailsModel
            {
                Values = new[] { new HistoricValueModel { Value = JsonValue.Create(1) } }
            });
        }

        private static HistoryUpdateRequestModel<UpdateEventsDetailsModel> UpdateEventsRequest()
        {
            return UpdateRequest(new UpdateEventsDetailsModel
            {
                Events = new[] { new HistoricEventModel { EventFields = Array.Empty<JsonNode?>() } }
            });
        }

        private static JsonElement RequestOf(JsonElement payload)
        {
            return payload.GetProperty("request");
        }
    }
}
