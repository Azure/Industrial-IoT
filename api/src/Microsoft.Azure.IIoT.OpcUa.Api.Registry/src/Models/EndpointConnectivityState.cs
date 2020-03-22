// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// State of the endpoint after activation
    /// </summary>
    [DataContract]
    public enum EndpointConnectivityState {

        /// <summary>
        /// Client connecting to endpoint
        /// </summary>
        [EnumMember]
        Connecting,

        /// <summary>
        /// Server not reachable
        /// </summary>
        [EnumMember]
        NotReachable,

        /// <summary>
        /// Server busy - try later
        /// </summary>
        [EnumMember]
        Busy,

        /// <summary>
        /// Client is not trusted - update client cert
        /// </summary>
        [EnumMember]
        NoTrust,

        /// <summary>
        /// Server certificate is invalid - update server certificate
        /// </summary>
        [EnumMember]
        CertificateInvalid,

        /// <summary>
        /// Connected and ready
        /// </summary>
        [EnumMember]
        Ready,

        /// <summary>
        /// Any other connection error
        /// </summary>
        [EnumMember]
        Error,

        /// <summary>
        /// Client disconnected
        /// </summary>
        [EnumMember]
        Disconnected,

        /// <summary>
        /// User is not authorized to connect.
        /// </summary>
        [EnumMember]
        Unauthorized
    }
}
