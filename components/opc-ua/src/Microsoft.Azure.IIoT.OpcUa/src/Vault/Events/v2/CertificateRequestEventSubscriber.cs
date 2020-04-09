// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Events.v2.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Certificate Request change listener
    /// </summary>
    public class CertificateRequestEventSubscriber : IEventHandler<CertificateRequestEventModel> {

        /// <summary>
        /// Create event subscriber
        /// </summary>
        /// <param name="listeners"></param>
        public CertificateRequestEventSubscriber(IEnumerable<ICertificateRequestListener> listeners) {
            _listeners = listeners?.ToList() ?? new List<ICertificateRequestListener>();
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

        private readonly List<ICertificateRequestListener> _listeners;
    }
}
