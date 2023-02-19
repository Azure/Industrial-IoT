// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Result of bulk request
    /// </summary>
    public record class PublishBulkResponseModel {

        /// <summary>
        /// Node to add
        /// </summary>
        [DataMember(Name = "nodesToAdd", Order = 0,
            EmitDefaultValue = false)]
        public List<ServiceResultModel> NodesToAdd { get; set; }

        /// <summary>
        /// Node to remove
        /// </summary>
        [DataMember(Name = "nodesToRemove", Order = 1,
            EmitDefaultValue = false)]
        public List<ServiceResultModel> NodesToRemove { get; set; }
    }
}
