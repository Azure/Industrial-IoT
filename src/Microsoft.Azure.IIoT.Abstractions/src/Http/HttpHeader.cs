// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {

    /// <summary>
    /// Custom header values
    /// </summary>
    public static class HttpHeader {

        /// <summary>
        /// Target resource id
        /// </summary>
        public const string ResourceId = "x-ms-resource-id";

        /// <summary>
        /// Source id
        /// </summary>
        public const string SourceId = "x-source";

        /// <summary>
        /// Continuation token
        /// </summary>
        public const string ContinuationToken = "x-ms-continuation";

        /// <summary>
        /// Max item count for paging.
        /// </summary>
        public const string MaxItemCount = "x-ms-max-item-count";

        /// <summary>
        /// Roles
        /// </summary>
        public const string Roles = "x-roles";
    }
}
