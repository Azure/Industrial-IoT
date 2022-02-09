// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Transport {
    using Opc.Ua;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Implemented by a server to attach to listeners
    /// </summary>
    public interface IServer {

        /// <summary>
        /// Server description that listner should use to
        /// stuff their endoint descriptions.
        /// </summary>
        ApplicationDescription ServerDescription { get; }

        /// <summary>
        /// Returns the listener callback for transport
        /// listeners to call.
        /// </summary>
        /// <returns></returns>
        ITransportListenerCallback Callback { get; }

        /// <summary>
        /// Returns the servers instance certificate to
        /// use by listeners as certificate fallback.
        /// </summary>
        X509Certificate2 Certificate { get; }

        /// <summary>
        /// Returns the servers certificate chain to
        /// use by listeners as fallback.
        /// </summary>
        X509Certificate2Collection CertificateChain { get; }

        /// <summary>
        /// Returns the servers message context
        /// </summary>
        IServiceMessageContext MessageContext { get; }

        /// <summary>
        /// Returns the servers certificate validator instance
        /// </summary>
        CertificateValidator CertificateValidator { get; }

        /// <summary>
        /// Register transport endpoints
        /// </summary>
        /// <param name="endpoints"></param>
        void Register(EndpointDescriptionCollection endpoints);

        /// <summary>
        /// Unregister endpoints
        /// </summary>
        /// <param name="endpoints"></param>
        void Unregister(EndpointDescriptionCollection endpoints);
    }
}
