// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class HistoryControllerUnitTests
    {
        [Fact]
        public void ConstructorThrowsWhenHistorianIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new HistoryController(null!));

            Assert.Equal("historian", exception.ParamName);
        }

        [Fact]
        public async Task HistoryUpsertValuesDelegatesConnectionRequestAndCancellationAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
            {
                Details = null!
            };
            var expected = new HistoryUpdateResponseModel();
            using var cts = new CancellationTokenSource();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryUpsertValuesAsync(connection, request, cts.Token))
                .ReturnsAsync(expected).Verifiable();
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryUpsertValuesAsync(
                new RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                }, cts.Token);

            Assert.Same(expected, actual);
            history.Verify();
        }

        [Fact]
        public async Task HistoryUpsertValuesPropagatesHistorianFailureAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
            {
                Details = null!
            };
            var expected = new InvalidOperationException("update failed");
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryUpsertValuesAsync(connection, request,
                    CancellationToken.None))
                .Returns(Task.FromException<HistoryUpdateResponseModel>(expected));
            var controller = new HistoryController(history.Object);

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await controller.HistoryUpsertValuesAsync(
                    new RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>>
                    {
                        Connection = connection,
                        Request = request
                    }));

            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task HistoryUpsertValuesThrowsWhenEnvelopeIsNullAsync()
        {
            var controller = CreateController();

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await controller.HistoryUpsertValuesAsync(null!));

            Assert.Equal("request", exception.ParamName);
        }

        [Fact]
        public async Task HistoryUpsertValuesThrowsWhenConnectionIsNullAsync()
        {
            var controller = CreateController();

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await controller.HistoryUpsertValuesAsync(
                    new RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>>
                    {
                        Connection = null!,
                        Request = new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
                        {
                            Details = null!
                        }
                    }));

            Assert.Equal("request.Connection", exception.ParamName);
        }

        [Fact]
        public async Task HistoryUpsertValuesThrowsWhenInnerRequestIsNullAsync()
        {
            var controller = CreateController();

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await controller.HistoryUpsertValuesAsync(
                    new RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>>
                    {
                        Connection = CreateConnection(),
                        Request = null
                    }));

            Assert.Equal("request.Request", exception.ParamName);
        }

        [Fact]
        public void HistoryStreamValuesReturnsHistorianStream()
        {
            var connection = CreateConnection();
            var request = new HistoryReadRequestModel<ReadValuesDetailsModel>
            {
                Details = new ReadValuesDetailsModel()
            };
            var expected = GetValuesAsync();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryStreamValuesAsync(connection, request,
                    CancellationToken.None))
                .Returns(expected);
            var controller = new HistoryController(history.Object);

            var actual = controller.HistoryStreamValuesAsync(
                new RequestEnvelope<HistoryReadRequestModel<ReadValuesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        [Fact]
        public void HistoryStreamValuesThrowsWhenInnerRequestIsNull()
        {
            var controller = CreateController();

            var exception = Assert.Throws<ArgumentNullException>(
                () => controller.HistoryStreamValuesAsync(
                    new RequestEnvelope<HistoryReadRequestModel<ReadValuesDetailsModel>>
                    {
                        Connection = CreateConnection(),
                        Request = null
                    }));

            Assert.Equal("request.Request", exception.ParamName);
        }

        private static HistoryController CreateController()
        {
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            return new HistoryController(history.Object);
        }

        private static ConnectionModel CreateConnection()
        {
            return new ConnectionModel
            {
                Endpoint = new EndpointModel
                {
                    Url = "opc.tcp://localhost:4840"
                }
            };
        }

        private static async IAsyncEnumerable<HistoricValueModel> GetValuesAsync()
        {
            await Task.Yield();
            yield return new HistoricValueModel();
        }

        private static async IAsyncEnumerable<HistoricEventModel> GetEventsAsync()
        {
            await Task.Yield();
            yield return new HistoricEventModel { EventFields = Array.Empty<JsonNode?>() };
        }

        // ── HistoryReplaceEvents ─────────────────────────────────────────────

        [Fact]
        public async Task HistoryReplaceEventsDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryUpdateRequestModel<UpdateEventsDetailsModel>
            {
                Details = new UpdateEventsDetailsModel { Events = Array.Empty<HistoricEventModel>() }
            };
            var expected = new HistoryUpdateResponseModel();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryReplaceEventsAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryReplaceEventsAsync(
                new RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task HistoryReplaceEventsThrowsForNullRequestAsync()
        {
            var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => controller.HistoryReplaceEventsAsync(null!));
        }

        // ── HistoryInsertEvents ──────────────────────────────────────────────

        [Fact]
        public async Task HistoryInsertEventsDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryUpdateRequestModel<UpdateEventsDetailsModel>
            {
                Details = new UpdateEventsDetailsModel { Events = Array.Empty<HistoricEventModel>() }
            };
            var expected = new HistoryUpdateResponseModel();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryInsertEventsAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryInsertEventsAsync(
                new RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryUpsertEvents ──────────────────────────────────────────────

        [Fact]
        public async Task HistoryUpsertEventsDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryUpdateRequestModel<UpdateEventsDetailsModel>
            {
                Details = new UpdateEventsDetailsModel { Events = Array.Empty<HistoricEventModel>() }
            };
            var expected = new HistoryUpdateResponseModel();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryUpsertEventsAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryUpsertEventsAsync(
                new RequestEnvelope<HistoryUpdateRequestModel<UpdateEventsDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryDeleteEvents ──────────────────────────────────────────────

        [Fact]
        public async Task HistoryDeleteEventsDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryUpdateRequestModel<DeleteEventsDetailsModel>
            {
                Details = new DeleteEventsDetailsModel { EventIds = Array.Empty<byte[]>() }
            };
            var expected = new HistoryUpdateResponseModel();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryDeleteEventsAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryDeleteEventsAsync(
                new RequestEnvelope<HistoryUpdateRequestModel<DeleteEventsDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryDeleteValuesAtTimes ───────────────────────────────────────

        [Fact]
        public async Task HistoryDeleteValuesAtTimesDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel>
            {
                Details = new DeleteValuesAtTimesDetailsModel { ReqTimes = Array.Empty<DateTime>() }
            };
            var expected = new HistoryUpdateResponseModel();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryDeleteValuesAtTimesAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryDeleteValuesAtTimesAsync(
                new RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryDeleteModifiedValues ──────────────────────────────────────

        [Fact]
        public async Task HistoryDeleteModifiedValuesDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryUpdateRequestModel<DeleteValuesDetailsModel>
            {
                Details = new DeleteValuesDetailsModel()
            };
            var expected = new HistoryUpdateResponseModel();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryDeleteModifiedValuesAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryDeleteModifiedValuesAsync(
                new RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryDeleteValues ──────────────────────────────────────────────

        [Fact]
        public async Task HistoryDeleteValuesDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryUpdateRequestModel<DeleteValuesDetailsModel>
            {
                Details = new DeleteValuesDetailsModel()
            };
            var expected = new HistoryUpdateResponseModel();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryDeleteValuesAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryDeleteValuesAsync(
                new RequestEnvelope<HistoryUpdateRequestModel<DeleteValuesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryReplaceValues ─────────────────────────────────────────────

        [Fact]
        public async Task HistoryReplaceValuesDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
            {
                Details = new UpdateValuesDetailsModel { Values = Array.Empty<HistoricValueModel>() }
            };
            var expected = new HistoryUpdateResponseModel();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryReplaceValuesAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryReplaceValuesAsync(
                new RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryInsertValues ──────────────────────────────────────────────

        [Fact]
        public async Task HistoryInsertValuesDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryUpdateRequestModel<UpdateValuesDetailsModel>
            {
                Details = new UpdateValuesDetailsModel { Values = Array.Empty<HistoricValueModel>() }
            };
            var expected = new HistoryUpdateResponseModel();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryInsertValuesAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryInsertValuesAsync(
                new RequestEnvelope<HistoryUpdateRequestModel<UpdateValuesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryReadEvents ────────────────────────────────────────────────

        [Fact]
        public async Task HistoryReadEventsDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryReadRequestModel<ReadEventsDetailsModel>
            {
                Details = new ReadEventsDetailsModel()
            };
            var expected = new HistoryReadResponseModel<HistoricEventModel[]> { History = null };
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryReadEventsAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryReadEventsAsync(
                new RequestEnvelope<HistoryReadRequestModel<ReadEventsDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryReadEventsNext ────────────────────────────────────────────

        [Fact]
        public async Task HistoryReadEventsNextDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryReadNextRequestModel { ContinuationToken = "token" };
            var expected = new HistoryReadNextResponseModel<HistoricEventModel[]> { History = null };
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryReadEventsNextAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryReadEventsNextAsync(
                new RequestEnvelope<HistoryReadNextRequestModel>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryReadValues ────────────────────────────────────────────────

        [Fact]
        public async Task HistoryReadValuesDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryReadRequestModel<ReadValuesDetailsModel>
            {
                Details = new ReadValuesDetailsModel()
            };
            var expected = new HistoryReadResponseModel<HistoricValueModel[]> { History = null };
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryReadValuesAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryReadValuesAsync(
                new RequestEnvelope<HistoryReadRequestModel<ReadValuesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryReadValuesAtTimes ─────────────────────────────────────────

        [Fact]
        public async Task HistoryReadValuesAtTimesDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                Details = new ReadValuesAtTimesDetailsModel { ReqTimes = Array.Empty<DateTime>() }
            };
            var expected = new HistoryReadResponseModel<HistoricValueModel[]> { History = null };
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryReadValuesAtTimesAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryReadValuesAtTimesAsync(
                new RequestEnvelope<HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryReadProcessedValues ───────────────────────────────────────

        [Fact]
        public async Task HistoryReadProcessedValuesDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
            {
                Details = new ReadProcessedValuesDetailsModel()
            };
            var expected = new HistoryReadResponseModel<HistoricValueModel[]> { History = null };
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryReadProcessedValuesAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryReadProcessedValuesAsync(
                new RequestEnvelope<HistoryReadRequestModel<ReadProcessedValuesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryReadModifiedValues ────────────────────────────────────────

        [Fact]
        public async Task HistoryReadModifiedValuesDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
            {
                Details = new ReadModifiedValuesDetailsModel()
            };
            var expected = new HistoryReadResponseModel<HistoricValueModel[]> { History = null };
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryReadModifiedValuesAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryReadModifiedValuesAsync(
                new RequestEnvelope<HistoryReadRequestModel<ReadModifiedValuesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── HistoryReadValuesNext ────────────────────────────────────────────

        [Fact]
        public async Task HistoryReadValuesNextDelegatesAsync()
        {
            var connection = CreateConnection();
            var request = new HistoryReadNextRequestModel { ContinuationToken = "token" };
            var expected = new HistoryReadNextResponseModel<HistoricValueModel[]> { History = null };
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryReadValuesNextAsync(connection, request,
                CancellationToken.None)).ReturnsAsync(expected);
            var controller = new HistoryController(history.Object);

            var actual = await controller.HistoryReadValuesNextAsync(
                new RequestEnvelope<HistoryReadNextRequestModel>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        // ── Stream methods ───────────────────────────────────────────────────

        [Fact]
        public void HistoryStreamModifiedValuesReturnsHistorianStream()
        {
            var connection = CreateConnection();
            var request = new HistoryReadRequestModel<ReadModifiedValuesDetailsModel>
            {
                Details = new ReadModifiedValuesDetailsModel()
            };
            var expected = GetValuesAsync();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryStreamModifiedValuesAsync(connection, request,
                CancellationToken.None)).Returns(expected);
            var controller = new HistoryController(history.Object);

            var actual = controller.HistoryStreamModifiedValuesAsync(
                new RequestEnvelope<HistoryReadRequestModel<ReadModifiedValuesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        [Fact]
        public void HistoryStreamValuesAtTimesReturnsHistorianStream()
        {
            var connection = CreateConnection();
            var request = new HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>
            {
                Details = new ReadValuesAtTimesDetailsModel { ReqTimes = Array.Empty<DateTime>() }
            };
            var expected = GetValuesAsync();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryStreamValuesAtTimesAsync(connection, request,
                CancellationToken.None)).Returns(expected);
            var controller = new HistoryController(history.Object);

            var actual = controller.HistoryStreamValuesAtTimesAsync(
                new RequestEnvelope<HistoryReadRequestModel<ReadValuesAtTimesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        [Fact]
        public void HistoryStreamProcessedValuesReturnsHistorianStream()
        {
            var connection = CreateConnection();
            var request = new HistoryReadRequestModel<ReadProcessedValuesDetailsModel>
            {
                Details = new ReadProcessedValuesDetailsModel()
            };
            var expected = GetValuesAsync();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryStreamProcessedValuesAsync(connection, request,
                CancellationToken.None)).Returns(expected);
            var controller = new HistoryController(history.Object);

            var actual = controller.HistoryStreamProcessedValuesAsync(
                new RequestEnvelope<HistoryReadRequestModel<ReadProcessedValuesDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }

        [Fact]
        public void HistoryStreamEventsReturnsHistorianStream()
        {
            var connection = CreateConnection();
            var request = new HistoryReadRequestModel<ReadEventsDetailsModel>
            {
                Details = new ReadEventsDetailsModel()
            };
            var expected = GetEventsAsync();
            var history = new Mock<IHistoryServices<ConnectionModel>>(MockBehavior.Strict);
            history.Setup(h => h.HistoryStreamEventsAsync(connection, request,
                CancellationToken.None)).Returns(expected);
            var controller = new HistoryController(history.Object);

            var actual = controller.HistoryStreamEventsAsync(
                new RequestEnvelope<HistoryReadRequestModel<ReadEventsDetailsModel>>
                {
                    Connection = connection,
                    Request = request
                });

            Assert.Same(expected, actual);
        }
    }
}
