// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Tests {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Sample;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.IO;
    using Xunit;

    [CollectionDefinition(Name)]
    public class ReadCollection : ICollectionFixture<ServerFixture> {

        public const string Name = "Read";
    }

    [CollectionDefinition(Name)]
    public class WriteCollection : ICollectionFixture<ServerFixture> {

        public const string Name = "Write";
    }

    /// <summary>
    /// Adds sample server as fixture to unit tests
    /// </summary>
    public class ServerFixture : IDisposable {

        /// <summary>
        /// Port server is listening on
        /// </summary>
        public int Port { get; } = _nextPort++;

        /// <summary>
        /// Logger
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Client
        /// </summary>
        public ClientServices Client { get; }

        /// <summary>
        /// Create fixture
        /// </summary>
        public ServerFixture() {
            Logger = new TraceLogger();
            Client = new ClientServices(Logger);
            _serverHost = new ServerConsoleHost(new SampleServerFactory(Logger) {
                LogStatus = false
            }, Logger) {
                AutoAccept = true
            };
            _serverHost.StartAsync(new int[] { Port }).Wait();
        }

        /// <inheritdoc/>
        public void Dispose() {
            _serverHost.Dispose();

            // Clean up all created certificates
            Try.Op(() => Directory.Delete(
                Path.Combine(Directory.GetCurrentDirectory(),
                "OPC Foundation"), true));
        }

        private static int _nextPort = 58888;
        private readonly IServerHost _serverHost;
    }
}
