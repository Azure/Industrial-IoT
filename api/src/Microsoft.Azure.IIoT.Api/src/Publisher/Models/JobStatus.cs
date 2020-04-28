// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Job status
    /// </summary>
    [DataContract]
    public enum JobStatus {

        /// <summary>
        /// Active
        /// </summary>
        [EnumMember]
        Active,

        /// <summary>
        /// Job cancelled
        /// </summary>
        [EnumMember]
        Canceled,

        /// <summary>
        /// Job completed
        /// </summary>
        [EnumMember]
        Completed,

        /// <summary>
        /// Error
        /// </summary>
        [EnumMember]
        Error,

        /// <summary>
        /// Removed
        /// </summary>
        [EnumMember]
        Deleted
    }
}