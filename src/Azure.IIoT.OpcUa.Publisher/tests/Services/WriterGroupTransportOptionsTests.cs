// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="WriterGroupTransportOptions"/>.
    /// Covers the transport-client selection logic in the constructor and
    /// the factory-based configuration code path.
    /// </summary>
    public sealed class WriterGroupTransportOptionsTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static IOptions<PublisherOptions> CreateOptions(
            WriterGroupTransport? defaultTransport = null)
        {
            var opts = new PublisherConfig(new ConfigurationBuilder().Build()).ToOptions();
            opts.Value.DefaultTransport = defaultTransport;
            return opts;
        }

        private static Mock<IEventClient> CreateClientMock(string name)
        {
            var mock = new Mock<IEventClient>();
            mock.SetupGet(c => c.Name).Returns(name);
            return mock;
        }

        // ── Transport name selection ──────────────────────────────────────────

        [Fact]
        public void Ctor_SelectsClientMatchingGroupTransportName()
        {
            var iotHub = CreateClientMock("IoTHub");
            var mqtt = CreateClientMock("Mqtt");
            var group = new WriterGroupModel { Id = "g1", Transport = WriterGroupTransport.Mqtt };

            using var sut = new WriterGroupTransportOptions(group,
                [iotHub.Object, mqtt.Object], [],
                CreateOptions(), NullLogger.Instance);

            Assert.Same(mqtt.Object, sut.EventClient);
        }

        [Fact]
        public void Ctor_TransportNameMatchIsCaseInsensitive()
        {
            var mqttClient = CreateClientMock("MQTT");
            var group = new WriterGroupModel { Id = "g1", Transport = WriterGroupTransport.Mqtt };

            using var sut = new WriterGroupTransportOptions(group,
                [mqttClient.Object], [],
                CreateOptions(), NullLogger.Instance);

            Assert.Same(mqttClient.Object, sut.EventClient);
        }

        [Fact]
        public void Ctor_FallsBackToDefaultTransportWhenGroupTransportNotFound()
        {
            var iotHub = CreateClientMock("IoTHub");
            var eventHub = CreateClientMock("EventHub");
            var group = new WriterGroupModel { Id = "g1", Transport = null };

            using var sut = new WriterGroupTransportOptions(group,
                [iotHub.Object, eventHub.Object], [],
                CreateOptions(WriterGroupTransport.EventHub), NullLogger.Instance);

            Assert.Same(eventHub.Object, sut.EventClient);
        }

        [Fact]
        public void Ctor_FallsBackToFirstClientWhenNeitherTransportMatches()
        {
            var first = CreateClientMock("IoTHub");
            var second = CreateClientMock("Mqtt");
            var group = new WriterGroupModel { Id = "g1", Transport = null };

            using var sut = new WriterGroupTransportOptions(group,
                [first.Object, second.Object], [],
                CreateOptions(), NullLogger.Instance);

            Assert.Same(first.Object, sut.EventClient);
        }

        [Fact]
        public void Ctor_SelectsFirstClientWhenGroupTransportNullAndDefaultNull()
        {
            var only = CreateClientMock("Null");
            var group = new WriterGroupModel { Id = "g1" };

            using var sut = new WriterGroupTransportOptions(group,
                [only.Object], [],
                CreateOptions(), NullLogger.Instance);

            Assert.Same(only.Object, sut.EventClient);
        }

        // ── TransportConfiguration code path ─────────────────────────────────

        [Fact]
        public void Ctor_EmptyTransportConfiguration_DoesNotInvokeFactory()
        {
            var client = CreateClientMock("IoTHub");
            var factory = new Mock<IEventClientFactory>();
            var group = new WriterGroupModel
            {
                Id = "g1",
                Transport = WriterGroupTransport.IoTHub,
                TransportConfiguration = null
            };

            using var sut = new WriterGroupTransportOptions(group,
                [client.Object],
                new Dictionary<string, IEventClientFactory> { ["IoTHub"] = factory.Object },
                CreateOptions(), NullLogger.Instance);

            factory.Verify(f => f.CreateEventClient(
                It.IsAny<string>(), out It.Ref<IEventClient>.IsAny), Times.Never);
        }

        [Fact]
        public void Ctor_WithTransportConfigAndFactory_CreatesClientFromFactory()
        {
            var original = CreateClientMock("Mqtt");
            var created = CreateClientMock("Mqtt");
            var scope = new Mock<IDisposable>();

            var factory = new Mock<IEventClientFactory>();
            factory.Setup(f => f.CreateEventClient(
                    "conn-string", out It.Ref<IEventClient>.IsAny))
                .Callback(new CreateEventClientCallback((string _, out IEventClient c) =>
                    c = created.Object))
                .Returns(scope.Object);

            var group = new WriterGroupModel
            {
                Id = "g1",
                Transport = WriterGroupTransport.Mqtt,
                TransportConfiguration = "conn-string"
            };

            using var sut = new WriterGroupTransportOptions(group,
                [original.Object],
                new Dictionary<string, IEventClientFactory> { ["Mqtt"] = factory.Object },
                CreateOptions(), NullLogger.Instance);

            Assert.Same(created.Object, sut.EventClient);
        }

        [Fact]
        public void Ctor_IoTHubConnectionStringUsesIoTHubFactory()
        {
            var edge = CreateClientMock("IoTHub");
            var dedicated = CreateClientMock("IoTHub");
            var scope = new Mock<IDisposable>();
            var factory = new Mock<IEventClientFactory>();
            factory.SetupGet(f => f.Name).Returns("IoTHub");
            factory.Setup(f => f.CreateEventClient(
                    "device-connection-string",
                    out It.Ref<IEventClient>.IsAny))
                .Callback(new CreateEventClientCallback(
                    (string _, out IEventClient client) =>
                        client = dedicated.Object))
                .Returns(scope.Object);
            var group = new WriterGroupModel
            {
                Id = "g1",
                Transport = WriterGroupTransport.IoTHub,
                TransportConfiguration = "device-connection-string"
            };

            using var sut = new WriterGroupTransportOptions(group,
                [edge.Object],
                new Dictionary<string, IEventClientFactory>
                {
                    ["IoTHub"] = factory.Object
                },
                CreateOptions(), NullLogger.Instance);

            Assert.Same(dedicated.Object, sut.EventClient);
            factory.Verify(f => f.CreateEventClient(
                "device-connection-string",
                out It.Ref<IEventClient>.IsAny), Times.Once);
        }

        [Fact]
        public void Ctor_WithTransportConfigAndNoMatchingFactory_Throws()
        {
            var original = CreateClientMock("Mqtt");
            var group = new WriterGroupModel
            {
                Id = "g1",
                Transport = WriterGroupTransport.Mqtt,
                TransportConfiguration = "conn-string"
            };

            var error = Assert.Throws<InvalidOperationException>(() =>
                new WriterGroupTransportOptions(group, [original.Object], [],
                    CreateOptions(), NullLogger.Instance));

            Assert.Contains("does not support", error.Message,
                StringComparison.Ordinal);
            Assert.Contains("g1", error.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Ctor_WithTransportConfigAndFactoryThrows_Throws()
        {
            var original = CreateClientMock("Mqtt");
            var factory = new Mock<IEventClientFactory>();
            factory.Setup(f => f.CreateEventClient(
                    It.IsAny<string>(), out It.Ref<IEventClient>.IsAny))
                .Throws(new InvalidOperationException("bad config"));

            var group = new WriterGroupModel
            {
                Id = "g1",
                Transport = WriterGroupTransport.Mqtt,
                TransportConfiguration = "conn-string"
            };

            var error = Assert.Throws<InvalidOperationException>(() =>
                new WriterGroupTransportOptions(group, [original.Object],
                    new Dictionary<string, IEventClientFactory>
                    {
                        ["Mqtt"] = factory.Object
                    },
                    CreateOptions(), NullLogger.Instance));

            Assert.Contains("g1", error.Message, StringComparison.Ordinal);
            Assert.Equal("bad config", error.InnerException?.Message);
        }

        // ── Dispose ───────────────────────────────────────────────────────────

        [Fact]
        public void Dispose_DisposesFactoryCreatedScope()
        {
            var original = CreateClientMock("Mqtt");
            var created = CreateClientMock("Mqtt");
            var scope = new Mock<IDisposable>();

            var factory = new Mock<IEventClientFactory>();
            factory.Setup(f => f.CreateEventClient(
                    "conn", out It.Ref<IEventClient>.IsAny))
                .Callback(new CreateEventClientCallback((string _, out IEventClient c) =>
                    c = created.Object))
                .Returns(scope.Object);

            var group = new WriterGroupModel
            {
                Id = "g1",
                Transport = WriterGroupTransport.Mqtt,
                TransportConfiguration = "conn"
            };

            var sut = new WriterGroupTransportOptions(group,
                [original.Object],
                new Dictionary<string, IEventClientFactory> { ["Mqtt"] = factory.Object },
                CreateOptions(), NullLogger.Instance);

            sut.Dispose();

            scope.Verify(s => s.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_WithNoScope_DoesNotThrow()
        {
            var client = CreateClientMock("IoTHub");
            var group = new WriterGroupModel { Id = "g1" };

            var sut = new WriterGroupTransportOptions(group,
                [client.Object], [],
                CreateOptions(), NullLogger.Instance);

            var ex = Record.Exception(() => sut.Dispose());
            Assert.Null(ex);
        }

        // Moq delegate for out-param factory setup
        private delegate void CreateEventClientCallback(
            string connectionString, out IEventClient client);
    }
}
