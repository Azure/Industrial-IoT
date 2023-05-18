// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Data change trigger
    /// </summary>
    [DataContract]
    public enum DataChangeTriggerType
    {
        /// <summary>
        /// Status
        /// </summary>
        [EnumMember(Value = "Status")]
        Status,

        /// <summary>
        /// Status value
        /// </summary>
        [EnumMember(Value = "StatusValue")]
        StatusValue,

        /// <summary>
        /// Status value and timestamp
        /// </summary>
        [EnumMember(Value = "StatusValueTimestamp")]
        StatusValueTimestamp
    }
}
