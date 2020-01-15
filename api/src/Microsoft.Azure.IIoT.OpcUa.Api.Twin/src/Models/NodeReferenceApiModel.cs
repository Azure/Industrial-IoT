// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// reference model
    /// </summary>
    public class NodeReferenceApiModel {

        /// <summary>
        /// Reference Type id
        /// </summary>
        [JsonProperty(PropertyName = "referenceTypeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ReferenceTypeId { get; set; }

        /// <summary>
        /// Browse direction of reference
        /// </summary>
        [JsonProperty(PropertyName = "direction",
            NullValueHandling = NullValueHandling.Ignore)]
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        [JsonProperty(PropertyName = "target")]
        [Required]
        public NodeApiModel Target { get; set; }

        // Legacy

        /// <ignore/>
        [JsonIgnore]
        [Obsolete]
        public string TypeId => ReferenceTypeId;

        /// <ignore/>
        [JsonIgnore]
        [Obsolete]
        public string BrowseName => Target?.BrowseName;

        /// <ignore/>
        [JsonIgnore]
        [Obsolete]
        public string DisplayName => Target?.DisplayName;

        /// <ignore/>
        [JsonIgnore]
        [Obsolete]
        public string TypeDefinition => Target?.TypeDefinitionId;
    }
}
