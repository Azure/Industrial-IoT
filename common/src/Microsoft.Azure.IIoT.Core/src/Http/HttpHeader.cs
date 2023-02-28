// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http
{
    /// <summary>
    /// Custom header values
    /// </summary>
    public static class HttpHeader2
    {
        // Common

        /// <summary>
        /// Continuation token
        /// </summary>
        public const string ContinuationToken = "x-ms-continuation";

        /// <summary>
        /// Max item count for paging.
        /// </summary>
        public const string MaxItemCount = "x-ms-max-item-count";

        // Auth and reverse proxy

        /// <summary>
        /// Target resource id
        /// </summary>
        public const string ResourceId = "x-resource-id";

        /// <summary>
        /// Unix transport (internal)
        /// </summary>
        public const string UdsPath = "x-internal-uds-transport";
    }
}
