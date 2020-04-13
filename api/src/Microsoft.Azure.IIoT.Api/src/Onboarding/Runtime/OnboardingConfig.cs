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
        private const string kOnboardingServiceUrlKey = "OnboardingServiceUrl";
        private const string kOnboardingServiceIdKey = "OnboardingServiceResourceId";

        /// <summary>onboarding endpoint url</summary>
        public string OpcUaOnboardingServiceUrl => GetStringOrDefault(
            kOnboardingServiceUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_ONBOARDING_SERVICE_URL,
                () => GetDefaultUrl("9060", "onboarding")));

        /// <inheritdoc/>
        public OnboardingConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
