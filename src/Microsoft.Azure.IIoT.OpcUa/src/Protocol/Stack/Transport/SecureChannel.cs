/* Copyright (c) 1996-2016, OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
   version 2 of the License are accompanied with this source code. See http://opcfoundation.org/License/GPLv2
   This source code is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
*/

namespace Opc.Ua.Transport {
    using Opc.Ua.Bindings;
    using System;
    using System.Security.Cryptography.X509Certificates;
    // TODO: Remove when changes are in stack
    using TcpServerChannel = Bindings.Fork.TcpServerChannel;
    using TcpChannelState = Bindings.Fork.TcpChannelState;
    using ITcpServerChannelListener = Bindings.Fork.ITcpServerChannelListener;
    // END TODO

    /// <summary>
    /// Manages the server side of a UA TCP channel.
    /// </summary>
    public class SecureChannel : TcpServerChannel {

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
        public SecureChannel(string contextId, ITcpServerChannelListener listener,
            BufferManager bufferManager, ChannelQuotas quotas,
            X509Certificate2 serverCertificate,
            X509Certificate2Collection serverCertificateChain,
            EndpointDescriptionCollection endpoints) :
            base(contextId, listener, bufferManager, quotas,
                serverCertificate, serverCertificateChain, endpoints) {
        }

        /// <summary>
        /// The real channel url
        /// </summary>
        public Uri EndpointUrl { get; private set; }

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

                Utils.Trace("TCPSERVERCHANNEL SOCKET ATTACHED: {0:X8}, ChannelId={1}",
                    Socket.Handle, ChannelId);
                Socket.ReadNextMessage();
                // automatically clean up the channel if no hello received.
                StartCleanupTimer(StatusCodes.BadTimeout);
            }
        }

        /// <inheritdoc/>
        protected override bool SetEndpointUrl(string endpointUrl) {
            var url = Utils.ParseUri(endpointUrl);
            if (url == null) {
                return false;
            }

            if (url.Segments.Length != 0) {
                // Not discovery url.
                ReviseSecurityMode(true, MessageSecurityMode.SignAndEncrypt);
            }
            EndpointUrl = url;
            return true;
        }
    }
}
