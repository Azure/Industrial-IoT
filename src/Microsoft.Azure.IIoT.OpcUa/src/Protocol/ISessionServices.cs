// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Session manager
    /// </summary>
    public interface ISessionServices : IDisposable{

        /// <summary>
        /// Activation events
        /// </summary>
        event EventHandler SessionActivated;

        /// <summary>
        /// Session creation events
        /// </summary>
        event EventHandler SessionCreated;

        /// <summary>
        /// Session timeout events
        /// </summary>
        event EventHandler SessionTimeout;

        /// <summary>
        /// Session closing events
        /// </summary>
        event EventHandler SessionClosing;

        /// <summary>
        /// Create session
        /// </summary>
        /// <param name="context"></param>
        /// <param name="serverCertificate"></param>
        /// <param name="clientNonce"></param>
        /// <param name="clientCertificate"></param>
        /// <param name="requestedimeout"></param>
        /// <param name="sessionId"></param>
        /// <param name="authenticationToken"></param>
        /// <param name="serverNonce"></param>
        /// <param name="revisedTimeout"></param>
        /// <returns></returns>
        ISession CreateSession(RequestContextModel context,
            X509Certificate2 serverCertificate,
            byte[] clientNonce, X509Certificate2 clientCertificate,
            double requestedimeout,
            out NodeId sessionId, out NodeId authenticationToken,
            out byte[] serverNonce, out double revisedTimeout);

        /// <summary>
        /// Activate session identified by the passed authentication token
        /// using the specified identity.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="authenticationToken"></param>
        /// <param name="clientSignature"></param>
        /// <param name="clientSoftwareCertificates"></param>
        /// <param name="userIdentityToken"></param>
        /// <param name="userTokenSignature"></param>
        /// <param name="localeIds"></param>
        /// <param name="serverNonce"></param>
        /// <returns></returns>
        bool ActivateSession(RequestContextModel context,
            NodeId authenticationToken, SignatureData clientSignature,
            List<SoftwareCertificate> clientSoftwareCertificates,
            ExtensionObject userIdentityToken, SignatureData userTokenSignature,
            StringCollection localeIds, out byte[] serverNonce);

        /// <summary>
        /// Validate request against sessions
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="requestType"></param>
        /// <returns></returns>
        RequestContextModel GetContext(RequestHeader requestHeader,
            RequestType requestType);

        /// <summary>
        /// Get all active sessions
        /// </summary>
        /// <returns></returns>
        IList<ISession> GetSessions();

        /// <summary>
        /// Close session with specified id
        /// </summary>
        /// <param name="sessionId"></param>
        void CloseSession(NodeId sessionId);
    }
}
