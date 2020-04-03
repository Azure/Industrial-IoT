// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Heatbeat response
    /// </summary>
    [DataContract]
    public class HeartbeatResponseApiModel {

        /// <summary>
        /// Instructions
        /// </summary>
        [DataMember(Name = "heartbeatInstruction", Order = 0)]
        public HeartbeatInstruction HeartbeatInstruction { get; set; }

        /// <summary>
        /// Last active
        /// </summary>
        [DataMember(Name = "lastActiveHeartbeat", Order = 1,
            EmitDefaultValue = false)]
        public DateTime? LastActiveHeartbeat { get; set; }

        /// <summary>
        /// Job continuation in case of updates
        /// </summary>
        [DataMember(Name = "updatedJob", Order = 2,
            EmitDefaultValue = false)]
        public JobProcessingInstructionApiModel UpdatedJob { get; set; }
    }
}