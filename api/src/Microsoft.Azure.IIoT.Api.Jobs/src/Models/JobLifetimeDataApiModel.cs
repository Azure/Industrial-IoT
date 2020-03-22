// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
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

        [DataMember(Name = "status",
            EmitDefaultValue = false)]
        public JobStatus Status { get; set; }

        /// <summary>
        /// Processing status
        /// </summary>
        [DataMember(Name = "processingStatus",
            EmitDefaultValue = false)]
        public Dictionary<string, ProcessingStatusApiModel> ProcessingStatus { get; set; }

        /// <summary>
        /// Updated at
        /// </summary>
        [DataMember(Name = "updated")]
        public DateTime Updated { get; set; }

        /// <summary>
        /// Created at
        /// </summary>
        [DataMember(Name = "created")]
        public DateTime Created { get; set; }
    }
}