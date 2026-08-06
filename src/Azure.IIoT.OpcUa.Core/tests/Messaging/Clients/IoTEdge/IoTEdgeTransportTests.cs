// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Hosting;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using global::IoTHubby;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class IoTEdgeTransportTests
    {
        [Fact]
        public void ConstructorRejectsNullClient()
        {
            Assert.Throws<ArgumentNullException>(() => new IoTEdgeTransport(
                null!, NullLogger<IoTEdgeTransport>.Instance));
        }

        [Fact]
        public async Task IdentitiesComeFromModuleClientIdentityAsync()
        {
            await using var transport = CreateTransport(new IoTEdgeTestModuleClient(),
                new IoTEdgeTestIdentity { Gateway = "gateway" });

            Assert.Equal("device/module", transport.Identity);
            Assert.Equal("gateway", ((IProcessIdentity)transport).Identity);
        }

        [Fact]
        public async Task SendWithoutTopicMapsEventToTelemetryAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var transport = CreateTransport(sdk);
            var timestamp = DateTimeOffset.Parse("2026-08-04T00:00:00Z");
            using var @event = transport.CreateEvent()
                .SetContentType("application/json")
                .SetContentEncoding("utf-8")
                .SetTimestamp(timestamp)
                .SetQoS(QoS.AtMostOnce)
                .AddProperty("keep", "value")
                .AddProperty("skip", null)
                .AddBuffers([
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("hel")),
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("lo"))
                ]);

            await @event.SendAsync();

            var message = Assert.Single(sdk.Telemetry);
            Assert.Equal("hello", Encoding.UTF8.GetString(message.Payload.ToArray()));
            Assert.Equal("application/json", message.ContentType);
            Assert.Equal("utf-8", message.ContentEncoding);
            Assert.Equal(timestamp, message.CreationTimeUtc);
            Assert.Equal(IoTHubQoS.AtMostOnce, message.QoS);
            Assert.Equal("value", message.Properties["keep"]);
            Assert.False(message.Properties.ContainsKey("skip"));
            Assert.Empty(sdk.OutputTelemetry);
            Assert.Equal(1, sdk.ConnectCount);
        }

        [Fact]
        public async Task SendWithTopicMapsEventToOutputTelemetryAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var transport = CreateTransport(sdk);
            using var @event = transport.CreateEvent()
                .SetTopic("alerts")
                .SetQoS(QoS.AtLeastOnce)
                .AddBuffers([new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 })]);

            await @event.SendAsync();

            var sent = Assert.Single(sdk.OutputTelemetry);
            Assert.Equal("alerts", sent.Output);
            Assert.Equal(new byte[] { 1, 2, 3 }, sent.Message.Payload.ToArray());
            Assert.Equal(IoTHubQoS.AtLeastOnce, sent.Message.QoS);
            Assert.Empty(sdk.Telemetry);
        }

        [Fact]
        public async Task SendPropagatesSdkErrorsAsync()
        {
            var expected = new InvalidOperationException("send failed");
            var sdk = new IoTEdgeTestModuleClient
            {
                SendException = expected
            };
            await using var transport = CreateTransport(sdk);
            using var @event = transport.CreateEvent();

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await @event.SendAsync());

            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task SubscribeReceivesMatchingInputAndMapsPropertiesAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var transport = CreateTransport(sdk);
            var handled = new TaskCompletionSource<IReadOnlyDictionary<string, string?>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var consumer = new Mock<IEventConsumer>();
            consumer.Setup(c => c.HandleAsync("input1",
                    It.Is<ReadOnlySequence<byte>>(p =>
                        p.ToArray().SequenceEqual(Encoding.UTF8.GetBytes("payload"))),
                    "application/json",
                    It.IsAny<IReadOnlyDictionary<string, string?>>(),
                    transport,
                    It.IsAny<CancellationToken>()))
                .Callback((string _, ReadOnlySequence<byte> _, string _,
                    IReadOnlyDictionary<string, string?> properties, IEventClient? _,
                    CancellationToken _) => handled.SetResult(properties))
                .Returns(Task.CompletedTask);

            await using var subscription =
                await transport.SubscribeAsync("input1", consumer.Object);
            await sdk.Inputs.Writer.WriteAsync(
                IoTEdgeTestModuleClient.CreateInputMessage(
                    Encoding.UTF8.GetBytes("payload"),
                    new Dictionary<string, string>
                    {
                        ["$.inp"] = "input1",
                        ["$.ct"] = "application/json",
                        ["$.ce"] = "utf-8",
                        ["sys"] = "system"
                    },
                    new Dictionary<string, string>
                    {
                        ["app"] = "custom"
                    }));

            var properties = await handled.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal("custom", properties["app"]);
            Assert.Equal("system", properties["sys"]);
            Assert.Equal("utf-8", properties["ContentEncoding"]);
            consumer.VerifyAll();
        }

        [Fact]
        public async Task ConnectRegistersAndDisposesMethodHandlerAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var transport = CreateTransport(sdk);
            var handler = new Mock<IRpcHandler>();
            handler.SetupGet(h => h.MountPoint).Returns(string.Empty);
            handler.Setup(h => h.InvokeAsync("ping",
                    It.IsAny<ReadOnlySequence<byte>>(), ContentMimeType.Json,
                    It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<ReadOnlySequence<byte>>(
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("pong"))));

            await using var connection =
                await transport.ConnectAsync(handler.Object);
            var response = await sdk.MethodHandler!(
                IoTEdgeTestModuleClient.CreateDirectMethodRequest("ping",
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{}"))),
                default);

            Assert.Equal((int)HttpStatusCode.OK, response.Status);
            Assert.Equal("pong", Encoding.UTF8.GetString(response.Payload.ToArray()));
            var connected = Assert.Single(transport.Connected);
            Assert.Same(handler.Object, connected);
            await connection.DisposeAsync();
            Assert.Null(sdk.MethodHandler);
        }

        [Fact]
        public async Task MethodHandlerMapsFailuresToDirectMethodResponsesAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var transport = CreateTransport(sdk);
            var handler = new Mock<IRpcHandler>();
            handler.SetupGet(h => h.MountPoint).Returns(string.Empty);
            handler.SetupSequence(h => h.InvokeAsync("fail",
                    It.IsAny<ReadOnlySequence<byte>>(), ContentMimeType.Json,
                    It.IsAny<CancellationToken>()))
                .Throws(new NotSupportedException())
                .Throws(new MethodCallStatusException(
                    (int)HttpStatusCode.BadRequest, "bad"))
                .Throws(new OperationCanceledException())
                .Throws(new InvalidOperationException("boom"));

            await using var connection =
                await transport.ConnectAsync(handler.Object);

            Assert.Equal((int)HttpStatusCode.NotFound,
                (await InvokeAsync("fail")).Status);
            Assert.Equal((int)HttpStatusCode.BadRequest,
                (await InvokeAsync("fail")).Status);
            Assert.Equal((int)HttpStatusCode.RequestTimeout,
                (await InvokeAsync("fail")).Status);
            Assert.Equal((int)HttpStatusCode.InternalServerError,
                (await InvokeAsync("fail")).Status);

            ValueTask<DirectMethodResponse> InvokeAsync(string method)
            {
                return sdk.MethodHandler!(
                    IoTEdgeTestModuleClient.CreateDirectMethodRequest(method,
                        new ReadOnlySequence<byte>()),
                    default);
            }
        }

        [Fact]
        public async Task DisposeCancelsReceiverAndUnregistersMethodHandlerAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var transport = CreateTransport(sdk);
            await transport.SubscribeAsync("input", IEventConsumer.Null);
            await transport.ConnectAsync(Mock.Of<IRpcHandler>());

            await transport.DisposeAsync();

            Assert.Null(sdk.MethodHandler);
        }

        [Fact]
        public async Task DisposeContainsSdkUnregisterErrorsAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var transport = CreateTransport(sdk);
            await transport.ConnectAsync(Mock.Of<IRpcHandler>());
            sdk.SetMethodHandlerException = new InvalidOperationException("unregister failed");

            await transport.DisposeAsync();
        }

        [Fact]
        public async Task CreateEventRejectsUseAfterDisposeAsync()
        {
            var transport = CreateTransport(new IoTEdgeTestModuleClient());

            await transport.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() => transport.CreateEvent());
        }

        [Fact]
        public async Task MaxPayloadSizePropertiesHaveExpectedValuesAsync()
        {
            await using var transport = CreateTransport(new IoTEdgeTestModuleClient());

            Assert.Equal((256 * 1024) - 4 * 1024, transport.MaxEventPayloadSizeInBytes);
            Assert.Equal(120 * 1024, transport.MaxMethodPayloadSizeInBytes);
        }

        [Fact]
        public async Task CapabilitiesIncludeExpectedFlagsAsync()
        {
            await using var transport = CreateTransport(new IoTEdgeTestModuleClient());

            Assert.True(transport.Capabilities.HasFlag(EventClientCapabilities.Payload));
            Assert.True(transport.Capabilities.HasFlag(EventClientCapabilities.Topic));
            Assert.True(transport.Capabilities.HasFlag(EventClientCapabilities.CloudEvents));
        }

        [Fact]
        public async Task StartIsANoopAsync()
        {
            await using var transport = CreateTransport(new IoTEdgeTestModuleClient());

            // Start() must not throw.
            transport.Start();
        }

        [Fact]
        public async Task CallAsyncThrowsNotSupportedExceptionAsync()
        {
            await using var transport = CreateTransport(new IoTEdgeTestModuleClient());

            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await transport.CallAsync("target", "method",
                    ReadOnlySequence<byte>.Empty, "application/json"));
        }

        [Fact]
        public async Task SubscribeAsyncRejectsInvalidTopicFilterAsync()
        {
            await using var transport = CreateTransport(new IoTEdgeTestModuleClient());

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await transport.SubscribeAsync("+foo",
                    IEventConsumer.Null));
        }

        [Fact]
        public async Task AsCloudEvent_SetsStandardPropertiesOnEventAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var transport = CreateTransport(sdk);
            var header = new CloudEventHeader
            {
                Id = "evt-1",
                Source = new Uri("urn:test"),
                Type = "com.test.event",
                Time = DateTimeOffset.UtcNow,
                DataContentType = "application/json",
                Subject = "subject1"
            };
            using var @event = transport.CreateEvent()
                .AsCloudEvent(header)
                .AddBuffers([new ReadOnlySequence<byte>(new byte[] { 1 })]);

            await @event.SendAsync();

            var sent = Assert.Single(sdk.Telemetry);
            Assert.Equal("1.0", sent.Properties["specversion"]);
            Assert.Equal("evt-1", sent.Properties["id"]);
            Assert.Equal("com.test.event", sent.Properties["type"]);
            Assert.Equal("subject1", sent.Properties["subject"]);
        }

        [Fact]
        public async Task SetSchema_SetsDataSchemaPropertyAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var transport = CreateTransport(sdk);
            var schema = new Mock<IEventSchema>();
            schema.SetupGet(s => s.Id).Returns("schema://v1");

            using var @event = transport.CreateEvent()
                .SetSchema(schema.Object)
                .AddBuffers([new ReadOnlySequence<byte>(new byte[] { 1 })]);
            await @event.SendAsync();

            var sent = Assert.Single(sdk.Telemetry);
            Assert.Equal("schema://v1", sent.Properties["dataschema"]);
        }

        [Fact]
        public async Task SetRetain_IsAcceptedWithoutEffectAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var transport = CreateTransport(sdk);

            using var @event = transport.CreateEvent()
                .SetRetain(true)
                .SetTtl(TimeSpan.FromMinutes(1))
                .AddBuffers([new ReadOnlySequence<byte>(new byte[] { 42 })]);
            await @event.SendAsync();

            Assert.Single(sdk.Telemetry);
        }

        [Fact]
        public async Task AddBuffers_MultipleSegments_ConcatenatesPayloadAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            await using var transport = CreateTransport(sdk);

            using var @event = transport.CreateEvent()
                .AddBuffers([
                    new ReadOnlySequence<byte>(new byte[] { 1, 2 }),
                    new ReadOnlySequence<byte>(new byte[] { 3, 4 })
                ]);
            await @event.SendAsync();

            var sent = Assert.Single(sdk.Telemetry);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, sent.Payload.ToArray());
        }

        private static IoTEdgeTransport CreateTransport(IoTEdgeTestModuleClient sdk,
            IoTEdgeTestIdentity? identity = null)
        {
            var client = new IoTEdgeModuleClient(
                Options.Create(new IoTEdgeClientOptions()),
                identity ?? new IoTEdgeTestIdentity(),
                [],
                clientFactory: new IoTEdgeTestModuleClientFactory(sdk));
            return new IoTEdgeTransport(client,
                NullLogger<IoTEdgeTransport>.Instance);
        }
    }
}
