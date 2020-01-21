// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding {

    /// <summary>
    /// Configuration for service
    /// </summary>
    public interface IOnboardingConfig {

        /// <summary>
        /// Opc onboarding service url
        /// </summary>
        string OpcUaOnboardingServiceUrl { get; }

        /// <summary>
        /// Resource id of onboarding service
        /// </summary>
        string OpcUaOnboardingServiceResourceId { get; }
    }
}
