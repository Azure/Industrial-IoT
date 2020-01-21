// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Job status
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum JobStatus {

        /// <summary>
        /// Active
        /// </summary>
        Active,

        /// <summary>
        /// Job cancelled
        /// </summary>
        Canceled,

        /// <summary>
        /// Job completed
        /// </summary>
        Completed,

        /// <summary>
        /// Error
        /// </summary>
        Error,

        /// <summary>
        /// Removed
        /// </summary>
        Deleted
    }
}