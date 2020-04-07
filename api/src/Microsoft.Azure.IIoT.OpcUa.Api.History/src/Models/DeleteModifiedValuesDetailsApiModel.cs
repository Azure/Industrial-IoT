// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Delete raw modified data
    /// </summary>
    [DataContract]
    public class DeleteModifiedValuesDetailsApiModel {

        /// <summary>
        /// Start time
        /// </summary>
        [DataMember(Name = "startTime", Order = 0,
            EmitDefaultValue = false)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time to delete until
        /// </summary>
        [DataMember(Name = "endTime", Order = 1,
            EmitDefaultValue = false)]
        public DateTime? EndTime { get; set; }
    }
}
