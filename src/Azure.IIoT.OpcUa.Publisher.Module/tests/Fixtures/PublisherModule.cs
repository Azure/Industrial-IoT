// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Azure.IIoT.OpcUa.Services.Clients.Adapters;
    using Azure.IIoT.OpcUa.Testing.Runtime;
    using Autofac;
    using Furly.Azure;
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Mock;
    using Furly.Azure.IoT.Mock.Services;
    using Furly.Azure.IoT.Models;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Furly.Tunnel.Protocol;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Xunit.Abstractions;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Furly.Azure.IoT.Edge.Services;
    using Furly.Extensions.Mqtt;

    /// <summary>
    /// Publisher telemetry
    /// </summary>
    /// <param name="DeviceId"></param>
    /// <param name="ModuleId"></param>
    /// <param name="Topic"></param>
    /// <param name="Data"></param>
    /// <param name="ContentType"></param>
    /// <param name="ContentEncoding"></param>
    /// <param name="Properties"></param>
    public sealed record class PublisherTelemetry(string DeviceId, string ModuleId,
        string Topic, ReadOnlyMemory<byte> Data, string ContentType, string ContentEncoding,
        IReadOnlyDictionary<string, string> Properties);

    /// <summary>
    /// Harness for opc publisher module
    /// </summary>
    public class PublisherModule : WebApplicationFactory<ModuleStartup>, ISdkConfig
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
        public IContainer HubContainer { get; }

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="messageSink"></param>
        /// <param name="devices"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="testOutputHelper"></param>
        /// <param name="arguments"></param>
        public PublisherModule(IMessageSink messageSink, IEnumerable<DeviceTwinModel> devices = null,
            string deviceId = null, string moduleId = null, ITestOutputHelper testOutputHelper = null,
            string[] arguments = default)
        {
            HubContainer = CreateClientContainer(messageSink, testOutputHelper, devices);

            // Create module identitity
            deviceId ??= Utils.GetHostName();
            moduleId ??= Guid.NewGuid().ToString();
            arguments ??= Array.Empty<string>();

            var publisherModule = new DeviceTwinModel
            {
                Id = deviceId,
                ModuleId = moduleId
            };

            var service = HubContainer.Resolve<IIoTHubTwinServices>();
            var twin = service.CreateOrUpdateAsync(publisherModule).AsTask().Result;
            var device = service.GetRegistrationAsync(twin.Id, twin.ModuleId).AsTask().Result;

            // Register with the telemetry handler to receive telemetry events
            var register = HubContainer.Resolve<IEventRegistration<IIoTHubTelemetryHandler>>();
            _telemetry = new IoTHubTelemetryHandler();
            _handler = register.Register(_telemetry);

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
            _ = Server; // Ensure server is created
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
        /// Resolve service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>()
        {
            return (T)Server.Services.GetService(typeof(T));
        }

        /// <summary>
        /// Read publisher telemetry
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public IAsyncEnumerable<PublisherTelemetry> ReadTelemetryAsync(CancellationToken ct)
        {
            return _telemetry.Reader.ReadAllAsync(ct);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection.Close();
                _handler.Dispose();

                if (Directory.Exists(ServerPkiRootPath))
                {
                    Try.Op(() => Directory.Delete(ServerPkiRootPath, true));
                }
                HubContainer.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Register publisher services
            builder.AddPublisherServices();
            // Override client config
            builder.RegisterInstance(_config).AsImplementedInterfaces();
            builder.RegisterType<TestClientServicesConfig>()
                .AsImplementedInterfaces();

            // Register connectivity services
            builder.RegisterType<IoTEdgeIdentity>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterInstance(_connection.EventClient);
            builder.RegisterInstance(_connection.RpcServer);
            builder.RegisterInstance(_connection.Twin);
        }

        /// <summary>
        /// Create hub container
        /// </summary>
        /// <param name="messageSink"></param>
        /// <param name="testOutputHelper"></param>
        /// <param name="devices"></param>
        /// <param name="mqttVersion"></param>
        /// <returns></returns>
        private IContainer CreateClientContainer(IMessageSink messageSink = null,
            ITestOutputHelper testOutputHelper = null,
            IEnumerable<DeviceTwinModel> devices = null, MqttVersion? mqttVersion = null)
        {
            var builder = new ContainerBuilder();

            builder.AddNewtonsoftJsonSerializer();
            builder.RegisterInstance(this)
                .AsImplementedInterfaces().ExternallyOwned();

            builder.AddDiagnostics(logging =>
            {
                if (messageSink != null)
                {
                    // logging.AddXunit(messageSink); // TODO
                }
                if (testOutputHelper != null)
                {
                    logging.AddXunit(testOutputHelper);
                }
                else
                {
                    logging.AddConsole();
                }
            });
            builder.ConfigureServices(services => services.AddHttpClient());

            builder.Configure<IoTHubServiceOptions>(option =>
            {
                option.ConnectionString = ConnectionString.CreateServiceConnectionString(
                    "test.test.org", "iothubowner", Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToString();
            });

            if (devices != null)
            {
                builder.Register(ctx => IoTHubMock.Create(devices, ctx.Resolve<IJsonSerializer>()))
                   .AsImplementedInterfaces().SingleInstance();
            }
            else
            {
                builder.RegisterType<IoTHubMock>()
                    .AsImplementedInterfaces().SingleInstance();
            }

            if (mqttVersion != null)
            {
                // Override the iothub rpcclient with an mqtt server implementation
                builder.AddMqttServer();
                builder.Configure<MqttOptions>(options =>
                {
                    options.AllowUntrustedCertificates = true;
                    options.Version = mqttVersion.Value;
                });
            }

            builder.RegisterType<ChunkMethodClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherApiClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<TwinApiClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistoryApiClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscoveryApiClient>()
                .AsImplementedInterfaces();

            builder.RegisterType<PublisherApiAdapter>()
                .AsImplementedInterfaces();
            builder.RegisterType<TwinApiAdapter>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistoryApiAdapter>()
                .AsImplementedInterfaces();
            builder.RegisterType<DiscoveryApiAdapter>()
                .AsImplementedInterfaces();

            return builder.Build();
        }

        /// <summary>
        /// Adapter for telemetry handler
        /// </summary>
        internal sealed class IoTHubTelemetryHandler : IIoTHubTelemetryHandler
        {
            public ChannelReader<PublisherTelemetry> Reader => _channel.Reader;

            internal IoTHubTelemetryHandler()
            {
                _channel = Channel.CreateUnbounded<PublisherTelemetry>();
            }

            public ValueTask HandleAsync(string deviceId, string moduleId, string topic,
                ReadOnlyMemory<byte> data, string contentType, string contentEncoding,
                IReadOnlyDictionary<string, string> properties, CancellationToken ct)
            {
                return _channel.Writer.WriteAsync(new PublisherTelemetry(
                    deviceId, moduleId, topic, data, contentType, contentEncoding,
                    properties), ct);
            }

            private readonly Channel<PublisherTelemetry> _channel;
        }

        private readonly IIoTHubConnection _connection;
        private readonly IConfiguration _config;
        private readonly IoTHubTelemetryHandler _telemetry;
        private readonly IDisposable _handler;
    }

    public class ModuleStartup : Startup
    {
        public ModuleStartup(IWebHostEnvironment env, IConfiguration configuration)
            : base(env, configuration)
        {
        }

        public override void ConfigureContainer(ContainerBuilder builder)
        {
        }
    }
}
