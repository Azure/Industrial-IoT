// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Microsoft.Extensions.Options;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="HistoryServices{T}"/> that verify the validation
    /// logic (argument checks) inside the decode/encode lambdas by capturing them
    /// through a mocked <see cref="INodeServicesInternal{T}"/>.
    /// </summary>
    public sealed class HistoryServicesTests
    {
        private const string kEndpoint = "opc.tcp://test:4840";

        private static HistoryServices<string> CreateSut(INodeServicesInternal<string> services)
        {
            return new HistoryServices<string>(Options.Create(new PublisherOptions()), services);
        }

        // ── HistoryDeleteEventsAsync ──────────────────────────────────────────

        [Fact]
        public async Task HistoryDeleteEventsAsyncThrowsWhenEventIdsAreNullAsync()
        {
            var mock = CreateUpdateMock<DeleteEventsDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<DeleteEventsDetailsModel>
            {
                Details = new DeleteEventsDetailsModel { EventIds = null! }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryDeleteEventsAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryDeleteEventsAsyncThrowsWhenEventIdsAreEmptyAsync()
        {
            var mock = CreateUpdateMock<DeleteEventsDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<DeleteEventsDetailsModel>
            {
                Details = new DeleteEventsDetailsModel { EventIds = [] }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryDeleteEventsAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── HistoryDeleteValuesAtTimesAsync ───────────────────────────────────

        [Fact]
        public async Task HistoryDeleteValuesAtTimesAsyncThrowsWhenReqTimesAreNullAsync()
        {
            var mock = CreateUpdateMock<DeleteValuesAtTimesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel>
            {
                Details = new DeleteValuesAtTimesDetailsModel { ReqTimes = null! }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryDeleteValuesAtTimesAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryDeleteValuesAtTimesAsyncThrowsWhenReqTimesAreEmptyAsync()
        {
            var mock = CreateUpdateMock<DeleteValuesAtTimesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel>
            {
                Details = new DeleteValuesAtTimesDetailsModel { ReqTimes = [] }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryDeleteValuesAtTimesAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── HistoryDeleteModifiedValuesAsync ──────────────────────────────────

        [Fact]
        public async Task HistoryDeleteModifiedValuesAsyncThrowsWhenBothTimesAreNullAsync()
        {
            var mock = CreateUpdateMock<DeleteValuesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<DeleteValuesDetailsModel>
            {
                Details = new DeleteValuesDetailsModel { StartTime = null, EndTime = null }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryDeleteModifiedValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── HistoryDeleteValuesAsync ──────────────────────────────────────────

        [Fact]
        public async Task HistoryDeleteValuesAsyncThrowsWhenBothTimesAreNullAsync()
        {
            var mock = CreateUpdateMock<DeleteValuesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<DeleteValuesDetailsModel>
            {
                Details = new DeleteValuesDetailsModel { StartTime = null, EndTime = null }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryDeleteValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── HistoryReadEventsAsync ────────────────────────────────────────────

        [Fact]
        public async Task HistoryReadEventsAsyncThrowsWhenBothTimesAreNullAsync()
        {
            var mock = CreateReadMock<ReadEventsDetailsModel, HistoricEventModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadEventsDetailsModel>
            {
                Details = new ReadEventsDetailsModel { StartTime = null, EndTime = null }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadEventsAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadEventsAsyncThrowsWhenOnlyStartTimeAndNoNumEventsAsync()
        {
            var mock = CreateReadMock<ReadEventsDetailsModel, HistoricEventModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadEventsDetailsModel>
            {
                Details = new ReadEventsDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = null,
                    NumEvents = 0
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadEventsAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadEventsAsyncThrowsWhenOnlyEndTimeAndNoNumEventsAsync()
        {
            var mock = CreateReadMock<ReadEventsDetailsModel, HistoricEventModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadEventsDetailsModel>
            {
                Details = new ReadEventsDetailsModel
                {
                    StartTime = null,
                    EndTime = DateTime.UtcNow,
                    NumEvents = null
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadEventsAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── HistoryReadValuesAsync ────────────────────────────────────────────

        [Fact]
        public async Task HistoryReadValuesAsyncThrowsWhenBothTimesAreNullAsync()
        {
            var mock = CreateReadMock<ReadValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesDetailsModel>
            {
                Details = new ReadValuesDetailsModel { StartTime = null, EndTime = null }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadValuesAsyncThrowsWhenOnlyStartTimeAndNoNumValuesAsync()
        {
            var mock = CreateReadMock<ReadValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesDetailsModel>
            {
                Details = new ReadValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = null,
                    NumValues = 0
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── HistoryReadValuesAtTimesAsync ─────────────────────────────────────

        [Fact]
        public async Task HistoryReadValuesAtTimesAsyncThrowsWhenReqTimesAreNullAsync()
        {
            var mock = CreateReadMock<ReadValuesAtTimesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                Details = new ReadValuesAtTimesDetailsModel { ReqTimes = null! }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadValuesAtTimesAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadValuesAtTimesAsyncThrowsWhenReqTimesAreEmptyAsync()
        {
            var mock = CreateReadMock<ReadValuesAtTimesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                Details = new ReadValuesAtTimesDetailsModel { ReqTimes = [] }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadValuesAtTimesAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── HistoryReadProcessedValuesAsync ───────────────────────────────────

        [Fact]
        public async Task HistoryReadProcessedValuesAsyncThrowsWhenBothTimesAreNullAsync()
        {
            var mock = CreateReadMock<ReadProcessedValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
            {
                Details = new ReadProcessedValuesDetailsModel { StartTime = null, EndTime = null }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadProcessedValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── HistoryReadModifiedValuesAsync ────────────────────────────────────

        [Fact]
        public async Task HistoryReadModifiedValuesAsyncThrowsWhenBothTimesAreNullAsync()
        {
            var mock = CreateReadMock<ReadModifiedValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
            {
                Details = new ReadModifiedValuesDetailsModel { StartTime = null, EndTime = null }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadModifiedValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReadModifiedValuesAsyncThrowsWhenOnlyStartTimeAndNoNumValuesAsync()
        {
            var mock = CreateReadMock<ReadModifiedValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
            {
                Details = new ReadModifiedValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = null,
                    NumValues = 0
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadModifiedValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── HistoryReplaceEventsAsync (via HistoryUpdateEventsAsync) ──────────

        [Fact]
        public async Task HistoryReplaceEventsAsyncThrowsWhenEventsAreNullAsync()
        {
            var mock = CreateUpdateMock<UpdateEventsDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<UpdateEventsDetailsModel>
            {
                Details = new UpdateEventsDetailsModel
                {
                    Events = null!,
                    Filter = null
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReplaceEventsAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReplaceEventsAsyncThrowsWhenEventsAreEmptyAsync()
        {
            var mock = CreateUpdateMock<UpdateEventsDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<UpdateEventsDetailsModel>
            {
                Details = new UpdateEventsDetailsModel
                {
                    Events = new List<HistoricEventModel>(),
                    Filter = null
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReplaceEventsAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── HistoryReplaceValuesAsync (via HistoryUpdateValuesAsync) ──────────

        [Fact]
        public async Task HistoryReplaceValuesAsyncThrowsWhenValuesAreNullAsync()
        {
            var mock = CreateUpdateMock<UpdateValuesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
            {
                Details = new UpdateValuesDetailsModel { Values = null! }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReplaceValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryReplaceValuesAsyncThrowsWhenValuesAreEmptyAsync()
        {
            var mock = CreateUpdateMock<UpdateValuesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
            {
                Details = new UpdateValuesDetailsModel { Values = new List<HistoricValueModel>() }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReplaceValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── Insert / Upsert variants delegate to the same HistoryUpdateAsync lambdas ──

        [Fact]
        public async Task HistoryInsertEventsAsyncThrowsWhenEventsAreNullAsync()
        {
            var mock = CreateUpdateMock<UpdateEventsDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<UpdateEventsDetailsModel>
            {
                Details = new UpdateEventsDetailsModel { Events = null!, Filter = null }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryInsertEventsAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryInsertValuesAsyncThrowsWhenValuesAreNullAsync()
        {
            var mock = CreateUpdateMock<UpdateValuesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
            {
                Details = new UpdateValuesDetailsModel { Values = null! }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryInsertValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── Happy paths — update lambdas (no session used) ────────────────────

        [Fact]
        public async Task HistoryDeleteEventsAsyncHappyPathSucceeds()
        {
            var mock = CreateUpdateMock<DeleteEventsDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<DeleteEventsDetailsModel>
            {
                Details = new DeleteEventsDetailsModel
                {
                    EventIds = [new byte[] { 0x01, 0x02 }]
                }
            };

            var result = await sut.HistoryDeleteEventsAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryDeleteValuesAtTimesAsyncHappyPathSucceeds()
        {
            var mock = CreateUpdateMock<DeleteValuesAtTimesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel>
            {
                Details = new DeleteValuesAtTimesDetailsModel
                {
                    ReqTimes = [DateTime.UtcNow]
                }
            };

            var result = await sut.HistoryDeleteValuesAtTimesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryDeleteModifiedValuesAsyncHappyPathWithStartTimeSucceeds()
        {
            var mock = CreateUpdateMock<DeleteValuesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<DeleteValuesDetailsModel>
            {
                Details = new DeleteValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = null
                }
            };

            var result = await sut.HistoryDeleteModifiedValuesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryDeleteValuesAsyncHappyPathWithEndTimeSucceeds()
        {
            var mock = CreateUpdateMock<DeleteValuesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<DeleteValuesDetailsModel>
            {
                Details = new DeleteValuesDetailsModel
                {
                    StartTime = null,
                    EndTime = DateTime.UtcNow
                }
            };

            var result = await sut.HistoryDeleteValuesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        // ── Happy paths — read lambdas (session param is '_' / ignored) ────────

        [Fact]
        public async Task HistoryReadValuesAsyncHappyPathWithBothTimesSucceeds()
        {
            var mock = CreateReadMock<ReadValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesDetailsModel>
            {
                Details = new ReadValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                }
            };

            var result = await sut.HistoryReadValuesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadValuesAsyncHappyPathWithStartTimeAndNumValuesSucceeds()
        {
            var mock = CreateReadMock<ReadValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesDetailsModel>
            {
                Details = new ReadValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = null,
                    NumValues = 10
                }
            };

            var result = await sut.HistoryReadValuesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadValuesAtTimesAsyncHappyPathSucceeds()
        {
            var mock = CreateReadMock<ReadValuesAtTimesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                Details = new ReadValuesAtTimesDetailsModel
                {
                    ReqTimes = [DateTime.UtcNow]
                }
            };

            var result = await sut.HistoryReadValuesAtTimesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadModifiedValuesAsyncHappyPathWithBothTimesSucceeds()
        {
            var mock = CreateReadMock<ReadModifiedValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
            {
                Details = new ReadModifiedValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                }
            };

            var result = await sut.HistoryReadModifiedValuesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadProcessedValuesAsyncHappyPathWithoutAggregateTypeSucceeds()
        {
            var mock = CreateReadMock<ReadProcessedValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
            {
                Details = new ReadProcessedValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    AggregateType = null     // no aggregate → session is never accessed
                }
            };

            var result = await sut.HistoryReadProcessedValuesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        // ── Stream methods — with items yielded ───────────────────────────────

        [Fact]
        public async Task HistoryStreamValuesAsyncYieldsItemsFromFirstPage()
        {
            var mock = CreateStreamReadMock<ReadValuesDetailsModel, HistoricValueModel[]>(
                [new HistoricValueModel(), new HistoricValueModel()]);
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesDetailsModel>
            {
                Details = new ReadValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                }
            };

            var items = new List<HistoricValueModel>();
            await foreach (var item in sut.HistoryStreamValuesAsync(
                kEndpoint, request, CancellationToken.None))
            {
                items.Add(item);
            }

            Assert.Equal(2, items.Count);
        }

        [Fact]
        public async Task HistoryStreamModifiedValuesAsyncYieldsItemsFromFirstPage()
        {
            var mock = CreateStreamReadMock<ReadModifiedValuesDetailsModel, HistoricValueModel[]>(
                [new HistoricValueModel()]);
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
            {
                Details = new ReadModifiedValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                }
            };

            var items = new List<HistoricValueModel>();
            await foreach (var item in sut.HistoryStreamModifiedValuesAsync(
                kEndpoint, request, CancellationToken.None))
            {
                items.Add(item);
            }

            Assert.Single(items);
        }

        [Fact]
        public async Task HistoryStreamValuesAtTimesAsyncYieldsItemsFromFirstPage()
        {
            var mock = CreateStreamReadMock<ReadValuesAtTimesDetailsModel, HistoricValueModel[]>(
                [new HistoricValueModel()]);
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                Details = new ReadValuesAtTimesDetailsModel { ReqTimes = [DateTime.UtcNow] }
            };

            var items = new List<HistoricValueModel>();
            await foreach (var item in sut.HistoryStreamValuesAtTimesAsync(
                kEndpoint, request, CancellationToken.None))
            {
                items.Add(item);
            }

            Assert.Single(items);
        }

        [Fact]
        public async Task HistoryStreamProcessedValuesAsyncYieldsItemsFromFirstPage()
        {
            var mock = CreateStreamReadMock<ReadProcessedValuesDetailsModel, HistoricValueModel[]>(
                [new HistoricValueModel()]);
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
            {
                Details = new ReadProcessedValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                }
            };

            var items = new List<HistoricValueModel>();
            await foreach (var item in sut.HistoryStreamProcessedValuesAsync(
                kEndpoint, request, CancellationToken.None))
            {
                items.Add(item);
            }

            Assert.Single(items);
        }

        [Fact]
        public async Task HistoryStreamEventsAsyncYieldsItemsFromFirstPage()
        {
            var mock = CreateStreamReadMock<ReadEventsDetailsModel, HistoricEventModel[]>(
                [new HistoricEventModel { EventFields = [] }]);
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadEventsDetailsModel>
            {
                Details = new ReadEventsDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                }
            };

            var items = new List<HistoricEventModel>();
            await foreach (var item in sut.HistoryStreamEventsAsync(
                kEndpoint, request, CancellationToken.None))
            {
                items.Add(item);
            }

            Assert.Single(items);
        }

        [Fact]
        public async Task HistoryStreamValuesAsyncWithNullHistoryYieldsNothing()
        {
            var mock = CreateStreamReadMock<ReadValuesDetailsModel, HistoricValueModel[]>(null);
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesDetailsModel>
            {
                Details = new ReadValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                }
            };

            var items = new List<HistoricValueModel>();
            await foreach (var item in sut.HistoryStreamValuesAsync(
                kEndpoint, request, CancellationToken.None))
            {
                items.Add(item);
            }

            Assert.Empty(items);
        }

        // ── Next-page methods ─────────────────────────────────────────────────

        [Fact]
        public async Task HistoryReadValuesNextAsyncDelegatesToNextAsyncAsync()
        {
            var mock = new Mock<INodeServicesInternal<string>>();
            mock.Setup(s => s.HistoryReadNextAsync(
                    It.IsAny<string>(),
                    It.IsAny<HistoryReadNextRequestModel>(),
                    It.IsAny<Func<ExtensionObject, IOpcUaSession, HistoricValueModel[]>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HistoryReadNextResponseModel<HistoricValueModel[]>
                {
                    History = [new HistoricValueModel()],
                    ContinuationToken = null
                });

            var sut = CreateSut(mock.Object);
            var result = await sut.HistoryReadValuesNextAsync(kEndpoint,
                new HistoryReadNextRequestModel { ContinuationToken = "tok" },
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Single(result.History!);
        }

        [Fact]
        public async Task HistoryReadEventsNextAsyncDelegatesToNextAsyncAsync()
        {
            var mock = new Mock<INodeServicesInternal<string>>();
            mock.Setup(s => s.HistoryReadNextAsync(
                    It.IsAny<string>(),
                    It.IsAny<HistoryReadNextRequestModel>(),
                    It.IsAny<Func<ExtensionObject, IOpcUaSession, HistoricEventModel[]>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HistoryReadNextResponseModel<HistoricEventModel[]>
                {
                    History = [new HistoricEventModel { EventFields = [] }],
                    ContinuationToken = null
                });

            var sut = CreateSut(mock.Object);
            var result = await sut.HistoryReadEventsNextAsync(kEndpoint,
                new HistoryReadNextRequestModel { ContinuationToken = "tok" },
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Single(result.History!);
        }

        // ── Upsert variants ───────────────────────────────────────────────────

        [Fact]
        public async Task HistoryUpsertEventsAsyncThrowsWhenEventsAreNullAsync()
        {
            var mock = CreateUpdateMock<UpdateEventsDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<UpdateEventsDetailsModel>
            {
                Details = new UpdateEventsDetailsModel { Events = null!, Filter = null }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryUpsertEventsAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryUpsertEventsAsyncThrowsWhenEventsAreEmptyAsync()
        {
            var mock = CreateUpdateMock<UpdateEventsDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<UpdateEventsDetailsModel>
            {
                Details = new UpdateEventsDetailsModel
                {
                    Events = new List<HistoricEventModel>(),
                    Filter = null
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryUpsertEventsAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryUpsertValuesAsyncThrowsWhenValuesAreNullAsync()
        {
            var mock = CreateUpdateMock<UpdateValuesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
            {
                Details = new UpdateValuesDetailsModel { Values = null! }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryUpsertValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        [Fact]
        public async Task HistoryUpsertValuesAsyncThrowsWhenValuesAreEmptyAsync()
        {
            var mock = CreateUpdateMock<UpdateValuesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
            {
                Details = new UpdateValuesDetailsModel { Values = new List<HistoricValueModel>() }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryUpsertValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── Multi-page streaming ──────────────────────────────────────────────

        [Fact]
        public async Task HistoryStreamValuesAsyncFollowsContinuationTokenAsync()
        {
            var mock = new Mock<INodeServicesInternal<string>>();
            // First page returns 1 item + a continuation token.
            mock.Setup(s => s.HistoryReadAsync(
                    It.IsAny<string>(),
                    It.IsAny<HistoryReadRequestModel<ReadValuesDetailsModel>>(),
                    It.IsAny<Func<ReadValuesDetailsModel, IOpcUaSession, ExtensionObject>>(),
                    It.IsAny<Func<ExtensionObject, IOpcUaSession, HistoricValueModel[]>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HistoryReadResponseModel<HistoricValueModel[]>
                {
                    History = [new HistoricValueModel()],
                    ContinuationToken = "page2"
                });
            // Continuation page returns 1 item + no more tokens.
            mock.Setup(s => s.HistoryReadNextAsync(
                    It.IsAny<string>(),
                    It.IsAny<HistoryReadNextRequestModel>(),
                    It.IsAny<Func<ExtensionObject, IOpcUaSession, HistoricValueModel[]>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HistoryReadNextResponseModel<HistoricValueModel[]>
                {
                    History = [new HistoricValueModel()],
                    ContinuationToken = null
                });

            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesDetailsModel>
            {
                Details = new ReadValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                }
            };

            var items = new List<HistoricValueModel>();
            await foreach (var item in sut.HistoryStreamValuesAsync(
                kEndpoint, request, CancellationToken.None))
            {
                items.Add(item);
            }

            Assert.Equal(2, items.Count);
            mock.Verify(s => s.HistoryReadNextAsync(
                It.IsAny<string>(),
                It.Is<HistoryReadNextRequestModel>(r => r.ContinuationToken == "page2"),
                It.IsAny<Func<ExtensionObject, IOpcUaSession, HistoricValueModel[]>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HistoryStreamEventsAsyncFollowsContinuationTokenAsync()
        {
            var mock = new Mock<INodeServicesInternal<string>>();
            mock.Setup(s => s.HistoryReadAsync(
                    It.IsAny<string>(),
                    It.IsAny<HistoryReadRequestModel<ReadEventsDetailsModel>>(),
                    It.IsAny<Func<ReadEventsDetailsModel, IOpcUaSession, ExtensionObject>>(),
                    It.IsAny<Func<ExtensionObject, IOpcUaSession, HistoricEventModel[]>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HistoryReadResponseModel<HistoricEventModel[]>
                {
                    History = [new HistoricEventModel { EventFields = [] }],
                    ContinuationToken = "pageE2"
                });
            mock.Setup(s => s.HistoryReadNextAsync(
                    It.IsAny<string>(),
                    It.IsAny<HistoryReadNextRequestModel>(),
                    It.IsAny<Func<ExtensionObject, IOpcUaSession, HistoricEventModel[]>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HistoryReadNextResponseModel<HistoricEventModel[]>
                {
                    History = null,
                    ContinuationToken = null
                });

            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadEventsDetailsModel>
            {
                Details = new ReadEventsDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                }
            };

            var items = new List<HistoricEventModel>();
            await foreach (var item in sut.HistoryStreamEventsAsync(
                kEndpoint, request, CancellationToken.None))
            {
                items.Add(item);
            }

            Assert.Single(items);
        }

        // ── HistoryReadModifiedValuesAsync edge cases ─────────────────────────

        [Fact]
        public async Task HistoryReadModifiedValuesAsyncHappyPathWithOnlyEndTimeAndNumValuesSucceeds()
        {
            var mock = CreateReadMock<ReadModifiedValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
            {
                Details = new ReadModifiedValuesDetailsModel
                {
                    StartTime = null,
                    EndTime = DateTime.UtcNow,
                    NumValues = 5
                }
            };

            var result = await sut.HistoryReadModifiedValuesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadModifiedValuesAsyncThrowsWhenOnlyEndTimeAndNoNumValuesAsync()
        {
            var mock = CreateReadMock<ReadModifiedValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
            {
                Details = new ReadModifiedValuesDetailsModel
                {
                    StartTime = null,
                    EndTime = DateTime.UtcNow,
                    NumValues = null
                }
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.HistoryReadModifiedValuesAsync(kEndpoint, request, CancellationToken.None));
        }

        // ── HistoryReadValuesAsync edge: only EndTime + NumValues ─────────────

        [Fact]
        public async Task HistoryReadValuesAsyncHappyPathWithEndTimeAndNumValuesSucceeds()
        {
            var mock = CreateReadMock<ReadValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadValuesDetailsModel>
            {
                Details = new ReadValuesDetailsModel
                {
                    StartTime = null,
                    EndTime = DateTime.UtcNow,
                    NumValues = 10
                }
            };

            var result = await sut.HistoryReadValuesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        // ── HistoryReadEventsAsync edge: StartTime + EndTime happy path ────────
        // These use CreateStreamReadMock because the HistoryReadEventsAsync decode lambda
        // always accesses session.Codec.Decode() even when Filter is null, so a null
        // session would cause a NullReferenceException before the result is returned.

        [Fact]
        public async Task HistoryReadEventsAsyncHappyPathWithBothTimesSucceeds()
        {
            var mock = CreateStreamReadMock<ReadEventsDetailsModel, HistoricEventModel[]>(null);
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadEventsDetailsModel>
            {
                Details = new ReadEventsDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                }
            };

            var result = await sut.HistoryReadEventsAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task HistoryReadEventsAsyncHappyPathWithStartTimeAndNumEventsSucceeds()
        {
            var mock = CreateStreamReadMock<ReadEventsDetailsModel, HistoricEventModel[]>(null);
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadEventsDetailsModel>
            {
                Details = new ReadEventsDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = null,
                    NumEvents = 5
                }
            };

            var result = await sut.HistoryReadEventsAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        // ── HistoryDeleteModifiedValuesAsync edge: only EndTime ────────────────

        [Fact]
        public async Task HistoryDeleteModifiedValuesAsyncHappyPathWithEndTimeSucceeds()
        {
            var mock = CreateUpdateMock<DeleteValuesDetailsModel>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryUpdateRequestModel<DeleteValuesDetailsModel>
            {
                Details = new DeleteValuesDetailsModel
                {
                    StartTime = null,
                    EndTime = DateTime.UtcNow
                }
            };

            var result = await sut.HistoryDeleteModifiedValuesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        // ── HistoryReadProcessedValuesAsync — BothTimes but no aggregate ───────

        [Fact]
        public async Task HistoryReadProcessedValuesAsyncHappyPathWithOnlyStartTimeSucceeds()
        {
            var mock = CreateReadMock<ReadProcessedValuesDetailsModel, HistoricValueModel[]>();
            var sut = CreateSut(mock.Object);
            var request = new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
            {
                Details = new ReadProcessedValuesDetailsModel
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = null,
                    AggregateType = null
                }
            };

            var result = await sut.HistoryReadProcessedValuesAsync(
                kEndpoint, request, CancellationToken.None);

            Assert.NotNull(result);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a mock that returns a fixed history page without invoking
        /// the decode/encode lambdas. Use this for testing streaming methods
        /// where the focus is on the iteration logic, not the decode step.
        /// </summary>
        private static Mock<INodeServicesInternal<string>> CreateStreamReadMock<TDetails, TOutput>(
            TOutput? history)
            where TDetails : class
            where TOutput : class
        {
            var mock = new Mock<INodeServicesInternal<string>>();
            mock.Setup(s => s.HistoryReadAsync(
                    It.IsAny<string>(),
                    It.IsAny<HistoryReadRequestModel<TDetails>>(),
                    It.IsAny<Func<TDetails, IOpcUaSession, ExtensionObject>>(),
                    It.IsAny<Func<ExtensionObject, IOpcUaSession, TOutput>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HistoryReadResponseModel<TOutput>
                {
                    History = history,
                    ContinuationToken = null
                });
            return mock;
        }

        /// <summary>
        /// Creates a mock that captures the decode lambda passed to
        /// <see cref="INodeServicesInternal{T}.HistoryUpdateAsync"/> and immediately
        /// invokes it so validation code inside the lambda is exercised.
        /// </summary>
        private static Mock<INodeServicesInternal<string>> CreateUpdateMock<TDetails>()
            where TDetails : class
        {
            var mock = new Mock<INodeServicesInternal<string>>();
            mock.Setup(s => s.HistoryUpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<HistoryUpdateRequestModel<TDetails>>(),
                    It.IsAny<Func<NodeId, TDetails, IOpcUaSession, Task<ExtensionObject>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string,
                    HistoryUpdateRequestModel<TDetails>,
                    Func<NodeId, TDetails, IOpcUaSession, Task<ExtensionObject>>,
                    CancellationToken>(async (_, req, decode, ct) =>
                {
                    // Invoke the real lambda captured from HistoryServices.
                    // Null is safe for session because validation throws before accessing it.
                    await decode(NodeId.Null, req.Details!, null!);
                    return new HistoryUpdateResponseModel();
                });
            return mock;
        }

        /// <summary>
        /// Creates a mock that captures the decode lambda passed to
        /// <see cref="INodeServicesInternal{T}.HistoryReadAsync"/> and immediately
        /// invokes it so validation code inside the lambda is exercised.
        /// </summary>
        private static Mock<INodeServicesInternal<string>> CreateReadMock<TDetails, TOutput>()
            where TDetails : class
            where TOutput : class
        {
            var mock = new Mock<INodeServicesInternal<string>>();
            mock.Setup(s => s.HistoryReadAsync(
                    It.IsAny<string>(),
                    It.IsAny<HistoryReadRequestModel<TDetails>>(),
                    It.IsAny<Func<TDetails, IOpcUaSession, ExtensionObject>>(),
                    It.IsAny<Func<ExtensionObject, IOpcUaSession, TOutput>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string,
                    HistoryReadRequestModel<TDetails>,
                    Func<TDetails, IOpcUaSession, ExtensionObject>,
                    Func<ExtensionObject, IOpcUaSession, TOutput>,
                    CancellationToken>((_, req, decode, encode, ct) =>
                {
                    // Invoke the real lambda; null is safe when validation throws first.
                    decode(req.Details!, null!);
                    return Task.FromResult(new HistoryReadResponseModel<TOutput> { History = null });
                });
            return mock;
        }
    }
}
