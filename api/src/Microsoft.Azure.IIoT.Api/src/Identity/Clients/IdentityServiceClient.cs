// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Identity.Clients {
    using Microsoft.Azure.IIoT.Api.Identity.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Collections.Generic;

    /// <summary>
    /// Implementation of identity service api.
    /// </summary>
    public sealed class IdentityServiceClient : IIdentityServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public IdentityServiceClient(IHttpClient httpClient, IIdentityConfig config,
            ISerializer serializer) : this(httpClient, config.IdentityServiceUrl,
                config.IdentityServiceResourceId, serializer) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        /// <param name="serializer"></param>
        public IdentityServiceClient(IHttpClient httpClient, string serviceUri,
            string resourceId, ISerializer serializer) {
            if (string.IsNullOrEmpty(serviceUri)) {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the onboarding micro service.");
            }
            _serviceUri = serviceUri;
            _resourceId = resourceId;
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public async Task<string> GetServiceStatusAsync(CancellationToken ct) {
            var request = _httpClient.NewRequest($"{_serviceUri}/healthz",
                _resourceId);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<string>(response);
        }

        /// <inheritdoc/>
        public async Task CreateUserAsync(UserApiModel user, CancellationToken ct) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/users");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            _serializer.SerializeToRequest(request, user);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<UserApiModel> GetUserByNameAsync(string name,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/users") {
                Query = $"name={name}"
            };
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<UserApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<UserApiModel> GetUserByEmailAsync(string email,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(email)) {
                throw new ArgumentNullException(nameof(email));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/users") {
                Query = $"email={email}"
            };
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<UserApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<UserApiModel> GetUserByIdAsync(string userId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/users/{userId}");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<UserApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task DeleteUserAsync(string userId, CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/users/{userId}");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task AddClaimAsync(string userId, ClaimApiModel model,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/users/{userId}/claims");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            _serializer.SerializeToRequest(request, model);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task RemoveClaimAsync(string userId, ClaimApiModel model,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var uri = new UriBuilder(
                $"{_serviceUri}/v2/users/{userId}/claims/{model.Type}/{model.Value}");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task AddRoleToUserAsync(string userId, string role,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            if (string.IsNullOrEmpty(role)) {
                throw new ArgumentNullException(nameof(role));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/users/{userId}/roles/{role}");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<UserApiModel>> GetUsersInRoleAsync(
            string role, CancellationToken ct) {
            if (string.IsNullOrEmpty(role)) {
                throw new ArgumentNullException(nameof(role));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/users/roles/{role}");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<IEnumerable<UserApiModel>>(response);
        }

        /// <inheritdoc/>
        public async Task RemoveRoleFromUserAsync(string userId, string role,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentNullException(nameof(userId));
            }
            if (string.IsNullOrEmpty(role)) {
                throw new ArgumentNullException(nameof(role));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/users/{userId}/roles/{role}");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task CreateRoleAsync(RoleApiModel role, CancellationToken ct) {
            if (role == null) {
                throw new ArgumentNullException(nameof(role));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/roles");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            _serializer.SerializeToRequest(request, role);
            var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        /// <inheritdoc/>
        public async Task<RoleApiModel> GetRoleByIdAsync(string roleId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(roleId)) {
                throw new ArgumentNullException(nameof(roleId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/roles/{roleId}");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            _serializer.SetAcceptHeaders(request);
            var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            return _serializer.DeserializeResponse<RoleApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task DeleteRoleAsync(string roleId, CancellationToken ct) {
            if (string.IsNullOrEmpty(roleId)) {
                throw new ArgumentNullException(nameof(roleId));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/roles/{roleId}");
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        private readonly IHttpClient _httpClient;
        private readonly string _serviceUri;
        private readonly string _resourceId;
        private readonly ISerializer _serializer;
    }
}
