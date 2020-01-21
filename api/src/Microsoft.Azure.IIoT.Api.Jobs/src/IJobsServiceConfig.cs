// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs {

    /// <summary>
    /// Configuration for client
    /// </summary>
    public interface IJobsServiceConfig {

        /// <summary>
        /// Job service url
        /// </summary>
        string JobServiceUrl { get; }

        /// <summary>
        /// The Job service resource id
        /// </summary>
        string JobServiceResourceId { get; }
    }
}
