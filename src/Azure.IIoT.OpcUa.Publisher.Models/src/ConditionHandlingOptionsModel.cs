// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Condition handling options model
    /// </summary>
    [DataContract]
    public sealed record class ConditionHandlingOptionsModel
    {
        /// <summary>
        /// Time interval for sending pending interval updates in seconds.
        /// </summary>
        [DataMember(Name = "updateInterval", Order = 1,
            EmitDefaultValue = false)]
        public int? UpdateInterval { get; set; }

        /// <summary>
        /// Time interval for sending pending interval snapshot in seconds.
        /// </summary>
        [DataMember(Name = "snapshotInterval", Order = 2,
            EmitDefaultValue = false)]
        public int? SnapshotInterval { get; set; }
    }
}
