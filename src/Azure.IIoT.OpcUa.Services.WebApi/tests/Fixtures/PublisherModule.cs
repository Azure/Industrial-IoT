// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi
{
    using Azure.IIoT.OpcUa.Publisher.Module;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Testing.Runtime;
    using Autofac;
    using Furly.Azure;
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Edge;
    using Furly.Azure.IoT.Mock;
    using Furly.Azure.IoT.Models;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Configuration;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Autofac.Extensions.DependencyInjection;

    /// <summary>
    /// Opc Publisher module fixture
    /// </summary>
    public sealed class PublisherModule : WebApplicationFactory<Publisher.Module.Startup>, IStartable
    {
        /// <summary>
        /// Taret
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
        public ILifetimeScope HubContainer { get; }

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="serviceContainer"></param>
        public PublisherModule(ILifetimeScope serviceContainer)
        {
            HubContainer = serviceContainer;
            var deviceId = Utils.GetHostName();
            var moduleId = Guid.NewGuid().ToString();
            var arguments = Array.Empty<string>();
            var publisherModule = new DeviceTwinModel
            {
                Id = deviceId,
                ModuleId = moduleId
            };

            var service = HubContainer.Resolve<IIoTHubTwinServices>();
            var twin = service.CreateOrUpdateAsync(publisherModule).AsTask().Result;
            var device = service.GetRegistrationAsync(twin.Id, twin.ModuleId).AsTask().Result;

            // Start publisher module with the created identity
            Target = HubResource.Format(null, device.Id, device.ModuleId);

            ServerPkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), "pki",
                Guid.NewGuid().ToByteArray().ToBase16String());
            ClientPkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), "pki",
                Guid.NewGuid().ToByteArray().ToBase16String());

            // Create a virtual connection betwenn publisher module and hub
            var hub = HubContainer.Resolve<IIoTHub>();
            _connection = hub.Connect(device.Id, device.ModuleId);


            // Start module
            var publisherCs = ConnectionString.CreateModuleConnectionString(
                "test.test.org", device.Id, device.ModuleId, device.PrimaryKey);
            arguments = arguments.Concat(
                new[]
                {
                    $"--ec={publisherCs}",
                    "--aa"
                }).ToArray();

            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "EnableMetrics", "false" },
                    { "PkiRootPath", ClientPkiRootPath }
                })
                .AddInMemoryCollection(new PublisherCliOptions(arguments))
                ;
            _config = configBuilder.Build();
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
                .UseStartup<Startup>()
                .UseConfiguration(_config)
                .ConfigureTestContainer<ContainerBuilder>(ConfigureContainer)
                ;
            base.ConfigureWebHost(builder);
        }

        /// <inheritdoc/>
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            return base.CreateHost(builder);
        }

        /// <summary>
        /// Resolve service
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
            if (disposing)
            {
                _connection.Close();

                if (Directory.Exists(ServerPkiRootPath))
                {
                    Try.Op(() => Directory.Delete(ServerPkiRootPath, true));
                }
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Add connection to hub
            builder.RegisterInstance(_connection.EventClient);
            builder.RegisterInstance(_connection.RpcServer);
            builder.RegisterInstance(_connection.Twin);

            // Override client config
            builder.RegisterInstance(_config).AsImplementedInterfaces();
            builder.RegisterType<TestClientServicesConfig>()
                .AsImplementedInterfaces();
        }

        private readonly IIoTHubConnection _connection;
        private readonly IConfiguration _config;
    }
}
