// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Fixtures {
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

    /// <summary>
    /// Adds sample server as fixture to unit tests
    /// </summary>
    public abstract class BaseServerFixture : IDisposable {

        /// <summary>
        /// Port server is listening on
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Logger
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Client
        /// </summary>
        public ClientServices Client { get; }

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
            Logger = LogEx.Trace(LogEventLevel.Debug);
            Client = new ClientServices(Logger);
            _serverHost = new ServerConsoleHost(
                new ServerFactory(Logger, nodes) {
                LogStatus = false
            }, Logger) {
                AutoAccept = true
            };
            var port = Interlocked.Increment(ref _nextPort);
            for (var i = 0; i < 200; i++) { // Retry 200 times
                try {
                    _serverHost.StartAsync(new int[] { port }).Wait();
                    Port = port;
                    break;
                }
                catch (Exception ex) {
                    Logger.Error(ex, "Failed to create server host, retrying...");
                    port = Interlocked.Increment(ref _nextPort);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _serverHost.Dispose();

            // Clean up all created certificates
            var certFolder = Path.Combine(Directory.GetCurrentDirectory(),
                "OPC Foundation");
            if (Directory.Exists(certFolder)) {
                Try.Op(() => Directory.Delete(certFolder, true));
            }
        }

        private static Random _rand = new Random();
        private static volatile int _nextPort = _rand.Next(53000, 58000);
        private readonly IServerHost _serverHost;
    }
}
