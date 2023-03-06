// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi
{
    using Azure.IIoT.OpcUa.Publisher.Module;
    using Azure.IIoT.OpcUa.Testing.Runtime;
    using Autofac;
    using Furly.Extensions.Utils;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Opc Publisher module fixture
    /// </summary>
    public sealed class PublisherModule : IInjector, IStartable, IDisposable
    {
        /// <summary>
        /// ServerPkiRootPath
        /// </summary>
        public string ServerPkiRootPath { get; }

        /// <summary>
        /// ClientPkiRootPath
        /// </summary>
        public string ClientPkiRootPath { get; }

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="serviceContainer"></param>
        public PublisherModule(ILifetimeScope serviceContainer)
        {
            var deviceId = Utils.GetHostName();
            var moduleId = Guid.NewGuid().ToString();

            ServerPkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), "pki",
                Guid.NewGuid().ToByteArray().ToBase16String());
            ClientPkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), "pki",
                Guid.NewGuid().ToByteArray().ToBase16String());

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "EnableMetrics", "false" },
                    { "PkiRootPath", ClientPkiRootPath }
                })
                .Build();
            _hub = serviceContainer.Resolve<IIoTHubTwinServices>();

            // Create or udpate the module identitity
            var twin = _hub.CreateOrUpdateAsync(new DeviceTwinModel
            {
                Id = deviceId,
                ModuleId = moduleId
            }, true).Result;

            // Get device registration and create module host with controller
            _device = _hub.GetRegistrationAsync(twin.Id, twin.ModuleId).Result;
            _module = new ModuleProcess(_config, this);
            _running = new TaskCompletionSource<bool>();
            _module.OnRunning += (_, e) => _running.TrySetResult(e);
            _process = Task.Run(() => _module.RunAsync());

            // Wait until running
            _running.Task.Wait();
        }

        /// <inheritdoc/>
        public void Start()
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _module.Exit(1);
            var result = _process.Result;
            Assert.Equal(1, result);

            if (Directory.Exists(ServerPkiRootPath))
            {
                Try.Op(() => Directory.Delete(ServerPkiRootPath, true));
            }
        }

        /// <inheritdoc/>
        public void Inject(ContainerBuilder builder)
        {
            // Register mock iot hub
            builder.RegisterInstance(_hub)
                .AsImplementedInterfaces().ExternallyOwned();

            builder.RegisterInstance(new TestModuleConfig(_device))
                .AsImplementedInterfaces();

            // Add mock sdk
            builder.RegisterModule<IoTHubMockModule>();

            // Override client config
            builder.RegisterInstance(_config).AsImplementedInterfaces();
            builder.RegisterType<TestClientServicesConfig>()
                .AsImplementedInterfaces();
        }

        /// <inheritdoc/>
        public class TestModuleConfig : IModuleConfig
        {
            /// <inheritdoc/>
            public TestModuleConfig(DeviceModel device)
            {
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

        private readonly IIoTHubTwinServices _hub;
        private readonly DeviceModel _device;
        private readonly ModuleProcess _module;
        private readonly TaskCompletionSource<bool> _running;
        private readonly IConfiguration _config;
        private readonly Task<int> _process;
    }
}
