// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Azure.IIoT.OpcUa.Publisher.Module.Tests.Clients;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Clients.Adapters;
    using Azure.IIoT.OpcUa.Publisher.Testing.Runtime;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Furly.Azure;
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Edge;
    using Furly.Azure.IoT.Mock;
    using Furly.Azure.IoT.Mock.Services;
    using Furly.Azure.IoT.Models;
    using Furly.Extensions.Hosting;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Mqtt;
    using Furly.Extensions.Mqtt.Clients;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Furly.Tunnel.Protocol;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Neovolve.Logging.Xunit;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Xunit.Abstractions;

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
        string Topic, ReadOnlySequence<byte> Data, string ContentType, string ContentEncoding,
        IReadOnlyDictionary<string, string> Properties);

    /// <summary>
    /// Opc Publisher module fixture
    /// </summary>
    public sealed class PublisherModule : WebApplicationFactory<ModuleStartup>, IHttpClientFactory
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
        public IContainer ClientContainer { get; }

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="messageSink"></param>
        /// <param name="devices"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="testOutputHelper"></param>
        /// <param name="arguments"></param>
        /// <param name="version"></param>
        public PublisherModule(IMessageSink messageSink, IEnumerable<DeviceTwinModel> devices = null,
            string deviceId = null, string moduleId = null, ITestOutputHelper testOutputHelper = null,
            string[] arguments = default, MqttVersion? version = null)
        {
            _logFactory = testOutputHelper != null ? LogFactory.Create(testOutputHelper, Logging.Config) : null;
            ClientContainer = CreateIoTHubSdkClientContainer(messageSink, testOutputHelper, devices, version);

            // Create module identitity
            deviceId ??= Utils.GetHostName();
            moduleId ??= Guid.NewGuid().ToString();
            arguments ??= Array.Empty<string>();

            var publisherModule = new DeviceTwinModel
            {
                Id = deviceId,
                ModuleId = moduleId
            };

            var service = ClientContainer.Resolve<IIoTHubTwinServices>();
            var twin = service.CreateOrUpdateAsync(publisherModule).AsTask().GetAwaiter().GetResult();
            var device = service.GetRegistrationAsync(twin.Id, twin.ModuleId).AsTask().GetAwaiter().GetResult();

            _useMqtt = version != null;
            if (_useMqtt)
            {
                // Resolve the mqtt server to make sure it is running
                _ = ClientContainer.Resolve<MqttServer>().GetAwaiter().GetResult();
            }

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
            var mqttOptions = ClientContainer.Resolve<IOptions<MqttOptions>>();
            var mqttCs = $"HostName={mqttOptions.Value.HostName};Port={mqttOptions.Value.Port};" +
                $"UserName={mqttOptions.Value.UserName};Password={mqttOptions.Value.Password};" +
                $"UseTls={mqttOptions.Value.UseTls};Protocol={mqttOptions.Value.Protocol};" +
                $"Partitions={mqttOptions.Value.NumberOfClientPartitions};" +
                $"KeepAlivePeriod={mqttOptions.Value.KeepAlivePeriod}"
                ;
            var publisherId = Guid.NewGuid().ToString();
            arguments = arguments.Concat(
                new[]
                {
                    $"--id={publisherId}",
                    $"--ec={edgeHubCs}",
                    $"--mqc={mqttCs}",
                    "--ki=90",
                    "--aa"
                }).ToArray();
            if (OperatingSystem.IsLinux())
            {
                arguments = arguments.Append("--pol").ToArray();
            }
            if (_useMqtt)
            {
                arguments = arguments.Append("-t=Mqtt").ToArray();
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

            // Register with the telemetry handler to receive telemetry events
            if (!_useMqtt)
            {
                var register = ClientContainer.Resolve<IEventRegistration<IIoTHubTelemetryHandler>>();
                _telemetry = new IoTHubTelemetryHandler();
                _handler1 = register.Register(_telemetry);
                Target = HubResource.Format(null, device.Id, device.ModuleId);
            }
            else
            {
                _consumer = new EventConsumer();
                var register = ClientContainer.Resolve<IEventSubscriber>();
                var options = Resolve<IOptions<PublisherOptions>>();

                var topicBuilder = new TopicBuilder(options.Value);
                _handler2 = register.SubscribeAsync(topicBuilder.RootTopic + "/messages/#", _consumer)
                    .AsTask().GetAwaiter().GetResult();
                Target = topicBuilder.MethodTopic;
            }
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
                .ConfigureServices(services => services
                    .AddMvc()
                        .AddApplicationPart(typeof(Startup).Assembly)
                        .AddControllersAsServices())
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

        /// <inheritdoc/>
        public HttpClient CreateClient(string name)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Api key
            var apiKey = _connection.Twin.State[Constants.TwinPropertyApiKeyKey].ConvertTo<string>();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("ApiKey", apiKey);
            client.Timeout = TimeSpan.FromMinutes(10);
            return client;
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

        /// <summary>
        /// Read publisher telemetry
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IAsyncEnumerable<PublisherTelemetry> ReadTelemetryAsync(CancellationToken ct)
        {
            if (_telemetry != null)
            {
                return _telemetry.Reader.ReadAllAsync(ct);
            }
            else if (_consumer != null)
            {
                return _consumer.Reader.ReadAllAsync(ct);
            }
            else
            {
                throw new InvalidOperationException("No consumer configured.");
            }
        }

        /// <inheritdoc/>
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            // Throw if we cannot dispose
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            await InnerDisposeAsync().WaitAsync(cts.Token);

            _logFactory?.Dispose();

            async Task InnerDisposeAsync()
            {
                _connection.Close();
                _handler1?.Dispose();
                if (_handler2 != null)
                {
                    await _handler2.DisposeAsync();
                }
                if (Directory.Exists(ServerPkiRootPath))
                {
                    Try.Op(() => Directory.Delete(ServerPkiRootPath, true));
                }
                ClientContainer.Dispose();
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

            if (_logFactory != null)
            {
                builder.RegisterInstance(_logFactory);
            }

            // Register transport services
            if (_useMqtt)
            {
                // Dont just register if the broker is not running or
                // otherwise the connect hangs during startup.
                // TODO: Look into this.
                builder.AddMqttClient(_config);
            }
            // Override client config
            builder.RegisterInstance(_config).AsImplementedInterfaces();
            // Override process control
            builder.RegisterType<ExitOverride>()
                .AsImplementedInterfaces().SingleInstance();
        }

        /// <summary>
        /// Create client container
        /// </summary>
        /// <param name="output"></param>
        /// <param name="serializerType"></param>
        /// <returns></returns>
        public IContainer CreateClientScope(ITestOutputHelper output,
            TestSerializerType serializerType)
        {
            var builder = new ContainerBuilder();

            builder.ConfigureServices(services => services.AddLogging());
            builder.AddOptions();
#pragma warning disable CA2000 // Dispose objects before losing scope
            builder.RegisterInstance(LogFactory.Create(output, Logging.Config))
                .AsImplementedInterfaces();
#pragma warning restore CA2000 // Dispose objects before losing scope

            // Add API
            builder.Configure<SdkOptions>(options =>
                options.Target = Server.BaseAddress.ToString());
            builder.RegisterType<NodeServicesRestClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistoryServicesRestClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<FileSystemServicesRestClient>()
                .AsImplementedInterfaces();

            switch (serializerType)
            {
                case TestSerializerType.NewtonsoftJson:
                    builder.AddNewtonsoftJsonSerializer();
                    break;
                case TestSerializerType.Json:
                    builder.AddDefaultJsonSerializer();
                    break;
                case TestSerializerType.MsgPack:
                    builder.AddMessagePackSerializer();
                    break;
            }

            // Register http client factory
            builder.RegisterInstance(this)
                .As<IHttpClientFactory>().ExternallyOwned(); // Do not dispose
            return builder.Build();
        }

        /// <summary>
        /// Create hub container
        /// </summary>
        /// <param name="messageSink"></param>
        /// <param name="testOutputHelper"></param>
        /// <param name="devices"></param>
        /// <param name="mqttVersion"></param>
        /// <returns></returns>
        private IContainer CreateIoTHubSdkClientContainer(IMessageSink messageSink = null,
            ITestOutputHelper testOutputHelper = null, IEnumerable<DeviceTwinModel> devices = null,
            MqttVersion? mqttVersion = null)
        {
            var builder = new ContainerBuilder();

            builder.AddNewtonsoftJsonSerializer();
            builder.Configure<SdkOptions>(options => options.Target = Target);
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient();
                services.AddLogging(logging =>
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
            });

            builder.Configure<IoTHubServiceOptions>(option =>
            {
                option.ConnectionString = ConnectionString.CreateServiceConnectionString(
                    "test.test.org", "iothubowner", Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToString();
            });

            // Configure mqtt
            builder.ConfigureMqtt(options =>
            {
                options.AllowUntrustedCertificates = true;
                options.UseTls = false;
                options.Protocol = mqttVersion ?? MqttVersion.v5;
                options.KeepAlivePeriod = TimeSpan.Zero;
                options.NumberOfClientPartitions = 1;
                options.Port = Interlocked.Increment(ref _mqttPort);
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
        /// Mock exiting
        /// </summary>
        internal sealed class ExitOverride : IProcessControl
        {
            public bool Shutdown(bool failFast)
            {
                return true;
            }
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
                ReadOnlySequence<byte> data, string contentType, string contentEncoding,
                IReadOnlyDictionary<string, string> properties, CancellationToken ct)
            {
                return _channel.Writer.WriteAsync(new PublisherTelemetry(
                    deviceId, moduleId, topic, data, contentType, contentEncoding,
                    properties), ct);
            }

            private readonly Channel<PublisherTelemetry> _channel;
        }

        /// <summary>
        /// Adapter for event consumer
        /// </summary>
        internal sealed class EventConsumer : IEventConsumer
        {
            public ChannelReader<PublisherTelemetry> Reader => _channel.Reader;

            internal EventConsumer()
            {
                _channel = Channel.CreateUnbounded<PublisherTelemetry>();
            }

            public async Task HandleAsync(string topic, ReadOnlySequence<byte> data,
                string contentType, IReadOnlyDictionary<string, string> properties,
                IEventClient responder = null, CancellationToken ct = default)
            {
                properties.TryGetValue("ContentEncoding", out var contentEncoding);
                await _channel.Writer.WriteAsync(new PublisherTelemetry(
                    null, null, topic, data, contentType, contentEncoding,
                    properties), ct).ConfigureAwait(false);
            }

            private readonly Channel<PublisherTelemetry> _channel;
        }

        private static int _mqttPort = 48882;
        private readonly IIoTHubConnection _connection;
        private readonly IConfiguration _config;
        private readonly bool _useMqtt;
        private readonly IoTHubTelemetryHandler _telemetry;
        private readonly IDisposable _handler1;
        private readonly IAsyncDisposable _handler2;
        private readonly EventConsumer _consumer;
        private readonly ILoggerFactory _logFactory;
    }

    public class ModuleStartup : Startup
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
