// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {

    /// <summary>
    /// Trust group registration model
    /// </summary>
    public sealed class TrustGroupRegistrationModel {

        /// <summary>
        /// The registered id of the trust group
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Trust group
        /// </summary>
        public TrustGroupModel Group { get; set; }
    }
}
