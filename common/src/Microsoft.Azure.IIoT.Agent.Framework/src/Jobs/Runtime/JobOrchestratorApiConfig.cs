// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Jobs.Runtime {
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Default endpoint configuration
    /// </summary>
    public class JobOrchestratorApiConfig : ConfigBase, IJobOrchestratorEndpoint {

        /// <summary>
        /// Property keys
        /// </summary>
        private const string kJobOrchestratorUrlKey = "JobOrchestratorUrl";

        /// <inheritdoc/>
        public string JobOrchestratorUrl => GetStringOrDefault(kJobOrchestratorUrlKey,
            GetStringOrDefault("PCS_JOB_ORCHESTRATOR_SERVICE_URL",
                GetDefaultUrl("9051", "edge/jobs")));

        /// <summary>
        /// Create endpoint config
        /// </summary>
        /// <param name="configuration"></param>
        public JobOrchestratorApiConfig(IConfiguration configuration) :
            base(configuration) {
        }

        /// <summary>
        /// Get endpoint url
        /// </summary>
        /// <param name="port"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetDefaultUrl(string port, string path) {
            var cloudEndpoint = GetStringOrDefault("PCS_SERVICE_URL");
            if (string.IsNullOrEmpty(cloudEndpoint)) {
                var host = GetStringOrDefault("_HOST", System.Net.Dns.GetHostName());
                return $"http://{host}:{port}";
            }
            return $"{cloudEndpoint}/{path}";
        }
    }
}
