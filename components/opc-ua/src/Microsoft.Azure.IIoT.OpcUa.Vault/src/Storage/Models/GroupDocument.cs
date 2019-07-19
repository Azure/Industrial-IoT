// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Storage.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Group document
    /// </summary>
    [Serializable]
    public sealed class GroupDocument {

        /// <summary>
        /// The id of the group.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string GroupId { get; set; }

        /// <summary>
        /// Etag
        /// </summary>
        [JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }

        /// <summary>
        /// The parent id of the group.
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// The name of the group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Document type
        /// </summary>
        public string ClassType { get; set; } = ClassTypeName;

        /// <summary>
        /// The group type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The subject as distinguished name.
        /// </summary>
        public string SubjectName { get; set; }

        /// <summary>
        /// The issued certificate lifetime.
        /// </summary>
        public TimeSpan IssuedLifetime { get; set; }

        /// <summary>
        /// The issued certificate key size in bits.
        /// </summary>
        public ushort IssuedKeySize { get; set; }

        /// <summary>
        /// The issued certificate signature algorithm.
        /// </summary>
        public SignatureAlgorithm IssuedSignatureAlgorithm { get; set; }

        /// <summary>
        /// The issuer CA certificate lifetime.
        /// </summary>
        public TimeSpan Lifetime { get; set; }

        /// <summary>
        /// The issuer CA certificate key size in bits.
        /// </summary>
        public ushort KeySize { get; set; }

        /// <summary>
        /// The issuer signature algorithm.
        /// </summary>
        public SignatureAlgorithm SignatureAlgorithm { get; set; }

        /// <inheritdoc/>
        public static readonly string ClassTypeName = "Group";
    }
}
