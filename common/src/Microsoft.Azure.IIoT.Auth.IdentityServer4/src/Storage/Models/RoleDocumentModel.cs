// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Role document
    /// </summary>
    [DataContract]
    public class RoleDocumentModel {

        /// <summary>
        /// Role id
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Etag
        /// </summary>
        [DataMember(Name = "etag")]
        public string ConcurrencyStamp { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Normalized name
        /// </summary>
        [DataMember]
        public string NormalizedName { get; set; }

        /// <summary>
        /// Claims
        /// </summary>
        [DataMember]
        public List<ClaimModel> Claims { get; set; }
    }
}
