// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// File open for write options
    /// </summary>
    [DataContract]
    public record FileOpenWriteOptionsModel
    {
        /// <summary>
        /// The write mode to use when writing to the file.
        /// The default mode is "Create", erasing the file
        /// if it exists and writing a new one.
        /// </summary>
        [DataMember(Name = "mode", Order = 1,
            EmitDefaultValue = false)]
        public FileWriteMode Mode { get; init; }

        /// <summary>
        /// Optional method to call to close and commit the
        /// file after writing is done. Many file types require
        /// to call a special method when closing to make the
        /// content persist, e.g., during device or software
        /// update or when uploding configuration. The method
        /// id is specific to the file type opened.
        /// </summary>
        [DataMember(Name = "closeMethodId", Order = 2,
            EmitDefaultValue = false)]
        public string? CloseAndCommitMethodId { get; init; }
    }
}
