// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Default {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint registry stubs
    /// </summary>
    public sealed class EndpointRegistryStub : IApplicationEndpointRegistry,
        IEndpointBulkProcessor, IEndpointRegistry {

        /// <inheritdoc/>
        public Task ActivateEndpointAsync(string endpointId,
            RegistryOperationContextModel context, CancellationToken ct) {
            return Task.FromException(new ResourceNotFoundException());
        }

        /// <inheritdoc/>
        public Task DeactivateEndpointAsync(string endpointId,
            RegistryOperationContextModel context, CancellationToken ct) {
            return Task.FromException(new ResourceNotFoundException());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<EndpointInfoModel>> GetApplicationEndpoints(
            string applicationId, bool includeDeleted, bool filterInactiveTwins,
            CancellationToken ct) {
            return Task.FromResult(Enumerable.Empty<EndpointInfoModel>());
        }

        /// <inheritdoc/>
        public Task<EndpointInfoModel> GetEndpointAsync(string endpointId,
            bool onlyServerState, CancellationToken ct) {
            return Task.FromException<EndpointInfoModel>(new ResourceNotFoundException());
        }

        /// <inheritdoc/>
        public Task<EndpointInfoListModel> ListEndpointsAsync(string continuation,
            bool onlyServerState, int? pageSize, CancellationToken ct) {
            return Task.FromResult(new EndpointInfoListModel());
        }

        /// <inheritdoc/>
        public Task ProcessDiscoveryEventsAsync(IEnumerable<EndpointInfoModel> found,
            DiscoveryResultModel context, string supervisorId, string applicationId,
            bool hardDelete) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<EndpointInfoListModel> QueryEndpointsAsync(EndpointRegistrationQueryModel query,
            bool onlyServerState, int? pageSize, CancellationToken ct) {
            return Task.FromResult(new EndpointInfoListModel());
        }

        /// <inheritdoc/>
        public Task UpdateEndpointAsync(string endpointId,
            EndpointRegistrationUpdateModel request, CancellationToken ct) {
            return Task.FromException<EndpointInfoModel>(new ResourceNotFoundException());
        }
    }
}
