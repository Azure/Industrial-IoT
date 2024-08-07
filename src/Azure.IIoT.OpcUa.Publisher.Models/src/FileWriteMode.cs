// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// File write mode
    /// </summary>
    [DataContract]
    public enum FileWriteMode
    {
        /// <summary>
        /// The file is opened for writing.
        /// </summary>
        [EnumMember(Value = "Write")]
        Write,

        /// <summary>
        /// The existing content of the file is erased.
        /// </summary>
        [EnumMember(Value = "Create")]
        Create,

        /// <summary>
        /// The file is opened and positioned
        /// at end of the file
        /// </summary>
        [EnumMember(Value = "Append")]
        Append
    }
}
