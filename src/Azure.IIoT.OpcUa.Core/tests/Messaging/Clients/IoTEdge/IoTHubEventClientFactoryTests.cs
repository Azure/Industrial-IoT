// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.Hosting;
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using global::IoTHubby;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class IoTHubEventClientFactoryTests
    {
        [Fact]
        public async Task CreatesDedicatedClientFromConnectionStringAsync()
        {
            var sdk = new IoTEdgeTestModuleClient();
            var clientFactory = new IoTEdgeTestModuleClientFactory(sdk);
            var factory = new IoTHubEventClientFactory(
                Options.Create(new IoTEdgeClientOptions
                {
                    Product = "product",
                    KeepAlivePeriodSeconds = 42,
                    DefaultMethodCallTimeout = TimeSpan.FromSeconds(7)
                }),
                [],
                NullLoggerFactory.Instance,
                clientFactory);
            var connectionString =
                "HostName=test.azure-devices.net;DeviceId=child;" +
                "SharedAccessKey=ZmFrZWtleQ==";

            var scope = factory.CreateEventClient(connectionString,
                out var client);

            var transport = Assert.IsType<IoTEdgeTransport>(client);
            Assert.Equal("IoTHub", factory.Name);
            Assert.Equal("IoTHub", transport.Name);
            Assert.Equal("child", transport.Identity);
            Assert.Equal(connectionString,
                clientFactory.Options?.EdgeHubConnectionString);
            Assert.Equal("product", clientFactory.Options?.Product);
            Assert.Equal(TimeSpan.FromSeconds(7),
                clientFactory.ConfiguredOptions?.OperationTimeout);

            using var @event = transport.CreateEvent()
                .SetContentType("application/json")
                .AddBuffers([
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{}"))
                ]);
            await @event.SendAsync();
            Assert.Single(sdk.Telemetry);

            scope.Dispose();
            scope.Dispose();

            Assert.Equal(1, sdk.DisposeCount);
        }

        [Fact]
        public async Task SharedIdentityOverlappingLeasesReleaseClientOnlyAfterLastLeaseAsync()
        {
            var clients = new List<IoTEdgeTestModuleClient>();
            var clientFactory = new Mock<IIoTHubModuleClientFactory>();
            clientFactory.Setup(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()))
                .Returns(() =>
                {
                    // Each actual SDK creation must be visible, not masked by a singleton fake.
                    var sdk = new IoTEdgeTestModuleClient();
                    clients.Add(sdk);
                    return sdk;
                });
            var factory = new IoTHubEventClientFactory(
                Options.Create(new IoTEdgeClientOptions()),
                [],
                NullLoggerFactory.Instance,
                clientFactory.Object);
            var connectionString =
                "HostName=test.azure-devices.net;DeviceId=child;" +
                "SharedAccessKey=ZmFrZWtleQ==";

            using var firstLease = factory.CreateEventClient(connectionString,
                out var firstClient);
            using var secondLease = factory.CreateEventClient(connectionString,
                out var secondClient);

            using var firstEvent = firstClient.CreateEvent()
                .SetContentType("application/json")
                .AddBuffers([
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{}"))
                ]);
            await firstEvent.SendAsync();

            var sharedSdk = Assert.Single(clients);
            Assert.Equal(1, sharedSdk.ConnectCount);
            Assert.Single(sharedSdk.Telemetry);
            Assert.Equal(0, sharedSdk.DisposeCount);

            firstLease.Dispose();
            firstLease.Dispose();
            Assert.Equal(0, sharedSdk.DisposeCount);

            using var secondEvent = secondClient.CreateEvent()
                .SetContentType("application/json")
                .AddBuffers([
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("{}"))
                ]);
            await secondEvent.SendAsync();

            Assert.Single(clients);
            Assert.Equal(1, sharedSdk.ConnectCount);
            Assert.Equal(0, sharedSdk.DisposeCount);
            Assert.Collection(sharedSdk.Telemetry,
                message =>
                {
                    Assert.Equal("{}", Encoding.UTF8.GetString(message.Payload.ToArray()));
                    Assert.Equal("application/json", message.ContentType);
                },
                message =>
                {
                    Assert.Equal("{}", Encoding.UTF8.GetString(message.Payload.ToArray()));
                    Assert.Equal("application/json", message.ContentType);
                });

            secondLease.Dispose();
            Assert.Equal(1, sharedSdk.DisposeCount);
            secondLease.Dispose();
            Assert.Equal(1, sharedSdk.DisposeCount);
        }

        [Theory]
        [InlineData("sharedaccesskey=ZmFrZWtleQ==;MODULEID=module;deviceid=device;" +
            "hostname=TEST.AZURE-DEVICES.NET;gatewayhostname=GATEWAY.EXAMPLE.NET;" +
            "sharedaccesskeyname=policy")]
        [InlineData("HostName=TEST.AZURE-DEVICES.NET.;DeviceId=device;ModuleId=module;" +
            "SharedAccessKey=ZmFrZWtleQ==;GatewayHostName=GATEWAY.EXAMPLE.NET.;" +
            "SharedAccessKeyName=policy")]
        [InlineData("HostName=test.azure-devices.net;DeviceId=device;ModuleId=module;" +
            "SharedAccessKey= ZmFr ZWtl eQ== ;SharedAccessKeyName=policy;" +
            "GatewayHostName=gateway.example.net")]
        [InlineData("; HostName =test.azure-devices.net; DeviceId =device; ModuleId =module;;" +
            " SharedAccessKey =ZmFrZWtleQ==; GatewayHostName =gateway.example.net;" +
            " SharedAccessKeyName =policy;")]
        [InlineData("HostName=test.azure-devices.net;DeviceId= device ;ModuleId= module ;" +
            "SharedAccessKey=ZmFrZWtleQ==;GatewayHostName=gateway.example.net;" +
            "SharedAccessKeyName=policy")]
        [InlineData("HostName= test.azure-devices.net ;DeviceId=device;ModuleId=module;" +
            "SharedAccessKey=ZmFrZWtleQ==;GatewayHostName= gateway.example.net ;" +
            "SharedAccessKeyName= policy ")]
        public async Task CanonicalConnectionStringsShareOneOverlappingClientAsync(
            string connectionString)
        {
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var factory = CreateFactory(sdkFactory.Object);
            using var firstLease = factory.CreateEventClient(connectionString,
                out var firstClient);
            using var secondLease = factory.CreateEventClient(
                kConnectionString + ";GatewayHostName=gateway.example.net;" +
                "SharedAccessKeyName=policy", out var secondClient);

            Assert.Same(firstClient, secondClient);
            Assert.Equal("device/module", firstClient.Identity);
            var sdk = Assert.Single(sdks);
            sdkFactory.Verify(f => f.Create(It.Is<IoTEdgeClientOptions>(o =>
                o.EdgeHubConnectionString == connectionString),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
            await SendAsync(firstClient, "first");

            firstLease.Dispose();
            await Assert.IsAssignableFrom<IAsyncDisposable>(firstLease).DisposeAsync();
            Assert.Equal(0, sdk.DisposeCount);
            await SendAsync(secondClient, "second");
            AssertTelemetry(sdk, "first", "second");

            await Assert.IsAssignableFrom<IAsyncDisposable>(secondLease).DisposeAsync();
            secondLease.Dispose();
            Assert.Equal(1, sdk.DisposeCount);
            Assert.Throws<ObjectDisposedException>(() => secondClient.CreateEvent());
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ConcurrentOverlappingLeasesCreateAndCloseExactlyOneSdkAsync(
            bool disposeFinalSynchronously)
        {
            const int callerCount = 32;
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var factory = CreateFactory(sdkFactory.Object);
            var ready = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var start = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var readyCount = 0;
            var acquisitions = Enumerable.Range(0, callerCount).Select(_ =>
                Task.Run(async () =>
                {
                    if (Interlocked.Increment(ref readyCount) == callerCount)
                    {
                        ready.SetResult();
                    }
                    await start.Task;
                    var lease = factory.CreateEventClient(kConnectionString,
                        out var client);
                    return (Lease: lease, Client: client);
                })).ToArray();

            await ready.Task;
            start.SetResult();
            // No caller releases its lease until every acquisition has completed.
            var leases = await Task.WhenAll(acquisitions);
            try
            {
                var sdk = Assert.Single(sdks);
                Assert.All(leases, lease => Assert.Same(leases[0].Client, lease.Client));
                Assert.Equal(0, sdk.DisposeCount);
                Assert.Equal(0, sdk.ConnectCount);
                await SendAsync(leases[0].Client, "before releases");

                await Task.WhenAll(leases.Take(callerCount - 1).Select((lease, index) =>
                    Task.Run(async () =>
                    {
                        var asyncLease = Assert.IsAssignableFrom<IAsyncDisposable>(lease.Lease);
                        if (index % 2 == 0)
                        {
                            lease.Lease.Dispose();
                        }
                        else
                        {
                            await asyncLease.DisposeAsync();
                        }
                        lease.Lease.Dispose();
                        await asyncLease.DisposeAsync();
                    })));

                Assert.Equal(0, sdk.DisposeCount);
                await SendAsync(leases[^1].Client, "last lease still owns the client");
                AssertTelemetry(sdk, "before releases", "last lease still owns the client");
                var finalLease = Assert.IsAssignableFrom<IAsyncDisposable>(leases[^1].Lease);
                if (disposeFinalSynchronously)
                {
                    leases[^1].Lease.Dispose();
                }
                else
                {
                    await finalLease.DisposeAsync();
                }
                leases[^1].Lease.Dispose();
                await finalLease.DisposeAsync();

                Assert.Equal(1, sdk.DisposeCount);
                Assert.Throws<ObjectDisposedException>(() => leases[^1].Client.CreateEvent());
                sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                    It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
            }
            finally
            {
                foreach (var lease in leases)
                {
                    lease.Lease.Dispose();
                }
            }
        }

        [Theory]
        [InlineData("HostName=other.azure-devices.net;DeviceId=device;ModuleId=module",
            "device/module", "device")]
        [InlineData("HostName=test.azure-devices.net;DeviceId=other;ModuleId=module",
            "other/module", "other")]
        [InlineData("HostName=test.azure-devices.net;DeviceId=device;ModuleId=other",
            "device/other", "device")]
        [InlineData("HostName=test.azure-devices.net;DeviceId=device",
            "device", "device")]
        [InlineData("HostName=test.azure-devices.net;DeviceId=Device;ModuleId=module",
            "Device/module", "Device")]
        [InlineData("HostName=test.azure-devices.net;DeviceId=device;ModuleId=Module",
            "device/Module", "device")]
        [InlineData("HostName=test.azure-devices.net;DeviceId=device/module",
            "device/module", "device/module")]
        public async Task DistinctIdentityComponentsHaveIndependentClientsAndLifetimesAsync(
            string identity, string expectedIdentity, string expectedProcessIdentity)
        {
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var factory = CreateFactory(sdkFactory.Object);
            using var firstLease = factory.CreateEventClient(kConnectionString,
                out var firstClient);
            using var secondLease = factory.CreateEventClient(
                identity + ";SharedAccessKey=ZmFrZWtleQ==", out var secondClient);

            Assert.NotSame(firstClient, secondClient);
            Assert.Equal("device/module", firstClient.Identity);
            Assert.Equal(expectedIdentity, secondClient.Identity);
            Assert.Equal("device",
                Assert.IsAssignableFrom<IProcessIdentity>(firstClient).Identity);
            Assert.Equal(expectedProcessIdentity,
                Assert.IsAssignableFrom<IProcessIdentity>(secondClient).Identity);
            var clients = sdks.ToArray();
            Assert.Equal(2, clients.Length);
            await SendAsync(firstClient, "first identity");
            await SendAsync(secondClient, "second identity");

            firstLease.Dispose();
            Assert.Equal(1, clients[0].DisposeCount);
            Assert.Equal(0, clients[1].DisposeCount);
            Assert.Throws<ObjectDisposedException>(() => firstClient.CreateEvent());
            await SendAsync(secondClient, "independent survivor");
            AssertTelemetry(clients[0], "first identity");
            AssertTelemetry(clients[1], "second identity", "independent survivor");

            await Assert.IsAssignableFrom<IAsyncDisposable>(secondLease).DisposeAsync();
            Assert.Equal(1, clients[0].DisposeCount);
            Assert.Equal(1, clients[1].DisposeCount);
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Exactly(2));
        }

        [Theory]
        [InlineData(kConnectionString, kIdentity + "SharedAccessKey=cm90YXRlZGtleQ==")]
        [InlineData(kConnectionString, kConnectionString + ";GatewayHostName=gateway.example.net")]
        [InlineData(kConnectionString + ";GatewayHostName=one.example.net",
            kConnectionString + ";GatewayHostName=two.example.net")]
        [InlineData(kConnectionString + ";SharedAccessKeyName=policy",
            kConnectionString + ";SharedAccessKeyName=Policy")]
        [InlineData(kConnectionString + ";SharedAccessKeyName=policy",
            kConnectionString + ";SharedAccessKeyName= other-policy ")]
        [InlineData(kIdentity + "SharedAccessSignature=" + kSasToken,
            kIdentity + "SharedAccessSignature=" + kOtherSasToken)]
        [InlineData(kIdentity + "SharedAccessSignature=" + kSasToken,
            kIdentity + "SharedAccessSignature= " + kOtherSasToken + " ")]
        [InlineData(kConnectionString, kIdentity + "SharedAccessSignature=" + kSasToken)]
        public async Task ActiveCredentialOrGatewayChangeIsRejectedUntilEveryLeaseClosesAsync(
            string originalConnectionString, string changedConnectionString)
        {
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var logger = new Mock<ILogger>(MockBehavior.Strict);
            var loggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                .Returns(logger.Object);
            var factory = new IoTHubEventClientFactory(
                Options.Create(new IoTEdgeClientOptions()), [],
                loggerFactory.Object, sdkFactory.Object);
            using var firstLease = factory.CreateEventClient(originalConnectionString,
                out var firstClient);
            using var lastLease = factory.CreateEventClient(originalConnectionString,
                out var lastClient);
            var originalSdk = Assert.Single(sdks);

            var error = Assert.Throws<InvalidOperationException>(() =>
                factory.CreateEventClient(changedConnectionString, out _));
            AssertSanitized(error);
            Assert.Equal(0, originalSdk.DisposeCount);
            await SendAsync(firstClient, "after conflict");
            firstLease.Dispose();

            Assert.Throws<InvalidOperationException>(() =>
                factory.CreateEventClient(changedConnectionString, out _));
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
            Assert.Equal(0, originalSdk.DisposeCount);
            await SendAsync(lastClient, "after partial release");
            AssertTelemetry(originalSdk, "after conflict", "after partial release");

            await Assert.IsAssignableFrom<IAsyncDisposable>(lastLease).DisposeAsync();
            Assert.Equal(1, originalSdk.DisposeCount);
            using var replacementLease = factory.CreateEventClient(changedConnectionString,
                out var replacementClient);
            Assert.NotSame(firstClient, replacementClient);
            var clients = sdks.ToArray();
            Assert.Equal(2, clients.Length);
            firstLease.Dispose();
            await Assert.IsAssignableFrom<IAsyncDisposable>(lastLease).DisposeAsync();
            Assert.Equal(0, clients[1].DisposeCount);
            await SendAsync(replacementClient, "new configuration");
            AssertTelemetry(clients[1], "new configuration");
            sdkFactory.Verify(f => f.Create(It.Is<IoTEdgeClientOptions>(o =>
                o.EdgeHubConnectionString == changedConnectionString),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
            replacementLease.Dispose();
            Assert.Equal(1, originalSdk.DisposeCount);
            Assert.Equal(1, clients[1].DisposeCount);
            logger.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("product", 42)]
        [InlineData("keepAlive", 42)]
        [InlineData("keepAlive", 1)]
        [InlineData("timeout", 42)]
        public async Task ActiveOptionChangeIsRejectedWithoutMutatingTheOriginalClientAsync(
            string changedOption, int initialKeepAlive)
        {
            var options = new IoTEdgeClientOptions
            {
                Product = "product",
                KeepAlivePeriodSeconds = initialKeepAlive,
                DefaultMethodCallTimeout = TimeSpan.FromSeconds(7)
            };
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var factory = CreateFactory(sdkFactory.Object, options);
            using var lease = factory.CreateEventClient(kConnectionString, out var client);
            switch (changedOption)
            {
                case "product":
                    options.Product = "changed-product";
                    break;
                case "keepAlive":
                    options.KeepAlivePeriodSeconds = initialKeepAlive == 1 ? 0 : 43;
                    break;
                case "timeout":
                    options.DefaultMethodCallTimeout = TimeSpan.FromSeconds(8);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(changedOption));
            }

            Assert.Throws<InvalidOperationException>(() =>
                factory.CreateEventClient(kConnectionString, out _));
            var originalSdk = Assert.Single(sdks);
            Assert.Equal(0, originalSdk.DisposeCount);
            await SendAsync(client, "original options remain usable");
            AssertTelemetry(originalSdk, "original options remain usable");
            sdkFactory.Verify(f => f.Create(It.Is<IoTEdgeClientOptions>(o =>
                o.Product == "product" && o.KeepAlivePeriodSeconds == initialKeepAlive
                    && o.DefaultMethodCallTimeout == TimeSpan.FromSeconds(7)),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);

            lease.Dispose();
            using var replacementLease = factory.CreateEventClient(kConnectionString,
                out var replacementClient);
            Assert.NotSame(client, replacementClient);
            var clients = sdks.ToArray();
            Assert.Equal(2, clients.Length);
            await SendAsync(replacementClient, "updated options");
            AssertTelemetry(clients[1], "updated options");
            sdkFactory.Verify(f => f.Create(It.Is<IoTEdgeClientOptions>(o =>
                o.Product == options.Product
                    && o.KeepAlivePeriodSeconds == options.KeepAlivePeriodSeconds
                    && o.DefaultMethodCallTimeout == options.DefaultMethodCallTimeout),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
            replacementLease.Dispose();
            Assert.Equal(1, originalSdk.DisposeCount);
            Assert.Equal(1, clients[1].DisposeCount);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public async Task EquivalentEffectiveDefaultOptionsShareTheExistingClientAsync(
            int keepAlive)
        {
            var options = new IoTEdgeClientOptions();
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var factory = CreateFactory(sdkFactory.Object, options);
            using var firstLease = factory.CreateEventClient(kConnectionString,
                out var firstClient);
            var defaults = new IoTHubClientOptions();
            options.Product = string.Empty;
            options.KeepAlivePeriodSeconds = keepAlive > 0
                ? checked((int)defaults.KeepAlive.TotalSeconds) : keepAlive;
            options.DefaultMethodCallTimeout = defaults.OperationTimeout;

            using var secondLease = factory.CreateEventClient(
                kConnectionString + ";GatewayHostName=", out var secondClient);
            Assert.Same(firstClient, secondClient);
            var sdk = Assert.Single(sdks);
            firstLease.Dispose();
            Assert.Equal(0, sdk.DisposeCount);
            await SendAsync(secondClient, "effective defaults");
            AssertTelemetry(sdk, "effective defaults");
            await Assert.IsAssignableFrom<IAsyncDisposable>(secondLease).DisposeAsync();
            Assert.Equal(1, sdk.DisposeCount);
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FailedConstructionOrEventSubscriptionDoesNotPoisonIdentityAsync(
            bool failEventSubscription)
        {
            var failure = new InvalidOperationException("SDK initialization failed.");
            var brokenSdk = CreateDisposableSdk();
            brokenSdk.SetupAdd(s => s.ConnectionStateChanged +=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>())
                .Throws(failure);
            var healthySdk = new IoTEdgeTestModuleClient();
            var sdkFactory = new Mock<IIoTHubModuleClientFactory>(MockBehavior.Strict);
            var creations = sdkFactory.SetupSequence(f =>
                f.Create(It.IsAny<IoTEdgeClientOptions>(),
                    It.IsAny<Action<IoTHubClientOptions>>()));
            if (failEventSubscription)
            {
                creations.Returns(brokenSdk.Object);
            }
            else
            {
                creations.Throws(failure);
            }
            creations.Returns(healthySdk);
            var factory = CreateFactory(sdkFactory.Object);

            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                factory.CreateEventClient(kConnectionString, out _)));
            brokenSdk.Verify(s => s.DisposeAsync(),
                failEventSubscription ? Times.Once() : Times.Never());
            brokenSdk.VerifyAdd(s => s.ConnectionStateChanged +=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>(),
                failEventSubscription ? Times.Once() : Times.Never());
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);

            using var firstLease = factory.CreateEventClient(kConnectionString,
                out var firstClient);
            using var lastLease = factory.CreateEventClient(kConnectionString,
                out var lastClient);
            Assert.Same(firstClient, lastClient);
            await SendAsync(firstClient, "recovered");
            firstLease.Dispose();
            Assert.Equal(0, healthySdk.DisposeCount);
            await SendAsync(lastClient, "shared after recovery");
            AssertTelemetry(healthySdk, "recovered", "shared after recovery");
            await Assert.IsAssignableFrom<IAsyncDisposable>(lastLease).DisposeAsync();
            Assert.Equal(1, healthySdk.DisposeCount);
            brokenSdk.Verify(s => s.DisposeAsync(),
                failEventSubscription ? Times.Once() : Times.Never());
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TransportConstructionFailureDisposesSdkAndAllowsRetryAsync()
        {
            var failure = new InvalidOperationException("Transport logger construction failed.");
            var loggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
            loggerFactory.SetupSequence(f => f.CreateLogger(It.IsAny<string>()))
                .Throws(failure)
                .Returns(NullLogger.Instance);
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var factory = new IoTHubEventClientFactory(
                Options.Create(new IoTEdgeClientOptions()), [],
                loggerFactory.Object, sdkFactory.Object);

            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                factory.CreateEventClient(kConnectionString, out _)));
            var failedSdk = Assert.Single(sdks);
            Assert.Equal(1, failedSdk.DisposeCount);
            Assert.Equal(0, failedSdk.ConnectCount);
            Assert.Empty(failedSdk.Telemetry);

            using var firstLease = factory.CreateEventClient(kConnectionString,
                out var firstClient);
            using var lastLease = factory.CreateEventClient(kConnectionString,
                out var lastClient);
            Assert.Same(firstClient, lastClient);
            var clients = sdks.ToArray();
            Assert.Equal(2, clients.Length);
            firstLease.Dispose();
            Assert.Equal(0, clients[1].DisposeCount);
            await SendAsync(lastClient, "recovered after transport construction failed");
            AssertTelemetry(clients[1], "recovered after transport construction failed");
            lastLease.Dispose();
            Assert.Equal(1, failedSdk.DisposeCount);
            Assert.Equal(1, clients[1].DisposeCount);
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Exactly(2));
            loggerFactory.Verify(f => f.CreateLogger(It.IsAny<string>()), Times.Exactly(2));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FinalDisposalJoinsAllCallersAndReservesIdentityUntilBothClosesFinishAsync(
            bool startSynchronously)
        {
            var transportStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var transportGate = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var sdkStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var sdkGate = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var order = new ConcurrentQueue<string>();
            var sdk = CreateDisposableSdk();
            sdk.Setup(s => s.SetMethodHandlerAsync(null, It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    order.Enqueue("transport");
                    transportStarted.SetResult();
                })
                .Returns(transportGate.Task);
            sdk.Setup(s => s.DisposeAsync())
                .Callback(() =>
                {
                    order.Enqueue("sdk");
                    sdkStarted.SetResult();
                })
                .Returns(() => new ValueTask(sdkGate.Task));
            var state = new Mock<IIoTEdgeClientState>(MockBehavior.Strict);
            state.Setup(s => s.OnClosed(0, "device", "module", "Disposed"));
            var replacementSdk = new IoTEdgeTestModuleClient();
            var sdkFactory = new Mock<IIoTHubModuleClientFactory>(MockBehavior.Strict);
            sdkFactory.SetupSequence(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()))
                .Returns(sdk.Object)
                .Returns(replacementSdk);
            var factory = CreateFactory(sdkFactory.Object, null, state.Object);
            var lease = factory.CreateEventClient(kConnectionString, out var client);
            var asyncLease = Assert.IsAssignableFrom<IAsyncDisposable>(lease);
            Task? synchronousDisposal = null;
            if (startSynchronously)
            {
                synchronousDisposal = Task.Run(() =>
                {
                    lease.Dispose();
                    Assert.True(sdkGate.Task.IsCompletedSuccessfully);
                });
                await transportStarted.Task;
            }
            var disposal = asyncLease.DisposeAsync().AsTask();
            try
            {
                Assert.True(transportStarted.Task.IsCompletedSuccessfully);
                Assert.False(disposal.IsCompleted);
                Assert.Same(disposal, asyncLease.DisposeAsync().AsTask());
                Assert.Equal(["transport"], order);
                sdk.Verify(s => s.DisposeAsync(), Times.Never);
                state.Verify(s => s.OnClosed(0, "device", "module", "Disposed"), Times.Never);
                Assert.Throws<ObjectDisposedException>(() => client.CreateEvent());
                Assert.Throws<InvalidOperationException>(() =>
                    factory.CreateEventClient(kConnectionString, out _));
                Assert.Throws<InvalidOperationException>(() =>
                    factory.CreateEventClient(
                        kIdentity + "SharedAccessKey=cm90YXRlZGtleQ==", out _));

                transportGate.SetResult();
                await sdkStarted.Task;
                Assert.Equal(["transport", "sdk"], order);
                Assert.False(disposal.IsCompleted);
                Assert.Same(disposal, asyncLease.DisposeAsync().AsTask());
                if (!startSynchronously)
                {
                    var synchronousStarted = new TaskCompletionSource(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    synchronousDisposal = Task.Run(() =>
                    {
                        synchronousStarted.SetResult();
                        lease.Dispose();
                        Assert.True(sdkGate.Task.IsCompletedSuccessfully);
                    });
                    await synchronousStarted.Task;
                }
                Assert.False(synchronousDisposal!.IsCompleted);
                Assert.Throws<InvalidOperationException>(() =>
                    factory.CreateEventClient(kConnectionString, out _));
                state.Verify(s => s.OnClosed(0, "device", "module", "Disposed"), Times.Never);
                sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                    It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
                sdk.Verify(s => s.DisposeAsync(), Times.Once);

                sdkGate.SetResult();
                await Task.WhenAll(disposal, synchronousDisposal);
            }
            finally
            {
                transportGate.TrySetResult();
                sdkGate.TrySetResult();
                await disposal;
                if (synchronousDisposal != null)
                {
                    await synchronousDisposal;
                }
            }

            lease.Dispose();
            Assert.Same(disposal, asyncLease.DisposeAsync().AsTask());
            state.Verify(s => s.OnClosed(0, "device", "module", "Disposed"), Times.Once);
            sdk.Verify(s => s.SetMethodHandlerAsync(null, It.IsAny<CancellationToken>()),
                Times.Once);
            sdk.VerifyRemove(s => s.ConnectionStateChanged -=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>(), Times.Once);
            sdk.Verify(s => s.DisposeAsync(), Times.Once);

            using var replacementLease = factory.CreateEventClient(
                kIdentity + "SharedAccessKey=cm90YXRlZGtleQ==", out var replacementClient);
            Assert.NotSame(client, replacementClient);
            await SendAsync(replacementClient, "after complete shutdown");
            AssertTelemetry(replacementSdk, "after complete shutdown");
            replacementLease.Dispose();
            Assert.Equal(1, replacementSdk.DisposeCount);
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Exactly(2));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FailedOrCanceledFinalDisposalIsCachedAndKeepsIdentityReservedAsync(
            bool cancelDisposal)
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var failure = new InvalidOperationException("SDK close failed.");
            var sdk = CreateDisposableSdk();
            sdk.Setup(s => s.DisposeAsync()).Returns(() => new ValueTask(cancelDisposal
                ? Task.FromCanceled(cancellation.Token) : Task.FromException(failure)));
            var otherSdk = new IoTEdgeTestModuleClient();
            var sdkFactory = new Mock<IIoTHubModuleClientFactory>(MockBehavior.Strict);
            sdkFactory.SetupSequence(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()))
                .Returns(sdk.Object)
                .Returns(otherSdk);
            var factory = CreateFactory(sdkFactory.Object);
            var firstLease = factory.CreateEventClient(kConnectionString, out var firstClient);
            var lastLease = factory.CreateEventClient(kConnectionString, out var lastClient);
            Assert.Same(firstClient, lastClient);
            await Assert.IsAssignableFrom<IAsyncDisposable>(firstLease).DisposeAsync();
            sdk.Verify(s => s.DisposeAsync(), Times.Never);

            var asyncLease = Assert.IsAssignableFrom<IAsyncDisposable>(lastLease);
            var disposal = asyncLease.DisposeAsync().AsTask();
            AssertFailure(await Record.ExceptionAsync(() => disposal));
            var repeatedDisposal = asyncLease.DisposeAsync().AsTask();
            Assert.Same(disposal, repeatedDisposal);
            AssertFailure(await Record.ExceptionAsync(() => repeatedDisposal));
            AssertFailure(Record.Exception(lastLease.Dispose));
            Assert.Equal(cancelDisposal, disposal.IsCanceled);
            Assert.Equal(!cancelDisposal, disposal.IsFaulted);
            firstLease.Dispose();
            await Assert.IsAssignableFrom<IAsyncDisposable>(firstLease).DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() => firstClient.CreateEvent());
            Assert.Throws<InvalidOperationException>(() =>
                factory.CreateEventClient(kConnectionString, out _));
            Assert.Throws<InvalidOperationException>(() =>
                factory.CreateEventClient(kIdentity + "SharedAccessKey=cm90YXRlZGtleQ==", out _));
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
            sdk.Verify(s => s.SetMethodHandlerAsync(null, It.IsAny<CancellationToken>()),
                Times.Once);
            sdk.VerifyRemove(s => s.ConnectionStateChanged -=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>(), Times.Once);
            sdk.Verify(s => s.DisposeAsync(), Times.Once);

            // A poisoned identity must not poison unrelated identities.
            using var otherLease = factory.CreateEventClient(
                "HostName=test.azure-devices.net;DeviceId=other;SharedAccessKey=ZmFrZWtleQ==",
                out var otherClient);
            await SendAsync(otherClient, "unrelated identity");
            AssertTelemetry(otherSdk, "unrelated identity");
            otherLease.Dispose();
            Assert.Equal(1, otherSdk.DisposeCount);
            sdk.Verify(s => s.DisposeAsync(), Times.Once);
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Exactly(2));

            void AssertFailure(Exception? error)
            {
                if (cancelDisposal)
                {
                    var canceled = Assert.IsAssignableFrom<OperationCanceledException>(error);
                    Assert.Equal(cancellation.Token, canceled.CancellationToken);
                }
                else
                {
                    Assert.Same(failure, error);
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t")]
        [InlineData("credential-sentinel")]
        [InlineData(kConnectionString + ";credential-sentinel")]
        [InlineData(kConnectionString + ";=credential-sentinel")]
        [InlineData(kConnectionString + ";Unknown=credential-sentinel")]
        [InlineData(kConnectionString + ";HOSTNAME=credential-sentinel")]
        [InlineData(kConnectionString + ";HOSTNAME=test.azure-devices.net")]
        [InlineData(kConnectionString + ";DeviceId=credential-sentinel")]
        [InlineData(kConnectionString + ";MODULEID=credential-sentinel")]
        [InlineData(kConnectionString + ";sharedaccesskey=credential-sentinel")]
        [InlineData(kConnectionString + ";SharedAccessKey=ZmFrZWtleQ==")]
        [InlineData(kConnectionString + ";GatewayHostName=one;gatewayhostname=two")]
        [InlineData(kConnectionString + ";SharedAccessKeyName=one;SHAREDACCESSKEYNAME=two")]
        [InlineData("DeviceId=device;SharedAccessKey=credential-sentinel")]
        [InlineData("HostName=test.azure-devices.net;SharedAccessKey=credential-sentinel")]
        [InlineData("HostName=;DeviceId=device;SharedAccessKey=credential-sentinel")]
        [InlineData("HostName=test.azure-devices.net;DeviceId=;SharedAccessKey=credential-sentinel")]
        [InlineData("HostName=test.azure-devices.net;DeviceId= \t;SharedAccessKey=credential-sentinel")]
        [InlineData("HostName=test.azure-devices.net;DeviceId=device;ModuleId=;" +
            "SharedAccessKey=credential-sentinel")]
        [InlineData("HostName=test.azure-devices.net;DeviceId=device;ModuleId= \t;" +
            "SharedAccessKey=credential-sentinel")]
        [InlineData("HostName=https://credential-sentinel/;DeviceId=device;" +
            "SharedAccessKey=ZmFrZWtleQ==")]
        [InlineData(kConnectionString + ";GatewayHostName=https://credential-sentinel/")]
        [InlineData(kIdentity)]
        [InlineData(kIdentity + "SharedAccessKey=")]
        [InlineData(kIdentity + "SharedAccessSignature=")]
        [InlineData(kIdentity + "SharedAccessKey=credential-sentinel")]
        [InlineData(kConnectionString + ";X509=true")]
        [InlineData(kConnectionString + ";SharedAccessSignature=" + kSasToken)]
        [InlineData(kIdentity + "X509=true;SharedAccessSignature=" + kSasToken)]
        [InlineData(kIdentity + "X509=credential-sentinel")]
        [InlineData(kIdentity + "X509=false")]
        [InlineData(kIdentity + "SharedAccessSignature=" + kSasToken +
            ";SHAREDACCESSSIGNATURE=" + kOtherSasToken)]
        [InlineData(kIdentity + "X509=true;x509=false")]
        public async Task InvalidConnectionStringsAreSanitizedBeforeSdkCreationAsync(
            string? connectionString)
        {
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var loggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
            var factory = new IoTHubEventClientFactory(
                Options.Create(new IoTEdgeClientOptions()), [],
                loggerFactory.Object, sdkFactory.Object);

            var error = Assert.ThrowsAny<ArgumentException>(() =>
                factory.CreateEventClient(connectionString!, out _));
            AssertSanitized(error);
            Assert.Empty(sdks);
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Never);
            loggerFactory.VerifyNoOtherCalls();

            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                .Returns(NullLogger.Instance);
            using var lease = factory.CreateEventClient(kConnectionString, out var client);
            var sdk = Assert.Single(sdks);
            await SendAsync(client, "valid after invalid");
            AssertTelemetry(sdk, "valid after invalid");
            lease.Dispose();
            Assert.Equal(1, sdk.DisposeCount);
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
        }

        [Theory]
        [InlineData("SharedAccessKey= ZmFr ZWtl eQ== ;SharedAccessKeyName= policy ;X509=false")]
        [InlineData("SharedAccessSignature= " + kSasToken + " ")]
        [InlineData("X509=true")]
        public async Task SupportedConnectionValuesUseSdkIdentityNormalizationAsync(
            string credentials)
        {
            var connectionString = "HostName=test.azure-devices.net;" +
                "DeviceId= device ;ModuleId= module ;" +
                "GatewayHostName=GATEWAY.EXAMPLE.NET.;" + credentials;
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var state = new Mock<IIoTEdgeClientState>(MockBehavior.Strict);
            state.Setup(s => s.OnOpened(1, "device", "module"));
            state.Setup(s => s.OnClosed(1, "device", "module", "Disposed"));
            var factory = CreateFactory(sdkFactory.Object, null, state.Object);
            using var lease = factory.CreateEventClient(connectionString, out var client);
            var sdk = Assert.Single(sdks);

            Assert.Equal("device/module", client.Identity);
            Assert.Equal("GATEWAY.EXAMPLE.NET.",
                Assert.IsAssignableFrom<IProcessIdentity>(client).Identity);
            sdkFactory.Verify(f => f.Create(It.Is<IoTEdgeClientOptions>(o =>
                o.EdgeHubConnectionString == connectionString),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
            await SendAsync(client, "verbatim identity");
            sdk.RaiseStateChanged(IoTHubConnectionState.Connected);
            AssertTelemetry(sdk, "verbatim identity");
            lease.Dispose();
            Assert.Equal(1, sdk.DisposeCount);
            state.Verify(s => s.OnOpened(1, "device", "module"), Times.Once);
            state.Verify(s => s.OnClosed(1, "device", "module", "Disposed"), Times.Once);
            state.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task PaddedSasCredentialsShareTheExistingIdentityAsync()
        {
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var factory = CreateFactory(sdkFactory.Object);
            using var first = factory.CreateEventClient(
                kIdentity + "SharedAccessSignature= " + kSasToken + " ", out var firstClient);
            using var second = factory.CreateEventClient(
                kIdentity + "SharedAccessSignature=" + kSasToken, out var secondClient);

            Assert.Same(firstClient, secondClient);
            var sdk = Assert.Single(sdks);
            await SendAsync(firstClient, "first");
            first.Dispose();
            Assert.Equal(0, sdk.DisposeCount);
            await SendAsync(secondClient, "second");
            AssertTelemetry(sdk, "first", "second");
            second.Dispose();
            Assert.Equal(1, sdk.DisposeCount);
            sdkFactory.Verify(instance => instance.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
        }

        [Theory]
        [InlineData("SharedAccessKey=ZmFrZWtleQ==")]
        [InlineData("SharedAccessSignature=" + kSasToken)]
        [InlineData("X509=true")]
        public void ExplicitIdentitySupportsSdkCredentialsWithoutEnvironmentFallback(
            string credentials)
        {
            var identity = new IoTEdgeIdentity(Options.Create(new IoTEdgeClientOptions
            {
                EdgeHubConnectionString = "HostName= explicit.azure-devices.net ;" +
                    "DeviceId= explicit-device ;ModuleId= explicit-module ;" +
                    "GatewayHostName= explicit-gateway ;" + credentials
            }), NullLogger<IoTEdgeIdentity>.Instance);

            Assert.Equal("explicit.azure-devices.net", identity.Hub);
            Assert.Equal("explicit-device", identity.DeviceId);
            Assert.Equal("explicit-module", identity.ModuleId);
            Assert.Equal("explicit-gateway", identity.Gateway);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EquivalentGlobalTransportIsBorrowedAndNeverDisposedByGroupLeasesAsync(
            bool usePublicConstructor)
        {
            var options = Options.Create(new IoTEdgeClientOptions
            {
                EdgeHubConnectionString = kConnectionString + ";GatewayHostName=gateway.example.net",
                Product = "product",
                KeepAlivePeriodSeconds = 42,
                DefaultMethodCallTimeout = TimeSpan.FromSeconds(7)
            });
            var identity = new IoTEdgeIdentity("TEST.AZURE-DEVICES.NET.",
                "device", "module", "gateway.example.net");
            var globalSdk = new IoTEdgeTestModuleClient();
            await using var globalModule = new IoTEdgeModuleClient(
                options, identity, [], NullLoggerFactory.Instance,
                new IoTEdgeTestModuleClientFactory(globalSdk));
            await using var globalTransport = new IoTEdgeTransport(
                globalModule, NullLogger<IoTEdgeTransport>.Instance);
            var sdkFactory = new Mock<IIoTHubModuleClientFactory>(MockBehavior.Strict);
            var factory = usePublicConstructor
                ? new IoTHubEventClientFactory(options, [], NullLoggerFactory.Instance,
                    globalTransport, identity)
                : new IoTHubEventClientFactory(options, [], NullLoggerFactory.Instance,
                    sdkFactory.Object, globalTransport, identity);
            using var firstLease = factory.CreateEventClient(
                "sharedaccesskey= ZmFr ZWtl eQ== ;MODULEID=module;deviceid=device;" +
                "hostname=TEST.AZURE-DEVICES.NET;gatewayhostname=GATEWAY.EXAMPLE.NET.",
                out var firstClient);
            using var secondLease = factory.CreateEventClient(
                options.Value.EdgeHubConnectionString, out var secondClient);

            Assert.Same(globalTransport, firstClient);
            Assert.Same(globalTransport, secondClient);
            var error = Assert.Throws<InvalidOperationException>(() =>
                factory.CreateEventClient(kIdentity + "SharedAccessKey=cm90YXRlZGtleQ==" +
                    ";GatewayHostName=gateway.example.net", out _));
            AssertSanitized(error);
            await SendAsync(firstClient, "borrowed first");
            firstLease.Dispose();
            await Assert.IsAssignableFrom<IAsyncDisposable>(firstLease).DisposeAsync();
            await Assert.IsAssignableFrom<IAsyncDisposable>(secondLease).DisposeAsync();
            secondLease.Dispose();
            Assert.Equal(0, globalSdk.DisposeCount);
            await SendAsync(globalTransport, "global survives group releases");
            AssertTelemetry(globalSdk, "borrowed first", "global survives group releases");

            using var laterLease = factory.CreateEventClient(
                options.Value.EdgeHubConnectionString, out var laterClient);
            Assert.Same(globalTransport, laterClient);
            laterLease.Dispose();
            Assert.Equal(0, globalSdk.DisposeCount);
            sdkFactory.VerifyNoOtherCalls();

            await globalTransport.DisposeAsync();
            Assert.Throws<ObjectDisposedException>(() => globalTransport.CreateEvent());
            Assert.Equal(0, globalSdk.DisposeCount);
            await globalModule.DisposeAsync();
            Assert.Equal(1, globalSdk.DisposeCount);
        }

        [Theory]
        [InlineData("SharedAccessKey=ZmFrZWtleQ==")]
        [InlineData("SharedAccessSignature=" + kSasToken)]
        public async Task ExplicitGlobalSdkIdentityPreventsCompetingWriterGroupClientAsync(
            string credentials)
        {
            var options = Options.Create(new IoTEdgeClientOptions
            {
                EdgeHubConnectionString = kIdentity + credentials
            });
            var reportedIdentity = new IoTEdgeIdentity("environment.azure-devices.net",
                "environment-device", "environment-module", null);
            var globalSdk = new IoTEdgeTestModuleClient();
            await using var globalModule = new IoTEdgeModuleClient(options, reportedIdentity,
                [], NullLoggerFactory.Instance, new IoTEdgeTestModuleClientFactory(globalSdk));
            await using var global = new IoTEdgeTransport(globalModule,
                NullLogger<IoTEdgeTransport>.Instance);
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var factory = new IoTHubEventClientFactory(options, [],
                NullLoggerFactory.Instance, sdkFactory.Object, global, reportedIdentity);
            using var first = factory.CreateEventClient(options.Value.EdgeHubConnectionString,
                out var firstClient);
            using var second = factory.CreateEventClient(options.Value.EdgeHubConnectionString,
                out var secondClient);

            Assert.Same(global, firstClient);
            Assert.Same(global, secondClient);
            Assert.Empty(sdks);
            first.Dispose();
            second.Dispose();
            Assert.Equal(0, globalSdk.DisposeCount);
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Never);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DefaultTransportAndIdentityMustBeProvidedTogetherAsync(
            bool includeTransport)
        {
            var options = Options.Create(new IoTEdgeClientOptions
            {
                EdgeHubConnectionString = kConnectionString
            });
            var identity = new IoTEdgeIdentity(
                "test.azure-devices.net", "device", "module", null);
            var globalSdk = new IoTEdgeTestModuleClient();
            await using var globalModule = new IoTEdgeModuleClient(
                options, identity, [], NullLoggerFactory.Instance,
                new IoTEdgeTestModuleClientFactory(globalSdk));
            await using var globalTransport = new IoTEdgeTransport(
                globalModule, NullLogger<IoTEdgeTransport>.Instance);

            Assert.Throws<ArgumentException>(() => new IoTHubEventClientFactory(
                options, [], NullLoggerFactory.Instance,
                includeTransport ? globalTransport : null,
                includeTransport ? null : identity));
            Assert.Equal(0, globalSdk.DisposeCount);
            await SendAsync(globalTransport, "global survives invalid constructor arguments");
            AssertTelemetry(globalSdk, "global survives invalid constructor arguments");
            await globalTransport.DisposeAsync();
            await globalModule.DisposeAsync();
            Assert.Equal(1, globalSdk.DisposeCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(kIdentity + "SharedAccessKey=cm90YXRlZGtleQ==")]
        public async Task WorkloadOrConflictingGlobalCredentialsRefuseACompetingClientAsync(
            string? globalConnectionString)
        {
            var options = Options.Create(new IoTEdgeClientOptions
            {
                EdgeHubConnectionString = globalConnectionString
            });
            var identity = new IoTEdgeIdentity(
                "TEST.AZURE-DEVICES.NET.", "device", "module", null);
            var globalSdk = new IoTEdgeTestModuleClient();
            await using var globalModule = new IoTEdgeModuleClient(
                options, identity, [], NullLoggerFactory.Instance,
                new IoTEdgeTestModuleClientFactory(globalSdk));
            await using var globalTransport = new IoTEdgeTransport(
                globalModule, NullLogger<IoTEdgeTransport>.Instance);
            var sdkFactory = new Mock<IIoTHubModuleClientFactory>(MockBehavior.Strict);
            var factory = new IoTHubEventClientFactory(
                options, [], NullLoggerFactory.Instance, sdkFactory.Object,
                globalTransport, identity);

            var error = Assert.Throws<InvalidOperationException>(() =>
                factory.CreateEventClient(kConnectionString, out _));
            AssertSanitized(error);
            sdkFactory.VerifyNoOtherCalls();
            Assert.Equal(0, globalSdk.DisposeCount);
            await SendAsync(globalTransport, "global still usable");
            AssertTelemetry(globalSdk, "global still usable");
            await globalTransport.DisposeAsync();
            await globalModule.DisposeAsync();
            Assert.Equal(1, globalSdk.DisposeCount);
        }

        [Fact]
        public async Task DifferentGlobalIdentityDoesNotPreventAnIndependentlyOwnedClientAsync()
        {
            var options = Options.Create(new IoTEdgeClientOptions());
            var identity = new IoTEdgeIdentity(
                "test.azure-devices.net", "other", "module", null);
            var globalSdk = new IoTEdgeTestModuleClient();
            await using var globalModule = new IoTEdgeModuleClient(
                options, identity, [], NullLoggerFactory.Instance,
                new IoTEdgeTestModuleClientFactory(globalSdk));
            await using var globalTransport = new IoTEdgeTransport(
                globalModule, NullLogger<IoTEdgeTransport>.Instance);
            var sdks = new ConcurrentQueue<IoTEdgeTestModuleClient>();
            var sdkFactory = CreateSdkFactory(sdks);
            var factory = new IoTHubEventClientFactory(
                options, [], NullLoggerFactory.Instance, sdkFactory.Object,
                globalTransport, identity);

            using var lease = factory.CreateEventClient(kConnectionString, out var client);
            Assert.NotSame(globalTransport, client);
            var sdk = Assert.Single(sdks);
            await SendAsync(client, "owned group client");
            AssertTelemetry(sdk, "owned group client");
            lease.Dispose();
            Assert.Equal(1, sdk.DisposeCount);
            Assert.Equal(0, globalSdk.DisposeCount);
            await SendAsync(globalTransport, "global identity survives");
            AssertTelemetry(globalSdk, "global identity survives");
            sdkFactory.Verify(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()), Times.Once);
            await globalTransport.DisposeAsync();
            await globalModule.DisposeAsync();
            Assert.Equal(1, globalSdk.DisposeCount);
        }

        [Fact]
        public void RejectsEmptyConnectionString()
        {
            var factory = new IoTHubEventClientFactory(
                Options.Create(new IoTEdgeClientOptions()),
                [],
                NullLoggerFactory.Instance,
                new IoTEdgeTestModuleClientFactory(
                    new IoTEdgeTestModuleClient()));

            Assert.Throws<ArgumentException>(() =>
                factory.CreateEventClient(string.Empty, out _));
        }

        [Fact]
        public async Task RealFactoryAcceptsDeviceConnectionStringAsync()
        {
            var client = IoTHubModuleClientFactory.Instance.Create(
                new IoTEdgeClientOptions
                {
                    EdgeHubConnectionString =
                        "HostName=test.azure-devices.net;DeviceId=child;" +
                        "SharedAccessKey=ZmFrZWtleQ=="
                },
                _ => { });
            await using (client)
            {
                Assert.IsType<IoTHubModuleClientFactory.DeviceAdapter>(client);
                Assert.Equal(IoTHubConnectionState.Disconnected, client.State);
            }
        }

        [Fact]
        public void RealFactoryRoutesX509DeviceConnectionStringToDeviceClient()
        {
            var error = Assert.Throws<InvalidOperationException>(() =>
                IoTHubModuleClientFactory.Instance.Create(
                    new IoTEdgeClientOptions
                    {
                        EdgeHubConnectionString =
                            "HostName=test.azure-devices.net;DeviceId=child;" +
                            "X509=true"
                    },
                    _ => { }));

            Assert.Contains("ClientCertificate", error.Message,
                StringComparison.Ordinal);
        }

        [Fact]
        public async Task RealFactoryAcceptsModuleConnectionStringAsync()
        {
            var client = IoTHubModuleClientFactory.Instance.Create(
                new IoTEdgeClientOptions
                {
                    EdgeHubConnectionString =
                        "HostName=test.azure-devices.net;DeviceId=device;" +
                        "ModuleId=module;SharedAccessKey=ZmFrZWtleQ=="
                },
                _ => { });
            await using (client)
            {
                Assert.IsType<IoTHubModuleClientFactory.Adapter>(client);
            }
        }

        private static IoTHubEventClientFactory CreateFactory(
            IIoTHubModuleClientFactory sdkFactory,
            IoTEdgeClientOptions? options = null,
            params IIoTEdgeClientState[] stateHandlers)
        {
            return new IoTHubEventClientFactory(
                Options.Create(options ?? new IoTEdgeClientOptions()),
                stateHandlers, NullLoggerFactory.Instance, sdkFactory);
        }

        private static Mock<IIoTHubModuleClientFactory> CreateSdkFactory(
            ConcurrentQueue<IoTEdgeTestModuleClient> sdks)
        {
            var factory = new Mock<IIoTHubModuleClientFactory>(MockBehavior.Strict);
            factory.Setup(f => f.Create(It.IsAny<IoTEdgeClientOptions>(),
                It.IsAny<Action<IoTHubClientOptions>>()))
                .Returns((IoTEdgeClientOptions _, Action<IoTHubClientOptions> configure) =>
                {
                    configure(new IoTHubClientOptions());
                    var sdk = new IoTEdgeTestModuleClient();
                    sdks.Enqueue(sdk);
                    return sdk;
                });
            return factory;
        }

        private static Mock<IIoTHubModuleClient> CreateDisposableSdk()
        {
            var sdk = new Mock<IIoTHubModuleClient>(MockBehavior.Strict);
            sdk.SetupGet(s => s.State).Returns(IoTHubConnectionState.Disconnected);
            sdk.SetupAdd(s => s.ConnectionStateChanged +=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>());
            sdk.SetupRemove(s => s.ConnectionStateChanged -=
                It.IsAny<EventHandler<IoTHubConnectionStateChangedEventArgs>>());
            sdk.Setup(s => s.SetMethodHandlerAsync(null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            sdk.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);
            return sdk;
        }

        private static async Task SendAsync(IEventClient client, string payload)
        {
            using var @event = client.CreateEvent()
                .SetContentType("application/json")
                .AddBuffers([
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(payload))
                ]);
            await @event.SendAsync();
        }

        private static void AssertTelemetry(IoTEdgeTestModuleClient sdk,
            params string[] payloads)
        {
            Assert.Equal(1, sdk.ConnectCount);
            Assert.Equal(payloads, sdk.Telemetry.Select(message =>
                Encoding.UTF8.GetString(message.Payload.ToArray())));
            Assert.All(sdk.Telemetry, message =>
                Assert.Equal("application/json", message.ContentType));
            Assert.Empty(sdk.OutputTelemetry);
        }

        private static void AssertSanitized(Exception error)
        {
            Assert.Null(error.InnerException);
            Assert.DoesNotContain("ZmFrZWtleQ==", error.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("cm90YXRlZGtleQ==", error.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("credential-sentinel", error.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(kSasToken, error.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(kOtherSasToken, error.ToString(), StringComparison.Ordinal);
        }

        private const string kIdentity =
            "HostName=test.azure-devices.net;DeviceId=device;ModuleId=module;";
        private const string kConnectionString = kIdentity + "SharedAccessKey=ZmFrZWtleQ==";
        private const string kSasToken = "SharedAccessSignature " +
            "sr=test.azure-devices.net%2Fdevices%2Fdevice%2Fmodules%2Fmodule" +
            "&sig=credential-sentinel-one&se=4102444800";
        private const string kOtherSasToken = "SharedAccessSignature " +
            "sr=test.azure-devices.net%2Fdevices%2Fdevice%2Fmodules%2Fmodule" +
            "&sig=credential-sentinel-two&se=4102444800";
    }
}
