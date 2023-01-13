// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using Autofac;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http.HealthChecks;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Hosting;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controller;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Opc.Ua;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using static Microsoft.Azure.IIoT.Hub.Mock.IoTHubServices;

    /// <summary>
    /// Base class for integration testing, it connects to the server, runs publisher and injects mocked IoTHub services.
    /// </summary>
    public class PublisherIntegrationTestBase {
        /// <summary>
        /// Whether the module is running.
        /// </summary>
        private BlockingCollection<EventMessage> Events { get; set; }

        /// <summary>
        /// Device Id
        /// </summary>
        protected string DeviceId { get; } = Utils.GetHostName();

        /// <summary>
        /// Module Id
        /// </summary>
        protected string ModuleId { get; }

        public PublisherIntegrationTestBase(ReferenceServerFixture serverFixture) {
            // This is a fake but correctly formatted connection string.
            var connectionString = $"HostName=dummy.azure-devices.net;" +
                $"DeviceId={DeviceId};" +
                $"SharedAccessKeyName=iothubowner;" +
                $"SharedAccessKey=aXRpc25vdGFuYWNjZXNza2V5";
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
            string[] arguments = default) {
            // Collect messages from server with default settings
            return ProcessMessagesAsync(publishedNodesFile, TimeSpan.FromMinutes(2), 1, predicate, messageType, arguments);
        }

        protected Task<(JsonElement? Metadata, List<JsonElement> Messages)> ProcessMessagesAndMetadataAsync(
            string publishedNodesFile,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default) {
            // Collect messages from server with default settings
            return ProcessMessagesAndMetadataAsync(publishedNodesFile, TimeSpan.FromMinutes(2), 1, predicate, messageType, arguments);
        }

        protected async Task<List<JsonElement>> ProcessMessagesAsync(
            string publishedNodesFile,
            TimeSpan messageCollectionTimeout,
            int messageCount,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default) {

            var (_, messages) = await ProcessMessagesAndMetadataAsync(publishedNodesFile,
                messageCollectionTimeout, messageCount, predicate, messageType, arguments);
            return messages;
        }

        protected async Task<(JsonElement? Metadata, List<JsonElement> Messages)> ProcessMessagesAndMetadataAsync(
            string publishedNodesFile,
            TimeSpan messageCollectionTimeout,
            int messageCount,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default) {

            await StartPublisherAsync(publishedNodesFile, arguments);

            JsonElement? metadata = null;
            var messages = WaitForMessagesAndMetadata(messageCollectionTimeout, messageCount, ref metadata, predicate, messageType);

            StopPublisher();

            return (metadata, messages);
        }

        /// <summary>
        /// Wait for one message
        /// </summary>
        protected List<JsonElement> WaitForMessages(
            Func<JsonElement, JsonElement> predicate = null, string messageType = null) {
            // Collect messages from server with default settings
            JsonElement? metadata = null;
            return WaitForMessagesAndMetadata(TimeSpan.FromMinutes(2), 1, ref metadata, predicate, messageType);
        }

        /// <summary>
        /// Wait for one message
        /// </summary>
        protected List<JsonElement> WaitForMessagesAndMetadata(ref JsonElement? metadata,
            Func<JsonElement, JsonElement> predicate = null, string messageType = null) {
            // Collect messages from server with default settings
            return WaitForMessagesAndMetadata(TimeSpan.FromMinutes(2), 1, ref metadata, predicate, messageType);
        }

        /// <summary>
        /// Wait for one message
        /// </summary>
        protected List<JsonElement> WaitForMessages(TimeSpan messageCollectionTimeout, int messageCount,
            Func<JsonElement, JsonElement> predicate = null, string messageType = null) {
            // Collect messages from server with default settings
            JsonElement? metadata = null;
            return WaitForMessagesAndMetadata(messageCollectionTimeout, messageCount, ref metadata, predicate, messageType);
        }

        /// <summary>
        /// Wait for messages
        /// </summary>
        protected List<JsonElement> WaitForMessagesAndMetadata(TimeSpan messageCollectionTimeout, int messageCount,
            ref JsonElement? metadata, Func<JsonElement, JsonElement> predicate = null, string messageType = null) {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var messages = new List<JsonElement>();
            while (messages.Count < messageCount && messageCollectionTimeout > TimeSpan.Zero
                && Events.TryTake(out var evt, messageCollectionTimeout)) {
                messageCollectionTimeout -= stopWatch.Elapsed;
                var json = Encoding.UTF8.GetString(evt.Message.GetBytes());
                var document = JsonDocument.Parse(json);
                json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
                var element = document.RootElement;
                if (element.ValueKind == JsonValueKind.Array) {
                    foreach (var item in element.EnumerateArray()) {
                        Add(messages, item, ref metadata, predicate, messageType, _messageIds);
                    }
                }
                else if (element.ValueKind == JsonValueKind.Object) {
                    Add(messages, element, ref metadata, predicate, messageType, _messageIds);
                }
            }
            return messages.Take(messageCount).ToList();

            static void Add(List<JsonElement> messages, JsonElement item, ref JsonElement? metadata,
                Func<JsonElement, JsonElement> predicate, string messageType, HashSet<string> messageIds) {
                if (messageType != null) {
                    if (item.TryGetProperty("MessageType", out var v)) {
                        var type = v.GetString();
                        if (type == "ua-metadata") {
                            metadata = item;
                        }
                        if (type != messageType) {
                            return;
                        }
                    }
                    if (item.TryGetProperty("MessageId", out var id)) {
                        Assert.True(messageIds.Add(id.GetString()));
                    }
                }
                var add = item;
                if (predicate != null) {
                    add = predicate(item);
                }
                if (add.ValueKind == JsonValueKind.Object) {
                    messages.Add(add);
                }
            }
        }

        /// <summary>
        /// Start publisher
        /// </summary>
        protected Task StartPublisherAsync(string publishedNodesFile = null, string[] arguments = default) {
            _ = Task.Run(() => HostPublisherAsync(
                Mock.Of<ILogger>(),
                publishedNodesFile,
                arguments ?? Array.Empty<string>()
            ));
            return _running.Task;
        }

        /// <summary>
        /// Get publisher api
        /// </summary>
        protected IPublisherControlApi PublisherApi => _apiScope?.Resolve<IPublisherControlApi>();

        /// <summary>
        /// Stop publisher
        /// </summary>
        protected void StopPublisher() {
            // Shut down gracefully.
            _exit.TrySetResult(true);
        }

        /// <summary>
        /// Get endpoints from file
        /// </summary>
        /// <param name="publishedNodesFile"></param>
        /// <returns></returns>
        protected PublishNodesEndpointApiModel[] GetEndpointsFromFile(string publishedNodesFile) {
            IJsonSerializer serializer = new NewtonSoftJsonSerializer();
            var fileContent = File.ReadAllText(publishedNodesFile).Replace("{{Port}}", _serverFixture.Port.ToString());
            return serializer.Deserialize<PublishNodesEndpointApiModel[]>(fileContent);
        }

        /// <summary>
        /// Setup publishing from sample server.
        /// </summary>
        private async Task HostPublisherAsync(ILogger logger, string publishedNodesFile, string[] arguments) {
            var publishedNodesFilePath = Path.GetTempFileName();
            if (!string.IsNullOrEmpty(publishedNodesFile)) {
                File.WriteAllText(publishedNodesFilePath,
                    File.ReadAllText(publishedNodesFile).Replace("{{Port}}", _serverFixture.Port.ToString()));
            }
            try {
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
                                        .AddStandalonePublisherCommandLine(arguments.ToArray())
                                        .AddCommandLine(arguments.ToArray())
                                        .Build();

                using (var cts = new CancellationTokenSource()) {
                    // Start publisher module
                    var host = Task.Run(() =>
                    HostAsync(logger, configuration, new List<(DeviceTwinModel, DeviceModel)>() {
                        (new DeviceTwinModel(), new DeviceModel() { Id = _typedConnectionString.DeviceId }) }), cts.Token);
                    await Task.WhenAny(_exit.Task);
                    cts.Cancel();
                    await host;
                }
            }
            catch (OperationCanceledException) {
                Console.WriteLine("Cancellation operation.");
            }
            finally {
                if (File.Exists(publishedNodesFilePath)) {
                    File.Delete(publishedNodesFilePath);
                }
            }
        }

        /// <summary>
        /// Host the publisher module.
        /// </summary>
        private async Task HostAsync(ILogger logger, IConfiguration configurationRoot, List<(DeviceTwinModel, DeviceModel)> devices) {
            try {
                // Hook event source
                using (var broker = new EventSourceBroker()) {

                    using (var hostScope = ConfigureContainer(configurationRoot, devices)) {
                        var module = hostScope.Resolve<IModuleHost>();
                        var events = hostScope.Resolve<IEventEmitter>();
                        var workerSupervisor = hostScope.Resolve<IWorkerSupervisor>();
                        var moduleConfig = hostScope.Resolve<IModuleConfig>();
                        var identity = hostScope.Resolve<IIdentity>();
                        var healthCheckManager = hostScope.Resolve<IHealthCheckManager>();
                        ISessionManager sessionManager = null;

                        Events = hostScope.Resolve<IIoTHub>().Events;

                        try {
                            var version = GetType().Assembly.GetReleaseVersion().ToString();
                            logger.Information("Starting module OpcPublisher version {version}.", version);
                            healthCheckManager.Start();
                            // Start module
                            await module.StartAsync(IdentityType.Publisher, "IntegrationTests", "OpcPublisher", version, null);
                            await workerSupervisor.StartAsync();
                            sessionManager = hostScope.Resolve<ISessionManager>();

                            _apiScope = ConfigureContainer(configurationRoot, hostScope.Resolve<IIoTHubTwinServices>());
                            _running.TrySetResult(true);
                            await Task.WhenAny(_exit.Task);
                            logger.Information("Module exits...");
                        }
                        catch (Exception ex) {
                            _running.TrySetException(ex);
                        }
                        finally {
                            await workerSupervisor.StopAsync();
                            await sessionManager?.StopAsync();
                            healthCheckManager.Stop();
                            await module.StopAsync();

                            Events = null;
                            _apiScope?.Dispose();
                            _apiScope = null;
                        }
                    }
                }
            }
            catch (Exception ex) {
                logger.Error(ex, "Error when initializing module host.");
                throw;
            }
        }

        /// <summary>
        /// Configure DI for the API scope
        /// </summary>
        /// <param name="configurationRoot"></param>
        /// <param name="ioTHubTwinServices"></param>
        /// <returns></returns>
        private static IContainer ConfigureContainer(IConfiguration configurationRoot, IIoTHubTwinServices ioTHubTwinServices) {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(configurationRoot).AsImplementedInterfaces();
            builder.RegisterInstance(ioTHubTwinServices).ExternallyOwned();
            builder.AddConsoleLogger();
            builder.RegisterModule<NewtonSoftJsonModule>();
            builder.RegisterType<IoTHubTwinMethodClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<ChunkMethodClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherModuleControlClient>()
                .AsImplementedInterfaces().SingleInstance();
            return builder.Build();
        }

        /// <summary>
        /// Configures DI for the types required.
        /// </summary>
        private static IContainer ConfigureContainer(IConfiguration configuration, List<(DeviceTwinModel, DeviceModel)> devices) {
            var config = new Config(configuration);
            var builder = new ContainerBuilder();
            var standaloneCliOptions = new StandaloneCliOptions(configuration);

            // Register configuration interfaces
            builder.RegisterInstance(config).AsImplementedInterfaces();
            builder.RegisterInstance(config.Configuration).AsImplementedInterfaces();

            builder.RegisterModule<PublisherJobsConfiguration>();

            // Register module and agent framework ...
            builder.RegisterModule<AgentFramework>();
            builder.RegisterModule<ModuleFramework>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            builder.AddDiagnostics(config, standaloneCliOptions.ToLoggerConfiguration());
            builder.RegisterInstance(standaloneCliOptions).AsImplementedInterfaces();

            builder.RegisterType<IoTHubClientFactory>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.Register(ctx => Create(devices)).AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<ModuleHost>().AsImplementedInterfaces().SingleInstance();
            // Published nodes file provider
            builder.RegisterType<PublishedNodesProvider>().AsImplementedInterfaces().SingleInstance();
            // Local orchestrator
            builder.RegisterType<StandaloneJobOrchestrator>().AsImplementedInterfaces().SingleInstance();
            // Create jobs from published nodes file
            builder.RegisterType<PublishedNodesJobConverter>().SingleInstance();
            builder.RegisterType<PublisherMethodsController>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<IdentityTokenSettingsController>().AsImplementedInterfaces().SingleInstance();

            // Opc specific parts
            builder.RegisterType<DefaultSessionManager>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SubscriptionServices>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VariantEncoderFactory>().AsImplementedInterfaces();

            builder.RegisterType<HealthCheckManager>().AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }

        private readonly TaskCompletionSource<bool> _exit;
        private readonly TaskCompletionSource<bool> _running;
        private readonly ConnectionString _typedConnectionString;
        private readonly ReferenceServerFixture _serverFixture;
        HashSet<string> _messageIds = new HashSet<string>();
        private IContainer _apiScope;
    }
}
