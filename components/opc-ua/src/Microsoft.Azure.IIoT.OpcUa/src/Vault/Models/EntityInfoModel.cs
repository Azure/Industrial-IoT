// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Entity info model
    /// </summary>
    public class EntityInfoModel {

        /// <summary>
        /// Identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Entity name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Entity type
        /// </summary>
        public EntityType Type { get; set; }

        /// <summary>
        /// Entity role
        /// </summary>
        public EntityRoleType? Role { get; set; }

        /// <summary>
        /// Subject distinguished name
        /// </summary>
        public string SubjectName { get; set; }

        /// <summary>
        /// List of addresses
        /// </summary>
        public List<string> Addresses { get; set; }

        /// <summary>
        /// List of uris
        /// </summary>
        public List<string> Uris { get; set; }
    }
}
