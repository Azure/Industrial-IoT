// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Handler {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Handle registry entity events and cleanup requests
    /// </summary>
    public sealed class RegistryEventHandler : IApplicationRegistryListener,
        IEndpointRegistryListener {

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="requests"></param>
        public RegistryEventHandler(IRequestManagement requests) {
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
        }

        /// <inheritdoc/>
        public Task OnApplicationNewAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnApplicationUpdatedAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnApplicationEnabledAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnApplicationDisabledAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnApplicationDeletedAsync(RegistryOperationContextModel context,
            string applicationId, ApplicationInfoModel application) {
            return RemoveAllRequestsForEntityAsync(applicationId, context);
        }

        /// <inheritdoc/>
        public Task OnEndpointNewAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnEndpointActivatedAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnEndpointDeactivatedAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnEndpointDisabledAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnEndpointEnabledAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnEndpointUpdatedAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnEndpointDeletedAsync(RegistryOperationContextModel context,
            string endpointId, EndpointInfoModel endpoint) {
            return RemoveAllRequestsForEntityAsync(endpointId, context);
        }

        /// <summary>
        /// Delete all requests for the given entity
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task RemoveAllRequestsForEntityAsync(string entityId,
            RegistryOperationContextModel context) {
            string nextPageLink = null;
            var result = await _requests.QueryRequestsAsync(
                new CertificateRequestQueryRequestModel {
                    EntityId = entityId
                });
            while (true) {
                nextPageLink = result.NextPageLink;
                foreach (var request in result.Requests) {
                    if (request.State != CertificateRequestState.Accepted) {
                        await Try.Async(() => _requests.AcceptRequestAsync(
                            request.RequestId, new VaultOperationContextModel {
                                AuthorityId = context?.AuthorityId,
                                Time = context?.Time ?? DateTime.UtcNow
                            }));
                    }
                }
                if (result.NextPageLink == null) {
                    break;
                }
                result = await _requests.ListRequestsAsync(result.NextPageLink);
            }
        }

        private readonly IRequestManagement _requests;
    }
}
