// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Proxy {
    using Microsoft.Azure.IIoT.Proxy.Provider.Legacy;
    using Microsoft.Azure.IIoT.Proxy.Provider;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Stack;
    using Opc.Ua;
    using Opc.Ua.Bindings.Proxy;

    /// <summary>
    /// Opc ua stack client that registers proxy transport
    /// </summary>
    public class OpcUaProxyClient : OpcUaClient {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="logger"></param>
        public OpcUaProxyClient(ILogger logger, IIoTHubConfig config) :
            base(logger) {

            // initialize our custom transport via the proxy
            DefaultProvider.Set(new IoTHubProvider(config.IoTHubConnString));
            WcfChannelBase.g_CustomTransportChannel =
                new ProxyTransportChannelFactory();

            logger.Info("OPC stack configured with reverse proxy connection.",
                () => { });

            Timeout = 120000;
        }
    }
}
