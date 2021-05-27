// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Simple attribute operand model
    /// </summary>
    [DataContract]
    public class PendingAlarmsOptionsApiModel {

        /// <summary>
        /// Is pending alarm enabled?
        /// </summary>
        [DataMember(Name = "enabled", Order = 0)]
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Update interval for pending alarm
        /// </summary>
        [DataMember(Name = "updateInterval", Order = 1,
            EmitDefaultValue = false)]
        public int? UpdateInterval { get; set; }

        /// <summary>
        /// Snapshot interval for pending alarm
        /// </summary>
        [DataMember(Name = "snapshotInterval", Order = 2,
            EmitDefaultValue = false)]
        public int? SnapshotInterval { get; set; }

        /// <summary>
        /// Should we compress messages using GZip?
        /// </summary>
        [DataMember(Name = "compressMessages", Order = 3)]
        public bool CompressMessages { get; set; } = false;
    }
}