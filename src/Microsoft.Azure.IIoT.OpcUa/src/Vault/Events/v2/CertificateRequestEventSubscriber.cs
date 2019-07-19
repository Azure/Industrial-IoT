// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Events.v2.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Certificate Request change listener
    /// </summary>
    public class CertificateRequestEventSubscriber : IEventHandler<CertificateRequestEventModel>, IDisposable {

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="listeners"></param>
        public CertificateRequestEventSubscriber(IEventBus bus,
            IEnumerable<ICertificateRequestListener> listeners) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _listeners = listeners?.ToList() ?? new List<ICertificateRequestListener>();
            _token = _bus.RegisterAsync(this).Result;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _bus.UnregisterAsync(_token).Wait();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(CertificateRequestEventModel eventData) {
            switch (eventData.EventType) {
                case CertificateRequestEventType.Submitted:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnCertificateRequestSubmittedAsync(
                            eventData.Request)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case CertificateRequestEventType.Approved:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnCertificateRequestApprovedAsync(
                            eventData.Request)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case CertificateRequestEventType.Completed:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnCertificateRequestCompletedAsync(
                            eventData.Request)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case CertificateRequestEventType.Accepted:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnCertificateRequestAcceptedAsync(
                            eventData.Request)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
                case CertificateRequestEventType.Deleted:
                    await Task.WhenAll(_listeners
                        .Select(l => l.OnCertificateRequestDeletedAsync(
                            eventData.Request)
                        .ContinueWith(t => Task.CompletedTask)));
                    break;
            }
        }

        private readonly IEventBus _bus;
        private readonly List<ICertificateRequestListener> _listeners;
        private readonly string _token;
    }
}
