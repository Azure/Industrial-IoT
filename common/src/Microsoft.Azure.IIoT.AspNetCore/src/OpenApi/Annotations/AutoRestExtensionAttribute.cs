// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.OpenApi {
    using System;

    /// <summary>
    /// Operation extensions for auto rest
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AutoRestExtensionAttribute : Attribute {

        /// <summary>
        /// Describe the operation as long running
        /// </summary>
        public bool LongRunning { get; set; }

        /// <summary>
        /// Sets the next page link for x-ms-pageable.
        /// </summary>
        public string NextPageLinkName { get; set; }
    }
}
