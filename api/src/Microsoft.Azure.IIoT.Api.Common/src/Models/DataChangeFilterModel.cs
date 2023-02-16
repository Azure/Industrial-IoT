// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Data change filter
    /// </summary>
    [DataContract]
    public record class DataChangeFilterModel {

        /// <summary>
        /// Data change trigger type
        /// </summary>
        [DataMember(Name = "dataChangeTrigger", Order = 0,
            EmitDefaultValue = false)]
        public DataChangeTriggerType? DataChangeTrigger { get; set; }

        /// <summary>
        /// Dead band
        /// </summary>
        [DataMember(Name = "deadbandType", Order = 1,
            EmitDefaultValue = false)]
        public DeadbandType? DeadbandType { get; set; }

        /// <summary>
        /// Dead band value
        /// </summary>
        [DataMember(Name = "deadbandValue", Order = 2,
            EmitDefaultValue = false)]
        public double? DeadbandValue { get; set; }
    }
}