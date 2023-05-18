// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Deadband type
    /// </summary>
    [DataContract]
    public enum DeadbandType
    {
        /// <summary>
        /// Absolute
        /// </summary>
        [EnumMember(Value = "Absolute")]
        Absolute,

        /// <summary>
        /// Percentage
        /// </summary>
        [EnumMember(Value = "Percent")]
        Percent
    }
}
