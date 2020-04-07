// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Type of credential to use for serverauth
    /// </summary>
    [DataContract]
    public enum CredentialType {

        /// <summary>
        /// No credentials
        /// </summary>
        [EnumMember]
        None,

        /// <summary>
        /// User name with secret
        /// </summary>
        [EnumMember]
        UserName,

        /// <summary>
        /// Certificate
        /// </summary>
        [EnumMember]
        X509Certificate,

        /// <summary>
        /// Token is a jwt token
        /// </summary>
        [EnumMember]
        JwtToken
    }
}
