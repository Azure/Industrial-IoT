// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding {
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Onboarding api calls
    /// </summary>
    public interface IOnboardingServiceApi {

        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <returns></returns>
        Task<StatusResponseApiModel> GetServiceStatusAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Processes discovery events and onboards new entities
        /// to the opc registry.
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ProcessDiscoveryResultsAsync(string supervisorId,
            DiscoveryResultListApiModel request, CancellationToken ct = default);
    }
}
