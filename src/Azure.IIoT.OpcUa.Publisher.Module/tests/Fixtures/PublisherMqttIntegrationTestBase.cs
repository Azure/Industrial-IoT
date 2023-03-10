// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Azure.IIoT.OpcUa.Publisher.Module.Controller;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Sdk.Clients;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Models;
    using Azure.IIoT.OpcUa.Testing.Fixtures;
    using Autofac;
    using Furly.Exceptions;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Rpc;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Furly.Tunnel.Protocol;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using MQTTnet;
    using MQTTnet.Formatter;
    using MQTTnet.Protocol;
    using MQTTnet.Server;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Xunit;
    using Furly;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;

    public readonly record struct JsonMessage(string Topic, JsonElement Message, string ContentType);

    /// <summary>
    /// Base class for integration testing, it connects to the server, runs publisher and injects mocked IoTHub services.
    /// </summary>
    public class PublisherMqttIntegrationTestBase : ISdkConfig
    {
        public string Target => null;

        public PublisherMqttIntegrationTestBase(ReferenceServerFixture serverFixture, ILoggerFactory loggerFactory)
        {
            _exit = new TaskCompletionSource<bool>();
            _running = new TaskCompletionSource<bool>();
            _serverFixture = serverFixture;
            _loggerFactory = loggerFactory;
            _channel = Channel.CreateUnbounded<(string topic, ReadOnlyMemory<byte> buffer, string contentType)>();
        }

        protected Task<(JsonMessage? Metadata, List<JsonMessage> Messages)> ProcessMessagesAndMetadataAsync(
            string publishedNodesFile,
            bool useMqtt5,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default)
        {
            // Collect messages from server with default settings
            return ProcessMessagesAndMetadataAsync(publishedNodesFile, TimeSpan.FromMinutes(2), 1,
                useMqtt5, predicate, messageType, arguments);
        }

        protected async Task<(JsonMessage? Metadata, List<JsonMessage> Messages)> ProcessMessagesAndMetadataAsync(
            string publishedNodesFile,
            TimeSpan messageCollectionTimeout,
            int messageCount,
            bool useMqtt5,
            Func<JsonElement, JsonElement> predicate = null,
            string messageType = null,
            string[] arguments = default)
        {
            await StartPublisherAsync(useMqtt5, publishedNodesFile, arguments).ConfigureAwait(false);
            try
            {
                JsonMessage? metadata = null;
                return await WaitForMessagesAndMetadataAsync(messageCollectionTimeout, messageCount,
                    metadata, predicate, messageType).ConfigureAwait(false);
            }
            finally
            {
                await StopPublisherAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Wait for one message
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        protected Task<List<JsonMessage>> WaitForMessagesAsync(
            Func<JsonElement, JsonElement> predicate = null, string messageType = null)
        {
            // Collect messages from server with default settings
            return WaitForMessagesAsync(TimeSpan.FromMinutes(200), 1, predicate, messageType);
        }

        /// <summary>
        /// Wait for one message
        /// </summary>
        /// <param name="messageCollectionTimeout"></param>
        /// <param name="messageCount"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        protected async Task<List<JsonMessage>> WaitForMessagesAsync(TimeSpan messageCollectionTimeout, int messageCount,
            Func<JsonElement, JsonElement> predicate = null, string messageType = null)
        {
            // Collect messages from server with default settings
            JsonMessage? metadata = null;
            var (_, messages) = await WaitForMessagesAndMetadataAsync(
                messageCollectionTimeout, messageCount, metadata, predicate, messageType).ConfigureAwait(false);
            return messages;
        }

        /// <summary>
        /// Wait for messages
        /// </summary>
        /// <param name="messageCollectionTimeout"></param>
        /// <param name="messageCount"></param>
        /// <param name="metadata"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        protected async Task<(JsonMessage? metadata, List<JsonMessage> messages)> WaitForMessagesAndMetadataAsync(
            TimeSpan messageCollectionTimeout, int messageCount, JsonMessage? metadata,
            Func<JsonElement, JsonElement> predicate = null, string messageType = null)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var messages = new List<JsonMessage>();
            while (messages.Count < messageCount && messageCollectionTimeout > TimeSpan.Zero)
            {
                messageCollectionTimeout -= stopWatch.Elapsed;
                if (messageCollectionTimeout < TimeSpan.Zero)
                {
                    break;
                }

                var cts = new CancellationTokenSource(messageCollectionTimeout);
                var (topic, body, contentType) = await _channel.Reader.ReadAsync(cts.Token).ConfigureAwait(false);

                var json = Encoding.UTF8.GetString(body.ToArray());
                var document = JsonDocument.Parse(json);
                json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
                var element = document.RootElement;
                if (element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        Add(messages, item, ref metadata, predicate, messageType, _messageIds, topic, contentType);
                    }
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    Add(messages, element, ref metadata, predicate, messageType, _messageIds, topic, contentType);
                }
                if (messages.Count >= messageCount)
                {
                    break;
                }
            }
            return (metadata, messages.Take(messageCount).ToList());

            static void Add(List<JsonMessage> messages, JsonElement item, ref JsonMessage? metadata,
                Func<JsonElement, JsonElement> predicate, string messageType, HashSet<string> messageIds,
                string topic, string contentType)
            {
                if (messageType != null)
                {
                    if (item.TryGetProperty("MessageType", out var v))
                    {
                        var type = v.GetString();
                        if (type == "ua-metadata")
                        {
                            metadata = new JsonMessage(topic, item, contentType);
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
                    messages.Add(new JsonMessage(topic, add, contentType));
                }
            }
        }

        /// <summary>
        /// Start publisher
        /// </summary>
        /// <param name="useMqtt5"></param>
        /// <param name="publishedNodesFile"></param>
        /// <param name="arguments"></param>
        protected Task StartPublisherAsync(bool useMqtt5, string publishedNodesFile = null,
            string[] arguments = default)
        {
            _publisher = Task.Run(() => HostPublisherAsync(
                publishedNodesFile,
                useMqtt5 ? "v500" : "v311",
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
        protected Task StopPublisherAsync()
        {
            // Shut down gracefully.
            _exit.TrySetResult(true);
            return _publisher;
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
        /// <param name="publishedNodesFile"></param>
        /// <param name="protocol"></param>
        /// <param name="arguments"></param>
        private async Task HostPublisherAsync(string publishedNodesFile,
            string protocol, string[] arguments)
        {
            const string topicRoot = "/publishers/mypublishertest";

            using var broker = await MqttBroker.CreateAsync(protocol, async (topic, buffer, contentType) =>
                await _channel.Writer.WriteAsync((topic, buffer, contentType)).ConfigureAwait(false),
                topicRoot).ConfigureAwait(false);
            broker.UserName = "user";
            broker.Password = "pass";

            var publishedNodesFilePath = Path.GetTempFileName();
            if (!string.IsNullOrEmpty(publishedNodesFile))
            {
                File.WriteAllText(publishedNodesFilePath, File.ReadAllText(publishedNodesFile).Replace("{{Port}}",
                    _serverFixture.Port.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
            }
            try
            {
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
                    .AddInMemoryCollection(new PublisherCliOptions(arguments.ToArray()))
                    .AddCommandLine(arguments.ToArray())
                    .Build();

                using (var cts = new CancellationTokenSource())
                {
                    // Start publisher module
                    var host = Task.Run(() => HostAsync(configuration, broker), cts.Token);
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
        /// <param name="logger"></param>
        /// <param name="configurationRoot"></param>
        /// <param name="mqttBroker"></param>
        private async Task HostAsync(IConfiguration configurationRoot, MqttBroker mqttBroker)
        {
            var logger = _loggerFactory.CreateLogger("Publisher");
            try
            {
                using (var hostScope = ConfigureContainer(configurationRoot))
                {
                    //     var module = hostScope.Resolve<IModuleHost>();
                    //     var moduleConfig = hostScope.Resolve<IModuleConfig>();
                    ISessionProvider<ConnectionModel> sessionManager = null;

                    try
                    {
                        var version = GetType().Assembly.GetReleaseVersion().ToString();
                        logger.LogInformation("Starting module OpcPublisher version {Version}.", version);
                        // Start module
                        //    await module.StartAsync(IdentityType.Publisher, "OpcPublisher", version, null).ConfigureAwait(false);
                        sessionManager = hostScope.Resolve<ISessionProvider<ConnectionModel>>();

                        _apiScope = ConfigureContainer(configurationRoot, mqttBroker);
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
                        //   await module.StopAsync().ConfigureAwait(false);

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
        /// <param name="methodClient"></param>
        /// <returns></returns>
        private IContainer ConfigureContainer(IConfiguration configurationRoot,
            IRpcClient methodClient)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(configurationRoot)
                .AsImplementedInterfaces();
            builder.RegisterInstance(methodClient)
                .ExternallyOwned();
            builder.AddDiagnostics(logging => logging.AddConsole());
            builder.RegisterInstance(_loggerFactory)
                .As<ILoggerFactory>()
                .ExternallyOwned();
            builder.AddNewtonsoftJsonSerializer();
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
        /// <param name="configuration"></param>
        private IContainer ConfigureContainer(IConfiguration configuration)
        {
            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(configuration)
                .AsImplementedInterfaces();

            // Register module and agent framework ...
            builder.AddNewtonsoftJsonSerializer();

            builder.AddDiagnostics(logging => logging.AddConsole());
            builder.RegisterInstance(_loggerFactory)
                .As<ILoggerFactory>()
                .ExternallyOwned();
            builder.RegisterType<PublisherCliOptions>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();

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
            builder.RegisterType<RuntimeStateReporter>()
                .AsImplementedInterfaces().SingleInstance();

            // Opc specific parts
            builder.RegisterType<StackLogger>()
                .AsImplementedInterfaces().SingleInstance().AutoActivate();
            builder.RegisterType<OpcUaClientManager>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ClientConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces();

            return builder.Build();
        }

        /// <summary>
        /// Mqtt broker that can serve as event client
        /// </summary>
        internal sealed class MqttBroker : IDisposable, IRpcClient
        {
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
            public int MaxMethodPayloadSizeInBytes => 128 * 1024;

            /// <summary>
            /// Create service client
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="server"></param>
            /// <param name="port"></param>
            /// <param name="subscription"></param>
            /// <param name="topicRoot"></param>
            /// <param name="protocol"></param>
            private MqttBroker(ILogger logger, MqttServer server, int port,
                Func<string, ReadOnlyMemory<byte>, string, Task> subscription,
                string topicRoot, string protocol)
            {
                _logger = logger;
                _server = server;
                _subscription = subscription;
                _topicRoot = topicRoot;
                _useMqtt5 = Enum.Parse<MqttProtocolVersion>(protocol, true) == MqttProtocolVersion.V500;
                Port = port;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _server.Dispose();
                _lock.Dispose();
            }

            /// <inheritdoc/>
            public async Task<string> CallMethodAsync(string deviceId, string moduleId,
                string method, string json, TimeSpan? timeout = null, CancellationToken ct = default)
            {
                var payload = Encoding.UTF8.GetBytes(json);
                var requestId = Guid.NewGuid();

                // Cancel previous call, only one allowed at a time.
                _currentCall.callback?.TrySetCanceled(default);
                var responseFilter = _useMqtt5 ? $"{_topicRoot}/responses/{method}" : $"{_topicRoot}/methods/res/#";
                _currentCall = (responseFilter, new TaskCompletionSource<(string, MqttApplicationMessage)>());

                await SendMessageAsync(
                    _useMqtt5 ? $"{_topicRoot}/methods/{method}" : $"{_topicRoot}/methods/{method}/?$rid={requestId}",
                    _useMqtt5 ? $"{_topicRoot}/responses/{method}" : null,
                    _useMqtt5 ? requestId.ToByteArray() : null,
                    payload.AsMemory(), ContentMimeType.Json, ct: ct).ConfigureAwait(false);

                var result = await _currentCall.callback.Task.ConfigureAwait(false);
                var status = 0;
                if (_useMqtt5)
                {
                    status = int.Parse(result.Item2.UserProperties
                        .Find(p => p.Name == "StatusCode")?.Value ?? "500", CultureInfo.InvariantCulture);
                    if (!requestId.ToByteArray().SequenceEqual(result.Item2.CorrelationData))
                    {
                        throw new MethodCallException("Did not get correct correlation data back.");
                    }
                }
                else
                {
                    var components = result.Item1.Replace($"{_topicRoot}/methods/res/", "", StringComparison.Ordinal).Split('/');
                    status = int.Parse(components[^2], CultureInfo.InvariantCulture);
                    if (requestId.ToString() != components[^1]["?$rid=".Length..])
                    {
                        throw new MethodCallException("Did not get correct request id back.");
                    }
                }

                var jsonResponse = Encoding.UTF8.GetString(result.Item2.Payload);
                return status != 200 ? throw new MethodCallStatusException(jsonResponse, status) : jsonResponse;
            }

            /// <summary>
            /// Send message to client
            /// </summary>
            /// <param name="topic"></param>
            /// <param name="responseTopic"></param>
            /// <param name="correlationData"></param>
            /// <param name="payload"></param>
            /// <param name="contentType"></param>
            /// <param name="ct"></param>
            /// <exception cref="ArgumentNullException"><paramref name="topic"/> is <c>null</c>.</exception>
            private Task SendMessageAsync(string topic, string responseTopic,
                byte[] correlationData, ReadOnlyMemory<byte> payload,
                string contentType, CancellationToken ct = default)
            {
                if (topic == null)
                {
                    throw new ArgumentNullException(nameof(topic));
                }
                var injected = new InjectedMqttApplicationMessage(new MqttApplicationMessage
                {
                    Topic = topic,
                    Payload = payload.ToArray(),
                    ContentType = contentType,
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    ResponseTopic = responseTopic,
                    CorrelationData = correlationData,
                    Retain = false
                })
                {
                    SenderClientId = ClientId
                };
                return _server.InjectApplicationMessage(injected, ct);
            }

            /// <summary>
            /// Handle connection
            /// </summary>
            /// <param name="args"></param>
            private Task HandleClientConnectedAsync(ClientConnectedEventArgs args)
            {
                _logger.LogInformation("Client {ClientId} connected.", args.ClientId);
                return Task.CompletedTask;
            }

            /// <summary>
            /// Handle unsubscribe
            /// </summary>
            /// <param name="args"></param>
            private Task HandleClientUnsubscribedTopicAsync(ClientUnsubscribedTopicEventArgs args)
            {
                _logger.LogInformation("Client {ClientId} unsubscribed from {Topic}.",
                    args.ClientId, args.TopicFilter);
                return Task.CompletedTask;
            }

            /// <summary>
            /// Handle subscribe
            /// </summary>
            /// <param name="args"></param>
            private Task HandleClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs args)
            {
                _logger.LogInformation("Client {ClientId} subscribed to {Topic}.",
                    args.ClientId, args.TopicFilter);
                return Task.CompletedTask;
            }

            /// <summary>
            /// Handle message receival
            /// </summary>
            /// <param name="args"></param>
            private async Task HandleMessageReceivedAsync(InterceptingPublishEventArgs args)
            {
                if (args?.ApplicationMessage == null)
                {
                    return;
                }
                var topic = args.ApplicationMessage.Topic;
                _logger.LogDebug("Client received message from {Client} on {Topic}",
                    args.ClientId, topic);
                await _lock.WaitAsync().ConfigureAwait(false);
                try
                {
                    var current = _currentCall;
                    if (current.topic != null &&
                        MqttTopicFilterComparer.Compare(topic, current.topic) == MqttTopicFilterCompareResult.IsMatch)
                    {
                        current.callback.TrySetResult((topic, args.ApplicationMessage));
                    }
                    else if (MqttTopicFilterComparer.Compare(topic, $"{_topicRoot}/twin/#") != MqttTopicFilterCompareResult.IsMatch &&
                        MqttTopicFilterComparer.Compare(topic, $"{_topicRoot}/methods/#") != MqttTopicFilterCompareResult.IsMatch)
                    {
                        await _subscription.Invoke(topic, args.ApplicationMessage.Payload,
                            args.ApplicationMessage.ContentType ?? "NoContentType_UseMqttv5").ConfigureAwait(false);
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }

            /// <summary>
            /// Handle connection
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            private Task ValidateConnectionAsync(ValidatingConnectionEventArgs args)
            {
                args.ReasonCode = UserName != null && args.UserName != UserName
                    ? MqttConnectReasonCode.BadUserNameOrPassword
                    : Password != null && args.Password != Password ? MqttConnectReasonCode.BadUserNameOrPassword : MqttConnectReasonCode.Success;
                return Task.CompletedTask;
            }

            /// <summary>
            /// Handle disconnected
            /// </summary>
            /// <param name="args"></param>
            private Task HandleClientDisconnectedAsync(ClientDisconnectedEventArgs args)
            {
                _logger.LogInformation("Disconnected client {ClientId} with type {Reason}",
                    args.ClientId, args.DisconnectType);
                return Task.CompletedTask;
            }

            /// <summary>
            /// Create broker
            /// </summary>
            /// <param name="protocol"></param>
            /// <param name="subscription"></param>
            /// <param name="topicRoot"></param>
            /// <param name="logger"></param>
            /// <returns></returns>
            public static async Task<MqttBroker> CreateAsync(string protocol,
                Func<string, ReadOnlyMemory<byte>, string, Task> subscription, string topicRoot,
                ILogger logger = null)
            {
                for (var port = 1883; ; port++)
                {
                    try
                    {
                        return await CreateAsync(protocol, subscription, port, topicRoot, logger).ConfigureAwait(false);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            /// <summary>ping
            /// Create client and start it
            /// </summary>
            /// <param name="protocol"></param>
            /// <param name="subscription"></param>
            /// <param name="port"></param>
            /// <param name="topicRoot"></param>
            /// <param name="logger"></param>
            public static async Task<MqttBroker> CreateAsync(string protocol,
                Func<string, ReadOnlyMemory<byte>, string, Task> subscription, int port, string topicRoot,
                ILogger logger)
            {
                logger ??= Log.Console<MqttBroker>();
                var optionsBuilder = new MqttServerOptionsBuilder()
                    .WithDefaultEndpoint()
                    .WithDefaultEndpointPort(port)
                    ;
                var server = new MqttFactory().CreateMqttServer(optionsBuilder.Build());
                var mqttBroker = new MqttBroker(logger, server, port, subscription, topicRoot, protocol);
                try
                {
                    server.ValidatingConnectionAsync += mqttBroker.ValidateConnectionAsync;
                    server.ClientConnectedAsync += mqttBroker.HandleClientConnectedAsync;
                    server.ClientDisconnectedAsync += mqttBroker.HandleClientDisconnectedAsync;
                    server.ClientSubscribedTopicAsync += mqttBroker.HandleClientSubscribedTopicAsync;
                    server.ClientUnsubscribedTopicAsync += mqttBroker.HandleClientUnsubscribedTopicAsync;
                    server.InterceptingPublishAsync += mqttBroker.HandleMessageReceivedAsync;
                    await server.StartAsync().ConfigureAwait(false);
                    return mqttBroker;
                }
                catch
                {
                    server.Dispose();
                    throw;
                }
            }

            public ValueTask<string> CallMethodAsync(string target, string method, string payload, TimeSpan? timeout = null, CancellationToken ct = default)
            {
                throw new NotImplementedException();
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
        private readonly ILoggerFactory _loggerFactory;
        private readonly Channel<(string topic, ReadOnlyMemory<byte> buffer, string contentType)> _channel;
        private readonly HashSet<string> _messageIds = new();
        private IContainer _apiScope;
        private Task _publisher;
    }
}
