// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Worker state
    /// </summary>
    [DataContract]
    public enum WorkerStatus {

        /// <summary>
        /// Stopped
        /// </summary>
        [EnumMember]
        Stopped,

        /// <summary>
        /// Stopping
        /// </summary>
        [EnumMember]
        Stopping,

        /// <summary>
        /// Waiting
        /// </summary>
        [EnumMember]
        WaitingForJob,

        /// <summary>
        /// Processing
        /// </summary>
        [EnumMember]
        ProcessingJob
    }
}