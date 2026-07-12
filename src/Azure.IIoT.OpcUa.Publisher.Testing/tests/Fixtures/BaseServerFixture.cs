// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Fixtures
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Sample;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Core.Logging;
    using Try = Azure.IIoT.OpcUa.Core.Utils.Try;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using ITimer = Opc.Ua.Test.ITimer;

    /// <summary>
    /// Adds sample server as fixture to unit tests
    /// </summary>
    public abstract class BaseServerFixture : IDisposable
    {
        /// <summary>
        /// Host server is running on
        /// </summary>
        public IPHostEntry? Host { get; }

        /// <summary>
        /// Use reverse connect
        /// </summary>
        public bool UseReverseConnect { get; }

        /// <summary>
        /// Client port
        /// </summary>
        public int ReverseConnectPort { get; }

        /// <summary>
        /// Certificate of the server
        /// </summary>
        public X509Certificate2 Certificate => _serverHost.Certificate;

        /// <summary>
        /// Client
        /// </summary>
        public IOpcUaClientManager<ConnectionModel> Client
            => _container.GetRequiredService<IOpcUaClientManager<ConnectionModel>>();

        /// <summary>
        /// Now
        /// </summary>
        public DateTime Now { get; private set; }

        /// <summary>
        /// Time service
        /// </summary>
        public TimeService TimeService => _timeService.Object;

        /// <summary>
        /// Temporary path
        /// </summary>
        public string TempPath { get; }
            = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        /// <summary>
        /// Isolated server certificate store.
        /// </summary>
        public string ServerPkiRootPath => Path.Combine(TempPath, "server-pki");

        /// <summary>
        /// Isolated client certificate store.
        /// </summary>
        public string ClientPkiRootPath => Path.Combine(TempPath, "client-pki");

        /// <summary>
        /// Filter parser
        /// </summary>
        public IFilterParser Parser => _container.GetRequiredService<IFilterParser>();

        /// <summary>
        /// EndpointUrl
        /// </summary>
        public string EndpointUrl
            => $"opc.tcp://{HostName}:{_port}/{kSampleServerPath}";

        /// <summary>
        /// <para>Host name</para>
        /// <para>
        /// There is a quirk in registration matching inside the reconnect manager the
        /// server must present their endpoint in RHEL exactly like it is requested by
        /// the client. The reconnect manager compares to the endpoint url and if it is
        /// not the same it will reject.
        /// </para>
        /// <para>
        /// In this test the host name is the FQDN host name here, but the one it matches
        /// against and presented by the server is just the machine's host name and
        /// therefore rejects even though it is the same.
        /// </para>
        /// </summary>
        private string HostName
            => kLoopbackHost;

        /// <summary>
        /// Get server connection
        /// </summary>
        /// <returns></returns>
        public ConnectionModel GetConnection()
        {
            return new ConnectionModel
            {
                Endpoint = new EndpointModel
                {
                    Url = EndpointUrl,
                    AlternativeUrls = _alternativeHosts
                        .Select(host => GetEndpointUrl(host, _port))
                        .ToHashSet(),
                    Certificate = Certificate?.RawData?.ToThumbprint()
                },
                Options = UseReverseConnect ?
                    ConnectionOptions.UseReverseConnect : ConnectionOptions.None
            };
        }

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="nodesFactory"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="useReverseConnect"></param>
        /// <param name="alternativeHosts"></param>
        protected BaseServerFixture(
            Func<ILoggerFactory?, TimeService, IEnumerable<INodeManagerFactory>> nodesFactory,
            ILoggerFactory? loggerFactory = null, bool useReverseConnect = false,
            IEnumerable<string>? alternativeHosts = null)
        {
            var sw = Stopwatch.StartNew();
            Host = new IPHostEntry
            {
                HostName = kLoopbackHost,
                AddressList = [IPAddress.Loopback]
            };
            _alternativeHosts = alternativeHosts?
                .Where(host => !string.IsNullOrWhiteSpace(host))
                .Select(host => host.Trim())
                .Where(host => !StringComparer.OrdinalIgnoreCase.Equals(host, kLoopbackHost))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];
            _container = CreateContainer(loggerFactory ?? Log.ConsoleFactory(LogLevel.Debug),
                ClientPkiRootPath);

            Now = new DateTime(2023, 1, 1, 7, 15, 0, DateTimeKind.Utc);
            _timeService = CreateTimeServiceMock(Now);

            var logger = _container.GetRequiredService<ILogger<BaseServerFixture>>();
            var nodes = nodesFactory(_container.GetRequiredService<ILoggerFactory>(), TimeService);
            ServerConsoleHost? serverHost = null;
            ServerConsoleHost? startedServerHost = null;
            Exception? lastCollision = null;
            for (var attempt = 1; attempt <= kMaxStartupAttempts &&
                sw.Elapsed < kStartupTimeout; attempt++)
            {
                try
                {
                    // Serialize server construction/startup against other servers'
                    // startup AND condition refresh: startup loads predefined nodes
                    // and registers types into process-global stack state that is
                    // not safe to mutate while another server concurrently reads or
                    // mutates it (e.g. ConditionRefresh). See ServerStateLock.
                    if (!Monitor.TryEnter(ServerStateLock.Sync, kServerStateLockTimeout))
                    {
                        throw new TimeoutException(
                            $"Timed out waiting for the OPC UA server state lock after {kServerStateLockTimeout}.");
                    }
                    try
                    {
                        _port = ReserveServerPort(logger);
                        serverHost = new ServerConsoleHost(new ServerFactory(
                            _container.GetRequiredService<ILogger<ServerFactory>>(), TempPath, nodes)
                        {
                            LogStatus = false
                        }, _container.GetRequiredService<ILogger<ServerConsoleHost>>())
                        {
                            PkiRootPath = ServerPkiRootPath,
                            AutoAccept = true,
                            HostName = HostName,
                            AlternativeHosts = [.. _alternativeHosts]
                        };
                        logger.StartingServerHost(serverHost, _port);
                        serverHost.StartAsync([_port]).WaitAsync(kServerStartupTimeout)
                            .GetAwaiter().GetResult();
                    }
                    finally
                    {
                        Monitor.Exit(ServerStateLock.Sync);
                    }

                    //
                    // Test server connection. Sometimes the server has not
                    // started and tests are failing with Not reachable, this
                    // should ensure the server has started up correctly.
                    //
                    var endpoint =
                        _container.GetRequiredService<IConnectionServices<ConnectionModel>>();
                    var result = endpoint.TestConnectionAsync(new ConnectionModel
                    {
                        Endpoint = new EndpointModel
                        {
                            Url = EndpointUrl,
                            Certificate = serverHost.Certificate?.RawData?.ToThumbprint()
                        }
                    }, new TestConnectionRequestModel()).WaitAsync(kReadinessTimeout)
                        .GetAwaiter().GetResult();
                    if (result.ErrorInfo != null)
                    {
                        throw new IOException(
                            result.ErrorInfo.ErrorMessage ?? "Failed testing connection.");
                    }

                    if (!useReverseConnect)
                    {
                        startedServerHost = serverHost;
                        break;
                    }

                    var clientPort = ReserveReverseConnectPort(logger);
                    UseReverseConnect = true;
                    ReverseConnectPort = clientPort;
                    var clientUrl = $"opc.tcp://{HostName}:{clientPort}";
                    serverHost.AddReverseConnectionAsync(new Uri(clientUrl), 4)
                        .WaitAsync(kReverseConnectTimeout).GetAwaiter().GetResult();
                    logger.StartReverseConnect(clientUrl);
                    startedServerHost = serverHost;
                    break;
                }
                catch (Exception ex)
                {
                    kPorts.TryRemove(_port, out _);
                    if (serverHost != null)
                    {
                        Try.Op(serverHost.Dispose);
                    }
                    serverHost = null;
                    if (IsBindCollision(ex) && attempt < kMaxStartupAttempts &&
                        sw.Elapsed < kStartupTimeout)
                    {
                        lastCollision = ex;
                        logger.FailedToStartHost(ex, null, _port);
                        continue;
                    }
                    var failure = CreateStartupFailure(ex, attempt, sw.Elapsed);
                    CleanupStartupFailure();
                    throw failure;
                }
            }
            if (startedServerHost == null)
            {
                var failure = CreateStartupFailure(lastCollision ??
                    new TimeoutException("OPC UA server startup elapsed its time budget."),
                    kMaxStartupAttempts, sw.Elapsed);
                CleanupStartupFailure();
                throw failure;
            }
            _serverHost = startedServerHost;
            logger.ServerHostListening(_serverHost, EndpointUrl);
            logger.ServerHostStarted(_serverHost, sw.Elapsed);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Restart server
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public Task RestartAsync(Func<Task> predicate)
        {
            return _serverHost.RestartAsync(predicate);
        }

        /// <summary>
        /// Override to dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    var sw = Stopwatch.StartNew();
                    var logger = _container.GetRequiredService<ILogger<BaseServerFixture>>();
                    logger.DisposingServerHost(_serverHost);

                    // Serialize server teardown against other servers' startup
                    // and condition refresh. Disposing the host unregisters node
                    // managers / types from the same process-global OPC UA stack
                    // state that startup mutates; a teardown running concurrently
                    // with another server's startup corrupts that shared state and
                    // crashes the test host with a native access violation in
                    // Opc.Ua.ConditionState. See ServerStateLock. The GC drain
                    // below is deliberately left outside the lock.
                    if (!Monitor.TryEnter(ServerStateLock.Sync, kServerStateLockTimeout))
                    {
                        throw new TimeoutException(
                            $"Timed out waiting for the OPC UA server state lock after {kServerStateLockTimeout}.");
                    }
                    try
                    {
                        _container.Dispose();
                        _serverHost.Dispose();
                    }
                    finally
                    {
                        Monitor.Exit(ServerStateLock.Sync);
                    }
                    kPorts.TryRemove(_port, out _);

                    logger.ServerHostDisposed(_serverHost, ServerPkiRootPath, sw.Elapsed);

                    // Both certificate stores are fixture-owned and can only be
                    // removed after their client and server have been disposed.
                    foreach (var pkiPath in new[] { ClientPkiRootPath, ServerPkiRootPath })
                    {
                        if (Directory.Exists(pkiPath))
                        {
                            Try.Op(() => Directory.Delete(pkiPath, true));
                        }
                    }
                    logger.ServerDisposingElapsed(sw.Elapsed);

                    if (Directory.Exists(TempPath))
                    {
                        Try.Op(() => Directory.Delete(TempPath, true));
                    }

                    // The OPC UA stack creates several CertificateValidator /
                    // X509Certificate2 instances wrapping native CertContext and
                    // CNG SafeHandle objects whose Release runs on the finalizer
                    // queue. AdvancedPubSubIntegrationTests builds and tears
                    // down 12 ReferenceServer instances per slice (each holding
                    // 12 inner node managers), so cumulative finalizer pressure
                    // is enough to exhaust process-wide native handles on the
                    // Windows test-host and surface as the SEH crash family
                    // addressed in #2456, #2458 and #2464. Drain the finalizer
                    // queue here with a finite budget so teardown cannot hang.
                    GC.Collect();
                    _ = Task.Run(GC.WaitForPendingFinalizers)
                        .Wait(kFinalizerDrainTimeout);
                    GC.Collect();
                    Thread.Sleep(100);
                }
                _disposedValue = true;
            }
        }

        private static ServiceProvider CreateContainer(ILoggerFactory loggerFactory,
            string clientPkiRootPath)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [TestClientConfig.PkiRootPathKey] = clientPkiRootPath
                })
                .Build();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IConfigurationRoot>(configuration);
            services.AddSingleton(loggerFactory);

            services.AddTransientAsImplementedInterfaces<TestClientConfig>();

            services.AddOpcUaStack();
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Cause a subset of the mocked timers to fire a number of times,
        /// and the current mocked time to advance accordingly.
        /// </summary>
        /// <param name="period">Defines the timers to fire:
        /// only timers with this interval are fired.</param>
        /// <param name="numberOfTimes">Number of times the timer
        /// should be fired.</param>
#pragma warning disable CA1030 // Use events where appropriate
        public void FireTimersWithPeriod(TimeSpan period, int numberOfTimes)
#pragma warning restore CA1030 // Use events where appropriate
        {
            var matchedHandlers = GetTimerHandlersForPeriod((uint)period.TotalMilliseconds);
            for (var i = 0; i < numberOfTimes; i++)
            {
                Now += period;
                foreach (var handler in matchedHandlers)
                {
                    handler();
                }
            }
        }

        /// <summary>
        /// Retrieve the timer handlers for the given period
        /// </summary>
        /// <param name="periodInMilliseconds"></param>
        /// <returns></returns>
        private List<Action> GetTimerHandlersForPeriod(uint periodInMilliseconds)
        {
            var matchedTimers = _timers.Where(t
                    => t.timer.Enabled
                       && CloseTo(t.timer.Interval, periodInMilliseconds))
                .Select(t => (Action)(() => t.handler(null, null!)))
                .ToList();

            var matchedFastTimers = _fastTimers.Where(t
                    => t.timer.Enabled
                       && CloseTo(t.timer.Interval, periodInMilliseconds))
                .Select(t => (Action)(() => t.handler(null, null!)))
                .ToList();

            return matchedTimers.Union(matchedFastTimers).ToList();

            static bool CloseTo(double a, double b) =>
                Math.Abs(a - b) <= Math.Abs(a * .00001);
        }

        private static int NextPort()
        {
            for (var attempt = 0; attempt < kMaxPortReservationAttempts; attempt++)
            {
#pragma warning disable CA5394 // Do not use insecure randomness
                var port = Random.Shared.Next(53000, 58000);
#pragma warning restore CA5394 // Do not use insecure randomness
                if (kPorts.TryAdd(port, true))
                {
                    return port;
                }
            }
            throw new TimeoutException(
                $"Could not reserve an OPC UA test port after {kMaxPortReservationAttempts} attempts.");
        }

        private static int ReserveReverseConnectPort(ILogger logger)
        {
            for (var attempt = 0; attempt < kMaxPortReservationAttempts; attempt++)
            {
                var port = NextPort();
                try
                {
                    logger.TryAddingReverseConnect(port);
                    using var listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    return port;
                }
                catch (SocketException ex) when (IsBindCollision(ex))
                {
                    logger.PortNotAccessible(ex, port);
                    kPorts.TryRemove(port, out _);
                }
            }
            throw new TimeoutException(
                $"Could not reserve a reverse-connect port after {kMaxPortReservationAttempts} attempts.");
        }

        private static int ReserveServerPort(ILogger logger)
        {
            for (var attempt = 0; attempt < kMaxPortReservationAttempts; attempt++)
            {
                var port = NextPort();
                try
                {
                    using var listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    return port;
                }
                catch (SocketException ex) when (IsBindCollision(ex))
                {
                    logger.PortNotAccessible(ex, port);
                    kPorts.TryRemove(port, out _);
                }
            }
            throw new TimeoutException(
                $"Could not reserve an OPC UA server port after {kMaxPortReservationAttempts} attempts.");
        }

        private static bool IsBindCollision(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is SocketException socketException &&
                    socketException.SocketErrorCode is SocketError.AddressAlreadyInUse or
                    SocketError.AccessDenied)
                {
                    return true;
                }
                if (current.HResult == 10048 || current.HResult == 10013 ||
                    current.HResult == unchecked((int)0x80072740) ||
                    current.HResult == unchecked((int)0x8007271D) ||
                    current.Message.Contains("address already in use",
                        StringComparison.OrdinalIgnoreCase) ||
                    current.Message.Contains("only one usage of each socket address",
                        StringComparison.OrdinalIgnoreCase) ||
                    current.Message.Contains("failed to establish tcp listener sockets",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private InvalidOperationException CreateStartupFailure(Exception exception,
            int attempts, TimeSpan elapsed)
        {
            var alternativeUrls = _alternativeHosts.Length == 0
                ? "<none>"
                : string.Join(", ", _alternativeHosts.Select(host => GetEndpointUrl(host, _port)));
            return new InvalidOperationException(
                $"OPC UA test server startup failed after {attempts} attempt(s) in {elapsed}. " +
                $"Endpoint='{EndpointUrl}', alternative endpoints='{alternativeUrls}', " +
                $"server PKI='{ServerPkiRootPath}', client PKI='{ClientPkiRootPath}'.",
                exception);
        }

        private void CleanupStartupFailure()
        {
            kPorts.TryRemove(_port, out _);
            _container.Dispose();
            if (Directory.Exists(TempPath))
            {
                Try.Op(() => Directory.Delete(TempPath, true));
            }
        }

        private static string GetEndpointUrl(string host, int port)
        {
            return $"opc.tcp://{host}:{port}/{kSampleServerPath}";
        }

        /// <summary>
        /// Create a mocked time service for the tests to be able to
        /// control time in the server.
        /// </summary>
        /// <param name="now">The start time</param>
        /// <returns></returns>
        private Mock<TimeService> CreateTimeServiceMock(DateTime now)
        {
            var mock = new Mock<TimeService>();
            mock.Setup(f => f.NewTimer(
                It.IsAny<ElapsedEventHandler>(),
                It.IsAny<uint>()))
                .Returns((ElapsedEventHandler handler,
                    uint intervalInMilliseconds) =>
                {
                    var mockTimer = new Mock<ITimer>();
                    mockTimer.SetupAllProperties();
                    var timer = mockTimer.Object;
                    timer.Interval = intervalInMilliseconds;
                    timer.AutoReset = true;
                    timer.Enabled = true;
                    _timers.Add((timer, handler));
                    return timer;
                });
            mock.Setup(f => f.NewFastTimer(
                It.IsAny<EventHandler<FastTimerElapsedEventArgs>>(),
                It.IsAny<uint>()))
                .Returns((EventHandler<FastTimerElapsedEventArgs> handler,
                    uint intervalInMilliseconds) =>
                {
                    var mockTimer = new Mock<ITimer>();
                    mockTimer.SetupAllProperties();
                    var timer = mockTimer.Object;
                    timer.Interval = intervalInMilliseconds;
                    timer.AutoReset = true;
                    timer.Enabled = true;
                    _fastTimers.Add((timer, handler));
                    return timer;
                });

            mock.Setup(f => f.Now)
                .Returns(() => now);

            mock.Setup(f => f.UtcNow)
                .Returns(() => now);
            return mock;
        }

        /// <summary> Registry of mocked timers. </summary>
        private readonly ConcurrentBag<(ITimer timer,
            ElapsedEventHandler handler)> _timers = [];
        /// <summary> Registry of mocked fast timers. </summary>
        private readonly ConcurrentBag<(ITimer timer,
            EventHandler<FastTimerElapsedEventArgs> handler)> _fastTimers = [];
        private static readonly ConcurrentDictionary<int, bool> kPorts = new();
        private static readonly TimeSpan kStartupTimeout = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan kServerStartupTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan kReadinessTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan kReverseConnectTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan kServerStateLockTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan kFinalizerDrainTimeout = TimeSpan.FromSeconds(30);
        private const int kMaxStartupAttempts = 3;
        private const int kMaxPortReservationAttempts = 32;
        private const string kLoopbackHost = "127.0.0.1";
        private const string kSampleServerPath = "UA/SampleServer";
        private bool _disposedValue;
        private int _port;
        private readonly string[] _alternativeHosts;
        private readonly ServiceProvider _container;
        private readonly ServerConsoleHost _serverHost;
        private readonly Mock<TimeService> _timeService;
    }

    /// <summary>
    /// Generated logging methods for BaseServerFixture
    /// </summary>
    internal static partial class BaseServerFixtureLogging
    {
        private const int EventClass = 0;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Information,
            Message = "Starting server host {Host} on {Port}...")]
        public static partial void StartingServerHost(this ILogger logger, ServerConsoleHost host, int port);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Information,
            Message = "Server host {Host} listening on {EndpointUrl}!")]
        public static partial void ServerHostListening(this ILogger logger, ServerConsoleHost host, string endpointUrl);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Information,
            Message = "Try adding reverse connect client on {Port}...")]
        public static partial void TryAddingReverseConnect(this ILogger logger, int port);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Error,
            Message = "Port {Port} is not accessible...")]
        public static partial void PortNotAccessible(this ILogger logger, Exception ex, int port);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Information,
            Message = "Start reverse connect to client at {Url}...")]
        public static partial void StartReverseConnect(this ILogger logger, string url);

        [LoggerMessage(EventId = EventClass + 6, Level = LogLevel.Error,
            Message = "Failed to start host {Host}, retrying with port {Port}...")]
        public static partial void FailedToStartHost(this ILogger logger, Exception ex, ServerConsoleHost? host, int port);

        [LoggerMessage(EventId = EventClass + 7, Level = LogLevel.Information,
            Message = "Server host {Host} started in {Elapsed}...")]
        public static partial void ServerHostStarted(this ILogger logger, ServerConsoleHost host, TimeSpan elapsed);

        [LoggerMessage(EventId = EventClass + 8, Level = LogLevel.Information,
            Message = "Disposing server host {Host} and client fixture...")]
        public static partial void DisposingServerHost(this ILogger logger, ServerConsoleHost host);

        [LoggerMessage(EventId = EventClass + 9, Level = LogLevel.Information,
            Message = "Client fixture and server host {Host} disposed - cleaning up server certificates at '{PkiRoot}' ({Elapsed})...")]
        public static partial void ServerHostDisposed(this ILogger logger, ServerConsoleHost host, string? pkiRoot, TimeSpan elapsed);

        [LoggerMessage(EventId = EventClass + 10, Level = LogLevel.Information,
            Message = "Disposing Server took {Elapsed}...")]
        public static partial void ServerDisposingElapsed(this ILogger logger, TimeSpan elapsed);
    }
}
