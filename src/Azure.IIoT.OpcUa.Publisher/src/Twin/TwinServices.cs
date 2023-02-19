// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Twin {
    using Azure.IIoT.OpcUa.Shared.Models;
    using Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages the endpoint identity information in the twin and reports
    /// the endpoint's status back to the hub.
    /// </summary>
    public class TwinServices : IDisposable {

        /// <inheritdoc/>
        public EndpointConnectivityState State { get; private set; }
            = EndpointConnectivityState.Disconnected;

        /// <summary>
        /// Create twin services
        /// </summary>
        /// <param name="events"></param>
        /// <param name="serializer"></param>
        public TwinServices(IEventEmitter events, IJsonSerializer serializer) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _endpoint = new TaskCompletionSource<EndpointModel>();
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_session != null) {
                _session.OnConnectionStateChange -= _session_OnConnectionStateChange;
                _session.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _session = null;
            }
            _endpoint?.TrySetCanceled();
            _endpoint = null;
            _lock?.Dispose();
        }

        /// <summary>
        /// Report state changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="state"></param>
        private void _session_OnConnectionStateChange(object sender, EndpointConnectivityState state) {
            State = state;
            if (_events != null) {
                Task.Run(() => _events.ReportAsync("State", _serializer.FromObject(state)));
            }
        }

        private ISessionHandle _session;
        private TaskCompletionSource<EndpointModel> _endpoint;
        private readonly IJsonSerializer _serializer;
        private readonly SemaphoreSlim _lock;
        private readonly IEventEmitter _events;
    }
}
