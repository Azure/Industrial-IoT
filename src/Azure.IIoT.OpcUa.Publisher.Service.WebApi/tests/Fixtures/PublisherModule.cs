// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Testing.Runtime;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Furly.Azure;
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Edge;
    using Furly.Azure.IoT.Mock;
    using Furly.Azure.IoT.Models;
    using Furly.Extensions.Hosting;
    using Furly.Extensions.Utils;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Opc Publisher module fixture
    /// </summary>
    public sealed class PublisherModule : WebApplicationFactory<ModuleStartup>, IStartable
    {
        /// <summary>
        /// Sdk target
        /// </summary>
        public string Target { get; }

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
        public ILifetimeScope ClientContainer { get; }

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="serviceContainer"></param>
        public PublisherModule(ILifetimeScope serviceContainer)
        {
            ClientContainer = serviceContainer;
            var deviceId = Utils.GetHostName();
            var moduleId = Guid.NewGuid().ToString();
            var arguments = Array.Empty<string>();
            var publisherModule = new DeviceTwinModel
            {
                Id = deviceId,
                ModuleId = moduleId
            };

            var service = ClientContainer.Resolve<IIoTHubTwinServices>();
            var twin = service.CreateOrUpdateAsync(publisherModule).AsTask().GetAwaiter().GetResult();
            var device = service.GetRegistrationAsync(twin.Id, twin.ModuleId).AsTask().GetAwaiter().GetResult();

            ServerPkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), "pki",
                Guid.NewGuid().ToByteArray().ToBase16String());
            ClientPkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), "pki",
                Guid.NewGuid().ToByteArray().ToBase16String());

            // Create a virtual connection betwenn publisher module and hub
            var hub = ClientContainer.Resolve<IIoTHub>();
            _connection = hub.Connect(device.Id, device.ModuleId);

            // Start module
            var edgeHubCs = ConnectionString.CreateModuleConnectionString(
                "test.test.org", device.Id, device.ModuleId, device.PrimaryKey);
            arguments = arguments.Concat(
                new[]
                {
                    $"--ec={edgeHubCs}",
                    "--ki=90",
                    "--aa"
                }).ToArray();
            if (OperatingSystem.IsLinux())
            {
                arguments = arguments.Append("--pol").ToArray();
            }
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "PkiRootPath", ClientPkiRootPath }
                })
                .AddInMemoryCollection(new CommandLine(arguments))
                ;

            _config = configBuilder.Build();
            _ = Server; // Ensure server is created
            Target = Furly.Azure.HubResource.Format(null, device.Id, device.ModuleId);
        }

        /// <inheritdoc/>
        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder();
        }

        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseContentRoot(".")
                .UseStartup<ModuleStartup>()
                .UseConfiguration(_config)
                ;
            base.ConfigureWebHost(builder);
        }

        /// <inheritdoc/>
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(ConfigureContainer)
                ;
            return base.CreateHost(builder);
        }

        /// <summary>
        /// Resolve service from publisher module
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>()
        {
            return (T)Server.Services.GetService(typeof(T));
        }

        /// <inheritdoc/>
        public void Start()
        {
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Close();

                if (Directory.Exists(ServerPkiRootPath))
                {
                    Try.Op(() => Directory.Delete(ServerPkiRootPath, true));
                }
            }
        }

        internal sealed class IoTEdgeMockIdentity : IIoTEdgeDeviceIdentity
        {
            public string Hub { get; } = "Hub";
            public string DeviceId { get; } = "DeviceId";
            public string ModuleId { get; } = "ModuleId";
            public string Gateway { get; } = "Gateway";
        }

        /// <inheritdoc/>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Register publisher services
            builder.AddPublisherServices();
            // Override client config
            builder.RegisterInstance(_config).AsImplementedInterfaces();
            builder.RegisterType<TestClientConfig>()
                .AsImplementedInterfaces();

            builder.RegisterType<IoTEdgeMockIdentity>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            if (_connection.EventClient is IProcessIdentity identity)
            {
                builder.RegisterInstance(identity);
            }

            builder.RegisterInstance(_connection.EventClient);
            builder.RegisterInstance(_connection.RpcServer);
            builder.RegisterInstance(_connection.Twin);
        }

        private readonly IIoTHubConnection _connection;
        private readonly IConfiguration _config;
    }

    public class ModuleStartup : Publisher.Module.Startup
    {
        public ModuleStartup(IConfiguration configuration)
            : base(configuration)
        {
        }

        public override void ConfigureContainer(ContainerBuilder builder)
        {
        }
    }
}
