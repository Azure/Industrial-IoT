// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Testing.Fixtures {
    using Azure.IIoT.OpcUa.Testing.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Sample;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
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
        /// Cert folder
        /// </summary>
        public string PkiRootPath { get; private set; }

        /// <summary>
        /// Logger
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Client
        /// </summary>
        public OpcUaClientManager Client => _client.Value;

        /// <summary>
        /// Create fixture
        /// </summary>
        protected BaseServerFixture(IEnumerable<INodeManagerFactory> nodes) {
            if (nodes == null) {
                throw new ArgumentNullException(nameof(nodes));
            }
            Logger = Log.Console<BaseServerFixture>(LogLevel.Debug);
            _config = new TestClientServicesConfig();
            _client = new Lazy<OpcUaClientManager>(() => {
                return new OpcUaClientManager(Logger, _config);
            }, false);
            PkiRootPath = Path.Combine(Directory.GetCurrentDirectory(), "pki",
               Guid.NewGuid().ToByteArray().ToBase16String());
            var port = Interlocked.Increment(ref _nextPort);
            for (var i = 0; i < 200; i++) { // Retry 200 times
                try {
                    _serverHost = new ServerConsoleHost(
                        new ServerFactory(Logger, nodes) {
                            LogStatus = false
                        }, Logger) {
                        PkiRootPath = PkiRootPath,
                        AutoAccept = true
                    };
                    Logger.LogInformation("Starting server host on {port}...",
                        port);
                    _serverHost.StartAsync(new int[] { port }).Wait();
                    Port = port;
                    break;
                }
                catch (Exception ex) {
                    port = Interlocked.Increment(ref _nextPort);
                    Logger.LogError(ex, "Failed to start server host, retrying {port}...",
                        port);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            Logger.LogInformation("Disposing server and client fixture...");
            _serverHost.Dispose();
            // Clean up all created certificates
            if (Directory.Exists(PkiRootPath)) {
                Logger.LogInformation("Server disposed - cleaning up server certificates...");
                Try.Op(() => Directory.Delete(PkiRootPath, true));
            }
            if (_client.IsValueCreated) {
                Logger.LogInformation("Disposing client...");
                Task.Run(() => _client.Value.Dispose()).Wait();
            }
            Logger.LogInformation("Client disposed - cleaning up client certificates...");
            _config?.Dispose();
            Logger.LogInformation("Server and client fixture disposed.");
        }

        private static readonly Random kRand = new();
        private static volatile int _nextPort = kRand.Next(53000, 58000);
        private readonly IServerHost _serverHost;
        private readonly TestClientServicesConfig _config;
        private readonly Lazy<OpcUaClientManager> _client;
    }
}
