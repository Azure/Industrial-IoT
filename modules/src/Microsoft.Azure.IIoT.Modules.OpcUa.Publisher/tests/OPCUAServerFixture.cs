namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Sample;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Moq;    
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using static Microsoft.Azure.IIoT.Hub.Mock.IoTHubServices;

    /// <summary>
    /// Base class for integration testing, it instantiates OPC UA server, runs publisher and injects mocked IoTHub services.
    /// </summary>
    public class OPCUAServerFixture : IDisposable {
        /// <summary>
        /// Whether the module is running.
        /// </summary>
        public BlockingCollection<EventMessage> Events { get; set; } = new BlockingCollection<EventMessage>();

        public OPCUAServerFixture() {           
            // Start Server
            _serverWrapper = new ServerWrapper(Mock.Of<ILogger>());
        }

        /// <summary>
        /// Wraps server and disposes after use.
        /// </summary>
        private class ServerWrapper : IDisposable {
            /// <summary>
            /// Create a wrapper.
            /// </summary>
            public ServerWrapper(ILogger logger) {
                _cts = new CancellationTokenSource();
                _server = RunSampleServerAsync(_cts.Token, logger);
            }

            /// <inheritdoc/>
            public void Dispose() {
                _cts.Cancel();
                _server.Wait();
                _cts.Dispose();
            }

            /// <summary>
            /// Run server until cancelled
            /// </summary>
            private static async Task RunSampleServerAsync(CancellationToken ct, ILogger logger) {
                var tcs = new TaskCompletionSource<bool>();
                ct.Register(() => tcs.TrySetResult(true));

                // We can inject specific node managers in ServerFactory if needed.
                using (var server = new ServerConsoleHost(new ServerFactory(logger) { LogStatus = false }, logger) { AutoAccept = true }) {
                    logger.Information("Starting server.");
                    await server.StartAsync(new List<int> { SERVICE_PORT });
                    logger.Information("Server started.");
                    await tcs.Task;
                    logger.Information("Server exited.");
                }
            }

            private readonly CancellationTokenSource _cts;
            private readonly Task _server;
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose() => Dispose(true);

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }

            if (disposing) {
                // Dispose managed state (managed objects).
                try {
                    _serverWrapper.Dispose();
                }
                catch (Opc.Ua.ServiceResultException) {
                    // Ignored.
                }
            }

            _disposed = true;
        }

        private const int SERVICE_PORT = 52210;
        private readonly ServerWrapper _serverWrapper;
        // To detect redundant calls
        private bool _disposed = false;
    }
}
