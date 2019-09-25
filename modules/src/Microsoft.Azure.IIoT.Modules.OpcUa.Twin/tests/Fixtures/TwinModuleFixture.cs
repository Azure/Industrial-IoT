// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Tests {
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Servers;
    using Microsoft.Azure.IIoT.OpcUa.History.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Default;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Api.History;
    using Microsoft.Azure.IIoT.Utils;
    using Autofac;
    using AutofacSerilogIntegration;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using Serilog;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;

    /// <summary>
    /// Harness for opc twin module
    /// </summary>
    public class TwinModuleFixture : IInjector,
        IHistoryModuleConfig, ITwinModuleConfig, IDisposable {

        /// <summary>
        /// Device id
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Module id
        /// </summary>
        public string ModuleId { get; set; }

        /// <summary>
        /// Hub container
        /// </summary>
        public IContainer HubContainer { get; }

        /// <summary>
        /// Create fixture
        /// </summary>
        public TwinModuleFixture() {
            HubContainer = CreateHubContainer();
            _hub = HubContainer.Resolve<IIoTHubTwinServices>();

            DeviceId = Guid.NewGuid().ToString();
            ModuleId = Guid.NewGuid().ToString();

            // Create module identitity
            var twin = _hub.CreateAsync(new DeviceTwinModel {
                Id = DeviceId,
                ModuleId = ModuleId
            }).Result;
            _etag = twin.Etag;

            // Get device registration and create module host with controller
            _device = _hub.GetRegistrationAsync(twin.Id, twin.ModuleId).Result;
            _running = false;

            _module = new ModuleProcess(null, this);
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

            // Override publisher
            builder.RegisterType<ConfiguredPublisher>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherMethodClient>()
                .AsImplementedInterfaces();

            // Override client config
            builder.RegisterType<TestClientServicesConfig>()
                .AsImplementedInterfaces();
        }

        /// <summary>
        /// Twin Module supervisor test harness
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public async Task RunTestAsync(Func<string, string, IContainer, Task> test) {
            AssertRunning();
            try {
                await test(DeviceId, ModuleId, HubContainer);
            }
            finally {
                _module.Exit(1);
                var result = await _process;
                Assert.Equal(1, result);
                _running = false;
            }
            AssertStopped();
        }

        /// <summary>
        /// Twin module twin test harness
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="test"></param>
        /// <returns></returns>
        public async Task RunTestAsync(EndpointModel ep,
            Func<EndpointRegistrationModel, IContainer, Task> test) {
            var endpoint = new EndpointRegistrationModel {
                Endpoint = ep,
                SupervisorId = SupervisorModelEx.CreateSupervisorId(
                    DeviceId, ModuleId)
            };
            AssertRunning();
            try {
                endpoint = RegisterAndActivateTwinId(endpoint);
                await test(endpoint, HubContainer);
                DeactivateTwinId(endpoint);
            }
            finally {
                _module.Exit(1);
                var result = await _process;
                Assert.Equal(1, result);
                _running = false;
            }
            AssertStopped();
        }

        private void AssertStopped() {
            Assert.False(_running);
            var twin = _hub.GetAsync(DeviceId, ModuleId).Result;
            // Assert
            Assert.False((bool)twin.Properties.Reported[TwinProperty.kConnected]);

            // TODO : Fix cleanup!!!
            // TODO :Assert.NotEqual("testType", twin.Properties.Reported[TwinProperty.kType]);
            // TODO :Assert.NotEqual("TestSite", twin.Properties.Reported[TwinProperty.kSiteId]);
            // TODO :Assert.Equal("disconnected", twin.ConnectionState);
            Assert.NotEqual(_etag, twin.Etag);
        }

        /// <summary>
        /// Assert module running
        /// </summary>
        public void AssertRunning() {
            Assert.True(_running);
            var twin = _hub.GetAsync(DeviceId, ModuleId).Result;
            // Assert
            Assert.Equal("connected", twin.ConnectionState);
            Assert.Equal(true, twin.Properties.Reported[TwinProperty.kConnected]);
            Assert.Equal("supervisor", twin.Properties.Reported[TwinProperty.kType]);
            Assert.Equal(JValue.CreateNull(), twin.Properties.Reported[TwinProperty.kSiteId]);
        }

        /// <summary>
        /// Activate
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public EndpointRegistrationModel RegisterAndActivateTwinId(EndpointRegistrationModel endpoint) {
            var twin =
                new EndpointInfoModel {
                    Registration = endpoint,
                    ApplicationId = "uas" + Guid.NewGuid().ToString()
                }.ToEndpointRegistration().ToDeviceTwin();
            var result = _hub.CreateAsync(twin).Result;
            var registry = HubContainer.Resolve<IEndpointRegistry>();
            var endpoints = registry.ListAllEndpointsAsync().Result;
            var ep1 = endpoints.FirstOrDefault();

            if (ep1.ActivationState == EndpointActivationState.Deactivated) {
                // Activate
                registry.ActivateEndpointAsync(ep1.Registration.Id).Wait();
            }
            return ep1.Registration;
        }

        /// <summary>
        /// Deactivate
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public void DeactivateTwinId(EndpointRegistrationModel endpoint) {
            var registry = HubContainer.Resolve<IEndpointRegistry>();
            // Deactivate
            registry.DeactivateEndpointAsync(endpoint.Id).Wait();
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
            public bool BypassCertVerification => true;

            /// <inheritdoc/>
            public TransportOption Transport => TransportOption.Any;

            private readonly DeviceModel _device;
        }

        /// <inheritdoc/>
        public class TestIoTHubConfig : IIoTHubConfig {

            /// <inheritdoc/>
            public string IoTHubConnString =>
                ConnectionString.CreateServiceConnectionString(
                    "test.test.org", "iothubowner", Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToString();

            /// <inheritdoc/>
            public string IoTHubResourceId => null;
        }

        /// <summary>
        /// Create hub container
        /// </summary>
        /// <returns></returns>
        private IContainer CreateHubContainer() {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(this).AsImplementedInterfaces();
            builder.RegisterLogger(LogEx.ConsoleOut());
            builder.RegisterModule<IoTHubMockService>();
            builder.RegisterType<TestIoTHubConfig>()
                .AsImplementedInterfaces();

            // Twin and history clients
            builder.RegisterModule<TwinModuleClients>();

            builder.RegisterType<HistoryRawSupervisorAdapter>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinSupervisorAdapter>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinModuleClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistoryModuleClient>()
                .AsImplementedInterfaces();

            // Adapts to expanded hda
            builder.RegisterType<HistoricAccessAdapter<string>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<HistoricAccessAdapter<EndpointRegistrationModel>>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<HistoricAccessAdapter<EndpointApiModel>>()
                .AsImplementedInterfaces().SingleInstance();

            // Supervisor clients
            builder.RegisterType<ActivationClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiagnosticsClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscoveryClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<JsonVariantEncoder>()
                .AsImplementedInterfaces();
            builder.RegisterType<JsonVariantEncoder>()
                .AsImplementedInterfaces();

            // Add services
            builder.RegisterModule<RegistryServices>();
            builder.RegisterType<ApplicationTwins>()
                .AsImplementedInterfaces();
            builder.RegisterType<EndpointEventBrokerStub>()
                .AsImplementedInterfaces();
            builder.RegisterType<ApplicationEventBrokerStub>()
                .AsImplementedInterfaces();

            // Register http client module
            builder.RegisterModule<HttpClientModule>();
            return builder.Build();
        }

        private readonly IIoTHubTwinServices _hub;
        private readonly string _etag;
        private readonly DeviceModel _device;
        private bool _running;
        private readonly ModuleProcess _module;
        private readonly Task<int> _process;
    }
}
