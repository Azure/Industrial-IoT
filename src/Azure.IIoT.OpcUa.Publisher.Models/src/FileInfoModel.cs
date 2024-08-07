// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// File info
    /// </summary>
    [DataContract]
    public record FileInfoModel
    {
        /// <summary>
        /// The size of the file in Bytes. When a file is
        /// currently opened for write, the size might not be
        /// accurate or available.
        /// </summary>
        [DataMember(Name = "size", Order = 0,
            EmitDefaultValue = false)]
        public long? Size { get; init; }

        /// <summary>
        /// Whether the file is writable.
        /// </summary>
        [DataMember(Name = "writable", Order = 1,
           EmitDefaultValue = false)]
        public bool Writable { get; init; }

        /// <summary>
        /// The number of currently valid file handles on
        /// the file.
        /// </summary>
        [DataMember(Name = "openCount", Order = 2,
           EmitDefaultValue = false)]
        public ushort OpenCount { get; init; }

        /// <summary>
        /// The media type of the file based on RFC 2046.
        /// </summary>
        [DataMember(Name = "mimeType", Order = 3,
           EmitDefaultValue = false)]
        public string? MimeType { get; init; }

        /// <summary>
        /// The maximum number of bytes of
        /// the read and write buffers.
        /// </summary>
        [DataMember(Name = "maxBufferSize", Order = 4,
            EmitDefaultValue = false)]
        public uint? MaxBufferSize { get; init; }

        /// <summary>
        /// The time the file was last modified.
        /// </summary>
        [DataMember(Name = "lastModified", Order = 5,
            EmitDefaultValue = false)]
        public DateTime? LastModified { get; init; }
    }
}
