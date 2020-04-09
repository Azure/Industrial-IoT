// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Data change trigger
    /// </summary>
    [DataContract]
    public enum DataChangeTriggerType {

        /// <summary>
        /// Status
        /// </summary>
        [EnumMember]
        Status,

        /// <summary>
        /// Status value
        /// </summary>
        [EnumMember]
        StatusValue,

        /// <summary>
        /// Status value and timestamp
        /// </summary>
        [EnumMember]
        StatusValueTimestamp
    }
}