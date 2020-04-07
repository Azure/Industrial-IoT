// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Storage.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// Trust relationship document
    /// </summary>
    [DataContract]
    public sealed class TrustDocument {

        /// <summary>
        /// The id of the relationship.
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Etag
        /// </summary>
        [DataMember(Name = "_etag")]
        public string ETag { get; set; }

        /// <summary>
        /// The trusted entity, e.g. group (= issuer),
        /// single application, endpoint.
        /// </summary>
        [DataMember]
        public string TrustedId { get; set; }

        /// <summary>
        /// The type of the trusted entity
        /// </summary>
        [DataMember]
        public EntityType TrustedType { get; set; }

        /// <summary>
        /// The trusting entity, e.g. client
        /// </summary>
        [DataMember]
        public string TrustingId { get; set; }

        /// <summary>
        /// The type of the trusting entity
        /// </summary>
        [DataMember]
        public EntityType TrustingType { get; set; }

        /// <summary>
        /// Document type
        /// </summary>
        [DataMember]
        public string ClassType { get; set; } = ClassTypeName;

        /// <inheritdoc/>
        public static readonly string ClassTypeName = "Trust";
    }
}
