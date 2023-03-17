// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
#nullable enable
namespace Azure.IIoT.OpcUa.Publisher.Testing.Fixtures
{
    using Azure.IIoT.OpcUa.Publisher.Testing.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Stack.Sample;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Autofac;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography.X509Certificates;
    using Opc.Ua.Test;
    using Moq;
    using System.Timers;
    using System.Collections.Concurrent;

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
        /// Port server is listening on
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Certificate of the server
        /// </summary>
        public X509Certificate2 Certificate => _serverHost.Certificate;

        /// <summary>
        /// Client
        /// </summary>
        public OpcUaClientManager Client => _container.Resolve<OpcUaClientManager>();

        /// <summary>
        /// Now
        /// </summary>
        public DateTime Now { get; private set; }

        /// <summary>
        /// Time service
        /// </summary>
        public TimeService TimeService => _timeService.Object;

        /// <summary>
        /// Get server connection
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ConnectionModel GetConnection(string? path = null)
        {
            return new ConnectionModel
            {
                Endpoint = new EndpointModel
                {
                    Url = $"opc.tcp://{Host?.HostName ?? "localhost"}:{Port}/{path ?? "UA/SampleServer"}",
                    AlternativeUrls = Host?.AddressList
                        .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ip => $"opc.tcp://{ip}:{Port}/{path ?? "UA/SampleServer"}")
                        .ToHashSet(),
                    Certificate = Certificate?.RawData?.ToThumbprint()
                }
            };
        }

        /// <summary>
        /// Create fixture
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="loggerFactory"></param>
        protected BaseServerFixture(
            Func<ILoggerFactory?, TimeService, IEnumerable<INodeManagerFactory>> nodesFactory,
            ILoggerFactory? loggerFactory = null)
        {
            Host = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
            _container = CreateContainer(loggerFactory ?? Log.ConsoleFactory(LogLevel.Debug));

            var now = DateTime.UtcNow;
            Now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Kind);
            _timeService = CreateTimeServiceMock(Now);
            var port = NextPort();
            var logger = _container.Resolve<ILogger<BaseServerFixture>>();
            var options = _container.Resolve<IOptions<ClientOptions>>();
            var nodes = nodesFactory(_container.Resolve<ILoggerFactory>(), TimeService);
            while (true)
            {
                try
                {
                    var ep = $"{Host?.HostName ?? "localhost"}:{port}";
                    _serverHost = new ServerConsoleHost(new ServerFactory(
                        _container.Resolve<ILogger<ServerFactory>>(), nodes)
                    {
                        LogStatus = false
                    }, _container.Resolve<ILogger<ServerConsoleHost>>())
                    {
                        PkiRootPath = options.Value.Security.PkiRootPath,
                        AutoAccept = true
                    };
                    logger.LogInformation("Starting server host on {Port}...",
                        port);
                    _serverHost.StartAsync(new int[] { port }).Wait();
                    Port = port;
                    break;
                }
                catch (Exception ex)
                {
                    kPorts.AddOrUpdate(Port, false, (_, _) => false);
                    port = NextPort();
                    logger.LogError(ex,
                        "Failed to start server host, retrying {Port}...", port);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
                    var logger = _container.Resolve<ILogger<BaseServerFixture>>();
                    logger.LogInformation("Disposing server and client fixture...");
                    _serverHost.Dispose();

                    // Clean up all created certificates
                    if (_container.TryResolve<IOptions<ClientOptions>>(out var options) &&
                        Directory.Exists(options.Value.Security.PkiRootPath))
                    {
                        logger.LogInformation("Server disposed - cleaning up server certificates...");
                        Try.Op(() => Directory.Delete(options.Value.Security.PkiRootPath, true));
                    }
                    _container.Dispose();
                    logger.LogInformation("Client disposed - cleaning up client certificates...");
                    kPorts.TryRemove(Port, out _);
                }
                _disposedValue = true;
            }
        }

        private static IContainer CreateContainer(ILoggerFactory loggerFactory)
        {
            var builder = new ContainerBuilder();
            builder.AddLogging();
            builder.RegisterInstance(new ConfigurationBuilder().Build())
                .AsImplementedInterfaces();
            builder.RegisterInstance(loggerFactory)
                .AsImplementedInterfaces();

            builder.AddDefaultJsonSerializer();
            builder.RegisterType<TestClientConfig>()
                .AsImplementedInterfaces();

            builder.RegisterType<StackLogger>()
                .AsImplementedInterfaces().SingleInstance().AutoActivate();
            builder.RegisterType<OpcUaClientManager>()
                .AsImplementedInterfaces().SingleInstance().AsSelf();
            builder.RegisterType<ClientConfig>()
                .AsImplementedInterfaces().SingleInstance();
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
        public void FireTimersWithPeriod(TimeSpan period, int numberOfTimes)
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
        private IList<Action> GetTimerHandlersForPeriod(uint periodInMilliseconds)
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
        private readonly IContainer _container;
        private readonly IServerHost _serverHost;
        private readonly Mock<TimeService> _timeService;
    }
}
