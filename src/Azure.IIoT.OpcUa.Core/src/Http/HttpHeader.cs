// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Http
{
    /// <summary>
    /// Custom header values.
    /// </summary>
    public static class HttpHeader
    {
        /// <summary>
        /// Continuation token.
        /// </summary>
        public const string ContinuationToken = "x-ms-continuation";

        /// <summary>
        /// Max item count for paging.
        /// </summary>
        public const string MaxItemCount = "x-ms-max-item-count";
    }
}
