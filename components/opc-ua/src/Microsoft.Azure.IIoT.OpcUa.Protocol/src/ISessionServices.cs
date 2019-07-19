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
    public interface ISessionServices : IDisposable {

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
        /// User validation handlers
        /// </summary>
        event UserIdentityHandler ValidateUser;

        /// <summary>
        /// Creates a new session and monitors it for timeout.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <param name="serverCertificate"></param>
        /// <param name="clientNonce"></param>
        /// <param name="clientCertificate"></param>
        /// <param name="requestedTimeout"></param>
        /// <param name="sessionId"></param>
        /// <param name="authenticationToken"></param>
        /// <param name="serverNonce"></param>
        /// <param name="revisedTimeout"></param>
        /// <returns></returns>
        IServerSession CreateSession(RequestContextModel context,
            EndpointDescription endpoint, X509Certificate2 serverCertificate,
            byte[] clientNonce, X509Certificate2 clientCertificate,
            double requestedTimeout,
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
        IList<IServerSession> GetSessions();

        /// <summary>
        /// Close session with specified id
        /// </summary>
        /// <param name="sessionId"></param>
        void CloseSession(NodeId sessionId);
    }

    /// <summary>
    /// Handler args
    /// </summary>
    public class UserIdentityHandlerArgs {

        /// <summary>
        /// Token to validate
        /// </summary>
        public UserIdentityToken Token { get; set; }

        /// <summary>
        /// Effective identities
        /// </summary>
        public List<IUserIdentity> CurrentIdentities { get; set; }

        /// <summary>
        /// New set of identitites after token application
        /// </summary>
        public List<IUserIdentity> NewIdentities { get; set; }

        /// <summary>
        /// Exception to throw
        /// </summary>
        public ServiceResultException ValidationException { get; set; }
    }

    /// <summary>
    /// Delegate to validate user identity tokens
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void UserIdentityHandler(object sender, UserIdentityHandlerArgs args);
}
