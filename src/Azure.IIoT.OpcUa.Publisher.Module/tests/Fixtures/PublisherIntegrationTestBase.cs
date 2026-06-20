// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Encoders;
    using Autofac;
    using Furly.Exceptions;
    using Furly.Extensions.Mqtt;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Logging;
    using Neovolve.Logging.Xunit;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Json message
    /// </summary>
    /// <param name="Topic"></param>
    /// <param name="Message"></param>
    /// <param name="ContentType"></param>
    public readonly record struct JsonMessage(string Topic, JsonElement Message, string ContentType);

    /// <summary>
    /// Base class for integration testing, it connects to the server, runs
    /// publisher and injects mocked IoTHub services.
    /// </summary>
    public class PublisherIntegrationTestBase : IDisposable
    {
        protected string EndpointUrl { get; set; }
        protected CancellationToken Ct => _attemptToken ?? _cts.Token;

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="testOutputHelper"></param>
        /// <param name="timeout"></param>
        /// <param name="testName"></param>
        public PublisherIntegrationTestBase(ITestOutputHelper testOutputHelper,
            TimeSpan? timeout = null, string testName = null)
        {
            _cts = new CancellationTokenSource(timeout ?? kTotalTestTimeout);
            _testOutputHelper = testOutputHelper;
            _logFactory = LogFactory.Create(testOutputHelper, Logging.Config);
            _logger = _logFactory.CreateLogger(testName ?? "PublisherIntegrationTest");
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposedValue)
            {
                _disposedValue = true;

                if (_cts.IsCancellationRequested)
                {
                    _logger.TestTimeout();
                }

                if (_publisher != null)
                {
                    StopPublisherAsync().WaitAsync(TimeSpan.FromMinutes(1)).GetAwaiter().GetResult();
                }

                _cts.Dispose();
                _logFactory.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get one message from running publisher
        /// </summary>
        /// <param name="test"></param>
        /// <param name="publishedNodesFile"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected Task<List<JsonMessage>> ProcessMessagesAsync(string test, string publishedNodesFile,
            Func<JsonElement, JsonElement> predicate = null, string messageType = null,
            string[] arguments = default)
        {
            // Collect messages from server with default settings
            return ProcessMessagesAsync(test, publishedNodesFile, kTelemetryTimeout, 1,
                predicate, messageType, arguments);
        }

        /// <summary>
        /// Get one message from a running publisher
        /// </summary>
        /// <param name="test"></param>
        /// <param name="publishedNodesFile"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        /// <param name="arguments"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        protected Task<(JsonMessage? Metadata, List<JsonMessage> Messages)> ProcessMessagesAndMetadataAsync(
            string test, string publishedNodesFile, Func<JsonElement, JsonElement> predicate = null,
            string messageType = null, string[] arguments = default, MqttVersion? version = null)
        {
            // Collect messages from server with default settings
            return ProcessMessagesAndMetadataAsync(test, publishedNodesFile, kTelemetryTimeout, 1,
                predicate, messageType, arguments, version);
        }

        /// <summary>
        /// Process messages and return
        /// </summary>
        /// <param name="test"></param>
        /// <param name="publishedNodesFile"></param>
        /// <param name="messageCollectionTimeout"></param>
        /// <param name="messageCount"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected async Task<List<JsonMessage>> ProcessMessagesAsync(string test, string publishedNodesFile,
            TimeSpan messageCollectionTimeout, int messageCount, Func<JsonElement, JsonElement> predicate = null,
            string messageType = null, string[] arguments = default)
        {
            var (_, messages) = await ProcessMessagesAndMetadataAsync(test, publishedNodesFile,
                messageCollectionTimeout, messageCount, predicate, messageType, arguments).ConfigureAwait(false);
            return messages;
        }

        /// <summary>
        /// Start publisher and wait for messages and return them
        /// </summary>
        /// <param name="test"></param>
        /// <param name="publishedNodesFile"></param>
        /// <param name="messageCollectionTimeout"></param>
        /// <param name="messageCount"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        /// <param name="arguments"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        protected async Task<(JsonMessage? Metadata, List<JsonMessage> Messages)> ProcessMessagesAndMetadataAsync(
            string test, string publishedNodesFile, TimeSpan messageCollectionTimeout, int messageCount,
            Func<JsonElement, JsonElement> predicate = null, string messageType = null, string[] arguments = default,
            MqttVersion? version = null)
        {
            StartPublisher(test, publishedNodesFile, arguments, version);
            try
            {
                return await WaitForMessagesAndMetadataAsync(messageCollectionTimeout,
                    messageCount, predicate, messageType).ConfigureAwait(false);
            }
            finally
            {
                await StopPublisherAsync();
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
            return WaitForMessagesAsync(kTelemetryTimeout, 1, predicate, messageType);
        }

        /// <summary>
        /// Wait for one message
        /// </summary>
        /// <param name="messageCollectionTimeout"></param>
        /// <param name="messageCount"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        protected async Task<List<JsonMessage>> WaitForMessagesAsync(TimeSpan messageCollectionTimeout,
            int messageCount, Func<JsonElement, JsonElement> predicate = null, string messageType = null)
        {
            // Collect messages from server with default settings
            var (_, messages) = await WaitForMessagesAndMetadataAsync(messageCollectionTimeout,
                messageCount, predicate, messageType).ConfigureAwait(false);
            return messages;
        }

        /// <summary>
        /// Wait for messages
        /// </summary>
        /// <param name="messageCollectionTimeout"></param>
        /// <param name="messageCount"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        protected async Task<(JsonMessage? Metadata, List<JsonMessage> Messages)> WaitForMessagesAndMetadataAsync(
            TimeSpan messageCollectionTimeout, int messageCount, Func<JsonElement, JsonElement> predicate = null,
            string messageType = null)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var messages = new List<JsonMessage>();

            JsonMessage? metadata = null;
            using var cts = new CancellationTokenSource(messageCollectionTimeout);
            try
            {
                await foreach (var evt in _publisher.ReadTelemetryAsync(cts.Token))
                {
                    if (evt.Properties.TryGetValue(Constants.MessagePropertySchemaKey, out var schematype) &&
                        schematype != MessageSchemaTypes.NetworkMessageJson &&
                        schematype != MessageSchemaTypes.MonitoredItemMessageJson &&
                        schematype != MessageSchemaTypes.NetworkMessageUadp)
                    {
                        continue;
                    }
                    if (evt.Data.IsEmpty)
                    {
                        // Skip empty messages
                        continue;
                    }
                    var json = Encoding.UTF8.GetString(evt.Data.ToArray());
                    var document = JsonDocument.Parse(json);
                    json = JsonSerializer.Serialize(document, kIndented);
                    var element = document.RootElement;
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            Add(messages, item, ref metadata, predicate, messageType, _messageIds,
                                evt.Topic, evt.ContentType);
                        }
                    }
                    else if (element.ValueKind == JsonValueKind.Object)
                    {
                        Add(messages, element, ref metadata, predicate, messageType, _messageIds,
                            evt.Topic, evt.ContentType);
                    }
                    if (messages.Count >= messageCount)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            _logger.MessagesReceived(messages.Count, stopWatch.Elapsed);
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

        private static readonly JsonSerializerOptions kIndented = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Start publisher
        /// </summary>
        /// <param name="test"></param>
        /// <param name="publishedNodesFile"></param>
        /// <param name="arguments"></param>
        /// <param name="version"></param>
        /// <param name="reverseConnectPort"></param>
        /// <param name="keepAliveInterval"></param>
        /// <param name="securityMode"></param>
        protected void StartPublisher(string test, string publishedNodesFile = null,
            string[] arguments = default, MqttVersion? version = null, int? reverseConnectPort = null,
            int keepAliveInterval = 120, SecurityMode? securityMode = null)
        {
            var sw = Stopwatch.StartNew();
            _logger = _logFactory.CreateLogger(test);

            arguments ??= [];
            _publishedNodesFilePath = Path.GetTempFileName();
            WritePublishedNodes(test, publishedNodesFile, reverseConnectPort != null, securityMode);

            arguments =
            [
                .. arguments,
                .. new[]
                {
                    $"--pf={_publishedNodesFilePath}"
                },
            ];

            // Use the polling file-change watcher on all platforms in tests.
            // The native FileSystemWatcher's change-notification callback can
            // hit an access violation under the heavy create/modify/delete and
            // publisher-dispose churn of the integration suite, crashing the
            // test host (root-caused from a Windows WER dump; Linux already
            // forced polling). Production is unaffected - it watches a single
            // long-lived file - so this stays a test-only setting.
            arguments = [.. arguments, "--pol"];

            if (reverseConnectPort != null)
            {
                arguments =
                [
                    .. arguments,
                    .. new[]
                    {
                        $"--rcp={reverseConnectPort.Value}"
                    },
                ];
            }

            _publisher = new PublisherModule(null, null, null, null,
                _testOutputHelper, arguments, version, keepAliveInterval);
            _logger.PublisherStarted(sw.Elapsed);
        }

        /// <summary>
        /// Update published nodes file
        /// </summary>
        /// <param name="test"></param>
        /// <param name="publishedNodesFile"></param>
        /// <param name="useReverseConnect"></param>
        /// <param name="securityMode"></param>
        protected void WritePublishedNodes(string test, string publishedNodesFile, bool useReverseConnect = false,
            SecurityMode? securityMode = null)
        {
            if (!string.IsNullOrEmpty(publishedNodesFile))
            {
                var pnJson = File.ReadAllText(publishedNodesFile)
                    .Replace("\"{{UseReverseConnect}}\"", useReverseConnect ? "true" : "false", StringComparison.Ordinal)
                    .Replace("{{EndpointUrl}}", EndpointUrl, StringComparison.Ordinal)
                    .Replace("{{SecurityMode}}", (securityMode ?? SecurityMode.None).ToString(), StringComparison.Ordinal)
                    .Replace("{{DataSetWriterGroup}}", test, StringComparison.Ordinal);
                File.WriteAllText(_publishedNodesFilePath, pnJson);
            }
        }

        /// <summary>
        /// Get publisher api
        /// </summary>
        protected IPublisherApi PublisherApi => _publisher?.ClientContainer?.Resolve<IPublisherApi>();

        /// <summary>
        /// Stop publisher
        /// </summary>
        protected async Task StopPublisherAsync()
        {
            if (_publisher != null)
            {
                var sw = Stopwatch.StartNew();

                await _publisher.DisposeAsync();
                _publisher = null;

                if (File.Exists(_publishedNodesFilePath))
                {
                    File.Delete(_publishedNodesFilePath);
                }
                _logger.PublisherStopped(sw.Elapsed);
            }
        }

        /// <summary>
        /// Run an integration test body, retrying the whole test if the
        /// publisher's MQTT session is lost to a known upstream MQTTnet/Furly
        /// reconnect race. Tearing down a writer group (UnpublishNodes) makes
        /// the publisher's MQTT client briefly disconnect and reconnect; an
        /// in-flight QoS1 "$call" response PUBLISH then races the reconnect and
        /// MQTTnet throws "Received packet 'PubAck' at an unexpected time" while
        /// still connecting, losing the response and timing out the RPC. The
        /// session can then flap, so only a full publisher restart reliably
        /// recovers. Each attempt runs under its own cancellation budget
        /// (exposed through <see cref="Ct"/>) and a short method-call timeout so
        /// a lost call fails this attempt quickly and leaves room to retry on a
        /// fresh publisher. The test bodies are idempotent - each starts a fresh
        /// publisher (new temp published-nodes file) and disposes it in its own
        /// finally - so re-running the whole body recovers without masking
        /// genuine assertion failures (those are neither a
        /// <see cref="MethodCallException"/> nor a per-attempt cancellation and
        /// propagate immediately). This is a test-side mitigation for an
        /// upstream Furly/MQTTnet reconnect bug, not a product defect.
        /// </summary>
        /// <param name="testBody"></param>
        /// <param name="maxAttempts"></param>
        protected async Task ExecuteWithMqttRetryAsync(Func<Task> testBody, int maxAttempts = 6)
        {
            for (var attempt = 1; ; attempt++)
            {
                // Per-attempt budget. Sized above the telemetry collection
                // timeout so a legitimately slow (but healthy) test is never
                // cancelled while waiting for messages.
                using var attemptCts = new CancellationTokenSource(kMqttRetryAttemptTimeout);
                _attemptToken = attemptCts.Token;
                try
                {
                    await testBody().ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts &&
                    IsLostMqttSessionFailure(ex, attemptCts))
                {
                    _logger.LogWarning(ex,
                        "MQTT RPC call failed on attempt {Attempt}/{MaxAttempts} " +
                        "(known MQTTnet QoS1 reconnect protocol violation that " +
                        "loses the publisher's session). Restarting publisher " +
                        "and retrying the test.", attempt, maxAttempts);

                    // Safety net in case the body did not dispose its publisher.
                    await StopPublisherAsync().ConfigureAwait(false);
                }
                finally
                {
                    _attemptToken = null;
                }
            }
        }

        /// <summary>
        /// Whether the exception indicates the MQTT session/transport was lost,
        /// rather than a genuine test assertion failure. A lost session surfaces
        /// either as an explicit <see cref="MethodCallException"/> or, when the
        /// "$call" never completes, as an <see cref="OperationCanceledException"/>
        /// once the per-attempt budget elapses.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="attemptCts"></param>
        private static bool IsLostMqttSessionFailure(Exception ex,
            CancellationTokenSource attemptCts)
        {
            return ex is MethodCallException ||
                (ex is OperationCanceledException && attemptCts.IsCancellationRequested);
        }

        /// <summary>
        /// Get endpoints from file
        /// </summary>
        /// <param name="test"></param>
        /// <param name="publishedNodesFile"></param>
        /// <param name="useReverseConnect"></param>
        /// <param name="securityMode"></param>
        /// <returns></returns>
        protected PublishedNodesEntryModel[] GetEndpointsFromFile(string test, string publishedNodesFile,
            bool useReverseConnect = false, SecurityMode? securityMode = null)
        {
            IJsonSerializer serializer = new NewtonsoftJsonSerializer();
            var fileContent = File.ReadAllText(publishedNodesFile)
                .Replace("\"{{UseReverseConnect}}\"", useReverseConnect ? "true" : "false", StringComparison.Ordinal)
                .Replace("{{EndpointUrl}}", EndpointUrl, StringComparison.Ordinal)
                .Replace("{{SecurityMode}}", (securityMode ?? SecurityMode.None).ToString(), StringComparison.Ordinal)
                .Replace("{{DataSetWriterGroup}}", test, StringComparison.Ordinal);
            return serializer.Deserialize<PublishedNodesEntryModel[]>(fileContent);
        }

        private static readonly TimeSpan kTelemetryTimeout = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan kTotalTestTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan kMqttRetryAttemptTimeout = TimeSpan.FromSeconds(150);
        private readonly CancellationTokenSource _cts;
        private CancellationToken? _attemptToken;
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly HashSet<string> _messageIds = [];
        private readonly ILoggerFactory _logFactory;
        private ILogger _logger;
        private PublisherModule _publisher;
        private string _publishedNodesFilePath;
        private bool _disposedValue;
    }

    /// <summary>
    /// Source-generated logging definitions for PublisherIntegrationTestBase
    /// </summary>
    internal static partial class PublisherIntegrationTestLogging
    {
        private const int EventClass = 0;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Error,
            Message = "OperationCanceledException thrown due to test time out.")]
        public static partial void TestTimeout(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Information,
            Message = "Received {MessageCount} messages in {Elapsed}.")]
        public static partial void MessagesReceived(this ILogger logger, int messageCount, TimeSpan elapsed);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Information,
            Message = "Publisher started in {Elapsed}.")]
        public static partial void PublisherStarted(this ILogger logger, TimeSpan elapsed);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Information,
            Message = "Publisher stopped in {Elapsed}.")]
        public static partial void PublisherStopped(this ILogger logger, TimeSpan elapsed);
    }
}
