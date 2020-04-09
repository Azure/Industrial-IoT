// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Publish in bulk request
    /// </summary>
    public class PublishBulkRequestApiModel {

        /// <summary>
        /// Node to add
        /// </summary>
        [DataMember(Name = "nodesToAdd", Order = 0,
            EmitDefaultValue = false)]
        public List<PublishedItemApiModel> NodesToAdd { get; set; }

        /// <summary>
        /// Node to remove
        /// </summary>
        [DataMember(Name = "nodesToRemove", Order = 1,
            EmitDefaultValue = false)]
        public List<string> NodesToRemove { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 2,
            EmitDefaultValue = false)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
