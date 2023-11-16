// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Ordering model
    /// </summary>
    [DataContract]
    public enum DataSetOrderingType
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// Ascending writer id
        /// </summary>
        [EnumMember(Value = "AscendingWriterId")]
        AscendingWriterId = 1,

        /// <summary>
        /// Single
        /// </summary>
        [EnumMember(Value = "AscendingWriterIdSingle")]
        AscendingWriterIdSingle = 2,
    }
}
