// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Heartbeat
    /// </summary>
    [DataContract]
    public enum HeartbeatInstruction {

        /// <summary>
        /// Keep
        /// </summary>
        [EnumMember]
        Keep,

        /// <summary>
        /// Switch to active
        /// </summary>
        [EnumMember]
        SwitchToActive,

        /// <summary>
        /// Cancel processing
        /// </summary>
        [EnumMember]
        CancelProcessing
    }
}