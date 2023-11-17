// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Result of bulk request
    /// </summary>
    [DataContract]
    public sealed record class PublishBulkResponseModel
    {
        /// <summary>
        /// Node to add
        /// </summary>
        [DataMember(Name = "nodesToAdd", Order = 0,
            EmitDefaultValue = false)]
        public IReadOnlyList<ServiceResultModel>? NodesToAdd { get; set; }

        /// <summary>
        /// Node to remove
        /// </summary>
        [DataMember(Name = "nodesToRemove", Order = 1,
            EmitDefaultValue = false)]
        public IReadOnlyList<ServiceResultModel>? NodesToRemove { get; set; }
    }
}
