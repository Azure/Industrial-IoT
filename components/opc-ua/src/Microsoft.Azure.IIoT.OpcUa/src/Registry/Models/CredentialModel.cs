// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Credential model
    /// </summary>
    public class CredentialModel {

        /// <summary>
        /// Type of credential
        /// </summary>
        public CredentialType? Type { get; set; }

        /// <summary>
        /// Credential to pass to server.
        /// </summary>
        public JToken Value { get; set; }
    }
}
