// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.Subscriptions;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

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
            session.VerifyAll();
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
        /// State changes map from the public managed session event to Publisher events.
        /// </summary>
        [Fact]
        public void FacadeMapsManagedConnectionStateChanges()
        {
            var session = CreateSession(out _, out _);
            var connection = new FakeConnection(session.Object);
            using var facade = new ManagedOpcUaSession(connection, CreateTelemetry());
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

        private static ManagedSessionConnectionRequest CreateRequest(string endpointUrl)
        {
            var connection = new ConnectionModel
            {
                Endpoint = new EndpointModel
                {
                    Url = endpointUrl
                }
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

        private static Mock<ISession> CreateSession(out ConfiguredEndpoint endpoint,
            out IUserIdentity identity)
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
            session.SetupGet(s => s.Connected).Returns(false);
            session.SetupGet(s => s.Endpoint).Returns(description);
            return session;
        }

        private static ITelemetryContext CreateTelemetry()
        {
            return new LoggerTelemetryContext(NullLoggerFactory.Instance);
        }

        private sealed class FakeConnection : IManagedSessionConnection
        {
            public FakeConnection(ISession session)
            {
                Session = session;
            }

            public ISession Session { get; }

            public int DisposeCount { get; private set; }

            public TaskCompletionSource Disposed { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

            public ValueTask DisposeAsync()
            {
                DisposeCount++;
                Disposed.TrySetResult();
                return ValueTask.CompletedTask;
            }

            public void Raise(ConnectionState state)
            {
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
                {
                    NewState = state
                });
            }
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

            public ManagedSessionConnectionRequest? Request { get; private set; }

            private readonly IManagedSessionConnection _connection;
        }
    }
}
