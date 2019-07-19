// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Start New key pair request
    /// </summary>
    public sealed class StartNewKeyPairRequestModel {

        /// <summary>
        /// Entity identifier for which to get key pair
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Trust group identifier
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// Subject name to use
        /// </summary>
        public string SubjectName { get; set; }

        /// <summary>
        /// Additional domain names to use in certificate
        /// </summary>
        public List<string> DomainNames { get; set; }
    }
}
