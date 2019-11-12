// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class InternalApiConfig : ClientConfig, IOnboardingConfig {

        /// <summary>
        /// Onboarding configuration
        /// </summary>
        private const string kOpcUaOnboardingServiceUrlKey = "OpcOnboardingServiceUrl";
        private const string kOpcUaOnboardingServiceIdKey = "OpcOnboardingServiceResourceId";

        /// <summary>OPC twin endpoint url</summary>
        public string OpcUaOnboardingServiceUrl => GetStringOrDefault(
            kOpcUaOnboardingServiceUrlKey, GetStringOrDefault(
                "PCS_ONBOARDING_SERVICE_URL", GetDefaultUrl("9060", "onboarding")));
        /// <summary>OPC twin service audience</summary>
        public string OpcUaOnboardingServiceResourceId => GetStringOrDefault(
            kOpcUaOnboardingServiceIdKey, GetStringOrDefault(
                "OPC_ONBOARDING_APP_ID", Audience));

        /// <inheritdoc/>
        public InternalApiConfig(IConfiguration configuration) :
            base(configuration) {
            _hostName = GetStringOrDefault("_HOST", System.Net.Dns.GetHostName());
        }

        /// <summary>
        /// Make endpoint url from configruation
        /// </summary>
        /// <param name="port"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetDefaultUrl(string port, string path) {
            var cloudEndpoint = GetStringOrDefault("PCS_SERVICE_URL");
            if (string.IsNullOrEmpty(cloudEndpoint)) {
                return $"http://{_hostName}:{port}";
            }
            return $"{cloudEndpoint}/{path}";
        }

        private readonly string _hostName;
    }
}
