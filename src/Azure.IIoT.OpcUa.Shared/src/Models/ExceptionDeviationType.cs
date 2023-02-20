// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception deviation type
    /// </summary>
    [DataContract]
    public enum ExceptionDeviationType {

        /// <summary>
        /// Absolute value
        /// </summary>
        [EnumMember]
        AbsoluteValue,

        /// <summary>
        /// Percent of value
        /// </summary>
        [EnumMember]
        PercentOfValue,

        /// <summary>
        /// Percent of a range
        /// </summary>
        [EnumMember]
        PercentOfRange,

        /// <summary>
        /// Percent of range
        /// </summary>
        [EnumMember]
        PercentOfEURange
    }
}
