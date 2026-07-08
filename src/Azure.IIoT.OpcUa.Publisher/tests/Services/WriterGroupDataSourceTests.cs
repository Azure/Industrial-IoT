// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using Neovolve.Logging.Xunit;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;
    using IMessageSink = Azure.IIoT.OpcUa.Publisher.IMessageSink;

    /// <summary>
    /// Tests for the writer name management in <see cref="WriterGroupDataSource"/>.
    /// </summary>
    public sealed class WriterGroupDataSourceTests
    {
        public WriterGroupDataSourceTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output, Logging.Config);
            _options = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            // Disable metadata so the message pipeline does not attempt to load
            // metadata from the (mocked) subscription.
            _options.Value.DisableDataSetMetaData = true;

            _converter = new PublishedNodesConverter(
                _loggerFactory.CreateLogger<PublishedNodesConverter>(), _serializer, _options);
        }

        [Fact]
        public async Task ReconfiguringWriterDoesNotAppendSuffixToWriterNameAsync()
        {
            // Arrange - two configurations for the same writer ("expected") that
            // differ only in the telemetry queue name. Changing the queue name
            // changes the writer topic which in turn forces the writer to be
            // removed and re-added during the update (its equality/hash differ).
            var group1 = ToSingleWriterGroup("expected", "telemetry/a");
            var group2 = ToSingleWriterGroup("expected", "telemetry/b");

            var subscribers = new List<ISubscriber>();
            var captured = new List<OpcUaSubscriptionNotification>();

            var subscriptionMock = new Mock<ISubscription>();
            var clientsMock = new Mock<IOpcUaClientManager<ConnectionModel>>();
            clientsMock
                .Setup(c => c.CreateSubscriptionAsync(It.IsAny<ConnectionModel>(),
                    It.IsAny<SubscriptionModel>(), It.IsAny<ISubscriber>(),
                    It.IsAny<CancellationToken>()))
                .Returns((ConnectionModel _, SubscriptionModel _, ISubscriber cb,
                    CancellationToken _) =>
                {
                    subscribers.Add(cb);
                    return new ValueTask<ISubscription>(subscriptionMock.Object);
                });

            var sinkMock = new Mock<IMessageSink>();
            sinkMock
                .Setup(s => s.OnMessage(It.IsAny<OpcUaSubscriptionNotification>()))
                .Callback<OpcUaSubscriptionNotification>(n => captured.Add(n));

            await using var sut = new WriterGroupDataSource(clientsMock.Object, group1,
                sinkMock.Object, _serializer, _options, null, _loggerFactory);

            // Act / Assert - initial configuration uses the configured name.
            await sut.StartAsync(default);
            Assert.Equal("expected", EmitAndGetWriterName(subscribers, captured));

            // Reconfigure - this triggers a remove + re-add of the writer. Even
            // though there are no duplicate writers, a stale name would cause the
            // unique name generator to append a suffix ("expected1").
            await sut.UpdateAsync(group2, default);
            Assert.Equal("expected", EmitAndGetWriterName(subscribers, captured));
        }

        [Fact]
        public async Task GetStateReturnsEndpointInfoAndFailedNodeErrorsAsync()
        {
            // Arrange - a single writer with security disabled and user name
            // authentication so we can verify the endpoint identification info
            // that is returned alongside the failed node errors.
            var group = ToWriterGroupWithUserAuth("Asset1", "opc.tcp://opcplc:50000",
                "Usr", "ns=2;s=0");

            var subscribers = new List<ISubscriber>();
            var subscriptionMock = new Mock<ISubscription>();
            var clientsMock = new Mock<IOpcUaClientManager<ConnectionModel>>();
            clientsMock
                .Setup(c => c.CreateSubscriptionAsync(It.IsAny<ConnectionModel>(),
                    It.IsAny<SubscriptionModel>(), It.IsAny<ISubscriber>(),
                    It.IsAny<CancellationToken>()))
                .Returns((ConnectionModel _, SubscriptionModel _, ISubscriber cb,
                    CancellationToken _) =>
                {
                    subscribers.Add(cb);
                    return new ValueTask<ISubscription>(subscriptionMock.Object);
                });

            var sinkMock = new Mock<IMessageSink>();

            await using var sut = new WriterGroupDataSource(clientsMock.Object, group,
                sinkMock.Object, _serializer, _options, null, _loggerFactory);
            await sut.StartAsync(default);

            // Act - report an error for the configured node as the server would
            // when the node cannot be added as a monitored item.
            var subscriber = Assert.Single(subscribers);
            subscriber.OnMonitoredItemUpdate(
                new DataMonitoredItemModel { StartNodeId = "ns=2;s=0" },
                new ServiceResultModel
                {
                    StatusCode = 2150891520,
                    SymbolicId = "BadNodeIdUnknown"
                });

            var state = await sut.GetStateAsync(default);

            // Assert - endpoint identification info and failed node errors are
            // both surfaced and no password leaks into the diagnostics.
            var writer = Assert.Single(state.DataSetWriters);
            Assert.Equal("opc.tcp://opcplc:50000", writer.EndpointUrl);
            Assert.False(writer.UseSecurity);
            Assert.Equal(OpcAuthenticationMode.UsernamePassword,
                writer.OpcAuthenticationMode);
            Assert.Equal("Usr", writer.OpcAuthenticationUsername);

            var error = Assert.Single(writer.Source!.Errors!);
            Assert.Equal("ns=2;s=0", error.NodeId);
            Assert.Equal(2150891520u, error.ErrorInfo.StatusCode);
            Assert.Equal("BadNodeIdUnknown", error.ErrorInfo.SymbolicId);
        }

        /// <summary>
        /// Emit a data change on the most recently created subscriber and return
        /// the writer name carried by the produced message context.
        /// </summary>
        private static string EmitAndGetWriterName(IReadOnlyList<ISubscriber> subscribers,
            List<OpcUaSubscriptionNotification> captured)
        {
            captured.Clear();
            using var notification = new OpcUaSubscriptionNotification(DateTimeOffset.UtcNow);
            subscribers[^1].OnSubscriptionDataChangeReceived(notification);
            var context = Assert.IsType<DataSetWriterContext>(Assert.Single(captured).Context);
            return context.WriterName;
        }

        [Fact]
        public async Task MetricsAreReportedPerEndpointUrlAsync()
        {
            // Arrange - a writer group subscribing to two different endpoints.
            var group = ToTwoEndpointWriterGroup(
                "opc.tcp://server-a:50000", "opc.tcp://server-b:50000");

            var subscribers = new List<ISubscriber>();

            // Each endpoint gets its own client diagnostics with distinct state.
            var clientA = new Mock<IOpcUaClientDiagnostics>();
            clientA.SetupGet(c => c.State).Returns(EndpointConnectivityState.Ready);
            clientA.SetupGet(c => c.ReconnectCount).Returns(3);
            var clientB = new Mock<IOpcUaClientDiagnostics>();
            clientB.SetupGet(c => c.State).Returns(EndpointConnectivityState.Disconnected);
            clientB.SetupGet(c => c.ReconnectCount).Returns(7);

            var subscriptionA = new Mock<ISubscription>();
            subscriptionA.SetupGet(s => s.ClientDiagnostics).Returns(clientA.Object);
            subscriptionA.SetupGet(s => s.Diagnostics)
                .Returns(new Mock<ISubscriptionDiagnostics>().Object);
            var subscriptionB = new Mock<ISubscription>();
            subscriptionB.SetupGet(s => s.ClientDiagnostics).Returns(clientB.Object);
            subscriptionB.SetupGet(s => s.Diagnostics)
                .Returns(new Mock<ISubscriptionDiagnostics>().Object);

            var clientsMock = new Mock<IOpcUaClientManager<ConnectionModel>>();
            clientsMock
                .Setup(c => c.CreateSubscriptionAsync(It.IsAny<ConnectionModel>(),
                    It.IsAny<SubscriptionModel>(), It.IsAny<ISubscriber>(),
                    It.IsAny<CancellationToken>()))
                .Returns((ConnectionModel connection, SubscriptionModel _, ISubscriber cb,
                    CancellationToken _) =>
                {
                    subscribers.Add(cb);
                    var subscription = connection.Endpoint!.Url!.Contains("server-a",
                        StringComparison.Ordinal) ? subscriptionA : subscriptionB;
                    return new ValueTask<ISubscription>(subscription.Object);
                });

            var sinkMock = new Mock<IMessageSink>();

            await using var sut = new WriterGroupDataSource(clientsMock.Object, group,
                sinkMock.Object, _serializer, _options, null, _loggerFactory);
            await sut.StartAsync(default);

            // Report a handful of value changes for the server-a subscription only.
            var serverASubscriber = subscribers[0];
            serverASubscriber.OnSubscriptionDataDiagnosticsChange(true, 5, 0, 0);
            serverASubscriber.OnSubscriptionDataDiagnosticsChange(true, 5, 0, 0);

            // Act - collect all observable measurements from the writer group meter.
            var measurements = CollectMeasurements();

            // Assert - connection retries reported per endpoint url.
            var retries = measurements["iiot_edge_publisher_endpoint_connection_retries"];
            Assert.Equal(3, GetForEndpoint(retries, "opc.tcp://server-a:50000"));
            Assert.Equal(7, GetForEndpoint(retries, "opc.tcp://server-b:50000"));

            // Connectivity reported per endpoint url (server-a ready, server-b not).
            var connected = measurements["iiot_edge_publisher_endpoint_is_connection_ok"];
            Assert.Equal(1, GetForEndpoint(connected, "opc.tcp://server-a:50000"));
            Assert.Equal(0, GetForEndpoint(connected, "opc.tcp://server-b:50000"));

            // Value changes/second reported per endpoint url (only server-a saw data).
            var valueChanges = measurements["iiot_edge_publisher_endpoint_value_changes_per_second"];
            Assert.True(GetForEndpoint(valueChanges, "opc.tcp://server-a:50000") > 0.0);
            Assert.Equal(0.0, GetForEndpoint(valueChanges, "opc.tcp://server-b:50000"));

            // The group level metrics remain unchanged (single, endpoint-agnostic
            // measurement aggregating across the whole writer group).
            Assert.Equal(10, GetForEndpoint(
                measurements["iiot_edge_publisher_connection_retries"], string.Empty));
            Assert.Equal(1, GetForEndpoint(
                measurements["iiot_edge_publisher_is_connection_ok"], string.Empty));
        }

        [Fact]
        public async Task SendKeyFrameForGroupEmitsKeyFrameForEachWriterAsync()
        {
            // Arrange - a writer group with two data set writers. The mocked
            // subscription serves a cached key frame snapshot on demand.
            var group = ToTwoEndpointWriterGroup(
                "opc.tcp://server-a:50000", "opc.tcp://server-b:50000");

            var captured = new List<OpcUaSubscriptionNotification>();
            var (clientsMock, sinkMock) = SetupKeyFrameSubscription(captured);

            await using var sut = new WriterGroupDataSource(clientsMock.Object, group,
                sinkMock.Object, _serializer, _options, null, _loggerFactory);
            await sut.StartAsync(default);

            // Act - request a key frame for the whole group.
            await sut.SendKeyFrameAsync(null, default);

            // Assert - one key frame message was produced per data set writer.
            Assert.Equal(2, captured.Count);
            Assert.All(captured, n => Assert.Equal(MessageType.KeyFrame, n.MessageType));
        }

        [Fact]
        public async Task SendKeyFrameForSpecificWriterEmitsSingleKeyFrameAsync()
        {
            // Arrange - a writer group with two data set writers.
            var group = ToTwoEndpointWriterGroup(
                "opc.tcp://server-a:50000", "opc.tcp://server-b:50000");

            var captured = new List<OpcUaSubscriptionNotification>();
            var (clientsMock, sinkMock) = SetupKeyFrameSubscription(captured);

            await using var sut = new WriterGroupDataSource(clientsMock.Object, group,
                sinkMock.Object, _serializer, _options, null, _loggerFactory);
            await sut.StartAsync(default);

            // Discover the identifier of one of the writers via the state.
            var state = await sut.GetStateAsync(default);
            var writerId = state.DataSetWriters[0].Id;

            // Act - request a key frame for a single writer only.
            await sut.SendKeyFrameAsync(writerId, default);

            // Assert - exactly one key frame message was produced.
            var notification = Assert.Single(captured);
            Assert.Equal(MessageType.KeyFrame, notification.MessageType);
        }

        [Fact]
        public async Task SendKeyFrameForUnknownWriterThrowsAsync()
        {
            // Arrange - a writer group with two data set writers.
            var group = ToTwoEndpointWriterGroup(
                "opc.tcp://server-a:50000", "opc.tcp://server-b:50000");

            var captured = new List<OpcUaSubscriptionNotification>();
            var (clientsMock, sinkMock) = SetupKeyFrameSubscription(captured);

            await using var sut = new WriterGroupDataSource(clientsMock.Object, group,
                sinkMock.Object, _serializer, _options, null, _loggerFactory);
            await sut.StartAsync(default);

            // Act / Assert - an unknown writer id is reported as not found and no
            // message is emitted.
            await Assert.ThrowsAsync<ResourceNotFoundException>(
                () => sut.SendKeyFrameAsync("does-not-exist", default).AsTask());
            Assert.Empty(captured);
        }

        /// <summary>
        /// Create a client manager whose subscriptions serve a cached key frame
        /// snapshot when a keep alive is requested, and a sink that records every
        /// produced message.
        /// </summary>
        private (Mock<IOpcUaClientManager<ConnectionModel>>, Mock<IMessageSink>)
            SetupKeyFrameSubscription(List<OpcUaSubscriptionNotification> captured)
        {
            var subscriptionMock = new Mock<ISubscription>();
            subscriptionMock
                .Setup(s => s.CreateKeepAlive())
                .Returns(() => new OpcUaSubscriptionNotification(DateTimeOffset.UtcNow)
                {
                    // A real subscription upgrades a keep alive into a key frame
                    // from its value cache. Emulate an already upgraded snapshot.
                    MessageType = MessageType.KeyFrame
                });

            var clientsMock = new Mock<IOpcUaClientManager<ConnectionModel>>();
            clientsMock
                .Setup(c => c.CreateSubscriptionAsync(It.IsAny<ConnectionModel>(),
                    It.IsAny<SubscriptionModel>(), It.IsAny<ISubscriber>(),
                    It.IsAny<CancellationToken>()))
                .Returns((ConnectionModel _, SubscriptionModel _, ISubscriber _,
                    CancellationToken _) =>
                    new ValueTask<ISubscription>(subscriptionMock.Object));

            var sinkMock = new Mock<IMessageSink>();
            sinkMock
                .Setup(s => s.OnMessage(It.IsAny<OpcUaSubscriptionNotification>()))
                .Callback<OpcUaSubscriptionNotification>(n => captured.Add(n));
            return (clientsMock, sinkMock);
        }

        /// <summary>
        /// Collect the current value of every observable instrument on the writer
        /// group meter, keyed by instrument name. Each entry maps the endpointUrl
        /// tag value (empty when absent) to the reported measurement value.
        /// </summary>
        private static Dictionary<string, List<(string EndpointUrl, double Value)>> CollectMeasurements()
        {
            var result = new Dictionary<string, List<(string, double)>>();
            using var listener = new System.Diagnostics.Metrics.MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == Diagnostics.Namespace)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            void Record<T>(System.Diagnostics.Metrics.Instrument instrument, T measurement,
                ReadOnlySpan<KeyValuePair<string, object>> tags) where T : struct
            {
                var endpointUrl = string.Empty;
                foreach (var tag in tags)
                {
                    if (tag.Key == "endpointUrl" && tag.Value is string url)
                    {
                        endpointUrl = url;
                    }
                }
                if (!result.TryGetValue(instrument.Name, out var list))
                {
                    result[instrument.Name] = list = [];
                }
                list.Add((endpointUrl, Convert.ToDouble(measurement,
                    System.Globalization.CultureInfo.InvariantCulture)));
            }
            listener.SetMeasurementEventCallback<long>((i, m, t, _) => Record(i, m, t!));
            listener.SetMeasurementEventCallback<int>((i, m, t, _) => Record(i, m, t!));
            listener.SetMeasurementEventCallback<double>((i, m, t, _) => Record(i, m, t!));
            listener.Start();
            listener.RecordObservableInstruments();
            return result;
        }

        private static double GetForEndpoint(
            List<(string EndpointUrl, double Value)> measurements, string endpointUrl)
        {
            return Assert.Single(measurements, m => m.EndpointUrl == endpointUrl).Value;
        }

        private WriterGroupModel ToTwoEndpointWriterGroup(string endpointA, string endpointB)
        {
            var pn = $$"""
[
    {
        "EndpointUrl": "{{endpointA}}",
        "DataSetWriterId": "writerA",
        "DataSetWriterGroup": "group",
        "OpcNodes": [ { "Id": "i=2258" } ]
    },
    {
        "EndpointUrl": "{{endpointB}}",
        "DataSetWriterId": "writerB",
        "DataSetWriterGroup": "group",
        "OpcNodes": [ { "Id": "i=2258" } ]
    }
]
""";
            var entries = _converter.Read(pn);
            return Assert.Single(_converter.ToWriterGroups(entries));
        }

        private WriterGroupModel ToSingleWriterGroup(string writerName, string queueName)
        {
            var pn = $$"""
[
    {
        "EndpointUrl": "opc.tcp://localhost:50000",
        "DataSetWriterId": "{{writerName}}",
        "DataSetWriterGroup": "group",
        "QueueName": "{{queueName}}",
        "OpcNodes": [ { "Id": "i=2258" } ]
    }
]
""";
            var entries = _converter.Read(pn);
            return Assert.Single(_converter.ToWriterGroups(entries));
        }

        private WriterGroupModel ToWriterGroupWithUserAuth(string writerGroup, string endpointUrl,
            string userName, string nodeId)
        {
            var pn = $$"""
[
    {
        "EndpointUrl": "{{endpointUrl}}",
        "DataSetWriterGroup": "{{writerGroup}}",
        "UseSecurity": false,
        "OpcAuthenticationMode": "UsernamePassword",
        "OpcAuthenticationUsername": "{{userName}}",
        "OpcAuthenticationPassword": "secret",
        "OpcNodes": [ { "Id": "{{nodeId}}" } ]
    }
]
""";
            var entries = _converter.Read(pn);
            return Assert.Single(_converter.ToWriterGroups(entries));
        }

        private readonly NewtonsoftJsonSerializer _serializer = new();
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptions<PublisherOptions> _options;
        private readonly PublishedNodesConverter _converter;
    }
}
