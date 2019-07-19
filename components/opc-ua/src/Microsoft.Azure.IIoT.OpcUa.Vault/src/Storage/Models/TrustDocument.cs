// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Storage.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Trust relationship document
    /// </summary>
    [Serializable]
    public sealed class TrustDocument {

        /// <summary>
        /// The id of the relationship.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Etag
        /// </summary>
        [JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }

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

        /// <summary>
        /// Document type
        /// </summary>
        public string ClassType { get; set; } = ClassTypeName;

        /// <inheritdoc/>
        public static readonly string ClassTypeName = "Trust";
    }
}
