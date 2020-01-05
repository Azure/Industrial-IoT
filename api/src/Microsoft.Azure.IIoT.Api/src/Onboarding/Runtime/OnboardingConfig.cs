// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Runtime {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class OnboardingConfig : ApiConfigBase, IOnboardingConfig {

        /// <summary>
        /// Onboarding configuration
        /// </summary>
        private const string kOpcUaOnboardingServiceUrlKey = "OpcOnboardingServiceUrl";
        private const string kOpcUaOnboardingServiceIdKey = "OpcOnboardingServiceResourceId";

        /// <summary>OPC onboarding endpoint url</summary>
        public string OpcUaOnboardingServiceUrl => GetStringOrDefault(
            kOpcUaOnboardingServiceUrlKey, GetStringOrDefault(
                "PCS_ONBOARDING_SERVICE_URL", GetDefaultUrl("9060", "onboarding")));
        /// <summary>OPC onboarding service audience</summary>
        public string OpcUaOnboardingServiceResourceId => GetStringOrDefault(
            kOpcUaOnboardingServiceIdKey, GetStringOrDefault("OPC_ONBOARDING_APP_ID",
                GetStringOrDefault("PCS_AUTH_AUDIENCE", null)));

        /// <inheritdoc/>
        public OnboardingConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
