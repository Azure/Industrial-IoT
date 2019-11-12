// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs {

    /// <summary>
    /// Default configuration
    /// </summary>
    public class JobsServiceConfig : IJobsServiceConfig {

        /// <inheritdoc/>
        public string JobServiceUrl { get; set; }

        /// <inheritdoc/>
        public string JobServiceResourceId { get; set; }
    }
}