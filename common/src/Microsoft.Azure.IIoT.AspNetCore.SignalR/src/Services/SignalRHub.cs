// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.SignalR.Services {
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.AspNetCore.SignalR;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Signalr hub for hosting inside Asp.net core host.
    /// </summary>
    public class SignalRHub<THub> : ICallbackInvokerT<THub>,
        IGroupRegistrationT<THub> where THub : Hub {

        /// <inheritdoc/>
        public string Resource { get; }

        /// <summary>
        /// Create signalR event bus
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="logger"></param>
        public SignalRHub(IHubContext<THub> hub, ILogger logger) {
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Resource = NameAttribute.GetName(typeof(THub));
        }

        /// <inheritdoc/>
        public async Task BroadcastAsync(string method, object[] arguments,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(method)) {
                throw new ArgumentNullException(nameof(method));
            }
            try {
                await _hub.Clients.All.SendCoreAsync(method,
                    arguments ?? Array.Empty<object>(), ct);
            }
            catch (Exception ex) {
                _logger.Verbose(ex, "Failed to send broadcast message");
            }
        }

        /// <inheritdoc/>
        public async Task UnicastAsync(string target, string method, object[] arguments,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(method)) {
                throw new ArgumentNullException(nameof(method));
            }
            if (string.IsNullOrEmpty(target)) {
                throw new ArgumentNullException(nameof(target));
            }
            try {
                await _hub.Clients.User(target).SendCoreAsync(method,
                    arguments ?? Array.Empty<object>(), ct);
            }
            catch (Exception ex) {
                _logger.Debug(ex, "Failed to send unicast message");
            }
         }

        /// <inheritdoc/>
        public async Task MulticastAsync(string group, string method, object[] arguments,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(method)) {
                throw new ArgumentNullException(nameof(method));
            }
            if (string.IsNullOrEmpty(group)) {
                throw new ArgumentNullException(nameof(group));
            }
            try {
                await _hub.Clients.Group(group).SendCoreAsync(method,
                    arguments ?? Array.Empty<object>(), ct);
            }
            catch (Exception ex) {
                _logger.Verbose(ex, "Failed to send multicast message");
            }
        }

        /// <inheritdoc/>
        public Task SubscribeAsync(string group, string client,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(client)) {
                throw new ArgumentNullException(nameof(client));
            }
            if (string.IsNullOrEmpty(group)) {
                throw new ArgumentNullException(nameof(group));
            }
            return _hub.Groups.AddToGroupAsync(client, group, ct);
        }

        /// <inheritdoc/>
        public Task UnsubscribeAsync(string group, string client,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(client)) {
                throw new ArgumentNullException(nameof(client));
            }
            if (string.IsNullOrEmpty(group)) {
                throw new ArgumentNullException(nameof(group));
            }
            return _hub.Groups.RemoveFromGroupAsync(client, group, ct);
        }

        /// <inheritdoc/>
        public void Dispose() {
            // No op
        }

        private readonly IHubContext<THub> _hub;
        private readonly ILogger _logger;
    }
}