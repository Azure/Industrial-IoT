// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// State of the endpoint after activation
    /// </summary>
    [DataContract]
    public enum EndpointConnectivityState
    {
        /// <summary>
        /// Client connecting to endpoint
        /// </summary>
        [EnumMember(Value = "Connecting")]
        Connecting,

        /// <summary>
        /// Server not reachable
        /// </summary>
        [EnumMember(Value = "NotReachable")]
        NotReachable,

        /// <summary>
        /// Server busy - try later
        /// </summary>
        [EnumMember(Value = "Busy")]
        Busy,

        /// <summary>
        /// Client is not trusted - update client cert
        /// </summary>
        [EnumMember(Value = "NoTrust")]
        NoTrust,

        /// <summary>
        /// Server certificate is invalid - update server certificate
        /// </summary>
        [EnumMember(Value = "CertificateInvalid")]
        CertificateInvalid,

        /// <summary>
        /// Connected and ready
        /// </summary>
        [EnumMember(Value = "Ready")]
        Ready,

        /// <summary>
        /// Any other connection error
        /// </summary>
        [EnumMember(Value = "Error")]
        Error,

        /// <summary>
        /// Client disconnected
        /// </summary>
        [EnumMember(Value = "Disconnected")]
        Disconnected,

        /// <summary>
        /// User is not authorized to connect.
        /// </summary>
        [EnumMember(Value = "Unauthorized")]
        Unauthorized
    }
}
