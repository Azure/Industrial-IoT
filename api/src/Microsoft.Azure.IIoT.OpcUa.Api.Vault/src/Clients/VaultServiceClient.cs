// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Clients {
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Vault client.
    /// </summary>
    public class VaultServiceClient : IVaultServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        public VaultServiceClient(IHttpClient httpClient, IVaultConfig config) :
            this(httpClient, config.OpcUaVaultServiceUrl, config.OpcUaVaultServiceResourceId) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        public VaultServiceClient(IHttpClient httpClient, string serviceUri, string resourceId) {
            _serviceUri = serviceUri ?? throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the vault micro service.");
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _resourceId = resourceId;
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceStatusAsync(CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/healthz",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<string>();
        }

        /// <inheritdoc/>
        public async Task<TrustGroupListApiModel> ListGroupsAsync(string nextPageLink,
            int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/groups", _resourceId);
            if (nextPageLink != null) {
                request.AddHeader(HttpHeader.ContinuationToken, nextPageLink);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<TrustGroupListApiModel>();
        }

        /// <inheritdoc/>
        public async Task<TrustGroupRegistrationApiModel> GetGroupAsync(string groupId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(groupId)) {
                throw new ArgumentNullException(nameof(groupId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/groups/{groupId}",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<TrustGroupRegistrationApiModel>();
        }

        /// <inheritdoc/>
        public async Task UpdateGroupAsync(string groupId, TrustGroupUpdateRequestApiModel model,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(groupId)) {
                throw new ArgumentNullException(nameof(groupId));
            }
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            if (string.IsNullOrEmpty(groupId)) {
                throw new ArgumentNullException(nameof(groupId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/groups/{groupId}", _resourceId);
            request.SetContent(model);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<TrustGroupRegistrationResponseApiModel> CreateRootAsync(
            TrustGroupRootCreateRequestApiModel model, CancellationToken ct) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/groups/root", _resourceId);
            request.SetContent(model);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<TrustGroupRegistrationResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<TrustGroupRegistrationResponseApiModel> CreateGroupAsync(
            TrustGroupRegistrationRequestApiModel model, CancellationToken ct) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/groups", _resourceId);
            request.SetContent(model);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<TrustGroupRegistrationResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<X509CertificateApiModel> RenewIssuerCertificateAsync(
            string groupId, CancellationToken ct) {
            if (string.IsNullOrEmpty(groupId)) {
                throw new ArgumentNullException(nameof(groupId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/groups/{groupId}/renew", _resourceId);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<X509CertificateApiModel>();
        }

        /// <inheritdoc/>
        public async Task DeleteGroupAsync(string groupId, CancellationToken ct) {
            if (string.IsNullOrEmpty(groupId)) {
                throw new ArgumentNullException(nameof(groupId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/groups/{groupId}", _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainApiModel> GetIssuerCertificateChainAsync(
            string serialNumber, CancellationToken ct) {
            if (string.IsNullOrEmpty(serialNumber)) {
                throw new ArgumentNullException(nameof(serialNumber));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/certificates/{serialNumber}", _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<X509CertificateChainApiModel>();
        }

        /// <inheritdoc/>
        public async Task<X509CrlChainApiModel> GetIssuerCrlChainAsync(string serialNumber,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(serialNumber)) {
                throw new ArgumentNullException(nameof(serialNumber));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/certificates/{serialNumber}/crls", _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<X509CrlChainApiModel>();
        }

        /// <inheritdoc/>
        public async Task<StartSigningRequestResponseApiModel> StartSigningRequestAsync(
            StartSigningRequestApiModel model, CancellationToken ct) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/requests/sign",
                _resourceId);
            request.SetContent(model);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<StartSigningRequestResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<FinishSigningRequestResponseApiModel> FinishSigningRequestAsync(
            string requestId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/requests/sign/{requestId}",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<FinishSigningRequestResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<StartNewKeyPairRequestResponseApiModel> StartNewKeyPairRequestAsync(
            StartNewKeyPairRequestApiModel model, CancellationToken ct) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/requests/keypair",
                _resourceId);
            request.SetContent(model);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<StartNewKeyPairRequestResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<FinishNewKeyPairRequestResponseApiModel> FinishKeyPairRequestAsync(
            string requestId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/requests/keypair/{requestId}",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return response.GetContent<FinishNewKeyPairRequestResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task ApproveRequestAsync(string requestId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/requests/{requestId}/approve",
                _resourceId);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task RejectRequestAsync(string requestId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/requests/{requestId}/reject",
                _resourceId);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task AcceptRequestAsync(string requestId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/requests/{requestId}/accept",
                _resourceId);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task DeleteRequestAsync(string requestId, CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/requests/{requestId}",
                _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<CertificateRequestRecordApiModel> GetRequestAsync(string requestId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/requests/{requestId}",
                _resourceId);
            var response = await _httpClient.GetAsync(request, ct);
            response.Validate();
            return response.GetContent<CertificateRequestRecordApiModel>();
        }

        /// <inheritdoc/>
        public async Task<CertificateRequestQueryResponseApiModel> QueryRequestsAsync(
            CertificateRequestQueryRequestApiModel query, int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/requests/query",
                _resourceId);
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            if (query != null) {
                request.SetContent(query);
            }
            var response = await _httpClient.PostAsync(request, ct);
            response.Validate();
            return response.GetContent<CertificateRequestQueryResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task<CertificateRequestQueryResponseApiModel> ListRequestsAsync(
            string nextPageLink, int? pageSize, CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/requests",
                _resourceId);
            if (nextPageLink != null) {
                request.AddHeader(HttpHeader.ContinuationToken, nextPageLink);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request, ct);
            response.Validate();
            return response.GetContent<CertificateRequestQueryResponseApiModel>();
        }

        /// <inheritdoc/>
        public async Task AddTrustRelationshipAsync(string entityId,
            string trustedEntityId, CancellationToken ct) {
            if (string.IsNullOrEmpty(entityId)) {
                throw new ArgumentNullException(nameof(entityId));
            }
            if (string.IsNullOrEmpty(trustedEntityId)) {
                throw new ArgumentNullException(nameof(trustedEntityId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/trustlist/{entityId}/{trustedEntityId}",
                _resourceId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<X509CertificateListApiModel> ListTrustedCertificatesAsync(
            string entityId, string nextPageLink, int? pageSize, CancellationToken ct) {
            if (string.IsNullOrEmpty(entityId)) {
                throw new ArgumentNullException(nameof(entityId));
            }
            var request = _httpClient.NewRequest($"{_serviceUri}/v2/trustlist/{entityId}",
                _resourceId);
            if (nextPageLink != null) {
                request.AddHeader(HttpHeader.ContinuationToken, nextPageLink);
            }
            if (pageSize != null) {
                request.AddHeader(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            var response = await _httpClient.GetAsync(request, ct);
            response.Validate();
            return response.GetContent<X509CertificateListApiModel>();
        }

        /// <inheritdoc/>
        public async Task RemoveTrustRelationshipAsync(string entityId,
            string untrustedEntityId, CancellationToken ct) {
            if (string.IsNullOrEmpty(entityId)) {
                throw new ArgumentNullException(nameof(entityId));
            }
            if (string.IsNullOrEmpty(untrustedEntityId)) {
                throw new ArgumentNullException(nameof(untrustedEntityId));
            }
            var request = _httpClient.NewRequest(
                $"{_serviceUri}/v2/trustlist/{entityId}/{untrustedEntityId}",
                _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        private readonly IHttpClient _httpClient;
        private readonly string _serviceUri;
        private readonly string _resourceId;
    }
}
