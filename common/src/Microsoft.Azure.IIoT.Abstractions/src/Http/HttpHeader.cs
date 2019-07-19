// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http {

    /// <summary>
    /// Custom header values
    /// </summary>
    public static class HttpHeader {

        // Common

        /// <summary>
        /// Continuation token
        /// </summary>
        public const string ContinuationToken = "x-ms-continuation";

        /// <summary>
        /// Max item count for paging.
        /// </summary>
        public const string MaxItemCount = "x-ms-max-item-count";

        // Auditing

        /// <summary>
        /// Tracking id of a audited session
        /// </summary>
        public const string TrackingId = "x-ms-tracking-id";

        /// <summary>
        /// Audit identifier to use for request
        /// </summary>
        public const string ActivityId = "x-ms-activity-id";

        // Auth and reverse proxy

        /// <summary>
        /// Target resource id
        /// </summary>
        public const string ResourceId = "x-resource-id";

        /// <summary>
        /// Source of the request (for internal addressing)
        /// </summary>
        public const string SourceId = "x-source";

        /// <summary>
        /// Unix transport (internal)
        /// </summary>
        public const string UdsPath = "x-internal-uds-transport";

        /// <summary>
        /// Forwarded path information
        /// </summary>
        public const string Location = "x-location";
    }
}
