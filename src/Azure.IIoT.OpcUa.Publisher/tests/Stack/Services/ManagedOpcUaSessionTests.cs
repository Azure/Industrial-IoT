// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.Subscriptions;
    using Opc.Ua.Client.Subscriptions.MonitoredItems;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using OpcUaClientOptions = Azure.IIoT.OpcUa.Publisher.Stack.OpcUaClientOptions;
    using ManagedMonitoredItemOptions = Opc.Ua.Client.Subscriptions.MonitoredItems.MonitoredItemOptions;

    /// <summary>
    /// Characterization tests for the managed session composition seam.
    /// </summary>
    public sealed class ManagedOpcUaSessionTests
    {
        /// <summary>
        /// The facade exposes the managed session context, endpoint identity, and codec.
        /// </summary>
        [Fact]
        public async Task FacadeExposesManagedSessionIdentityAndCodecAsync()
        {
            var session = CreateSession(out var endpoint, out var identity);
            var connection = new FakeConnection(session.Object);
            var facade = new ManagedOpcUaSession(connection, CreateTelemetry());
            try
            {
                Assert.Same(endpoint, facade.Endpoint);
                Assert.Same(identity, facade.Identity);
                Assert.Same(session.Object.MessageContext, facade.MessageContext);
                Assert.NotNull(facade.Codec);
                Assert.NotNull(facade.LruNodeCache);
                Assert.Same(facade, facade.Services);
            }
            finally
            {
                await facade.DisposeAsync();
            }
        }

        /// <summary>
        /// The facade forwards a representative service call and its cancellation token.
        /// </summary>
        [Fact]
        public async Task FacadeDelegatesReadServiceToManagedSessionAsync()
        {
            var session = CreateSession(out _, out _);
            var expected = new ReadResponse();
            var request = new RequestHeader();
            var nodes = new ReadValueIdCollection
            {
                new()
                {
                    NodeId = ObjectIds.Server,
                    AttributeId = Attributes.Value
                }
            };
            using var cts = new CancellationTokenSource();
            session.Setup(s => s.ReadAsync(request, 0, Opc.Ua.TimestampsToReturn.Both,
                    It.Is<ArrayOf<ReadValueId>>(items =>
                        items.Count == 1 && items[0].NodeId == ObjectIds.Server),
                    cts.Token))
                .Returns(ValueTask.FromResult(expected));
            var connection = new FakeConnection(session.Object);
            await using var facade = new ManagedOpcUaSession(connection, CreateTelemetry());

            var result = await facade.Services.ReadAsync(request, 0,
                Opc.Ua.TimestampsToReturn.Both, nodes, cts.Token);

            Assert.Same(expected, result);
            session.Verify(s => s.ReadAsync(request, 0, Opc.Ua.TimestampsToReturn.Both,
                It.Is<ArrayOf<ReadValueId>>(items =>
                    items.Count == 1 && items[0].NodeId == ObjectIds.Server),
                cts.Token), Times.Once);
        }

        /// <summary>
        /// Operation limits and diagnostics are projected from the managed session.
        /// </summary>
        [Fact]
        public async Task FacadeProjectsManagedSessionDiagnosticsAndLimitsAsync()
        {
            var session = CreateSession(out _, out _);
            session.SetupGet(s => s.OperationLimits).Returns(new OperationLimits
            {
                MaxNodesPerRead = 11,
                MaxNodesPerBrowse = 12,
                MaxMonitoredItemsPerCall = 13
            });
            session.SetupGet(s => s.SessionId).Returns(new NodeId(123u, 2));
            session.SetupGet(s => s.SessionName).Returns("managed");
            session.SetupGet(s => s.SessionTimeout).Returns(2000);
            session.SetupGet(s => s.LastKeepAliveTime).Returns(DateTime.UnixEpoch);
            session.SetupGet(s => s.SubscriptionCount).Returns(4);
            session.SetupGet(s => s.OutstandingRequestCount).Returns(5);
            SetupOperationLimitsRead(session);
            var connection = new FakeConnection(session.Object);
            await using var facade = new ManagedOpcUaSession(connection, CreateTelemetry());

            var limits = await facade.GetOperationLimitsAsync();
            var diagnostics = await facade.GetServerDiagnosticAsync();

            Assert.Equal(11u, limits.MaxNodesPerRead);
            Assert.Equal(12u, limits.MaxNodesPerBrowse);
            Assert.Equal(13u, limits.MaxMonitoredItemsPerCall);
            Assert.Equal("managed", diagnostics.SessionName);
            Assert.Equal(2000, diagnostics.ActualSessionTimeout);
            Assert.Equal(4u, diagnostics.CurrentSubscriptionsCount);
            Assert.Equal(5u, diagnostics.CurrentPublishRequestsInQueue);
        }

        /// <summary>
        /// Operation limit values retain the server's small encoding and continuation limits.
        /// </summary>
        [Fact]
        public async Task FacadeMapsAllOperationLimitValuesAsync()
        {
            var session = CreateSession(out _, out _);
            session.SetupGet(s => s.OperationLimits).Returns(new OperationLimits
            {
                MaxNodesPerRead = 25,
                MaxNodesPerBrowse = 26,
                MaxNodesPerWrite = 27
            });
            SetupOperationLimitsRead(session);
            var connection = new FakeConnection(session.Object);
            await using var facade = new ManagedOpcUaSession(connection, CreateTelemetry());

            var limits = await facade.GetOperationLimitsAsync();

            Assert.Equal(32u, limits.MaxArrayLength);
            Assert.Equal((ushort?)2, limits.MaxBrowseContinuationPoints);
            Assert.Equal(8u, limits.MaxByteStringLength);
            Assert.Equal((ushort?)3, limits.MaxHistoryContinuationPoints);
            Assert.Equal((ushort?)4, limits.MaxQueryContinuationPoints);
            Assert.Equal(16u, limits.MaxStringLength);
            Assert.Equal(0.5, limits.MinSupportedSampleRate);
            Assert.Equal(25u, limits.MaxNodesPerRead);
            Assert.Equal(26u, limits.MaxNodesPerBrowse);
            Assert.Equal(27u, limits.MaxNodesPerWrite);
            Assert.Equal(28u, limits.MaxNodesPerHistoryReadData);
            Assert.Equal(29u, limits.MaxNodesPerHistoryReadEvents);
            Assert.Equal(30u, limits.MaxNodesPerHistoryUpdateData);
            Assert.Equal(31u, limits.MaxNodesPerHistoryUpdateEvents);
            Assert.Equal(33u, limits.MaxNodesPerMethodCall);
            Assert.Equal(34u, limits.MaxNodesPerRegisterNodes);
            Assert.Equal(35u, limits.MaxNodesPerTranslatePathsToNodeIds);
            Assert.Equal(36u, limits.MaxNodesPerNodeManagement);
            Assert.Equal(37u, limits.MaxMonitoredItemsPerCall);
        }

        /// <summary>
        /// State changes map from the public managed session event to Publisher events.
        /// </summary>
        [Fact]
        public async Task FacadeMapsManagedConnectionStateChangesAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            await using var facade = new ManagedOpcUaSession(connection, CreateTelemetry());
            EndpointConnectivityState? observed = null;
            facade.OnConnectionStateChange += (_, args) => observed = args.State;

            connection.Raise(ConnectionState.Connected);
            Assert.Equal(EndpointConnectivityState.Ready, observed);
            Assert.Equal(EndpointConnectivityState.Ready, facade.ConnectivityState);

            connection.Raise(ConnectionState.Closing);
            Assert.Equal(EndpointConnectivityState.Disconnected, observed);
            Assert.Equal(EndpointConnectivityState.Disconnected, facade.ConnectivityState);
        }

        /// <summary>
        /// An already connected managed session is immediately observable as ready.
        /// </summary>
        [Fact]
        public async Task FacadeStartsReadyForConnectedManagedSessionAsync()
        {
            var session = CreateSession(out _, out _, connected: true);
            var connection = new FakeConnection(session.Object);
            await using var facade = new ManagedOpcUaSession(connection, CreateTelemetry());

            var diagnostics = await facade.GetServerDiagnosticAsync();
            EndpointConnectivityState? observed = null;
            facade.OnConnectionStateChange += (_, args) => observed = args.State;

            Assert.Equal(EndpointConnectivityState.Ready, facade.ConnectivityState);
            Assert.Equal(EndpointConnectivityState.Ready, observed);
            Assert.Equal("urn:managed-session-tests", diagnostics.ServerUri);
        }

        /// <summary>
        /// The facade owns and asynchronously disposes its managed inner connection once.
        /// </summary>
        [Fact]
        public async Task FacadeOwnsManagedConnectionDisposalAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var facade = new ManagedOpcUaSession(connection, CreateTelemetry());

            await facade.DisposeAsync();
            await facade.DisposeAsync();

            Assert.Equal(1, connection.DisposeCount);
        }

        /// <summary>
        /// Notification values are never returned to a pool after Publisher dispatch.
        /// </summary>
        [Fact]
        public async Task FacadeDisablesNotificationPoolingAsync()
        {
            var session = CreateSession(out _, out _);
            var subscriptions = new Mock<ISubscriptionManager>();
            subscriptions.SetupProperty(s => s.PoolNotifications, true);
            ISubscriptionManager manager = subscriptions.Object;
            session.Setup(s => s.TryGetSubscriptionManager(out manager)).Returns(true);
            var connection = new FakeConnection(session.Object);

            await using var facade = new ManagedOpcUaSession(connection, CreateTelemetry());

            Assert.False(subscriptions.Object.PoolNotifications);
        }

        /// <summary>
        /// A disconnected managed session does not try to load complex types.
        /// </summary>
        [Fact]
        public async Task FacadeDefersComplexTypeLoadingUntilConnectedAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            await using var facade = new ManagedOpcUaSession(connection, CreateTelemetry());

            var typeSystem = await facade.GetComplexTypeSystemAsync();

            Assert.Null(typeSystem);
        }

        /// <summary>
        /// The pool reuses a connection identity and closes it only after the final lease.
        /// </summary>
        [Fact]
        public async Task PoolSharesConnectionIdentityAndHonorsLeaseOwnershipAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new FakeProvider(connection);
            await using var pool = new ManagedSessionPool(provider, CreateTelemetry(),
                new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.Zero
                });
            var first = CreateRequest("opc.tcp://localhost:4840");
            var equivalent = CreateRequest("opc.tcp://localhost:4840");

            using var lease1 = await pool.AcquireAsync(first);
            using var lease2 = await pool.AcquireAsync(equivalent);
            Assert.Same(lease1.Session, lease2.Session);
            Assert.Equal(1, provider.ConnectCount);

            lease1.Dispose();
            Assert.Equal(0, connection.DisposeCount);

            lease2.Dispose();
            await connection.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(1, connection.DisposeCount);
        }

        /// <summary>
        /// The pool passes connection credentials and cancellation settings to its provider.
        /// </summary>
        [Fact]
        public async Task PoolPassesConnectionInputsToProviderAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new FakeProvider(connection);
            await using var pool = new ManagedSessionPool(provider, CreateTelemetry());
            var identity = new Mock<IUserIdentity>().Object;
            var request = CreateRequest("opc.tcp://localhost:4841") with
            {
                Identity = identity,
                ConnectTimeout = TimeSpan.FromSeconds(7),
                ReverseConnectServerUri = new Uri("urn:managed-session-tests")
            };

            using var lease = await pool.AcquireAsync(request);

            Assert.Same(request, provider.Request);
            Assert.Same(identity, provider.Request!.Identity);
            Assert.Equal(TimeSpan.FromSeconds(7), provider.Request.ConnectTimeout);
            Assert.Equal(request.ReverseConnectServerUri,
                provider.Request.ReverseConnectServerUri);
        }

        /// <summary>
        /// Caller cancellation leaves the shared connection usable by another waiter.
        /// </summary>
        [Fact]
        public async Task PoolCallerCancellationDoesNotTearDownSharedConnectAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new DelayedProvider();
            await using var pool = new ManagedSessionPool(provider, CreateTelemetry(),
                new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.Zero
                });
            var request = CreateRequest("opc.tcp://localhost:4842") with
            {
                ConnectTimeout = TimeSpan.FromSeconds(5)
            };
            using var cancellation = new CancellationTokenSource();

            var canceledAcquire = pool.AcquireAsync(request, cancellation.Token);
            await provider.Started.Task;
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await canceledAcquire);
            Assert.False(provider.ConnectCancellation.IsCancellationRequested);

            var survivingAcquire = pool.AcquireAsync(request);
            provider.Complete(connection);
            using var lease = await survivingAcquire;

            Assert.Equal(1, provider.ConnectCount);
            Assert.Equal(0, connection.DisposeCount);
        }

        /// <summary>
        /// A sole canceled waiter releases a successful late connection after linger.
        /// </summary>
        [Fact]
        public async Task PoolCleansLateConnectionAfterSoleCallerCancellationAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new DelayedProvider();
            await using var pool = new ManagedSessionPool(provider, CreateTelemetry(),
                new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.FromMilliseconds(10)
                });
            var request = CreateRequest("opc.tcp://localhost:4844") with
            {
                ConnectTimeout = TimeSpan.FromSeconds(5)
            };
            using var cancellation = new CancellationTokenSource();

            var canceledAcquire = pool.AcquireAsync(request, cancellation.Token);
            await provider.Started.Task;
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await canceledAcquire);

            provider.Complete(connection);
            await connection.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Equal(1, provider.ConnectCount);
            Assert.Equal(1, connection.DisposeCount);
        }

        /// <summary>
        /// A timed-out shared connect after its sole caller cancels is evicted for retry.
        /// </summary>
        [Fact]
        public async Task PoolEvictsFailedConnectAfterSoleCallerCancellationAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new RetryProvider(connection);
            await using var pool = new ManagedSessionPool(provider, CreateTelemetry());
            var request = CreateRequest("opc.tcp://localhost:4846") with
            {
                ConnectTimeout = TimeSpan.FromMilliseconds(20)
            };
            using var cancellation = new CancellationTokenSource();

            var canceledAcquire = pool.AcquireAsync(request, cancellation.Token);
            await provider.FirstAttemptStarted.Task;
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await canceledAcquire);
            await provider.FirstAttemptCanceled.Task;
            await WaitUntilAsync(() => pool.Count == 0);

            using var lease = await pool.AcquireAsync(request);

            Assert.Equal(2, provider.ConnectCount);
            Assert.NotNull(lease.Session);
        }

        /// <summary>
        /// A provider that ignores cancellation is disposed when it returns after timeout.
        /// </summary>
        [Fact]
        public async Task PoolDisposesLateNonCooperativeConnectionAfterTimeoutAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new DelayedProvider();
            await using var pool = new ManagedSessionPool(provider, CreateTelemetry());
            var request = CreateRequest("opc.tcp://localhost:4843") with
            {
                ConnectTimeout = TimeSpan.FromMilliseconds(10)
            };

            var acquire = pool.AcquireAsync(request);
            await provider.Started.Task;
            await Assert.ThrowsAsync<TimeoutException>(async () => await acquire);

            provider.Complete(connection);
            await connection.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Equal(1, connection.DisposeCount);
        }

        /// <summary>
        /// Pool shutdown cancels a delayed connect without surfacing a timeout.
        /// </summary>
        [Fact]
        public async Task PoolDisposeCancelsDelayedConnectWithoutTimeoutAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new DelayedProvider();
            var pool = new ManagedSessionPool(provider, CreateTelemetry());
            var request = CreateRequest("opc.tcp://localhost:4845") with
            {
                ConnectTimeout = TimeSpan.FromSeconds(5)
            };

            var acquire = pool.AcquireAsync(request);
            await provider.Started.Task;

            await pool.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await acquire);

            provider.Complete(connection);
            await connection.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(1, connection.DisposeCount);
        }

        /// <summary>
        /// Pool disposal closes every entry even when an individual close fails.
        /// </summary>
        [Fact]
        public async Task PoolDisposeAttemptsAllEntriesWhenOneCloseFailsAsync()
        {
            var firstSession = CreateSession(out _, out _);
            var secondSession = CreateSession(out _, out _);
            var first = new FakeConnection(firstSession.Object);
            var second = new FakeConnection(secondSession.Object,
                new InvalidOperationException("Expected disposal failure."));
            var provider = new MultiProvider(first, second);
            var pool = new ManagedSessionPool(provider, CreateTelemetry());
            var firstRequest = CreateRequest("opc.tcp://localhost:4847");
            var secondRequest = CreateRequest("opc.tcp://localhost:4848");
            using var firstLease = await pool.AcquireAsync(firstRequest);
            using var secondLease = await pool.AcquireAsync(secondRequest);

            await Assert.ThrowsAsync<AggregateException>(
                async () => await pool.DisposeAsync());

            Assert.Equal(1, first.DisposeCount);
            Assert.Equal(1, second.DisposeCount);
        }

        /// <summary>
        /// Capability fallback retains managed session operation limits when the server
        /// does not expose the optional capability objects.
        /// </summary>
        [Fact]
        public async Task FacadeCapabilitiesFallbackRetainsManagedOperationLimitsAsync()
        {
            var session = CreateSession(out _, out _);
            session.SetupGet(s => s.OperationLimits).Returns(new OperationLimits
            {
                MaxNodesPerRead = 22,
                MaxNodesPerBrowse = 23,
                MaxMonitoredItemsPerCall = 24
            });
            SetupOperationLimitsRead(session);
            session.Setup(s => s.TranslateBrowsePathsToNodeIdsAsync(
                    It.IsAny<RequestHeader>(),
                    It.IsAny<ArrayOf<BrowsePath>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult(new TranslateBrowsePathsToNodeIdsResponse
                {
                    Results =
                    [
                        new BrowsePathResult
                        {
                            StatusCode = StatusCodes.BadNodeIdUnknown
                        }
                    ]
                }));
            var connection = new FakeConnection(session.Object);
            await using var facade = new ManagedOpcUaSession(connection, CreateTelemetry());

            var server = await facade.GetServerCapabilitiesAsync(NamespaceFormat.Uri);
            var history = await facade.GetHistoryCapabilitiesAsync(NamespaceFormat.Uri);

            Assert.Equal(22u, server.OperationLimits.MaxNodesPerRead);
            Assert.Equal(23u, server.OperationLimits.MaxNodesPerBrowse);
            Assert.Equal(24u, server.OperationLimits.MaxMonitoredItemsPerCall);
            Assert.False(history.AccessHistoryDataCapability);
            Assert.False(history.AccessHistoryEventsCapability);
        }

        /// <summary>
        /// The test-only runtime routes both a session handle and a service call through
        /// the managed facade/pool without constructing the classic client runtime.
        /// </summary>
        [Fact]
        public async Task ManagedRuntimeUsesPoolForHandlesAndServiceCallsAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new FakeProvider(connection);
            var request = CreateRequest("opc.tcp://localhost:4850");
            await using var strategy = new ManagedSessionRuntimeStrategy(provider,
                CreateTelemetry(), new FixedRequestFactory(request),
                new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.FromMinutes(1)
                });
            var closeCount = 0;
            var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var runtime = strategy.Create(new OpcUaClientRuntimeContext
            {
                Configuration = new ApplicationConfiguration(),
                Connection = request.Connection,
                LoggerFactory = NullLoggerFactory.Instance,
                TimeProvider = TimeProvider.System,
                Metrics = IMetricsContext.Empty,
                OnClose = () =>
                {
                    closeCount++;
                    closed.TrySetResult();
                    return Task.CompletedTask;
                },
                ReverseConnectManager = new ReverseConnectManager(CreateTelemetry()),
                DiagnosticsCallback = _ => { },
                ClientOptions = Options.Create(new OpcUaClientOptions
                {
                    DefaultServiceCallTimeoutDuration = TimeSpan.FromSeconds(5)
                }),
                SubscriptionOptions = Options.Create(new OpcUaSubscriptionOptions())
            });

            runtime.AddRef();
            using (var handle = await runtime.AcquireAsync(null, 1000, default))
            {
                Assert.IsType<ManagedOpcUaSession>(handle.Session);
                Assert.Equal(TimeSpan.FromSeconds(1), handle.ServiceCallTimeout);
            }
            var result = await runtime.RunAsync(context =>
            {
                Assert.IsType<ManagedOpcUaSession>(context.Session);
                return Task.FromResult(42);
            }, null, null, default);
            runtime.Dispose();
            await closed.Task;

            Assert.Equal(42, result);
            Assert.Equal(1, provider.ConnectCount);
            Assert.Equal(1, closeCount);
        }

        /// <summary>
        /// The manager accepts the managed strategy only through its internal
        /// constructor seam; normal production registration retains the classic default.
        /// </summary>
        [Fact]
        public async Task ManagerUsesInjectedManagedRuntimeStrategyAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new FakeProvider(connection);
            var request = CreateRequest("opc.tcp://localhost:4851");
            var configuration = new Mock<IOpcUaConfiguration>();
            configuration.SetupGet(item => item.Value).Returns(new ApplicationConfiguration
            {
                ApplicationName = "managed-runtime-test",
                ApplicationUri = "urn:managed-runtime-test",
                ApplicationType = Opc.Ua.ApplicationType.Client
            });
            var strategy = new ManagedSessionRuntimeStrategy(provider, CreateTelemetry(),
                new FixedRequestFactory(request), new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.FromMinutes(1)
                });
            using var manager = new OpcUaClientManager(NullLoggerFactory.Instance,
                configuration.Object, Options.Create(new OpcUaClientOptions()),
                Options.Create(new OpcUaSubscriptionOptions()), runtimeStrategy: strategy);

            using var handle = await manager.AcquireSessionAsync(request.Connection.Connection,
                header: null, ct: default);

            Assert.IsType<ManagedOpcUaSession>(handle.Session);
            Assert.Equal(1, provider.ConnectCount);
        }

        [Fact]
        public async Task ProductionRegistrationDefaultsToClassicRuntimeAndEngineAsync()
        {
            var application = CreateApplicationConfiguration();
            var configuration = new Mock<IOpcUaConfiguration>();
            configuration.SetupGet(item => item.Value).Returns(application);
            var clientOptions = Options.Create(new OpcUaClientOptions());
            var subscriptionOptions = Options.Create(new OpcUaSubscriptionOptions());
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions();
            services.AddOpcUaStack();
            services.Replace(ServiceDescriptor.Singleton(configuration.Object));
            services.AddSingleton<IOptions<OpcUaClientOptions>>(clientOptions);
            services.AddSingleton<IOptions<OpcUaSubscriptionOptions>>(subscriptionOptions);
            await using var provider = services.BuildServiceProvider();
            var manager = provider.GetRequiredService<OpcUaClientManager>();

            var field = typeof(OpcUaClientManager).GetField("_runtimeStrategy",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            var strategy = Assert.IsType<ClassicOpcUaClientRuntimeStrategy>(
                field!.GetValue(manager));
            Assert.Same(ClassicOpcUaClientRuntimeStrategy.Instance, strategy);

            var request = CreateRequest("opc.tcp://localhost:4858");
            using var reverseConnectManager = new ReverseConnectManager(CreateTelemetry());
            IOpcUaClientRuntime runtime = strategy.Create(CreateRuntimeContext(request.Connection,
                clientOptions, subscriptionOptions, reverseConnectManager));
            try
            {
                var classic = Assert.IsType<OpcUaClient>(runtime);
                var channel = CreateTransportChannel();
                using var created = classic.Create(channel.Object, application, request.Endpoint);

                var session = Assert.IsType<OpcUaSession>(created);
                Assert.IsType<ClassicSubscriptionEngineFactory>(
                    session.SubscriptionEngineFactory);
                Assert.False(session.TryGetSubscriptionManager(out var subscriptionManager));
                Assert.Null(subscriptionManager);
            }
            finally
            {
                await runtime.CloseAsync(shutdown: true);
            }
        }

        [Fact]
        public async Task InjectedManagedRuntimeUsesV2SubscriptionManagerAsync()
        {
            using var session = CreateEngineSession(
                DefaultSubscriptionEngineFactory.Instance);
            var connection = new FakeConnection(session);
            var provider = new FakeProvider(connection);
            var request = CreateRequest("opc.tcp://localhost:4859");
            var configuration = new Mock<IOpcUaConfiguration>();
            configuration.SetupGet(item => item.Value).Returns(
                CreateApplicationConfiguration());
            var strategy = new ManagedSessionRuntimeStrategy(provider, CreateTelemetry(),
                new FixedRequestFactory(request), new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.FromMinutes(1)
                });
            using var manager = new OpcUaClientManager(NullLoggerFactory.Instance,
                configuration.Object, Options.Create(new OpcUaClientOptions()),
                Options.Create(new OpcUaSubscriptionOptions()), runtimeStrategy: strategy);

            using var handle = await manager.AcquireSessionAsync(
                request.Connection.Connection, header: null, ct: default);

            var managed = Assert.IsType<ManagedOpcUaSession>(handle.Session);
            Assert.True(managed.TryGetSubscriptionManager(out var subscriptionManager));
            Assert.NotNull(subscriptionManager);
            Assert.Equal(0, subscriptionManager.Count);
            Assert.False(subscriptionManager.PoolNotifications);
        }

        [Fact]
        public async Task ConfiguredTimeoutResolversMatchAcrossRuntimesAsync()
        {
            var options = new OpcUaClientOptions
            {
                DefaultServiceCallTimeoutDuration = TimeSpan.FromSeconds(13),
                DefaultConnectTimeoutDuration = TimeSpan.FromSeconds(11)
            };
            var clientOptions = Options.Create(options);
            var subscriptionOptions = Options.Create(new OpcUaSubscriptionOptions());
            var request = CreateRequest("opc.tcp://localhost:4860");
            using var reverseConnectManager = new ReverseConnectManager(CreateTelemetry());
            IOpcUaClientRuntime classicRuntime =
                ClassicOpcUaClientRuntimeStrategy.Instance.Create(
                    CreateRuntimeContext(request.Connection, clientOptions,
                        subscriptionOptions, reverseConnectManager));
            var session = CreateSession(out _, out _);
            var provider = new FakeProvider(new FakeConnection(session.Object));
            await using var managedStrategy = new ManagedSessionRuntimeStrategy(provider,
                CreateTelemetry(), new FixedRequestFactory(request));
            IOpcUaClientRuntime managedRuntime = managedStrategy.Create(
                CreateRuntimeContext(request.Connection, clientOptions,
                    subscriptionOptions, reverseConnectManager));
            try
            {
                var classic = Assert.IsType<OpcUaClient>(classicRuntime);
                var managed = Assert.IsType<ManagedOpcUaClient>(managedRuntime);
                var classicDefaultService = InvokeTimeSpanMethod(
                    classic, "GetServiceCallTimeout", [null]);
                var managedDefaultService = InvokeTimeSpanMethod(
                    managed, "GetServiceCallTimeout", [null]);
                Assert.Equal(TimeSpan.FromSeconds(13), classicDefaultService);
                Assert.Equal(TimeSpan.FromSeconds(13), managedDefaultService);

                var classicExplicitService = InvokeTimeSpanMethod(
                    classic, "GetServiceCallTimeout", [1234]);
                var managedExplicitService = InvokeTimeSpanMethod(
                    managed, "GetServiceCallTimeout", [1234]);
                Assert.Equal(TimeSpan.FromMilliseconds(1234), classicExplicitService);
                Assert.Equal(TimeSpan.FromMilliseconds(1234), managedExplicitService);

                var classicDefaultConnect = InvokeTimeSpanMethod(
                    classic, "GetConnectCallTimeout", [null, null]);
                var managedDefaultConnect = InvokeTimeSpanMethod(
                    typeof(DefaultManagedSessionRequestFactory),
                    "GetConnectTimeout", [null, options]);
                Assert.Equal(TimeSpan.FromSeconds(11), classicDefaultConnect);
                Assert.Equal(TimeSpan.FromSeconds(11), managedDefaultConnect);

                var classicExplicitConnect = InvokeTimeSpanMethod(
                    classic, "GetConnectCallTimeout", [2345, null]);
                var managedExplicitConnect = InvokeTimeSpanMethod(
                    typeof(DefaultManagedSessionRequestFactory),
                    "GetConnectTimeout", [2345, options]);
                Assert.Equal(TimeSpan.FromMilliseconds(2345), classicExplicitConnect);
                Assert.Equal(TimeSpan.FromMilliseconds(2345), managedExplicitConnect);
            }
            finally
            {
                await managedRuntime.CloseAsync(shutdown: true);
                await classicRuntime.CloseAsync(shutdown: true);
            }
        }

        [Fact]
        public async Task PerCallServiceTimeoutIsClassicOnlyConnectFallbackAsync()
        {
            var options = new OpcUaClientOptions();
            var clientOptions = Options.Create(options);
            var subscriptionOptions = Options.Create(new OpcUaSubscriptionOptions());
            var request = CreateRequest("opc.tcp://localhost:4863");
            using var reverseConnectManager = new ReverseConnectManager(CreateTelemetry());
            IOpcUaClientRuntime runtime =
                ClassicOpcUaClientRuntimeStrategy.Instance.Create(
                    CreateRuntimeContext(request.Connection, clientOptions,
                        subscriptionOptions, reverseConnectManager));
            try
            {
                var classic = Assert.IsType<OpcUaClient>(runtime);
                Assert.Equal(TimeSpan.FromSeconds(7),
                    InvokeTimeSpanMethod(classic, "GetConnectCallTimeout", [null, 7000]));
                Assert.Equal(TimeSpan.FromMinutes(1),
                    InvokeTimeSpanMethod(typeof(DefaultManagedSessionRequestFactory),
                        "GetConnectTimeout", [null, options]));
            }
            finally
            {
                await runtime.CloseAsync(shutdown: true);
            }
        }

        [Theory]
        [InlineData(ConnectionOptions.None, true)]
        [InlineData(ConnectionOptions.NoSubscriptionTransfer, false)]
        public async Task ConnectionOptionControlsClassicTransferIntentAsync(
            ConnectionOptions connectionOptions, bool transferEnabled)
        {
            var request = CreateRequest("opc.tcp://localhost:4861", connectionOptions);
            var clientOptions = Options.Create(new OpcUaClientOptions());
            var subscriptionOptions = Options.Create(new OpcUaSubscriptionOptions());
            using var reverseConnectManager = new ReverseConnectManager(CreateTelemetry());
            IOpcUaClientRuntime runtime =
                ClassicOpcUaClientRuntimeStrategy.Instance.Create(
                    CreateRuntimeContext(request.Connection, clientOptions,
                        subscriptionOptions, reverseConnectManager));
            try
            {
                var classic = Assert.IsType<OpcUaClient>(runtime);
                var channel = CreateTransportChannel();
                using var classicSession = Assert.IsType<OpcUaSession>(
                    classic.Create(channel.Object, CreateApplicationConfiguration(),
                        request.Endpoint));

                Assert.Equal(transferEnabled,
                    classicSession.TransferSubscriptionsOnReconnect);
                Assert.Equal(!transferEnabled, classicSession.DeleteSubscriptionsOnClose);
            }
            finally
            {
                await runtime.CloseAsync(shutdown: true);
            }
        }

        [Theory]
        [InlineData(ConnectionOptions.None, true)]
        [InlineData(ConnectionOptions.NoSubscriptionTransfer, false)]
        public void ManagedOptionsMapTransferIntent(ConnectionOptions connectionOptions,
            bool transferEnabled)
        {
            var connection = new ConnectionModel
            {
                Endpoint = new EndpointModel
                {
                    Url = "opc.tcp://localhost:4840"
                },
                Options = connectionOptions
            };

            Assert.Equal(transferEnabled,
                ManagedSessionOptionsAdapter.TransferSubscriptionsOnRecreate(connection));
        }

        [Fact]
        public void ManagedOptionsMapReconnectWorkersLimitsAndEngine()
        {
            var options = new OpcUaClientOptions
            {
                MinReconnectDelayDuration = TimeSpan.FromSeconds(3),
                MaxReconnectDelayDuration = TimeSpan.FromSeconds(9),
                MinPublishRequests = 3,
                MaxPublishRequests = 7,
                PublishRequestsPerSubscriptionPercent = 150,
                MaxNodesPerReadOverride = 23,
                MaxNodesPerBrowseOverride = 29
            };

            var reconnect = ManagedSessionOptionsAdapter.CreateReconnectPolicy(options);
            var empty = ManagedSessionOptionsAdapter.GetPublishWorkerCounts(options, 0);
            var active = ManagedSessionOptionsAdapter.GetPublishWorkerCounts(options, 4);
            var limits = Assert.IsType<OperationLimits>(
                ManagedSessionOptionsAdapter.CreateOperationLimitOverrides(options));

            Assert.Equal(TimeSpan.FromSeconds(3), reconnect.InitialDelay);
            Assert.Equal(TimeSpan.FromSeconds(9), reconnect.MaxDelay);
            Assert.Equal(Timeout.InfiniteTimeSpan, reconnect.MaxTotalReconnectTime);
            Assert.Equal((3, 7), empty);
            Assert.Equal((6, 7), active);
            Assert.Equal(23u, limits.MaxNodesPerRead);
            Assert.Equal(29u, limits.MaxNodesPerBrowse);
            Assert.IsType<DefaultSubscriptionEngineFactory>(
                ManagedSessionOptionsAdapter.CreateSubscriptionEngineFactory(
                    TimeProvider.System));
        }

        [Theory]
        [InlineData(0, 5, 1, 5)]
        [InlineData(10, 3, 3, 3)]
        public void ManagedReconnectPolicyMatchesClassicBounds(int minimumSeconds,
            int maximumSeconds, int expectedMinimumSeconds, int expectedMaximumSeconds)
        {
            var reconnect = ManagedSessionOptionsAdapter.CreateReconnectPolicy(
                new OpcUaClientOptions
                {
                    MinReconnectDelayDuration = TimeSpan.FromSeconds(minimumSeconds),
                    MaxReconnectDelayDuration = TimeSpan.FromSeconds(maximumSeconds)
                });

            Assert.Equal(TimeSpan.FromSeconds(expectedMinimumSeconds),
                reconnect.InitialDelay);
            Assert.Equal(TimeSpan.FromSeconds(expectedMaximumSeconds),
                reconnect.MaxDelay);
            Assert.Equal(Timeout.InfiniteTimeSpan, reconnect.MaxTotalReconnectTime);
            Assert.NotNull(reconnect.GetNextDelay(1000));
        }

        [Fact]
        public void ManagedOptionsUseOperationQuotaForEndpointTimeout()
        {
            var options = Options.Create(new OpcUaClientOptions());
            options.Value.Quotas.OperationTimeout = 4321;
            using var reverseConnectManager = new ReverseConnectManager(CreateTelemetry());
            var context = new ManagedSessionClientContext
            {
                Configuration = CreateApplicationConfiguration(),
                Connection = CreateRequest("opc.tcp://localhost:4864").Connection,
                Logger = NullLogger.Instance,
                Options = options,
                ReverseConnectManager = reverseConnectManager,
                TimeProvider = TimeProvider.System
            };

            Assert.Equal(4321, ManagedSessionOptionsAdapter.GetEndpointOperationTimeout(
                context, TimeSpan.FromSeconds(11)));
        }

        [Fact]
        public async Task ConnectorRequiresAndConfiguresDefaultSubscriptionManagerAsync()
        {
            var managed = await CreateManagedSessionAsync(
                DefaultSubscriptionEngineFactory.Instance);
            var factory = new CapturingManagedSessionFactory(managed);
            var connector = new ManagedSessionConnector(CreateApplicationConfiguration(),
                CreateTelemetry(), TimeProvider.System, factory);
            var request = CreateRequest("opc.tcp://localhost:4865") with
            {
                Identity = new UserIdentity(),
                SubscriptionEngineFactory = DefaultSubscriptionEngineFactory.Instance,
                ReconnectPolicy = new ReconnectPolicy(),
                TransferSubscriptionsOnRecreate = true,
                KeepAliveInterval = TimeSpan.FromSeconds(4),
                MinPublishWorkerCount = 3,
                MaxPublishWorkerCount = 8,
                OperationLimitOverrides = new OperationLimits
                {
                    MaxNodesPerRead = 31,
                    MaxNodesPerBrowse = 37
                }
            };

            await using var connection = await connector.ConnectAsync(request, default);

            Assert.Same(request, factory.Request);
            Assert.Equal(4000, managed.KeepAliveInterval);
            Assert.Equal(31u, managed.OperationLimits.MaxNodesPerRead);
            Assert.Equal(37u, managed.OperationLimits.MaxNodesPerBrowse);
            Assert.True(managed.TryGetSubscriptionManager(out var manager));
            Assert.NotNull(manager);
            Assert.False(manager.PoolNotifications);
            Assert.Equal(3, manager.MinPublishWorkerCount);
            Assert.Equal(8, manager.MaxPublishWorkerCount);
        }

        [Fact]
        public async Task ConnectorRejectsManagedSessionWithoutDefaultManagerAsync()
        {
            var managed = await CreateManagedSessionAsync(
                ClassicSubscriptionEngineFactory.Instance);
            var factory = new CapturingManagedSessionFactory(managed);
            var connector = new ManagedSessionConnector(CreateApplicationConfiguration(),
                CreateTelemetry(), TimeProvider.System, factory);
            var request = CreateRequest("opc.tcp://localhost:4866") with
            {
                Identity = new UserIdentity(),
                SubscriptionEngineFactory = DefaultSubscriptionEngineFactory.Instance
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await connector.ConnectAsync(request, default));

            Assert.Contains("V2 subscription manager", exception.Message,
                StringComparison.Ordinal);
        }

        [Fact]
        public async Task PoolHonorsNoComplexTypeSystemOptionAsync()
        {
            var session = CreateSession(out _, out _);
            session.SetupGet(item => item.Connected).Returns(true);
            var provider = new FakeProvider(new FakeConnection(session.Object));
            await using var pool = new ManagedSessionPool(provider, CreateTelemetry());
            var request = CreateRequest("opc.tcp://localhost:4867") with
            {
                DisableComplexTypeLoading = true
            };

            using var lease = await pool.AcquireAsync(request);

            Assert.Null(await lease.Session.GetComplexTypeSystemAsync());
        }

        [Fact]
        public Task ManagedRuntimeKeepsSessionForBrowseNextContinuationAsync()
        {
            return VerifyContinuationSessionAffinityAsync("browse-next");
        }

        [Fact]
        public Task ManagedRuntimeKeepsSessionForHistoryReadNextContinuationAsync()
        {
            return VerifyContinuationSessionAffinityAsync("history-read-next");
        }

        [Fact]
        public async Task ManagerResetReconnectsManagedSessionAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new FakeProvider(connection);
            var request = CreateRequest("opc.tcp://localhost:4852");
            var configuration = new Mock<IOpcUaConfiguration>();
            configuration.SetupGet(item => item.Value).Returns(CreateApplicationConfiguration());
            var strategy = new ManagedSessionRuntimeStrategy(provider, CreateTelemetry(),
                new FixedRequestFactory(request), new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.FromMinutes(1)
                });
            using var manager = new OpcUaClientManager(NullLoggerFactory.Instance,
                configuration.Object, Options.Create(new OpcUaClientOptions()),
                Options.Create(new OpcUaSubscriptionOptions()), runtimeStrategy: strategy);
            using var handle = await manager.AcquireSessionAsync(request.Connection.Connection,
                header: null, ct: default);

            await manager.ResetAllConnectionsAsync(default);

            Assert.Equal(1, connection.ReconnectCount);
        }

        [Fact]
        public async Task ManagerWatchReceivesChangedManagedDiagnosticsOnceAsync()
        {
            var session = CreateSession(out _, out _);
            session.SetupGet(item => item.SessionId).Returns(new NodeId(123u, 2));
            var connection = new FakeConnection(session.Object);
            var provider = new FakeProvider(connection);
            var request = CreateRequest("opc.tcp://localhost:4853");
            var configuration = new Mock<IOpcUaConfiguration>();
            configuration.SetupGet(item => item.Value).Returns(CreateApplicationConfiguration());
            var strategy = new ManagedSessionRuntimeStrategy(provider, CreateTelemetry(),
                new FixedRequestFactory(request), new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.FromMinutes(1)
                });
            using var manager = new OpcUaClientManager(NullLoggerFactory.Instance,
                configuration.Object, Options.Create(new OpcUaClientOptions()),
                Options.Create(new OpcUaSubscriptionOptions()), runtimeStrategy: strategy);
            using var cancellation = new CancellationTokenSource();
            await using var watch = manager.WatchChannelDiagnosticsAsync(cancellation.Token)
                .GetAsyncEnumerator();
            var next = watch.MoveNextAsync().AsTask();

            using var first = await manager.AcquireSessionAsync(request.Connection.Connection,
                header: null, ct: default);
            Assert.True(await next.WaitAsync(TimeSpan.FromSeconds(2)));
            var diagnostic = watch.Current;
            using var second = await manager.AcquireSessionAsync(request.Connection.Connection,
                header: null, ct: default);
            cancellation.Cancel();

            Assert.Equal("ns=2;i=123", diagnostic.SessionId);
            Assert.Equal(1, provider.ConnectCount);
        }

        [Fact]
        public async Task ManagerRetriesWhenFinalReleaseRacesNewAcquireAsync()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new FakeProvider(connection);
            var request = CreateRequest("opc.tcp://localhost:4855");
            var configuration = new Mock<IOpcUaConfiguration>();
            configuration.SetupGet(item => item.Value).Returns(CreateApplicationConfiguration());
            var strategy = new ManagedSessionRuntimeStrategy(provider, CreateTelemetry(),
                new FixedRequestFactory(request), new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.FromMinutes(1)
                });
            using var manager = new OpcUaClientManager(NullLoggerFactory.Instance,
                configuration.Object, Options.Create(new OpcUaClientOptions()),
                Options.Create(new OpcUaSubscriptionOptions()), runtimeStrategy: strategy);
            using var first = await manager.AcquireSessionAsync(request.Connection.Connection,
                header: null, ct: default);

            var acquire = Task.Run(() => manager.AcquireSessionAsync(
                request.Connection.Connection, header: null, ct: default));
            first.Dispose();
            using var second = await acquire;

            Assert.IsType<ManagedOpcUaSession>(second.Session);
        }

        [Fact]
        public async Task ManagedRuntimeSnapshotsSubscriptionsForConcurrentDiagnosticsAsync()
        {
            var session = CreateSession(out _, out _);
            session.SetupGet(item => item.SessionId).Returns(new NodeId(124u, 2));
            session.SetupGet(item => item.SessionName).Returns("managed");
            var collection = new Mock<IMonitoredItemCollection>();
            collection.Setup(items => items.Update(It.IsAny<IReadOnlyList<(
                    string Name, IOptionsMonitor<ManagedMonitoredItemOptions> Options)>>()))
                .Returns([]);
            var subscription = new Mock<Opc.Ua.Client.Subscriptions.ISubscription>();
            subscription.SetupGet(item => item.MonitoredItems).Returns(collection.Object);
            var subscriptionManager = new Mock<ISubscriptionManager>();
            subscriptionManager.Setup(manager => manager.Add(
                    It.IsAny<ISubscriptionNotificationHandler>(),
                    It.IsAny<IOptionsMonitor<Opc.Ua.Client.Subscriptions.SubscriptionOptions>>()))
                .Returns(subscription.Object);
            ISubscriptionManager manager = subscriptionManager.Object;
            session.Setup(item => item.TryGetSubscriptionManager(out manager)).Returns(true);

            var connection = new FakeConnection(session.Object);
            var provider = new FakeProvider(connection);
            var request = CreateRequest("opc.tcp://localhost:4856");
            var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var strategy = new ManagedSessionRuntimeStrategy(provider,
                CreateTelemetry(), new FixedRequestFactory(request),
                new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.FromMinutes(1)
                });
            using var runtime = CreateManagedRuntime(strategy, request, closed);
            runtime.AddRef();
            var subscriber = new Mock<ISubscriber>();
            subscriber.SetupGet(item => item.MonitoredItems).Returns(
            [
                new DataMonitoredItemModel
                {
                    StartNodeId = "ns=2;s=value"
                }
            ]);
            await using var registration = await runtime.RegisterAsync(new SubscriptionModel(),
                subscriber.Object, default);
            await runtime.ResetAsync(default);

            var diagnostics = Enumerable.Range(0, 32)
                .Select(_ => runtime.GetSessionDiagnosticsAsync(default));
            var remove = registration.DisposeAsync().AsTask();
            await Task.WhenAll(diagnostics.Cast<Task>().Append(remove));
            runtime.Dispose();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Equal(1, subscriptionManager.Invocations.Count(invocation =>
                invocation.Method.Name == nameof(ISubscriptionManager.Add)));
            Assert.Equal(1, connection.ReconnectCount);
            collection.Verify(items => items.Update(It.IsAny<IReadOnlyList<(
                string Name, IOptionsMonitor<ManagedMonitoredItemOptions> Options)>>()), Times.AtLeast(2));
        }

        [Fact]
        public async Task ManagedRuntimeScalesPublishWorkersWithLogicalSubscriptionsAsync()
        {
            var session = CreateSession(out _, out _);
            var collection = new Mock<IMonitoredItemCollection>();
            collection.Setup(items => items.Update(It.IsAny<IReadOnlyList<(
                    string Name, IOptionsMonitor<ManagedMonitoredItemOptions> Options)>>() ))
                .Returns([]);
            var subscription = new Mock<Opc.Ua.Client.Subscriptions.ISubscription>();
            subscription.SetupGet(item => item.MonitoredItems).Returns(collection.Object);
            var manager = new Mock<ISubscriptionManager>();
            manager.SetupAllProperties();
            manager.Setup(item => item.Add(It.IsAny<ISubscriptionNotificationHandler>(),
                    It.IsAny<IOptionsMonitor<Opc.Ua.Client.Subscriptions.SubscriptionOptions>>()))
                .Returns(subscription.Object);
            ISubscriptionManager subscriptionManager = manager.Object;
            session.Setup(item => item.TryGetSubscriptionManager(out subscriptionManager))
                .Returns(true);
            var connection = new FakeConnection(session.Object);
            var provider = new FakeProvider(connection);
            var request = CreateRequest("opc.tcp://localhost:4868");
            await using var strategy = new ManagedSessionRuntimeStrategy(provider,
                CreateTelemetry(), new FixedRequestFactory(request));
            using var runtime = (ManagedOpcUaClient)strategy.Create(
                CreateRuntimeContext(request.Connection, Options.Create(new OpcUaClientOptions
                {
                    MinPublishRequests = 1,
                    MaxPublishRequests = 10,
                    PublishRequestsPerSubscriptionPercent = 200
                }), Options.Create(new OpcUaSubscriptionOptions()),
                new ReverseConnectManager(CreateTelemetry())));
            runtime.AddRef();
            var subscriber = new Mock<ISubscriber>();
            subscriber.SetupGet(item => item.MonitoredItems).Returns(
                [new DataMonitoredItemModel { StartNodeId = "ns=2;s=value" }]);

            await using var registration = await runtime.RegisterAsync(
                new SubscriptionModel(), subscriber.Object, default);
            Assert.Equal(2, manager.Object.MinPublishWorkerCount);
            Assert.Equal(10, manager.Object.MaxPublishWorkerCount);

            await registration.DisposeAsync();
            Assert.Equal(1, manager.Object.MinPublishWorkerCount);
            runtime.Dispose();
        }

        [Fact]
        public async Task ManagedRuntimeDoesNotPublishUnchangedDiagnosticsTwiceAsync()
        {
            var session = CreateSession(out _, out _);
            session.SetupGet(item => item.SessionId).Returns(new NodeId(125u, 2));
            var connection = new FakeConnection(session.Object);
            var provider = new FakeProvider(connection);
            var request = CreateRequest("opc.tcp://localhost:4857");
            var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var diagnostics = new List<ChannelDiagnosticModel>();
            await using var strategy = new ManagedSessionRuntimeStrategy(provider,
                CreateTelemetry(), new FixedRequestFactory(request),
                new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.FromMinutes(1)
                });
            using var runtime = CreateManagedRuntime(strategy, request, closed,
                diagnostics.Add);
            runtime.AddRef();
            using (await runtime.AcquireAsync(null, null, default))
            {
            }
            using (await runtime.AcquireAsync(null, null, default))
            {
            }
            runtime.Dispose();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Single(diagnostics);
        }

        private static async Task VerifyContinuationSessionAffinityAsync(string token)
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            var provider = new FakeProvider(connection);
            var request = CreateRequest("opc.tcp://localhost:4854");
            var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var strategy = new ManagedSessionRuntimeStrategy(provider,
                CreateTelemetry(), new FixedRequestFactory(request),
                new ManagedSessionPoolOptions
                {
                    LingerTimeout = TimeSpan.Zero
                });
            using var runtime = CreateManagedRuntime(strategy, request, closed);
            runtime.AddRef();
            IOpcUaSession? firstSession = null;

            await runtime.RunAsync(context =>
            {
                firstSession = context.Session;
                context.TrackedToken = token;
                return Task.FromResult(true);
            }, null, null, default);
            await runtime.RunAsync(context =>
            {
                Assert.Same(firstSession, context.Session);
                context.UntrackedToken = token;
                return Task.FromResult(true);
            }, null, null, default);
            runtime.Dispose();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Equal(1, provider.ConnectCount);
        }

        private static ManagedOpcUaClient CreateManagedRuntime(
            ManagedSessionRuntimeStrategy strategy, ManagedSessionConnectionRequest request,
            TaskCompletionSource closed, Action<ChannelDiagnosticModel>? diagnostics = null)
        {
            return (ManagedOpcUaClient)strategy.Create(new OpcUaClientRuntimeContext
            {
                Configuration = CreateApplicationConfiguration(),
                Connection = request.Connection,
                LoggerFactory = NullLoggerFactory.Instance,
                TimeProvider = TimeProvider.System,
                Metrics = IMetricsContext.Empty,
                OnClose = () =>
                {
                    closed.TrySetResult();
                    return Task.CompletedTask;
                },
                ReverseConnectManager = new ReverseConnectManager(CreateTelemetry()),
                DiagnosticsCallback = diagnostics ?? (_ => { }),
                ClientOptions = Options.Create(new OpcUaClientOptions()),
                SubscriptionOptions = Options.Create(new OpcUaSubscriptionOptions())
            });
        }

        private static OpcUaClientRuntimeContext CreateRuntimeContext(
            ConnectionIdentifier connection, IOptions<OpcUaClientOptions> clientOptions,
            IOptions<OpcUaSubscriptionOptions> subscriptionOptions,
            ReverseConnectManager reverseConnectManager)
        {
            return new OpcUaClientRuntimeContext
            {
                Configuration = CreateApplicationConfiguration(),
                Connection = connection,
                LoggerFactory = NullLoggerFactory.Instance,
                TimeProvider = TimeProvider.System,
                Metrics = IMetricsContext.Empty,
                OnClose = () => Task.CompletedTask,
                ReverseConnectManager = reverseConnectManager,
                DiagnosticsCallback = _ => { },
                ClientOptions = clientOptions,
                SubscriptionOptions = subscriptionOptions
            };
        }

        private static ApplicationConfiguration CreateApplicationConfiguration()
        {
            return new ApplicationConfiguration
            {
                ApplicationName = "managed-runtime-test",
                ApplicationUri = "urn:managed-runtime-test",
                ApplicationType = Opc.Ua.ApplicationType.Client,
                ClientConfiguration = new ClientConfiguration()
            };
        }

        private static ManagedSessionConnectionRequest CreateRequest(string endpointUrl,
            ConnectionOptions connectionOptions = ConnectionOptions.None)
        {
            var connection = new ConnectionModel
            {
                Endpoint = new EndpointModel
                {
                    Url = endpointUrl
                },
                Options = connectionOptions
            };
            var description = new EndpointDescription
            {
                EndpointUrl = endpointUrl,
                Server = new ApplicationDescription
                {
                    ApplicationUri = "urn:managed-session-tests"
                }
            };
            return new ManagedSessionConnectionRequest
            {
                Connection = new ConnectionIdentifier(connection),
                Endpoint = new ConfiguredEndpoint(null, description,
                    EndpointConfiguration.Create())
            };
        }

        private static Mock<ITransportChannel> CreateTransportChannel()
        {
            var channel = new Mock<ITransportChannel>();
            channel.SetupGet(item => item.MessageContext)
                .Returns(new ServiceMessageContext(CreateTelemetry()));
            channel.SetupGet(item => item.SupportedFeatures)
                .Returns(TransportChannelFeatures.Reconnect);
            return channel;
        }

        private static Session CreateEngineSession(
            ISubscriptionEngineFactory subscriptionEngineFactory,
            ApplicationConfiguration? application = null,
            ConfiguredEndpoint? endpoint = null)
        {
            application ??= CreateApplicationConfiguration();
            endpoint ??= CreateRequest("opc.tcp://localhost:4862").Endpoint;
            return new Session(CreateTransportChannel().Object, application, endpoint,
                engineFactory: subscriptionEngineFactory);
        }

        private static async Task<ManagedSession> CreateManagedSessionAsync(
            ISubscriptionEngineFactory engineFactory)
        {
            var telemetry = CreateTelemetry();
            var application = CreateApplicationConfiguration();
            var endpoint = CreateRequest("opc.tcp://localhost:4869").Endpoint;
            var inner = CreateEngineSession(engineFactory, application, endpoint);
            var factory = new Mock<ISessionFactory>();
            factory.SetupGet(item => item.Telemetry).Returns(telemetry);
            factory.Setup(item => item.CreateAsync(
                    It.IsAny<ApplicationConfiguration>(),
                    It.IsAny<ConfiguredEndpoint>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<uint>(),
                    It.IsAny<IUserIdentity>(),
                    It.IsAny<ArrayOf<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(inner);
            try
            {
                return await ManagedSession.CreateAsync(application, endpoint,
                    factory.Object, telemetry: telemetry, engineFactory: engineFactory);
            }
            catch
            {
                inner.Dispose();
                throw;
            }
        }

        private static TimeSpan InvokeTimeSpanMethod(object instance,
            string methodName, object?[] arguments)
        {
            var method = instance.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return Assert.IsType<TimeSpan>(method!.Invoke(instance, arguments));
        }

        private static TimeSpan InvokeTimeSpanMethod(Type type,
            string methodName, object?[] arguments)
        {
            var method = type.GetMethod(methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return Assert.IsType<TimeSpan>(method!.Invoke(null, arguments));
        }

        private static Mock<ISession> CreateSession(out ConfiguredEndpoint endpoint,
            out IUserIdentity identity, bool connected = false)
        {
            var context = new ServiceMessageContext
            {
                NamespaceUris = new NamespaceTable(),
                ServerUris = new StringTable()
            };
            var description = new EndpointDescription
            {
                EndpointUrl = "opc.tcp://localhost:4840",
                Server = new ApplicationDescription
                {
                    ApplicationUri = "urn:managed-session-tests"
                }
            };
            endpoint = new ConfiguredEndpoint(null, description,
                EndpointConfiguration.Create());
            var identityMock = new Mock<IUserIdentity>();
            identity = identityMock.Object;
            var session = new Mock<ISession>();
            session.SetupGet(s => s.MessageContext).Returns(context);
            session.SetupGet(s => s.ConfiguredEndpoint).Returns(endpoint);
            session.SetupGet(s => s.Identity).Returns(identity);
            session.SetupGet(s => s.Connected).Returns(connected);
            session.SetupGet(s => s.Endpoint).Returns(description);
            session.SetupGet(s => s.SystemContext).Returns(new SystemContext(CreateTelemetry())
            {
                NamespaceUris = context.NamespaceUris,
                ServerUris = context.ServerUris
            });
            return session;
        }

        private static void SetupOperationLimitsRead(Mock<ISession> session)
        {
            session.Setup(s => s.ReadAsync(
                    It.IsAny<RequestHeader>(),
                    It.IsAny<double>(),
                    It.IsAny<Opc.Ua.TimestampsToReturn>(),
                    It.IsAny<ArrayOf<ReadValueId>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((RequestHeader _, double _, Opc.Ua.TimestampsToReturn _,
                    ArrayOf<ReadValueId> nodes, CancellationToken _) =>
                {
                    var stackLimits = session.Object.OperationLimits;
                    var values = new DataValue[nodes.Count];
                    for (var i = 0; i < nodes.Count; i++)
                    {
                        values[i] = CreateOperationLimitValue(nodes[i].NodeId, stackLimits);
                    }
                    return ValueTask.FromResult(new ReadResponse
                    {
                        Results = new ArrayOf<DataValue>(values)
                    });
                });
        }

        private static DataValue CreateOperationLimitValue(NodeId nodeId,
            OperationLimits stackLimits)
        {
            if (nodeId == new NodeId(Variables.Server_ServerCapabilities_MaxArrayLength))
            {
                return new DataValue(32u);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_MaxBrowseContinuationPoints))
            {
                return new DataValue((ushort)2);
            }
            if (nodeId == new NodeId(Variables.Server_ServerCapabilities_MaxByteStringLength))
            {
                return new DataValue(8u);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_MaxHistoryContinuationPoints))
            {
                return new DataValue((ushort)3);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_MaxQueryContinuationPoints))
            {
                return new DataValue((ushort)4);
            }
            if (nodeId == new NodeId(Variables.Server_ServerCapabilities_MaxStringLength))
            {
                return new DataValue(16u);
            }
            if (nodeId == new NodeId(Variables.Server_ServerCapabilities_MinSupportedSampleRate))
            {
                return new DataValue(0.5);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryReadData))
            {
                return new DataValue(28u);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryReadEvents))
            {
                return new DataValue(29u);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerWrite))
            {
                return new DataValue(stackLimits.MaxNodesPerWrite == 0 ?
                    64u :
                    stackLimits.MaxNodesPerWrite);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerRead))
            {
                return new DataValue(stackLimits.MaxNodesPerRead == 0 ?
                    64u :
                    stackLimits.MaxNodesPerRead);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryUpdateData))
            {
                return new DataValue(30u);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerHistoryUpdateEvents))
            {
                return new DataValue(31u);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerMethodCall))
            {
                return new DataValue(33u);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerBrowse))
            {
                return new DataValue(stackLimits.MaxNodesPerBrowse == 0 ?
                    64u :
                    stackLimits.MaxNodesPerBrowse);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerRegisterNodes))
            {
                return new DataValue(34u);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerTranslateBrowsePathsToNodeIds))
            {
                return new DataValue(35u);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxNodesPerNodeManagement))
            {
                return new DataValue(36u);
            }
            if (nodeId == new NodeId(
                Variables.Server_ServerCapabilities_OperationLimits_MaxMonitoredItemsPerCall))
            {
                return new DataValue(stackLimits.MaxMonitoredItemsPerCall == 0 ?
                    37u :
                    stackLimits.MaxMonitoredItemsPerCall);
            }
            throw new InvalidOperationException($"Unexpected operation limit node {nodeId}.");
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException("The expected pool state was not reached.");
                }
                await Task.Delay(10);
            }
        }

        private static ITelemetryContext CreateTelemetry()
        {
            return new LoggerTelemetryContext(NullLoggerFactory.Instance);
        }

        private sealed class FakeConnection : IManagedSessionConnection
        {
            public FakeConnection(ISession session, Exception disposeException = null)
            {
                Session = session;
                _disposeException = disposeException;
            }

            public ISession Session { get; }

            public int DisposeCount { get; private set; }
            public int ReconnectCount { get; private set; }

            public TaskCompletionSource Disposed { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

            public ValueTask DisposeAsync()
            {
                DisposeCount++;
                Disposed.TrySetResult();
                if (_disposeException != null)
                {
                    return ValueTask.FromException(_disposeException);
                }
                return ValueTask.CompletedTask;
            }

            public Task ReconnectAsync(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                ReconnectCount++;
                Raise(ConnectionState.Reconnecting);
                Raise(ConnectionState.Connected);
                return Task.CompletedTask;
            }

            public void Raise(ConnectionState state)
            {
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
                {
                    NewState = state
                });
            }

            private readonly Exception _disposeException;
        }

        private sealed class FakeProvider : IManagedSessionProvider
        {
            public FakeProvider(IManagedSessionConnection connection)
            {
                _connection = connection;
            }

            public int ConnectCount { get; private set; }

            public Task<IManagedSessionConnection> ConnectAsync(
                ManagedSessionConnectionRequest request, CancellationToken ct)
            {
                ArgumentNullException.ThrowIfNull(request);
                ct.ThrowIfCancellationRequested();
                ConnectCount++;
                Request = request;
                return Task.FromResult(_connection);
            }

            public ManagedSessionConnectionRequest Request { get; private set; }

            private readonly IManagedSessionConnection _connection;
        }

        private sealed class CapturingManagedSessionFactory : IManagedSessionFactory
        {
            public CapturingManagedSessionFactory(ManagedSession session)
            {
                _session = session;
            }

            public ManagedSessionConnectionRequest Request { get; private set; }

            public Task<ManagedSession> CreateAsync(ApplicationConfiguration configuration,
                ManagedSessionConnectionRequest request, ISessionFactory sessionFactory,
                IUserIdentity? identity, ITelemetryContext telemetry, TimeProvider timeProvider,
                CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                Request = request;
                return Task.FromResult(_session);
            }

            private readonly ManagedSession _session;
        }

        private sealed class FixedRequestFactory : IManagedSessionRequestFactory
        {
            public FixedRequestFactory(ManagedSessionConnectionRequest request)
            {
                _request = request;
            }

            public Task<ManagedSessionConnectionRequest> CreateAsync(
                ManagedSessionClientContext context, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(_request with
                {
                    ConnectTimeout = context.ConnectTimeout is > 0 ?
                        TimeSpan.FromMilliseconds(context.ConnectTimeout.Value) :
                        _request.ConnectTimeout
                });
            }

            private readonly ManagedSessionConnectionRequest _request;
        }

        private sealed class DelayedProvider : IManagedSessionProvider
        {
            public int ConnectCount { get; private set; }

            public CancellationToken ConnectCancellation { get; private set; }

            public TaskCompletionSource Started { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public Task<IManagedSessionConnection> ConnectAsync(
                ManagedSessionConnectionRequest request, CancellationToken ct)
            {
                ArgumentNullException.ThrowIfNull(request);
                ConnectCount++;
                ConnectCancellation = ct;
                Started.TrySetResult();
                return _connection.Task;
            }

            public void Complete(IManagedSessionConnection connection)
            {
                _connection.TrySetResult(connection);
            }

            private readonly TaskCompletionSource<IManagedSessionConnection> _connection = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private sealed class RetryProvider : IManagedSessionProvider
        {
            public RetryProvider(IManagedSessionConnection connection)
            {
                _connection = connection;
            }

            public int ConnectCount { get; private set; }

            public TaskCompletionSource FirstAttemptStarted { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public TaskCompletionSource FirstAttemptCanceled { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public Task<IManagedSessionConnection> ConnectAsync(
                ManagedSessionConnectionRequest request, CancellationToken ct)
            {
                ArgumentNullException.ThrowIfNull(request);
                ConnectCount++;
                return ConnectCount == 1 ?
                    WaitForFirstAttemptCancellationAsync(ct) :
                    Task.FromResult(_connection);
            }

            private async Task<IManagedSessionConnection> WaitForFirstAttemptCancellationAsync(
                CancellationToken ct)
            {
                FirstAttemptStarted.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                    throw new InvalidOperationException("The first connect unexpectedly completed.");
                }
                catch (OperationCanceledException)
                {
                    FirstAttemptCanceled.TrySetResult();
                    throw;
                }
            }

            private readonly IManagedSessionConnection _connection;
        }

        private sealed class MultiProvider : IManagedSessionProvider
        {
            public MultiProvider(params IManagedSessionConnection[] connections)
            {
                _connections = connections;
            }

            public Task<IManagedSessionConnection> ConnectAsync(
                ManagedSessionConnectionRequest request, CancellationToken ct)
            {
                ArgumentNullException.ThrowIfNull(request);
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(_connections[_next++]);
            }

            private int _next;
            private readonly IManagedSessionConnection[] _connections;
        }
    }
}
