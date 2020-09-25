// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Tests.Jobs.Hub {
    using Autofac;
    using Autofac.Extras.Moq;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs.Runtime;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Test to check execution logic of JobOrchestratorEndpointSync service.
    /// </summary>
    public class JobOrchestratorEndpointSyncTests {

        [Fact]
        public async Task StartStopTestAsync() {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();
            configuration["JobOrchestratorUrl"] = "https://so.me/end/point/";
            configuration["JobOrchestratorUrlSyncInterval"] = "00:00:00.010";

            using (var mock = Setup(null, configuration)) {
                var syncService = mock.Create<JobOrchestratorEndpointSync>();
                await syncService.StartAsync();
                await Task.Delay(100);
                await syncService.StopAsync();
            }
        }

        [Theory]
        [InlineData(typeof(OperationCanceledException))]
        [InlineData(typeof(HttpRequestException))]
        public async Task QueryThrowsTestAsync(Type exceptionType) {

            var queryCounter = 0;
            var patchCounter = 0;
            var cts = new CancellationTokenSource();

            var ioTHubTwinServicesMock = new Mock<IIoTHubTwinServices>();

            ioTHubTwinServicesMock
                .Setup(e => e.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns((string query, string continuation, int? pageSize, CancellationToken ct) => {
                    queryCounter++;
                    if (queryCounter >= 5) {
                        cts.Cancel();
                    }

                    var ex = (Exception)Activator.CreateInstance(exceptionType);
                    throw ex;
                });

            ioTHubTwinServicesMock
                .Setup(e => e.PatchAsync(It.IsAny<DeviceTwinModel>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns((DeviceTwinModel device, bool force, CancellationToken ct) => {
                    patchCounter++;
                    return Task.FromResult(new DeviceTwinModel());
                });

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();
            configuration["JobOrchestratorUrl"] = "https://so.me/end/point/";
            configuration["JobOrchestratorUrlSyncInterval"] = "00:00:00.001";

            using (var mock = Setup(ioTHubTwinServicesMock, configuration)) {
                var syncService = mock.Create<JobOrchestratorEndpointSync>();
                await syncService.StartAsync();
                await SilentTaskDelayAsync(1000, cts.Token);
                await syncService.StopAsync();

                Assert.InRange(queryCounter, 5, 1000);
                Assert.Equal(0, patchCounter);
            }
        }

        [Theory]
        [InlineData(typeof(OperationCanceledException))]
        [InlineData(typeof(HttpRequestException))]
        public async Task PatchThrowsTestAsync(Type exceptionType) {

            var jobOrchestratorUrl = "https://so.me/end/point/";
            var jobOrchestratorInterval = "00:00:00.001";
            var queryCounter = 0;
            var patchCounter = 0;
            var cts = new CancellationTokenSource();
            IJsonSerializer jsonSerializer = null;

            var ioTHubTwinServicesMock = new Mock<IIoTHubTwinServices>();

            ioTHubTwinServicesMock
                .Setup(e => e.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns((string query, string continuation, int? pageSize, CancellationToken ct) => {
                    queryCounter++;
                    var o = jsonSerializer?.FromObject(new {
                        Properties = new {
                            Desired = new { }
                        }
                    });
                    return Task.FromResult(new QueryResultModel {
                        Result = new List<VariantValue> {
                            o
                        }
                    });
                });

            ioTHubTwinServicesMock
                .Setup(e => e.PatchAsync(It.IsAny<DeviceTwinModel>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns((DeviceTwinModel device, bool force, CancellationToken ct) => {
                    patchCounter++;
                    if (patchCounter >= 5) {
                        cts.Cancel();
                    }

                    // Check that job orchestrator endpoint is set correctly.
                    Assert.Equal(jobOrchestratorUrl, device.Properties.Desired[TwinProperties.JobOrchestratorUrl]);

                    var ex = (Exception)Activator.CreateInstance(exceptionType);
                    throw ex;
                });

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();
            configuration["JobOrchestratorUrl"] = jobOrchestratorUrl;
            configuration["JobOrchestratorUrlSyncInterval"] = jobOrchestratorInterval;

            using (var mock = Setup(ioTHubTwinServicesMock, configuration)) {
                var syncService = mock.Create<JobOrchestratorEndpointSync>();
                jsonSerializer = mock.Container.Resolve<IJsonSerializer>();

                await syncService.StartAsync();
                await SilentTaskDelayAsync(1000, cts.Token);
                await syncService.StopAsync();

                Assert.InRange(queryCounter, 5, 1000);
                Assert.InRange(patchCounter, 5, 1000);
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
        /// <param name="ioTHubTwinServicesMock"></param>
        /// <param name="configuration"></param>
        private static AutoMock Setup(
            Mock<IIoTHubTwinServices> ioTHubTwinServicesMock = null,
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

                var syncConfig = new JobOrchestratorEndpointConfig(conf);
                builder.RegisterInstance(syncConfig)
                    .AsImplementedInterfaces()
                    .SingleInstance();

                // Setup JSON serializer
                builder.RegisterType<NewtonSoftJsonConverters>()
                    .As<IJsonSerializerConverterProvider>();
                builder.RegisterType<NewtonSoftJsonSerializer>()
                    .As<IJsonSerializer>();

                // Setup IIoTHubTwinServices mock
                if (ioTHubTwinServicesMock is null) {
                    var ioTHubTwinServices = new Mock<IIoTHubTwinServices>();

                    ioTHubTwinServices
                        .Setup(e => e.QueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                        .Returns((string query, string continuation, int? pageSize, CancellationToken ct) => Task.FromResult(new QueryResultModel()));

                    ioTHubTwinServices
                        .Setup(e => e.PatchAsync(It.IsAny<DeviceTwinModel>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                        .Returns((DeviceTwinModel device, bool force, CancellationToken ct) => Task.FromResult(new DeviceTwinModel()));

                    builder.RegisterMock(ioTHubTwinServices);
                }
                else {
                    builder.RegisterMock(ioTHubTwinServicesMock);
                }

                builder.RegisterType<JobOrchestratorEndpointSync>()
                    .AsSelf(); ;
            });
            return mock;
        }
    }
}