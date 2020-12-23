// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// State of the endpoint after activation
    /// </summary>
    public enum EndpointConnectivityState {

        /// <summary>
        /// Client connecting to endpoint
        /// </summary>
        Connecting,

        /// <summary>
        /// Server not reachable
        /// </summary>
        NotReachable,

        /// <summary>
        /// Server busy
        /// </summary>
        Busy,

        /// <summary>
        /// Client is not trusted
        /// </summary>
        NoTrust,

        /// <summary>
        /// Server certificate is invalid
        /// </summary>
        CertificateInvalid,

        /// <summary>
        /// Connected and ready
        /// </summary>
        Ready,

        /// <summary>
        /// General connection error
        /// </summary>
        Error,

        /// <summary>
        /// Client disconnected
        /// </summary>
        Disconnected,

        /// <summary>
        /// User is not authorized to connect.
        /// </summary>
        Unauthorized
    }
}
