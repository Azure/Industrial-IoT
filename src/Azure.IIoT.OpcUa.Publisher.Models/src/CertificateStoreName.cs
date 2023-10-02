// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// The types of certificate stores
    /// </summary>
    [DataContract]
    public enum CertificateStoreName
    {
        /// <summary>
        /// Own store
        /// </summary>
        [EnumMember(Value = "Application")]
        Application,

        /// <summary>
        /// Rejected store
        /// </summary>
        [EnumMember(Value = "Rejected")]
        Rejected,

        /// <summary>
        /// Trusted store
        /// </summary>
        [EnumMember(Value = "Trusted")]
        Trusted,

        /// <summary>
        /// Https certificates
        /// </summary>
        [EnumMember(Value = "Https")]
        Https,

        /// <summary>
        /// User store
        /// </summary>
        [EnumMember(Value = "User")]
        User,

        /// <summary>
        /// Opc Ua certificate issuer store
        /// </summary>
        [EnumMember(Value = "Issuer")]
        Issuer,

        /// <summary>
        /// Https certificate issuer store
        /// </summary>
        [EnumMember(Value = "HttpsIssuer")]
        HttpsIssuer,

        /// <summary>
        /// User issuer store
        /// </summary>
        [EnumMember(Value = "UserIssuer")]
        UserIssuer,
    }
}
