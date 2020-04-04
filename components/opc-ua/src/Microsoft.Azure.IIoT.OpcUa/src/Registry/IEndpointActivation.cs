// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint Activation
    /// </summary>
    public interface IEndpointActivation {

        /// <summary>
        /// Set the endpoint state to activated
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ActivateEndpointAsync(string endpointId,
            RegistryOperationContextModel context = null,
            CancellationToken ct = default);

        /// <summary>
        /// Performs synchronization of activated
        /// endpoints with available supervisors.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SynchronizeActivationAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Set the endpoint state to deactivated
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeactivateEndpointAsync(string endpointId,
            RegistryOperationContextModel context = null,
            CancellationToken ct = default);
    }
}
