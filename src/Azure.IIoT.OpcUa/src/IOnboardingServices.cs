// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa {
    using Azure.IIoT.OpcUa.Api.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Onboarding services
    /// </summary>
    public interface IOnboardingServices {

        /// <summary>
        /// Register server from discovery url.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task RegisterAsync(ServerRegistrationRequestModel request);

        /// <summary>
        /// Discover using discovery request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task DiscoverAsync(DiscoveryRequestModel request);

        /// <summary>
        /// Cancel discovery request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task CancelAsync(DiscoveryCancelModel request);
    }
}
