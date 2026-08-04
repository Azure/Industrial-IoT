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
    }
}
