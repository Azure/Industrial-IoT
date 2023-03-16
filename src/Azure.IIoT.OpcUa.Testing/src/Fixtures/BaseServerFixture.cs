// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Testing.Fixtures
{
    using Azure.IIoT.OpcUa.Testing.Runtime;
    using Azure.IIoT.OpcUa.Models;
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

    /// <summary>
    /// Adds sample server as fixture to unit tests
    /// </summary>
    public abstract class BaseServerFixture : IDisposable
    {
        /// <summary>
        /// Host server is running on
        /// </summary>
        public IPHostEntry Host { get; }

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
        /// Get server connection
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ConnectionModel GetConnection(string path = null)
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
        protected BaseServerFixture(IEnumerable<INodeManagerFactory> nodes,
            ILoggerFactory loggerFactory = null)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            Host = Try.Op(() => Dns.GetHostEntry(Utils.GetHostName()))
                ?? Try.Op(() => Dns.GetHostEntry("localhost"));
            _container = CreateContainer(loggerFactory ?? Log.ConsoleFactory(LogLevel.Debug));

            // Retry 200 times
            var logger = _container.Resolve<ILogger<BaseServerFixture>>();
            var options = _container.Resolve<IOptions<ClientOptions>>();
            for (var i = 0; i < 200; i++)
            {
                try
                {
                    _serverHost = new ServerConsoleHost(new ServerFactory(
                        _container.Resolve<ILogger<ServerFactory>>(), nodes)
                    {
                        LogStatus = false
                    }, _container.Resolve<ILogger<ServerConsoleHost>>())
                    {
                        PkiRootPath = options.Value.Security.PkiRootPath,
                        AutoAccept = true
                    };
                    var port = GetRandomPort();
                    logger.LogInformation("Starting server host on {Port}...",
                        port);
                    _serverHost.StartAsync(new int[] { port }).Wait();
                    Port = port;
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to start server host, retrying...");
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
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

        private static int GetRandomPort()
        {
#pragma warning disable CA5394 // Do not use insecure randomness
            return Random.Shared.Next(53000, 58000);
#pragma warning restore CA5394 // Do not use insecure randomness
        }

        private bool _disposedValue;
        private readonly IContainer _container;
        private readonly IServerHost _serverHost;
    }
}
