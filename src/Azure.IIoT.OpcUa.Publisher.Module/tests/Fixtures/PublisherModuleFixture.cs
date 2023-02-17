// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures {
    using Azure.IIoT.OpcUa.Api;
    using Azure.IIoT.OpcUa.Api.Clients;
    using Azure.IIoT.OpcUa.Api.Models;
    using Azure.IIoT.OpcUa.Api.Publisher.Adapter;
    using Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Azure.IIoT.OpcUa.Services.Clients;
    using Azure.IIoT.OpcUa.Protocol.Services;
    using Azure.IIoT.OpcUa.Testing.Runtime;
    using Autofac;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Harness for opc publisher module
    /// </summary>
    public class PublisherModuleFixture : IInjector,
        IModuleApiConfig, IDisposable {

        /// <summary>
        /// Device id
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        /// Module id
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// ServerPkiRootPath
        /// </summary>
        public string ServerPkiRootPath { get; }

        /// <summary>
        /// ClientPkiRootPath
        /// </summary>
        public string ClientPkiRootPath { get; }

        /// <summary>
        /// Hub container
        /// </summary>
        public IContainer HubContainer { get; }

        /// <summary>
        /// Create fixture
        /// </summary>
        public PublisherModuleFixture() {

            DeviceId = Utils.GetHostName();
            ModuleId = Guid.NewGuid().ToString();

            ServerPkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), "pki",
               Guid.NewGuid().ToByteArray().ToBase16String());
            ClientPkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), "pki",
               Guid.NewGuid().ToByteArray().ToBase16String());

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    {"EnableMetrics", "false"},
                    {"PkiRootPath", ClientPkiRootPath}
                })
                .Build();
            HubContainer = CreateHubContainer();
            _hub = HubContainer.Resolve<IIoTHubTwinServices>();

            // Create module identitity
            var twin = _hub.CreateOrUpdateAsync(new DeviceTwinModel {
                Id = DeviceId,
                ModuleId = ModuleId
            }).Result;

            // Get device registration and create module host with controller
            _device = _hub.GetRegistrationAsync(twin.Id, twin.ModuleId).Result;
            _running = false;
            _module = new ModuleProcess(_config, this);
            var tcs = new TaskCompletionSource<bool>();
            _module.OnRunning += (_, e) => tcs.TrySetResult(e);
            _process = Task.Run(() => _module.RunAsync());

            // Wait
            _running = tcs.Task.Result;
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_running) {
                _module.Exit(1);
                var result = _process.Result;
                Assert.Equal(1, result);
                _running = false;
            }
            if (Directory.Exists(ServerPkiRootPath)) {
                Try.Op(() => Directory.Delete(ServerPkiRootPath, true));
            }
            HubContainer.Dispose();
        }

        /// <inheritdoc/>
        public void Inject(ContainerBuilder builder) {

            // Register mock iot hub
            builder.RegisterInstance(_hub)
                .AsImplementedInterfaces().ExternallyOwned();

            // Only configure if not yet running - otherwise we use twin host config.
            if (!_running) {
                builder.RegisterInstance(new TestModuleConfig(_device))
                    .AsImplementedInterfaces();
            }

            // Add mock sdk
            builder.RegisterModule<IoTHubMockModule>();

            // Override client config
            builder.RegisterInstance(_config).AsImplementedInterfaces();
            builder.RegisterType<TestClientServicesConfig>()
                .AsImplementedInterfaces();
        }

        /// <inheritdoc/>
        public class TestModuleConfig : IModuleConfig {

            /// <inheritdoc/>
            public TestModuleConfig(DeviceModel device) {
                _device = device;
            }

            /// <inheritdoc/>
            public string EdgeHubConnectionString =>
                ConnectionString.CreateModuleConnectionString("test.test.org",
                    _device.Id, _device.ModuleId, _device.Authentication.PrimaryKey)
                .ToString();

            /// <inheritdoc/>
            public string MqttClientConnectionString => null;

            /// <inheritdoc/>
            public string TelemetryTopicTemplate => null;

            /// <inheritdoc/>
            public bool BypassCertVerification => true;

            /// <inheritdoc/>
            public TransportOption Transport => TransportOption.Any;

            /// <inheritdoc/>
            public bool EnableMetrics => false;
            /// <inheritdoc/>
            public bool EnableOutputRouting => false;

            private readonly DeviceModel _device;
        }

        /// <inheritdoc/>
        public class TestIoTHubConfig : IIoTHubConfig {

            /// <inheritdoc/>
            public string IoTHubConnString =>
                ConnectionString.CreateServiceConnectionString(
                    "test.test.org", "iothubowner", Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToString();
        }

        /// <summary>
        /// Create hub container
        /// </summary>
        /// <returns></returns>
        private IContainer CreateHubContainer() {
            var builder = new ContainerBuilder();

            builder.RegisterModule<NewtonSoftJsonModule>();
            builder.RegisterInstance(this).AsImplementedInterfaces();
            builder.RegisterInstance(_config).AsImplementedInterfaces();
            builder.AddDiagnostics();
            builder.RegisterModule<IoTHubMockService>();
            builder.RegisterType<TestIoTHubConfig>()
                .AsImplementedInterfaces();

            builder.RegisterType<PublisherApiClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<TwinApiClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistoryApiClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscoveryApiClient>()
                .AsImplementedInterfaces();

            builder.RegisterType<HistoryRawSupervisorAdapter>()
                .AsImplementedInterfaces();
            builder.RegisterType<TwinModuleApiAdapter>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistorianApiAdapter<ConnectionModel>>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherModuleApiAdapter>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscoveryModuleApiAdapter>()
                .AsImplementedInterfaces();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces();

            // Register http client module
            builder.RegisterModule<HttpClientModule>();
            return builder.Build();
        }

        private readonly IIoTHubTwinServices _hub;
        private readonly DeviceModel _device;
        private bool _running;
        private readonly ModuleProcess _module;
        private readonly IConfiguration _config;
        private readonly Task<int> _process;
    }
}
