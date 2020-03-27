// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Connects endpoint registry with supervisor activation client
    /// </summary>
    public interface IEndpointRegistryActivation {

        /// <summary>
        /// Activate endpoint registration
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ActivateEndpointAsync(EndpointRegistration registration,
            RegistryOperationContextModel context, CancellationToken ct = default);

        /// <summary>
        /// Apply activation filter
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="filter"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ApplyActivationFilterAsync(EndpointRegistration registration,
            EndpointActivationFilterModel filter, RegistryOperationContextModel context,
            CancellationToken ct = default);

        /// <summary>
        /// Deactivate endpoint
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeactivateEndpointAsync(EndpointRegistration registration,
            RegistryOperationContextModel context, CancellationToken ct = default);
    }
}