// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Opc.Ua;
    using Opc.Ua.Server;

    /// <summary>
    /// Extend operation context to provide additional context
    /// stashing options
    /// </summary>
    public class RequestContextModel : OperationContext {

        /// <summary>
        /// The session
        /// </summary>
        public new IServerSession Session { get; }

        /// <inheritdoc/>
        public RequestContextModel(RequestHeader requestHeader,
            RequestType requestType, IUserIdentity identity = null) :
            base(requestHeader, requestType, identity) {
        }

        /// <inheritdoc/>
        public RequestContextModel(RequestHeader requestHeader,
            RequestType requestType, IServerSession session) :
            base(requestHeader, requestType) {
            Session = session;
        }
    }
}
