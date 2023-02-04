// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controller;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Hosting;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Module;
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
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http.HealthChecks;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using Moq;
    using MQTTnet;
    using MQTTnet.Protocol;
    using MQTTnet.Server;
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
    using Microsoft.Azure.IIoT.Exceptions;
    using MQTTnet.Formatter;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.State;

    public readonly record struct JsonMessage(string Topic, JsonElement Message, string ContentType);

    /// <summary>
    /// Base class for integration testing, it connects to the server, runs publisher and injects mocked IoTHub services.
    /// </summary>
    public class PublisherMqttIntegrationTestBase {

        public PublisherMqttIntegrationTestBase(ReferenceServerFixture serverFixture) {
            _exit = new TaskCompletionSource<bool>();
            _running = new TaskCompletionSource<bool>();
            _serverFixture = serverFixture;
        }

        protected Task<List<JsonMessage>> ProcessMessagesAsync(
            string publishedNodesFile,
            bool useMqtt5,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default) {
            // Collect messages from server with default settings
            return ProcessMessagesAsync(publishedNodesFile, TimeSpan.FromMinutes(2), 1, useMqtt5, predicate, messageType, arguments);
        }

        protected Task<(JsonMessage? Metadata, List<JsonMessage> Messages)> ProcessMessagesAndMetadataAsync(
            string publishedNodesFile,
            bool useMqtt5,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default) {
            // Collect messages from server with default settings
            return ProcessMessagesAndMetadataAsync(publishedNodesFile, TimeSpan.FromMinutes(2), 1,
                useMqtt5, predicate, messageType, arguments);
        }

        protected async Task<List<JsonMessage>> ProcessMessagesAsync(
            string publishedNodesFile,
            TimeSpan messageCollectionTimeout,
            int messageCount,
            bool useMqtt5,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default) {

            var (_, messages) = await ProcessMessagesAndMetadataAsync(publishedNodesFile,
                messageCollectionTimeout, messageCount, useMqtt5, predicate, messageType, arguments);
            return messages;
        }

        protected async Task<(JsonMessage? Metadata, List<JsonMessage> Messages)> ProcessMessagesAndMetadataAsync(
            string publishedNodesFile,
            TimeSpan messageCollectionTimeout,
            int messageCount,
            bool useMqtt5,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default) {

            await StartPublisherAsync(useMqtt5, publishedNodesFile, arguments);

            JsonMessage? metadata = null;
            var messages = WaitForMessagesAndMetadata(messageCollectionTimeout, messageCount, ref metadata, predicate, messageType);

            StopPublisher();

            return (metadata, messages);
        }

        /// <summary>
        /// Wait for one message
        /// </summary>
        protected List<JsonMessage> WaitForMessages(
            Func<JsonElement, JsonElement> predicate = null, string messageType = null) {
            // Collect messages from server with default settings
            JsonMessage? metadata = null;
            return WaitForMessagesAndMetadata(TimeSpan.FromMinutes(2), 1, ref metadata, predicate, messageType);
        }

        /// <summary>
        /// Wait for one message
        /// </summary>
        protected List<JsonMessage> WaitForMessagesAndMetadata(ref JsonMessage? metadata,
            Func<JsonElement, JsonElement> predicate = null, string messageType = null) {
            // Collect messages from server with default settings
            return WaitForMessagesAndMetadata(TimeSpan.FromMinutes(2), 1, ref metadata, predicate, messageType);
        }

        /// <summary>
        /// Wait for one message
        /// </summary>
        protected List<JsonMessage> WaitForMessages(TimeSpan messageCollectionTimeout, int messageCount,
            Func<JsonElement, JsonElement> predicate = null, string messageType = null) {
            // Collect messages from server with default settings
            JsonMessage? metadata = null;
            return WaitForMessagesAndMetadata(messageCollectionTimeout, messageCount, ref metadata, predicate, messageType);
        }

        /// <summary>
        /// Wait for messages
        /// </summary>
        protected List<JsonMessage> WaitForMessagesAndMetadata(TimeSpan messageCollectionTimeout, int messageCount,
            ref JsonMessage? metadata, Func<JsonElement, JsonElement> predicate = null, string messageType = null) {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var messages = new List<JsonMessage>();
            while (messages.Count < messageCount && messageCollectionTimeout > TimeSpan.Zero
                && Events.TryTake(out var evt, messageCollectionTimeout)) {
                messageCollectionTimeout -= stopWatch.Elapsed;

                var (topic, body, contentType) = evt;

                var json = Encoding.UTF8.GetString(body.ToArray());
                var document = JsonDocument.Parse(json);
                json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
                var element = document.RootElement;
                if (element.ValueKind == JsonValueKind.Array) {
                    foreach (var item in element.EnumerateArray()) {
                        Add(messages, item, ref metadata, predicate, messageType, _messageIds, topic, contentType);
                    }
                }
                else if (element.ValueKind == JsonValueKind.Object) {
                    Add(messages, element, ref metadata, predicate, messageType, _messageIds, topic, contentType);
                }
                if (messages.Count >= messageCount) {
                    break;
                }
            }
            return messages.Take(messageCount).ToList();

            static void Add(List<JsonMessage> messages, JsonElement item, ref JsonMessage? metadata,
                Func<JsonElement, JsonElement> predicate, string messageType, HashSet<string> messageIds,
                string topic, string contentType) {
                if (messageType != null) {
                    if (item.TryGetProperty("MessageType", out var v)) {
                        var type = v.GetString();
                        if (type == "ua-metadata") {
                            metadata = new JsonMessage(topic, item, contentType);
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
                    messages.Add(new JsonMessage(topic, add, contentType));
                }
            }
        }

        /// <summary>
        /// Start publisher
        /// </summary>
        protected Task StartPublisherAsync(bool useMqtt5, string publishedNodesFile = null,
            string[] arguments = default) {
            _ = Task.Run(() => HostPublisherAsync(
                Mock.Of<ILogger>(),
                publishedNodesFile,
                useMqtt5 ? "v500" : "v311",
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

        /// </summary>
        private BlockingCollection<(string topic, ReadOnlyMemory<byte> buffer, string contentType)> Events { get; }
            = new BlockingCollection<(string topic, ReadOnlyMemory<byte> buffer, string contentType)>();

        /// <summary>
        /// Setup publishing from sample server.
        /// </summary>
        private async Task HostPublisherAsync(ILogger logger, string publishedNodesFile,
            string protocol, string[] arguments) {

            var topicRoot = "/publishers/mypublishertest";

            using var broker = await MqttBroker.CreateAsync(protocol, (topic, buffer, contentType) => {
                Events.Add((topic, buffer, contentType));
                return Task.CompletedTask;
            }, topicRoot);
            broker.UserName = "user";
            broker.Password = "pass";

            var publishedNodesFilePath = Path.GetTempFileName();
            if (!string.IsNullOrEmpty(publishedNodesFile)) {
                File.WriteAllText(publishedNodesFilePath,
                    File.ReadAllText(publishedNodesFile).Replace("{{Port}}", _serverFixture.Port.ToString()));
            }
            try {
                arguments = arguments.Concat(
                    new[]
                    {
$"--mqc=HostName=localhost;Port={broker.Port};Username={broker.UserName};Password={broker.Password};Protocol={protocol}",
$"--ttt={topicRoot}",
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
                    var host = Task.Run(() => HostAsync(logger, configuration, broker), cts.Token);
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
        private async Task HostAsync(ILogger logger, IConfiguration configurationRoot, MqttBroker mqttBroker) {
            try {
                // Hook event source
                using (var broker = new EventSourceBroker()) {

                    using (var hostScope = ConfigureContainer(configurationRoot)) {
                        var module = hostScope.Resolve<IModuleHost>();
                        var events = hostScope.Resolve<IEventEmitter>();
                        var moduleConfig = hostScope.Resolve<IModuleConfig>();
                        var identity = hostScope.Resolve<IIdentity>();
                        var healthCheckManager = hostScope.Resolve<IHealthCheckManager>();
                        ISessionManager sessionManager = null;

                        try {
                            var version = GetType().Assembly.GetReleaseVersion().ToString();
                            logger.Information("Starting module OpcPublisher version {version}.", version);
                            healthCheckManager.Start();
                            // Start module
                            await module.StartAsync(IdentityType.Publisher, "IntegrationTests", "OpcPublisher", version, null);
                            sessionManager = hostScope.Resolve<ISessionManager>();

                            _apiScope = ConfigureContainer(configurationRoot, mqttBroker);
                            _running.TrySetResult(true);
                            await Task.WhenAny(_exit.Task);
                            logger.Information("Module exits...");
                        }
                        catch (Exception ex) {
                            _running.TrySetException(ex);
                        }
                        finally {
                            healthCheckManager.Stop();
                            await module.StopAsync();

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
        /// <param name="methodClient"></param>
        /// <returns></returns>
        private static IContainer ConfigureContainer(IConfiguration configurationRoot,
            IJsonMethodClient methodClient) {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(configurationRoot).AsImplementedInterfaces();
            builder.RegisterInstance(methodClient).ExternallyOwned();
            builder.AddConsoleLogger();
            builder.RegisterModule<NewtonSoftJsonModule>();
            builder.RegisterType<ChunkMethodClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherModuleControlClient>()
                .AsImplementedInterfaces().SingleInstance();
            return builder.Build();
        }

        /// <summary>
        /// Configures DI for the types required.
        /// </summary>
        private static IContainer ConfigureContainer(IConfiguration configuration) {
            var config = new Config(configuration);
            var builder = new ContainerBuilder();
            var standaloneCliOptions = new StandaloneCliOptions(configuration);

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces();

            // Register module and agent framework ...
            builder.RegisterModule<ModuleFramework>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            builder.AddDiagnostics(config, standaloneCliOptions.ToLoggerConfiguration());
            builder.RegisterInstance(standaloneCliOptions).AsImplementedInterfaces();

            builder.RegisterType<PublishedNodesProvider>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublishedNodesJobConverter>()
                .SingleInstance();
            builder.RegisterType<PublisherConfigService>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherHostService>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<WriterGroupContainerFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<RuntimeStateReporter>()
                .AsImplementedInterfaces().SingleInstance();

            // Opc specific parts
            builder.RegisterType<OpcUaClientManager>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces();
            builder.RegisterType<HealthCheckManager>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }

        /// <summary>
        /// Mqtt broker that can serve as event client
        /// </summary>
        internal sealed class MqttBroker : IDisposable, IJsonMethodClient {

            /// <summary>
            /// Port number
            /// </summary>
            public int Port { get; }

            /// <summary>
            /// User name
            /// </summary>
            public string UserName { get; set; }

            /// <summary>
            /// Password
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Client id
            /// </summary>
            public string ClientId { get; } = Guid.NewGuid().ToString();

            /// <summary>
            /// Max method payload size
            /// </summary>
            public int MaxMethodPayloadCharacterCount => 128 * 1024;

            /// <summary>
            /// Create service client
            /// </summary>
            private MqttBroker(ILogger logger, MqttServer server, int port,
                Func<string, ReadOnlyMemory<byte>, string, Task> subscription,
                string topicRoot, string protocol) {
                _logger = logger;
                _server = server;
                _subscription = subscription;
                _topicRoot = topicRoot;
                _useMqtt5 = Enum.Parse<MqttProtocolVersion>(protocol, true) == MqttProtocolVersion.V500;
                Port = port;
            }

            /// <inheritdoc/>
            public void Dispose() {
                _server.Dispose();
                _lock.Dispose();
            }

            /// <inheritdoc/>
            public async Task<string> CallMethodAsync(string deviceId, string moduleId,
                string method, string json, TimeSpan? timeout = null, CancellationToken ct = default) {
                var payload = Encoding.UTF8.GetBytes(json);
                var requestId = Guid.NewGuid();

                // Cancel previous call, only one allowed at a time.
                _currentCall.callback?.TrySetCanceled(default);
                var responseFilter = _useMqtt5 ? $"{_topicRoot}/responses/{method}" : $"{_topicRoot}/methods/res/#";
                _currentCall = (responseFilter, new TaskCompletionSource<(string, MqttApplicationMessage)>());

                await SendMessageAsync(
                    _useMqtt5 ? $"{_topicRoot}/methods/{method}"    : $"{_topicRoot}/methods/{method}/?$rid={requestId}",
                    _useMqtt5 ? $"{_topicRoot}/responses/{method}"  : null,
                    _useMqtt5 ? requestId.ToByteArray()             : null,
                    payload.AsMemory(), "application/json", ct: ct);

                var result = await _currentCall.callback.Task;
                var status = 0;
                if (_useMqtt5) {
                    status = int.Parse(result.Item2.UserProperties
                        .FirstOrDefault(p => p.Name == "StatusCode")?.Value ?? "500");
                    if (!requestId.ToByteArray().SequenceEqual(result.Item2.CorrelationData)) {
                        throw new MethodCallException("Did not get correct correlation data back.");
                    }
                }
                else {
                    var components = result.Item1.Replace($"{_topicRoot}/methods/res/", "").Split('/');
                    status = int.Parse(components[components.Length - 2]);
                    if (requestId.ToString() != components[components.Length - 1].Substring("?$rid=".Length)) {
                        throw new MethodCallException("Did not get correct request id back.");
                    }
                }

                var jsonResponse = Encoding.UTF8.GetString(result.Item2.Payload);
                if (status != 200) {
                    throw new MethodCallStatusException(jsonResponse, status);
                }
                return jsonResponse;
            }

            /// <summary>
            /// Send message to client
            /// </summary>
            private Task SendMessageAsync(string topic, string responseTopic,
                byte[] correlationData, ReadOnlyMemory<byte> payload,
                string contentType, CancellationToken ct = default) {
                if (topic == null) {
                    throw new ArgumentNullException(nameof(topic));
                }
                var injected = new InjectedMqttApplicationMessage(new MqttApplicationMessage {
                    Topic = topic,
                    Payload = payload.ToArray(),
                    ContentType = contentType,
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    ResponseTopic = responseTopic,
                    CorrelationData = correlationData,
                    Retain = false
                }) {
                    SenderClientId = ClientId
                };
                return _server.InjectApplicationMessage(injected, ct);
            }

            /// <summary>
            /// Handle connection
            /// </summary>
            private Task HandleClientConnectedAsync(ClientConnectedEventArgs args) {
                _logger.Information("Client {ClientId} connected.", args.ClientId);
                return Task.CompletedTask;
            }

            /// <summary>
            /// Handle unsubscribe
            /// </summary>
            /// <param name="args"></param>
            private Task HandleClientUnsubscribedTopicAsync(ClientUnsubscribedTopicEventArgs args) {
                _logger.Information("Client {ClientId} unsubscribed from {Topic}.",
                    args.ClientId, args.TopicFilter);
                return Task.CompletedTask;
            }

            /// <summary>
            /// Handle subscribe
            /// </summary>
            /// <param name="args"></param>
            private Task HandleClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs args) {
                _logger.Information("Client {ClientId} subscribed to {Topic}.",
                    args.ClientId, args.TopicFilter);
                return Task.CompletedTask;
            }

            /// <summary>
            /// Handle message receival
            /// </summary>
            /// <param name="args"></param>
            private async Task HandleMessageReceivedAsync(InterceptingPublishEventArgs args) {
                if (args?.ApplicationMessage == null) {
                    return;
                }
                var topic = args.ApplicationMessage.Topic;
                _logger.Debug("Client received message from {Client} on {Topic}",
                    args.ClientId, topic);
                await _lock.WaitAsync().ConfigureAwait(false);
                try {
                    var current = _currentCall;
                    if (current.topic != null &&
                        MqttTopicFilterComparer.Compare(topic, current.topic) == MqttTopicFilterCompareResult.IsMatch) {
                        current.callback.TrySetResult((topic, args.ApplicationMessage));
                    }
                    else if (MqttTopicFilterComparer.Compare(topic, $"{_topicRoot}/twin/#") != MqttTopicFilterCompareResult.IsMatch &&
                        MqttTopicFilterComparer.Compare(topic, $"{_topicRoot}/methods/#") != MqttTopicFilterCompareResult.IsMatch) {
                        await _subscription.Invoke(topic, args.ApplicationMessage.Payload,
                            args.ApplicationMessage.ContentType ?? "NoContentType_UseMqttv5").ConfigureAwait(false);
                    }
                }
                finally {
                    _lock.Release();
                }
            }

            /// <summary>
            /// Handle connection
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            private Task ValidateConnectionAsync(ValidatingConnectionEventArgs args) {
                if (UserName != null && args.UserName != UserName) {
                    args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                }
                else if (Password != null && args.Password != Password) {
                    args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                }
                else {
                    args.ReasonCode = MqttConnectReasonCode.Success;
                }
                return Task.CompletedTask;
            }

            /// <summary>
            /// Handle disconnected
            /// </summary>
            /// <param name="args"></param>
            private Task HandleClientDisconnectedAsync(ClientDisconnectedEventArgs args) {
                _logger.Information("Disconnected client {ClientId} with type {Reason}",
                    args.ClientId, args.DisconnectType);
                return Task.CompletedTask;
            }

            /// <summary>
            /// Create broker
            /// </summary>
            /// <returns></returns>
            public static async Task<MqttBroker> CreateAsync(string protocol,
                Func<string, ReadOnlyMemory<byte>, string, Task> subscription, string topicRoot,
                ILogger logger = null) {
                for (var port = 1883; ; port++) {
                    try {
                        return await CreateAsync(protocol, subscription, port, topicRoot, logger);
                    }
                    catch {
                        continue;
                    }
                }
            }

            /// <summary>ping
            /// Create client and start it
            /// </summary>
            public static async Task<MqttBroker> CreateAsync(string protocol,
                Func<string, ReadOnlyMemory<byte>, string, Task> subscription, int port, string topicRoot,
                ILogger logger = null) {
                logger ??= ConsoleLogger.Create();
                var optionsBuilder = new MqttServerOptionsBuilder()
                    .WithDefaultEndpoint()
                    .WithDefaultEndpointPort(port)
                    ;
                var server = new MqttFactory().CreateMqttServer(optionsBuilder.Build());
                var mqttBroker = new MqttBroker(logger, server, port, subscription, topicRoot, protocol);
                try {
                    server.ValidatingConnectionAsync += mqttBroker.ValidateConnectionAsync;
                    server.ClientConnectedAsync += mqttBroker.HandleClientConnectedAsync;
                    server.ClientDisconnectedAsync += mqttBroker.HandleClientDisconnectedAsync;
                    server.ClientSubscribedTopicAsync += mqttBroker.HandleClientSubscribedTopicAsync;
                    server.ClientUnsubscribedTopicAsync += mqttBroker.HandleClientUnsubscribedTopicAsync;
                    server.InterceptingPublishAsync += mqttBroker.HandleMessageReceivedAsync;
                    await server.StartAsync().ConfigureAwait(false);
                    return mqttBroker;
                }
                catch {
                    server.Dispose();
                    throw;
                }
            }

            private readonly ILogger _logger;
            private readonly MqttServer _server;
            private readonly SemaphoreSlim _lock = new(1, 1);
            private readonly Func<string, ReadOnlyMemory<byte>, string, Task> _subscription;
            private (string topic, TaskCompletionSource<(string, MqttApplicationMessage)> callback) _currentCall;
            private readonly string _topicRoot;
            private readonly bool _useMqtt5;
        }

        private readonly TaskCompletionSource<bool> _exit;
        private readonly TaskCompletionSource<bool> _running;
        private readonly ReferenceServerFixture _serverFixture;
        HashSet<string> _messageIds = new HashSet<string>();
        private IContainer _apiScope;
    }
}
