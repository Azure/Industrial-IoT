// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Lifetime data
    /// </summary>
    [DataContract]
    public class JobLifetimeDataApiModel {

        /// <summary>
        /// Status
        /// </summary>

        [DataMember(Name = "status", Order = 0,
            EmitDefaultValue = false)]
        public JobStatus Status { get; set; }

        /// <summary>
        /// Processing status
        /// </summary>
        [DataMember(Name = "processingStatus", Order = 1,
            EmitDefaultValue = false)]
        public Dictionary<string, ProcessingStatusApiModel> ProcessingStatus { get; set; }

        /// <summary>
        /// Updated at
        /// </summary>
        [DataMember(Name = "updated", Order = 2)]
        public DateTime Updated { get; set; }

        /// <summary>
        /// Created at
        /// </summary>
        [DataMember(Name = "created", Order = 3)]
        public DateTime Created { get; set; }
    }
}