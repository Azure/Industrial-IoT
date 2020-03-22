// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Authentication Method model
    /// </summary>
    public class AuthenticationMethodModel {

        /// <summary>
        /// Method id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Type of credential
        /// </summary>
        public CredentialType? CredentialType { get; set; }

        /// <summary>
        /// Security policy to use when passing credential.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Method specific configuration
        /// </summary>
        public VariantValue Configuration { get; set; }
    }
}
