// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Tests.Services {
    using Autofac;
    using Autofac.Extras.Moq;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class ActivationSyncHostTests {

        [Fact]
        public async Task StartStopTestAsync() {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();
            configuration["SyncInterval"] = "00:00:00.010";

            using (var mock = Setup(null, configuration)) {
                var syncService = mock.Create<ActivationSyncHost>();
                await syncService.StartAsync();
                await Task.Delay(100);
                await syncService.StopAsync();
            }
        }

        [Theory]
        [InlineData(typeof(OperationCanceledException))]
        [InlineData(typeof(HttpRequestException))]
        public async Task SynchronizeActivationThrowsTestAsync(Type exceptionType) {

            var callCounter = 0;
            var cts = new CancellationTokenSource();

            var endpointActivation = new Mock<IEndpointActivation>();

            endpointActivation
                .Setup(e => e.SynchronizeActivationAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => {
                    callCounter++;
                    if (callCounter >= 5) {
                        cts.Cancel();
                    }

                    var ex = (Exception)Activator.CreateInstance(exceptionType);
                    throw ex;
                });

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();
            configuration["SyncInterval"] = "00:00:00.001";

            using (var mock = Setup(endpointActivation, configuration)) {
                var syncService = mock.Create<ActivationSyncHost>();
                await syncService.StartAsync();
                await SilentTaskDelayAsync(1000, cts.Token);
                await syncService.StopAsync();

                Assert.InRange(callCounter, 5, 1000);
            }
        }

        /// <summary>
        /// Call Task.Delay() and silently ignore OperationCanceledException
        /// errors if cancellation was requested.
        /// </summary>
        /// <param name="millisecondsDelay"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task SilentTaskDelayAsync(
            int millisecondsDelay,
            CancellationToken ct = default
        ) {
            try {
                await Task.Delay(millisecondsDelay, ct);
            }
            catch (OperationCanceledException) {
                if (!ct.IsCancellationRequested) {
                    throw;
                }
            }
        }

        /// <summary>
        /// Setup mock
        /// </summary>
        /// <param name="endpointActivationMock"></param>
        /// <param name="configuration"></param>
        private static AutoMock Setup(
            Mock<IEndpointActivation> endpointActivationMock = null,
            IConfiguration configuration = null
        ) {
            var mock = AutoMock.GetLoose(builder => {
                // Use empty configuration root if one is not passed.
                var conf = configuration ?? new ConfigurationBuilder()
                    .AddInMemoryCollection()
                    .Build();

                // Setup configuration
                builder.RegisterInstance(conf)
                    .As<IConfiguration>()
                    .SingleInstance();

                var syncConfig = new ActivationSyncConfig(conf);
                builder.RegisterInstance(syncConfig)
                    .AsImplementedInterfaces()
                    .SingleInstance();

                // Setup IEndpointActivation mock
                if (endpointActivationMock is null) {
                    var endpointActivation = new Mock<IEndpointActivation>();

                    endpointActivation
                        .Setup(e => e.SynchronizeActivationAsync(It.IsAny<CancellationToken>()))
                        .Returns((CancellationToken ct) => Task.CompletedTask);

                    builder.RegisterMock(endpointActivation);
                }
                else {
                    builder.RegisterMock(endpointActivationMock);
                }

                builder.RegisterType<ActivationSyncHost>()
                    .AsSelf(); ;
            });
            return mock;
        }
    }
}
