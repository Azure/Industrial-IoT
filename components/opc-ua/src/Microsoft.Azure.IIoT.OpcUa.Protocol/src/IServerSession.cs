// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Opc.Ua;
    using Opc.Ua.Server;
    using System.Collections.Generic;

    /// <summary>
    /// Server side session
    /// </summary>
    public interface IServerSession {

        /// <summary>
        /// Session id
        /// </summary>
        NodeId Id { get; }

        /// <summary>
        /// Endpoint the session is on
        /// </summary>
        EndpointDescription Endpoint { get; }

        /// <summary>
        /// Current user identities
        /// </summary>
        List<IUserIdentity> Identities { get; }

        /// <summary>
        /// Session scope message context
        /// </summary>
        IServiceMessageContext MessageContext { get; }

        /// <summary>
        /// Validate request
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="requestType"></param>
        void ValidateRequest(RequestHeader requestHeader,
            RequestType requestType);
    }
}
