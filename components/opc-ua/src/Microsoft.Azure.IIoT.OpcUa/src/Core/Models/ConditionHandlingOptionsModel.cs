// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Settings for condition handling
    /// </summary>
    [DataContract]
    public class ConditionHandlingOptionsModel {

        /// <summary>
        /// Time interval for sending pending interval updates in seconds.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public int? UpdateInterval { get; set; }

        /// <summary>
        /// Time interval for sending pending interval snapshot in seconds.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public int? SnapshotInterval { get; set; }
    }
}
