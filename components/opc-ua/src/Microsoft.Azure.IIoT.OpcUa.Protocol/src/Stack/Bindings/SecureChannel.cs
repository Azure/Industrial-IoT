// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Bindings {
    using System;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Manages the server side of a UA TCP channel.
    /// </summary>
    internal sealed class SecureChannel : TcpServerChannel {

        /// <summary>
        /// Create channel
        /// </summary>
        /// <param name="contextId"></param>
        /// <param name="listener"></param>
        /// <param name="bufferManager"></param>
        /// <param name="quotas"></param>
        /// <param name="serverCertificate"></param>
        /// <param name="serverCertificateChain"></param>
        /// <param name="endpoints"></param>
        public SecureChannel(string contextId, ITcpChannelListener listener,
            BufferManager bufferManager, ChannelQuotas quotas,
            X509Certificate2 serverCertificate,
            X509Certificate2Collection serverCertificateChain,
            EndpointDescriptionCollection endpoints) :
            base(contextId, listener, bufferManager, quotas, serverCertificate,
                serverCertificateChain, endpoints) {
            _endpoints = endpoints;
        }

        /// <summary>
        /// Attaches the channel to an existing socket.
        /// </summary>
        public void Attach(uint channelId, IMessageSocket socket) {
            if (socket == null) {
                throw new ArgumentNullException(nameof(socket));
            }
            lock (DataLock) {
                // check for existing socket.
                if (Socket != null) {
                    throw new InvalidOperationException(
                        "Channel is already attached to a socket.");
                }
                ChannelId = channelId;
                State = TcpChannelState.Connecting;
                Socket = socket;
                Socket.ReadNextMessage();
                // automatically clean up the channel if no hello received.
                StartCleanupTimer(StatusCodes.BadTimeout);
            }
        }

        /// <inheritdoc/>
        protected override bool SetEndpointUrl(string endpointUrl) {
            //
            // Called on HELLO.  We update all our endpoints to match the
            // requested url then call select.
            //
            var url = Utils.ParseUri(endpointUrl);
            if (url == null) {
                return false;
            }
            foreach (var ep in _endpoints) {
                var expectedUrl = Utils.ParseUri(ep.EndpointUrl);
                if (expectedUrl == null) {
                    continue;
                }
                if (expectedUrl.Scheme != url.Scheme) {
                    continue;
                }
                ep.EndpointUrl = endpointUrl;
            }
            return base.SetEndpointUrl(endpointUrl);
        }

        private readonly EndpointDescriptionCollection _endpoints;
    }
}
