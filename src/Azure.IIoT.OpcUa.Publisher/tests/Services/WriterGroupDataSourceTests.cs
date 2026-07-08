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
            var group = ToSecuredWriterGroup("Asset1", "opc.tcp://opcplc:50000",
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

        private WriterGroupModel ToSecuredWriterGroup(string writerGroup, string endpointUrl,
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
