// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Indicate the data location
    /// </summary>
    [DataContract]
    public enum DataLocation
    {
        /// <summary>
        ///  A raw data value.
        /// </summary>
        [EnumMember]
        Raw = 0,

        /// <summary>
        /// Calculated data
        /// </summary>
        [EnumMember]
        Calculated = 1,

        /// <summary>
        /// A data value which was interpolated.
        /// </summary>
        [EnumMember]
        Interpolated = 2,
    }
}
