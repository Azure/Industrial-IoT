// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Opc.Ua;
    using Opc.Ua.Server;

    /// <summary>
    /// Session interface
    /// </summary>
    public interface ISession {

        /// <summary>
        /// Session id
        /// </summary>
        NodeId Id { get; }

        /// <summary>
        /// Session scope message context
        /// </summary>
        ServiceMessageContext MessageContext { get; }

        /// <summary>
        /// Validate request
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="requestType"></param>
        void ValidateRequest(RequestHeader requestHeader,
            RequestType requestType);
    }
}
