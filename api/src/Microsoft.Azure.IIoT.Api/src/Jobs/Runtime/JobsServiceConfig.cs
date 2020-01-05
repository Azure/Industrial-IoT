// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Runtime {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class JobsServiceConfig : ApiConfigBase, IJobsServiceConfig {

        /// <summary>
        /// Jobs configuration
        /// </summary>
        private const string kJobServiceUrlKey = "JobServiceUrl";
        private const string kJobServiceIdKey = "JobServiceResourceId";

        /// <summary>Jobs service endpoint url</summary>
        public string JobServiceUrl => GetStringOrDefault(
            kJobServiceUrlKey, GetStringOrDefault(
                "PCS_JOBS_SERVICE_URL", GetDefaultUrl("9046", "jobs")));
        /// <summary>Jobs service audience</summary>
        public string JobServiceResourceId => GetStringOrDefault(
            kJobServiceIdKey, GetStringOrDefault("JOBS_APP_ID",
                GetStringOrDefault("PCS_AUTH_AUDIENCE", null)));

        /// <inheritdoc/>
        public JobsServiceConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
