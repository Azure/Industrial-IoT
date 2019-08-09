// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures {
    using Microsoft.Azure.IIoT.OpcUa.Testing.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Sample;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua.Server;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using Serilog;
    using Serilog.Events;
    using System.Threading;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Adds sample server as fixture to unit tests
    /// </summary>
    public abstract class BaseServerFixture : IDisposable {

        /// <summary>
        /// Port server is listening on
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Certificate of the server
        /// </summary>
        public X509Certificate2 Certificate => _serverHost.Certificate;

        /// <summary>
        /// Logger
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Client
        /// </summary>
        public ClientServices Client => _client.Value;

        /// <summary>
        /// Start port
        /// </summary>
        public static int StartPort { set => _nextPort = value; }

        /// <summary>
        /// Create fixture
        /// </summary>
        protected BaseServerFixture(IEnumerable<INodeManagerFactory> nodes) {
            if (nodes == null) {
                throw new ArgumentNullException(nameof(nodes));
            }
            Logger = LogEx.ConsoleOut(LogEventLevel.Debug);
            _config = new TestClientServicesConfig();
            _client = new Lazy<ClientServices>(() => {
                return new ClientServices(Logger, _config);
            }, false);
            _serverHost = new ServerConsoleHost(
                new ServerFactory(Logger, nodes) {
                    LogStatus = false
                }, Logger) {
                AutoAccept = true
            };
            var port = Interlocked.Increment(ref _nextPort);
            for (var i = 0; i < 200; i++) { // Retry 200 times
                try {
                    Logger.Information("Starting server host on {port}...",
                        port);
                    _serverHost.StartAsync(new int[] { port }).Wait();
                    Port = port;
                    break;
                }
                catch (Exception ex) {
                    port = Interlocked.Increment(ref _nextPort);
                    Logger.Error(ex, "Failed to start server host, retrying {port}...",
                        port);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            Logger.Information("Disposing server and client fixture...");
            _serverHost.Dispose();
            // Clean up all created certificates
            var certFolder = Path.Combine(Directory.GetCurrentDirectory(), "OPC Foundation");
            if (Directory.Exists(certFolder)) {
                Logger.Information("Server disposed - cleaning up server certificates...");
                Try.Op(() => Directory.Delete(certFolder, true));
            }
            if (_client.IsValueCreated) {
                Logger.Information("Disposing client...");
                Task.Run(() => _client.Value.Dispose()).Wait();
                Logger.Information("Client disposed - cleaning up client certificates...");
                _config?.Dispose();
            }
            Logger.Information("Server and client fixture disposed.");
        }

        private static readonly Random kRand = new Random();
        private static volatile int _nextPort = kRand.Next(53000, 58000);
        private readonly IServerHost _serverHost;
        private readonly TestClientServicesConfig _config;
        private readonly Lazy<ClientServices> _client;
    }
}
