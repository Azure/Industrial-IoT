// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Publish in bulk request
    /// </summary>
    [DataContract]
    public sealed record class PublishBulkRequestModel {

        /// <summary>
        /// Node to add
        /// </summary>
        [DataMember(Name = "nodesToAdd", Order = 0,
            EmitDefaultValue = false)]
        public List<PublishedItemModel>? NodesToAdd { get; set; }

        /// <summary>
        /// Node to remove
        /// </summary>
        [DataMember(Name = "nodesToRemove", Order = 1,
            EmitDefaultValue = false)]
        public List<string>? NodesToRemove { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 2,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }
    }
}
