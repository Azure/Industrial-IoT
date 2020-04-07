// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using System.Runtime.Serialization;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Browse view model
    /// </summary>
    [DataContract]
    public class BrowseViewApiModel {

        /// <summary>
        /// Node of the view to browse
        /// </summary>
        [DataMember(Name = "viewId", Order = 0)]
        [Required]
        public string ViewId { get; set; }

        /// <summary>
        /// Browses specific version of the view.
        /// </summary>
        [DataMember(Name = "version", Order = 1,
            EmitDefaultValue = false)]
        public uint? Version { get; set; }

        /// <summary>
        /// Browses at or before this timestamp.
        /// </summary>
        [DataMember(Name = "timestamp", Order = 2,
            EmitDefaultValue = false)]
        public DateTime? Timestamp { get; set; }
    }
}
