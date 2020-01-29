// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Scanner {
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// A connect probe is a ip endpoint consumer that tries to connect
    /// to the consumed ip endpoint.  if successful it uses the probe
    /// implementation to interrogate the server.
    /// </summary>
    /// <returns></returns>
    public sealed class ConnectProbe : BaseConnectProbe {

        /// <summary>
        /// Create connect probe
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="timeout"></param>
        public ConnectProbe(ILogger logger, int timeout) :
            base(0, new NullProbe(), logger) {
            _timeout = timeout;
            _queue = new BlockingCollection<IPEndPoint>();
            _tasks = new ConcurrentDictionary<IPEndPoint, TaskCompletionSource<bool>>();
        }

        /// <summary>
        /// Probe an address
        /// </summary>
        /// <param name="ep"></param>
        /// <returns></returns>
        public Task<bool> ProbeAsync(IPEndPoint ep) {
            var tcs = _tasks.GetOrAdd(ep, k => new TaskCompletionSource<bool>());
            return tcs.Task;
        }

        /// <summary>
        /// Test whether endpoint exists
        /// </summary>
        /// <param name="ep"></param>
        /// <returns></returns>
        public async Task<IPEndPoint> ExistsAsync(IPEndPoint ep) {
            try {
                return await ProbeAsync(ep) ? ep : null;
            }
            catch {
                return null;
            }
        }

        /// <inheritdoc/>
        protected override bool Next(out IPEndPoint ep, out int timeout) {
            timeout = _timeout;
            return _queue.TryTake(out ep);
        }

        /// <inheritdoc/>
        protected override void OnFail(IPEndPoint ep) {
            if (_tasks.TryRemove(ep, out var tcs)) {
                tcs.TrySetException(new Exception());
            }
        }

        /// <inheritdoc/>
        protected override void OnSuccess(IPEndPoint ep) {
            if (_tasks.TryRemove(ep, out var tcs)) {
                tcs.TrySetResult(true);
            }
        }

        /// <inheritdoc/>
        protected override void OnComplete(IPEndPoint ep) {
            if (_tasks.TryRemove(ep, out var tcs)) {
                tcs.TrySetResult(false);
            }
        }

        /// <inheritdoc/>
        public override void Dispose() {
            base.Dispose();
            _queue.Dispose();
        }

        private class NullProbe : IAsyncProbe {

            /// <inheritdoc />
            public bool CompleteAsync(int index, SocketAsyncEventArgs arg,
                out bool ok, out int timeout) {
                ok = true;
                timeout = 0;
                return true;
            }

            /// <inheritdoc />
            public void Dispose() { }

            /// <inheritdoc />
            public bool Reset() {
                return false;
            }
        }

        private readonly BlockingCollection<IPEndPoint> _queue;
        private readonly ConcurrentDictionary<IPEndPoint, TaskCompletionSource<bool>> _tasks;
        private readonly int _timeout;
    }
}
