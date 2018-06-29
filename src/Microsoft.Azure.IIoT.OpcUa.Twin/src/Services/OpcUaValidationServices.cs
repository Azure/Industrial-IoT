// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using Opc.Ua.Bindings.Proxy;
    using System;

    /// <summary>
    /// Validator opens a connection to the server to test connectivity.
    /// </summary>
    public class OpcUaProxyValidationServices : OpcUaValidationServices {

        /// <summary>
        /// Create edge endpoint validator
        /// </summary>
        /// <param name="client"></param>
        public OpcUaProxyValidationServices(IOpcUaClient client, ILogger logger) :
            base(client, logger) {
        }

        /// <summary>
        /// Retrieve the host name or ip address from the session
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        protected override string GetAddressFromChannel(ITransportChannel channel) {
            // Access underlying proxy socket
            if (channel is IMessageSocketChannel proxyChannel) {
                if (proxyChannel.Socket is ProxyMessageSocket socket) {
                    var proxySocket = socket.ProxySocket;
                    if (proxySocket == null) {
                        throw new InvalidProgramException(
                            "Unexpected - current proxy socket is null.");
                    }
                    _logger.Debug($"Connected.", () => proxySocket.LocalEndPoint);

                    var address = proxySocket.RemoteEndPoint.AsProxySocketAddress();
                    return address?.Host;
                }
                if (proxyChannel.Socket is TcpMessageSocket tcp) {
                    // TODO
                }
            }
            return null;
        }
    }
}
