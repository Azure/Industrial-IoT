// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Model upload start response
    /// </summary>
    [DataContract]
    public class ModelUploadStartResponseApiModel {

        /// <summary>
        /// Blob File name
        /// </summary>
        [DataMember(Name = "blobName", Order = 0,
            EmitDefaultValue = false)]
        public string BlobName { get; set; }

        /// <summary>
        /// Content encoding
        /// </summary>
        [DataMember(Name = "contentEncoding", Order = 1,
            EmitDefaultValue = false)]
        public string ContentEncoding { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        [DataMember(Name = "timeStamp", Order = 2,
            EmitDefaultValue = false)]
        public DateTime? TimeStamp { get; set; }
    }
}
