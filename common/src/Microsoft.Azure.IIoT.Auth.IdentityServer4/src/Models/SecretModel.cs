// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Models {
    using System;
    using System.Runtime.Serialization;
    using global::IdentityServer4;

    /// <summary>
    /// Models a client secret with identifier and expiration
    /// </summary>
    [DataContract]
    public class SecretModel {

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [DataMember]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the expiration.
        /// </summary>
        [DataMember]
        public DateTime? Expiration { get; set; }

        /// <summary>
        /// Gets or sets the type of the client secret.
        /// </summary>
        [DataMember]
        public string Type { get; set; } =
            IdentityServerConstants.SecretTypes.SharedSecret;
    }
}