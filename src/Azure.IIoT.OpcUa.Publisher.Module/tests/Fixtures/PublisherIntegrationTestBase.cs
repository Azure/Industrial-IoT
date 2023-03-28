// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Testing.Fixtures;
    using Azure.IIoT.OpcUa.Encoders;
    using Autofac;
    using Furly.Extensions.Mqtt;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using System;
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
        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="serverFixture"></param>
        /// <param name="testOutputHelper"></param>
        public PublisherIntegrationTestBase(ReferenceServer serverFixture,
            ITestOutputHelper testOutputHelper)
        {
            _serverFixture = serverFixture;
            _testOutputHelper = testOutputHelper;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            StopPublisher();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get one message from running publisher
        /// </summary>
        /// <param name="publishedNodesFile"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected Task<List<JsonMessage>> ProcessMessagesAsync(string publishedNodesFile,
            Func<JsonElement, JsonElement> predicate = null, string messageType = null,
            string[] arguments = default)
        {
            // Collect messages from server with default settings
            return ProcessMessagesAsync(publishedNodesFile, TimeSpan.FromMinutes(2), 1,
                predicate, messageType, arguments);
        }

        /// <summary>
        /// Get one message from a running publisher
        /// </summary>
        /// <param name="publishedNodesFile"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        /// <param name="arguments"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        protected Task<(JsonMessage? Metadata, List<JsonMessage> Messages)> ProcessMessagesAndMetadataAsync(
            string publishedNodesFile, Func<JsonElement, JsonElement> predicate = null,
            string messageType = null, string[] arguments = default, MqttVersion? version = null)
        {
            // Collect messages from server with default settings
            return ProcessMessagesAndMetadataAsync(publishedNodesFile, TimeSpan.FromMinutes(2), 1,
                predicate, messageType, arguments, version);
        }

        /// <summary>
        /// Process messages and return
        /// </summary>
        /// <param name="publishedNodesFile"></param>
        /// <param name="messageCollectionTimeout"></param>
        /// <param name="messageCount"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected async Task<List<JsonMessage>> ProcessMessagesAsync(string publishedNodesFile,
            TimeSpan messageCollectionTimeout, int messageCount, Func<JsonElement, JsonElement> predicate = null,
            string messageType = null, string[] arguments = default)
        {
            var (_, messages) = await ProcessMessagesAndMetadataAsync(publishedNodesFile,
                messageCollectionTimeout, messageCount, predicate, messageType, arguments).ConfigureAwait(false);
            return messages;
        }

        /// <summary>
        /// Start publisher and wait for messages and return them
        /// </summary>
        /// <param name="publishedNodesFile"></param>
        /// <param name="messageCollectionTimeout"></param>
        /// <param name="messageCount"></param>
        /// <param name="predicate"></param>
        /// <param name="messageType"></param>
        /// <param name="arguments"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        protected async Task<(JsonMessage? Metadata, List<JsonMessage> Messages)> ProcessMessagesAndMetadataAsync(
            string publishedNodesFile, TimeSpan messageCollectionTimeout, int messageCount,
            Func<JsonElement, JsonElement> predicate = null, string messageType = null, string[] arguments = default,
            MqttVersion? version = null)
        {
            StartPublisher(publishedNodesFile, arguments, version);
            try
            {
                return await WaitForMessagesAndMetadataAsync(messageCollectionTimeout,
                    messageCount, predicate, messageType).ConfigureAwait(false);
            }
            finally
            {
                StopPublisher();
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
            return WaitForMessagesAsync(TimeSpan.FromMinutes(2), 1, predicate, messageType);
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
                    var json = Encoding.UTF8.GetString(evt.Data.ToArray());
                    var document = JsonDocument.Parse(json);
                    json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
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
        /// <param name="publishedNodesFile"></param>
        /// <param name="arguments"></param>
        /// <param name="version"></param>
        protected void StartPublisher(string publishedNodesFile = null, string[] arguments = default,
            MqttVersion? version = null)
        {
            arguments ??= Array.Empty<string>();
            _publishedNodesFilePath = Path.GetTempFileName();
            if (!string.IsNullOrEmpty(publishedNodesFile))
            {
                File.WriteAllText(_publishedNodesFilePath,
                    File.ReadAllText(publishedNodesFile).Replace("{{Port}}",
                    _serverFixture.Port.ToString(CultureInfo.InvariantCulture),
                    StringComparison.Ordinal));
            }

            arguments = arguments.Concat(
                new[]
                {
                    $"--pf={_publishedNodesFilePath}"
                }).ToArray();

            _publisher = new PublisherModule(null, null, null, null,
                _testOutputHelper, arguments, version);
        }

        /// <summary>
        /// Get publisher api
        /// </summary>
        protected IPublisherApi PublisherApi => _publisher?.ClientContainer?.Resolve<IPublisherApi>();

        /// <summary>
        /// Stop publisher
        /// </summary>
        protected void StopPublisher()
        {
            if (_publisher != null)
            {
                _publisher.Dispose();
                _publisher = null;

                if (File.Exists(_publishedNodesFilePath))
                {
                    File.Delete(_publishedNodesFilePath);
                }
            }
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

        private readonly ReferenceServer _serverFixture;
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly HashSet<string> _messageIds = new();
        private PublisherModule _publisher;
        private string _publishedNodesFilePath;
    }
}
