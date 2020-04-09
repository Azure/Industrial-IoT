// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Implementation of onboarding service api.
    /// </summary>
    public sealed class OnboardingServiceClient : IOnboardingServiceApi {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public OnboardingServiceClient(IHttpClient httpClient, IOnboardingConfig config,
            ISerializer serializer) : this(httpClient, config.OpcUaOnboardingServiceUrl,
                config.OpcUaOnboardingServiceResourceId, serializer) {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="resourceId"></param>
        /// <param name="serializer"></param>
        public OnboardingServiceClient(IHttpClient httpClient, string serviceUri,
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
            try {
                var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
                response.Validate();
                return response.GetContentAsString();
            }
            catch (Exception ex) {
                return ex.Message;
            }
        }

        /// <inheritdoc/>
        public async Task ProcessDiscoveryResultsAsync(string discovererId,
            DiscoveryResultListApiModel content, CancellationToken ct) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }
            var uri = new UriBuilder($"{_serviceUri}/v2/discovery") {
                Query = $"discovererId={discovererId}"
            };
            var request = _httpClient.NewRequest(uri.Uri, _resourceId);
            _serializer.SerializeToRequest(request, content);
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
        }

        private readonly IHttpClient _httpClient;
        private readonly string _serviceUri;
        private readonly string _resourceId;
        private readonly ISerializer _serializer;
    }
}
