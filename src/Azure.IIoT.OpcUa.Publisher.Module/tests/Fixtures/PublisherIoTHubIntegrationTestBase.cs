// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Autofac;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Module.Controller;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Hosting;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Base class for integration testing, it connects to the server, runs
    /// publisher and injects mocked IoTHub services.
    /// </summary>
    public class PublisherIoTHubIntegrationTestBase : ISdkConfig
    {
        /// <summary>
        /// Whether the module is running.
        /// </summary>
        private BlockingCollection<ITelemetryEvent> Events { get; set; }

        /// <summary>
        /// Device Id
        /// </summary>
        public string DeviceId { get; } = Utils.GetHostName();

        /// <summary>
        /// Module Id
        /// </summary>
        public string ModuleId { get; }

        public PublisherIoTHubIntegrationTestBase(ReferenceServerFixture serverFixture)
        {
            // This is a fake but correctly formatted connection string.
            var connectionString = "HostName=dummy.azure-devices.net;" +
                $"DeviceId={DeviceId};" +
                "SharedAccessKeyName=iothubowner;" +
                "SharedAccessKey=aXRpc25vdGFuYWNjZXNza2V5";
            var config = connectionString.ToIoTHubConfig();

            _typedConnectionString = ConnectionString.Parse(config.IoTHubConnString);
            _exit = new TaskCompletionSource<bool>();
            _running = new TaskCompletionSource<bool>();
            _serverFixture = serverFixture;
        }

        protected Task<List<JsonElement>> ProcessMessagesAsync(
            string publishedNodesFile,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default)
        {
            // Collect messages from server with default settings
            return ProcessMessagesAsync(publishedNodesFile, TimeSpan.FromMinutes(2), 1, predicate, messageType, arguments);
        }

        protected Task<(JsonElement? Metadata, List<JsonElement> Messages)> ProcessMessagesAndMetadataAsync(
            string publishedNodesFile,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default)
        {
            // Collect messages from server with default settings
            return ProcessMessagesAndMetadataAsync(publishedNodesFile, TimeSpan.FromMinutes(2), 1, predicate, messageType, arguments);
        }

        protected async Task<List<JsonElement>> ProcessMessagesAsync(
            string publishedNodesFile,
            TimeSpan messageCollectionTimeout,
            int messageCount,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default)
        {
            var (_, messages) = await ProcessMessagesAndMetadataAsync(publishedNodesFile,
                messageCollectionTimeout, messageCount, predicate, messageType, arguments).ConfigureAwait(false);
            return messages;
        }

        protected async Task<(JsonElement? Metadata, List<JsonElement> Messages)> ProcessMessagesAndMetadataAsync(
            string publishedNodesFile,
            TimeSpan messageCollectionTimeout,
            int messageCount,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default)
        {
            await StartPublisherAsync(publishedNodesFile, arguments).ConfigureAwait(false);

            JsonElement? metadata = null;
            var messages = WaitForMessagesAndMetadata(messageCollectionTimeout, messageCount, ref metadata, predicate, messageType);

            StopPublisher();

            return (metadata, messages);
        }

        /// <summary>
        /// Wait for one message
        /// </summary>
        protected List<JsonElement> WaitForMessages(
            Func<JsonElement, JsonElement> predicate = null, string messageType = null)
        {
            // Collect messages from server with default settings
            JsonElement? metadata = null;
            return WaitForMessagesAndMetadata(TimeSpan.FromMinutes(2), 1, ref metadata, predicate, messageType);
        }

        /// <summary>
        /// Wait for one message
        /// </summary>
        protected List<JsonElement> WaitForMessages(TimeSpan messageCollectionTimeout, int messageCount,
            Func<JsonElement, JsonElement> predicate = null, string messageType = null)
        {
            // Collect messages from server with default settings
            JsonElement? metadata = null;
            return WaitForMessagesAndMetadata(messageCollectionTimeout, messageCount, ref metadata, predicate, messageType);
        }

        /// <summary>
        /// Wait for messages
        /// </summary>
        protected List<JsonElement> WaitForMessagesAndMetadata(TimeSpan messageCollectionTimeout, int messageCount,
            ref JsonElement? metadata, Func<JsonElement, JsonElement> predicate = null, string messageType = null)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var messages = new List<JsonElement>();
            while (messages.Count < messageCount && messageCollectionTimeout > TimeSpan.Zero
                && Events.TryTake(out var evt, messageCollectionTimeout))
            {
                messageCollectionTimeout -= stopWatch.Elapsed;
                foreach (var body in evt.Buffers)
                {
                    var json = Encoding.UTF8.GetString(body);
                    var document = JsonDocument.Parse(json);
                    json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
                    var element = document.RootElement;
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            Add(messages, item, ref metadata, predicate, messageType, _messageIds);
                        }
                    }
                    else if (element.ValueKind == JsonValueKind.Object)
                    {
                        Add(messages, element, ref metadata, predicate, messageType, _messageIds);
                    }
                    if (messages.Count >= messageCount)
                    {
                        break;
                    }
                }
            }
            return messages.Take(messageCount).ToList();

            static void Add(List<JsonElement> messages, JsonElement item, ref JsonElement? metadata,
                Func<JsonElement, JsonElement> predicate, string messageType, HashSet<string> messageIds)
            {
                if (messageType != null)
                {
                    if (item.TryGetProperty("MessageType", out var v))
                    {
                        var type = v.GetString();
                        if (type == "ua-metadata")
                        {
                            metadata = item;
                        }
                        if (type != messageType)
                        {
                            return;
                        }
                    }
                    if (item.TryGetProperty("MessageId", out var id))
                    {
                        Assert.True(messageIds.Add(id.GetString()));
                    }
                }
                var add = item;
                if (predicate != null)
                {
                    add = predicate(item);
                }
                if (add.ValueKind == JsonValueKind.Object)
                {
                    messages.Add(add);
                }
            }
        }

        /// <summary>
        /// Start publisher
        /// </summary>
        protected Task StartPublisherAsync(string publishedNodesFile = null, string[] arguments = default)
        {
            Task.Run(() => HostPublisherAsync(
                Mock.Of<ILogger>(),
                publishedNodesFile,
                arguments ?? Array.Empty<string>()
            ));
            return _running.Task;
        }

        /// <summary>
        /// Get publisher api
        /// </summary>
        protected IPublisherApi PublisherApi => _apiScope?.Resolve<IPublisherApi>();

        /// <summary>
        /// Stop publisher
        /// </summary>
        protected void StopPublisher()
        {
            // Shut down gracefully.
            _exit.TrySetResult(true);
        }

        /// <summary>
        /// Get endpoints from file
        /// </summary>
        /// <param name="publishedNodesFile"></param>
        /// <returns></returns>
        protected PublishedNodesEntryModel[] GetEndpointsFromFile(string publishedNodesFile)
        {
            IJsonSerializer serializer = new NewtonsoftJsonSerializer();
            var fileContent = File.ReadAllText(publishedNodesFile).Replace("{{Port}}",
                _serverFixture.Port.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
            return serializer.Deserialize<PublishedNodesEntryModel[]>(fileContent);
        }

        /// <summary>
        /// Setup publishing from sample server.
        /// </summary>
        private async Task HostPublisherAsync(ILogger logger, string publishedNodesFile, string[] arguments)
        {
            var publishedNodesFilePath = Path.GetTempFileName();
            if (!string.IsNullOrEmpty(publishedNodesFile))
            {
                File.WriteAllText(publishedNodesFilePath,
                    File.ReadAllText(publishedNodesFile).Replace("{{Port}}",
                    _serverFixture.Port.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
            }
            try
            {
                var config = _typedConnectionString.ToIoTHubConfig();
                arguments = arguments.Concat(
                    new[]
                    {
                        $"--ec={_typedConnectionString}",
                        "--aa",
                        $"--pf={publishedNodesFilePath}"
                    }
                    ).ToArray();

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true)
                    .AddEnvironmentVariables()
                    .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                    .AddInMemoryCollection(new PublisherCliOptions(arguments.ToArray()))
                    .AddCommandLine(arguments.ToArray())
                    .Build();

                using (var cts = new CancellationTokenSource())
                {
                    // Start publisher module
                    var host = Task.Run(() =>
                    HostAsync(logger, configuration, new List<(DeviceTwinModel, DeviceModel)>() {
                        (new DeviceTwinModel(), new DeviceModel() { Id = _typedConnectionString.DeviceId }) }), cts.Token);
                    await Task.WhenAny(_exit.Task).ConfigureAwait(false);
                    cts.Cancel();
                    await host.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancellation operation.");
            }
            finally
            {
                if (File.Exists(publishedNodesFilePath))
                {
                    File.Delete(publishedNodesFilePath);
                }
            }
        }

        /// <summary>
        /// Host the publisher module.
        /// </summary>
        private async Task HostAsync(ILogger logger, IConfiguration configurationRoot, List<(DeviceTwinModel, DeviceModel)> devices)
        {
            try
            {
                using (var hostScope = ConfigureContainer(configurationRoot, devices))
                {
                    var module = hostScope.Resolve<IModuleHost>();
                    var events = hostScope.Resolve<IEventEmitter>();
                    var moduleConfig = hostScope.Resolve<IModuleConfig>();

                    Events = hostScope.Resolve<IIoTHub>().Events;

                    try
                    {
                        var version = GetType().Assembly.GetReleaseVersion().ToString();
                        logger.LogInformation("Starting module OpcPublisher version {Version}.", version);
                        // Start module
                        await module.StartAsync(IdentityType.Publisher, "OpcPublisher", version, null).ConfigureAwait(false);

                        _apiScope = ConfigureContainer(configurationRoot, hostScope.Resolve<IIoTHubTwinServices>());
                        _running.TrySetResult(true);
                        await Task.WhenAny(_exit.Task).ConfigureAwait(false);
                        logger.LogInformation("Module exits...");
                    }
                    catch (Exception ex)
                    {
                        _running.TrySetException(ex);
                    }
                    finally
                    {
                        await module.StopAsync().ConfigureAwait(false);

                        Events = null;
                        _apiScope?.Dispose();
                        _apiScope = null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error when initializing module host.");
                throw;
            }
        }

        /// <summary>
        /// Configure DI for the API scope
        /// </summary>
        /// <param name="configurationRoot"></param>
        /// <param name="ioTHubTwinServices"></param>
        /// <returns></returns>
        private IContainer ConfigureContainer(IConfiguration configurationRoot,
            IIoTHubTwinServices ioTHubTwinServices)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(configurationRoot)
                .AsImplementedInterfaces();
            builder.RegisterInstance(ioTHubTwinServices)
                .ExternallyOwned();
            builder.AddDiagnostics(logging => logging.AddConsole());
            builder.AddNewtonsoftJsonSerializer();
            builder.RegisterType<IoTHubTwinMethodClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<ChunkMethodClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherApiClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(this)
                .As<ISdkConfig>();
            return builder.Build();
        }

        /// <summary>
        /// Configures DI for the types required.
        /// </summary>
        private static IContainer ConfigureContainer(IConfiguration configuration,
            List<(DeviceTwinModel, DeviceModel)> devices)
        {
            var config = new PublisherConfig(configuration);
            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherCliOptions>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();

            // Register module and agent framework ...
            builder.RegisterModule<ModuleFramework>();
            builder.AddNewtonsoftJsonSerializer();

            builder.AddDiagnostics(logging => logging.AddConsole());
            builder.RegisterType<IoTHubClientFactory>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.Register(ctx => IoTHubServices.Create(devices))
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<ModuleHost>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<PublisherIdentity>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublishedNodesProvider>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublishedNodesJobConverter>()
                .SingleInstance();
            builder.RegisterType<PublisherConfigurationService>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherHostService>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<WriterGroupScopeFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherDiagnosticCollector>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<StackLogger>()
                .AsImplementedInterfaces().SingleInstance().AutoActivate();
            builder.RegisterType<OpcUaClientManager>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces();

            return builder.Build();
        }

        private readonly TaskCompletionSource<bool> _exit;
        private readonly TaskCompletionSource<bool> _running;
        private readonly ConnectionString _typedConnectionString;
        private readonly ReferenceServerFixture _serverFixture;
        private readonly HashSet<string> _messageIds = new();
        private IContainer _apiScope;
    }
}
