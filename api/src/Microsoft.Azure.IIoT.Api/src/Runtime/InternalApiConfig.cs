// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding;
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class InternalApiConfig : ApiConfigBase, IOnboardingConfig {

        /// <inheritdoc/>
        public string OpcUaOnboardingServiceUrl => _oc.OpcUaOnboardingServiceUrl;
        /// <inheritdoc/>
        public string OpcUaOnboardingServiceResourceId => _oc.OpcUaOnboardingServiceResourceId;

        /// <inheritdoc/>
        public InternalApiConfig(IConfiguration configuration) :
            base(configuration) {
            _oc = new OnboardingConfig(configuration);
        }

        private readonly OnboardingConfig _oc;

    }
}
