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
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
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

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task MixedDisposalJoinsOneAsyncScopeCompletionAsync(
            bool failDisposal, bool cancelDisposal)
        {
            var client = CreateClientMock("Mqtt");
            var scope = new Mock<IDisposable>(MockBehavior.Strict);
            var asyncScope = scope.As<IAsyncDisposable>();
            var closing = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            asyncScope.Setup(instance => instance.DisposeAsync()).Returns(() =>
            {
                closing.TrySetResult();
                return new ValueTask(release.Task);
            });
            IEventClient selected = client.Object;
            var factory = new Mock<IEventClientFactory>(MockBehavior.Strict);
            factory.Setup(instance => instance.CreateEventClient("owned", out selected))
                .Returns(scope.Object);
            var sut = new WriterGroupTransportOptions(new WriterGroupModel
            {
                Id = "owned",
                TransportConfiguration = "owned"
            }, [client.Object], new() { ["Mqtt"] = factory.Object },
                CreateOptions(), NullLogger.Instance);
            var canceledToken = new CancellationToken(canceled: true);
            Exception failure = cancelDisposal
                ? new OperationCanceledException("close canceled", canceledToken)
                : new InvalidOperationException("close failed");
            var synchronous = Task.Run(sut.Dispose);
            Task? first = null;
            Task? second = null;
            try
            {
                // The synchronous caller is already inside the gated async scope.
                await closing.Task.WaitAsync(TimeSpan.FromSeconds(10));
                first = sut.DisposeAsync().AsTask();
                second = sut.DisposeAsync().AsTask();

                Assert.Same(first, second);
                Assert.False(synchronous.IsCompleted);
                Assert.False(first.IsCompleted);
                asyncScope.Verify(instance => instance.DisposeAsync(), Times.Once);
                scope.Verify(instance => instance.Dispose(), Times.Never);
                Assert.Same(client.Object, sut.EventClient);
            }
            finally
            {
                if (failDisposal)
                {
                    release.TrySetException(failure);
                }
                else
                {
                    release.TrySetResult();
                }
            }

            var error = await Record.ExceptionAsync(() =>
                Task.WhenAll(synchronous, first!, second!).WaitAsync(TimeSpan.FromSeconds(10)));
            if (cancelDisposal)
            {
                Assert.Equal(canceledToken,
                    Assert.IsAssignableFrom<OperationCanceledException>(error).CancellationToken);
                Assert.True(first!.IsCanceled);
                Assert.Equal(canceledToken,
                    Assert.IsAssignableFrom<OperationCanceledException>(
                        Record.Exception(sut.Dispose)).CancellationToken);
            }
            else if (failDisposal)
            {
                Assert.Same(failure, error);
                Assert.Same(failure, Record.Exception(sut.Dispose));
            }
            else
            {
                Assert.Null(error);
                sut.Dispose();
            }
            Assert.Same(first, sut.DisposeAsync().AsTask());
            asyncScope.Verify(instance => instance.DisposeAsync(), Times.Once);
            scope.Verify(instance => instance.Dispose(), Times.Never);
            factory.Verify(instance => instance.CreateEventClient("owned", out selected),
                Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConstructorFailureAfterAcquiringScopeCleansUpAndPreservesCause(
            bool cancellation)
        {
            var original = CreateClientMock("Mqtt");
            var created = new Mock<IEventClient>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Exception failure = cancellation
                ? new OperationCanceledException("selection canceled", cts.Token)
                : new InvalidOperationException("selected client unavailable");
            var reads = 0;
            created.SetupGet(client => client.Name).Returns(() =>
                ++reads == 1 ? throw failure : "Mqtt");
            var scope = new Mock<IDisposable>(MockBehavior.Strict);
            var asyncScope = scope.As<IAsyncDisposable>();
            asyncScope.Setup(instance => instance.DisposeAsync())
                .Returns(ValueTask.CompletedTask);
            var factory = new Mock<IEventClientFactory>(MockBehavior.Strict);
            IEventClient selected = created.Object;
            factory.Setup(instance => instance.CreateEventClient("owned", out selected))
                .Returns(scope.Object);

            var error = Record.Exception(() => new WriterGroupTransportOptions(
                new WriterGroupModel { Id = "owned", TransportConfiguration = "owned" },
                [original.Object], new() { ["Mqtt"] = factory.Object },
                CreateOptions(), NullLogger.Instance));

            if (cancellation)
            {
                var canceled = Assert.IsType<OperationCanceledException>(error);
                Assert.Same(failure, canceled);
                Assert.Equal(cts.Token, canceled.CancellationToken);
            }
            else
            {
                var wrapped = Assert.IsType<InvalidOperationException>(error);
                Assert.Same(failure, wrapped.InnerException);
                Assert.Contains("owned", wrapped.Message, StringComparison.Ordinal);
            }
            asyncScope.Verify(instance => instance.DisposeAsync(), Times.Once);
            scope.Verify(instance => instance.Dispose(), Times.Never);
            factory.Verify(instance => instance.CreateEventClient("owned", out selected),
                Times.Once);
        }

        [Fact]
        public void FactoryFailureLogsErrorTypeWithoutFormattingSecret()
        {
            const string secret = "SharedAccessKey=not-a-real-key-2535";
            var failure = new InvalidOperationException("Invalid credentials: " + secret);
            var factory = new Mock<IEventClientFactory>(MockBehavior.Strict);
            factory.Setup(instance => instance.CreateEventClient(
                    secret, out It.Ref<IEventClient>.IsAny))
                .Throws(failure);
            var messages = new List<string>();
            var logger = new Mock<ILogger>();
            logger.Setup(instance => instance.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            logger.Setup(instance => instance.Log(
                    It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var formatter = (Delegate)invocation.Arguments[4];
                    messages.Add((string)formatter.DynamicInvoke(
                        invocation.Arguments[2], invocation.Arguments[3])!);
                    Assert.Null(invocation.Arguments[3]);
                }));

            var error = Assert.Throws<InvalidOperationException>(() =>
                new WriterGroupTransportOptions(new WriterGroupModel
                {
                    Id = "secret-group",
                    TransportConfiguration = secret
                }, [CreateClientMock("Mqtt").Object], new() { ["Mqtt"] = factory.Object },
                    CreateOptions(), logger.Object));

            Assert.Same(failure, error.InnerException);
            Assert.Equal("Invalid credentials: " + secret, error.InnerException.Message);
            Assert.DoesNotContain(secret, error.Message, StringComparison.Ordinal);
            var message = Assert.Single(messages);
            Assert.Contains(nameof(InvalidOperationException), message, StringComparison.Ordinal);
            Assert.Contains("secret-group", message, StringComparison.Ordinal);
            Assert.Contains("Mqtt", message, StringComparison.Ordinal);
            Assert.DoesNotContain(secret, message, StringComparison.Ordinal);
            Assert.DoesNotContain(failure.Message, message, StringComparison.Ordinal);
        }

        // Moq delegate for out-param factory setup
        private delegate void CreateEventClientCallback(
            string connectionString, out IEventClient client);
    }
}
