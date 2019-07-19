// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {

    /// <summary>
    /// Trust relationship model
    /// </summary>
    public class TrustRelationshipModel {

        /// <summary>
        /// The trusted entity, e.g. group (= issuer),
        /// single application, endpoint.
        /// </summary>
        public string TrustedId { get; set; }

        /// <summary>
        /// The type of the trusted entity
        /// </summary>
        public EntityType TrustedType { get; set; }

        /// <summary>
        /// The trusting entity, e.g. client
        /// </summary>
        public string TrustingId { get; set; }

        /// <summary>
        /// The type of the trusting entity
        /// </summary>
        public EntityType TrustingType { get; set; }
    }
}