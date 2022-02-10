// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// processing status
    /// </summary>
    [DataContract]
    public class ProcessingStatusApiModel {

        /// <summary>
        /// Last known heartbeat
        /// </summary>
        [DataMember(Name = "lastKnownHeartbeat", Order = 0,
            EmitDefaultValue = false)]
        public DateTime? LastKnownHeartbeat { get; set; }

        /// <summary>
        /// Last known state
        /// </summary>
        [DataMember(Name = "lastKnownState", Order = 1,
            EmitDefaultValue = false)]
        public VariantValue LastKnownState { get; set; }

        /// <summary>
        /// Processing mode
        /// </summary>
        [DataMember(Name = "processMode", Order = 2,
            EmitDefaultValue = false)]
        public ProcessMode? ProcessMode { get; set; }
    }
}