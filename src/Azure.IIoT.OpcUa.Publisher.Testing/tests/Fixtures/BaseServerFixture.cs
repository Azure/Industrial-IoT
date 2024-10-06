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
    using Autofac;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
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
    using System.Timers;
    using System.Threading.Tasks;

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
            => _container.Resolve<IOpcUaClientManager<ConnectionModel>>();

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
        /// Filter parser
        /// </summary>
        public IFilterParser Parser => _container.Resolve<IFilterParser>();

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
            => (UseReverseConnect ? Utils.GetHostName() : Host?.HostName) ?? "localhost";

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
                    AlternativeUrls = Host?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{_port}/{kSampleServerPath}")
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
        protected BaseServerFixture(
            Func<ILoggerFactory?, TimeService, IEnumerable<INodeManagerFactory>> nodesFactory,
            ILoggerFactory? loggerFactory = null, bool useReverseConnect = false)
        {
            var sw = Stopwatch.StartNew();
            Host = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
            _container = CreateContainer(loggerFactory ?? Log.ConsoleFactory(LogLevel.Debug));

            Now = new DateTime(2023, 1, 1, 7, 15, 0, DateTimeKind.Utc);
            _timeService = CreateTimeServiceMock(Now);

            _port = NextPort();
            var logger = _container.Resolve<ILogger<BaseServerFixture>>();
            var options = _container.Resolve<IOptions<OpcUaClientOptions>>();
            var nodes = nodesFactory(_container.Resolve<ILoggerFactory>(), TimeService);
            ServerConsoleHost? serverHost = null;
            while (true)
            {
                try
                {
                    serverHost = new ServerConsoleHost(new ServerFactory(
                        _container.Resolve<ILogger<ServerFactory>>(), TempPath, nodes)
                    {
                        LogStatus = false
                    }, _container.Resolve<ILogger<ServerConsoleHost>>())
                    {
                        PkiRootPath = options.Value.Security.PkiRootPath,
                        AutoAccept = true
                    };
                    logger.LogInformation("Starting server host {Host} on {Port}...",
                        serverHost, _port);
                    serverHost.StartAsync(new int[] { _port }).Wait();

                    //
                    // Test server connection. Sometimes the server has not
                    // started and tests are failing with Not reachable, this
                    // should ensure the server has started up correctly.
                    //
                    var endpoint =
                        _container.Resolve<IConnectionServices<ConnectionModel>>();
                    var result = endpoint.TestConnectionAsync(new ConnectionModel
                    {
                        Endpoint = new EndpointModel
                        {
                            Url = EndpointUrl
                        }
                    }, new TestConnectionRequestModel()).WaitAsync(TimeSpan.FromSeconds(10))
                        .GetAwaiter().GetResult();
                    if (result.ErrorInfo != null)
                    {
                        throw new IOException(
                            result.ErrorInfo.ErrorMessage ?? "Failed testing connection.");
                    }

                    logger.LogInformation("Server host {Host} listening on {EndpointUrl}!",
                        serverHost, EndpointUrl);
                    _serverHost = serverHost;
                    if (!useReverseConnect)
                    {
                        break;
                    }

                    int clientPort;
                    // Find a port for the client
                    while (true)
                    {
                        clientPort = NextPort();
                        try
                        {
                            logger.LogInformation(
                                "Try adding reverse connect client on {Port}...", clientPort);
                            using var listener = TcpListener.Create(clientPort);
                            listener.Start(); // Throws if used and cleans up.
                            listener.Stop();  // Cleanup
                            break;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Port {Port} is not accessible...", clientPort);
                            kPorts.AddOrUpdate(clientPort, false, (_, _) => false);
                        }
                    }
                    UseReverseConnect = true;
                    ReverseConnectPort = clientPort;
                    var clientUrl = $"opc.tcp://{HostName}:{clientPort}";
                    _serverHost.AddReverseConnectionAsync(new Uri(clientUrl), 4)
                        .WaitAsync(TimeSpan.FromMinutes(1)).GetAwaiter().GetResult();
                    logger.LogInformation("Start reverse connect to client at {Url}...", clientUrl);
                    break;
                }
                catch (Exception ex)
                {
                    kPorts.AddOrUpdate(_port, false, (_, _) => false);
                    _port = NextPort();
                    logger.LogError(ex, "Failed to start host {Host}, retrying with port {Port}...",
                        serverHost, _port);
                    serverHost?.Dispose();
                    serverHost = null;
                }
            }
            logger.LogInformation("Server host {Host} started in {Elapsed}...", serverHost, sw.Elapsed);
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
                    var logger = _container.Resolve<ILogger<BaseServerFixture>>();
                    logger.LogInformation("Disposing server host {Host} and client fixture...",
                        _serverHost);

                    string? pkiPath = null;
                    if (_container.TryResolve<IOptions<OpcUaClientOptions>>(out var options) &&
                        Directory.Exists(options.Value.Security.PkiRootPath))
                    {
                        pkiPath = options.Value.Security.PkiRootPath;
                    }

                    _container.Dispose();
                    _serverHost.Dispose();
                    kPorts.TryRemove(_port, out _);

                    logger.LogInformation("Client fixture and server host {Host} disposed - " +
                        "cleaning up server certificates at '{PkiRoot}' ({Elapsed})...",
                        _serverHost, pkiPath, sw.Elapsed);

                    // Clean up all created certificates
                    if (!string.IsNullOrEmpty(pkiPath) && Directory.Exists(pkiPath))
                    {
                        Try.Op(() => Directory.Delete(pkiPath, true));
                    }
                    logger.LogInformation("Disposing Server took {Elapsed}...", sw.Elapsed);

                    if (Directory.Exists(TempPath))
                    {
                        Try.Op(() => Directory.Delete(TempPath, true));
                    }
                }
                _disposedValue = true;
            }
        }

        private static IContainer CreateContainer(ILoggerFactory loggerFactory)
        {
            var builder = new ContainerBuilder();
            builder.ConfigureServices(services => services.AddLogging());
            builder.RegisterInstance(new ConfigurationBuilder().Build())
                .AsImplementedInterfaces();
            builder.RegisterInstance(loggerFactory)
                .AsImplementedInterfaces();

            builder.AddDefaultJsonSerializer();
            // builder.AddNewtonsoftJsonSerializer();
            builder.RegisterType<TestClientConfig>()
                .AsImplementedInterfaces();

            builder.AddOpcUaStack();
            return builder.Build();
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

        /// <summary>
        /// Get another port
        /// </summary>
        /// <returns></returns>
        private static int NextPort()
        {
            while (true)
            {
#pragma warning disable CA5394 // Do not use insecure randomness
                var port = Random.Shared.Next(53000, 58000);
#pragma warning restore CA5394 // Do not use insecure randomness
                if (kPorts.TryAdd(port, true))
                {
                    return port;
                }
            }
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
            ElapsedEventHandler handler)> _timers = new();
        /// <summary> Registry of mocked fast timers. </summary>
        private readonly ConcurrentBag<(ITimer timer,
            EventHandler<FastTimerElapsedEventArgs> handler)> _fastTimers = new();
        private static readonly ConcurrentDictionary<int, bool> kPorts = new();
        private bool _disposedValue;
        private readonly int _port;
        private readonly IContainer _container;
        private readonly ServerConsoleHost _serverHost;
        private readonly Mock<TimeService> _timeService;
        private const string kSampleServerPath = "UA/SampleServer";
    }
}
