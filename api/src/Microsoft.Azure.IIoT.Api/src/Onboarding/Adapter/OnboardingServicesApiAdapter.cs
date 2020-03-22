// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding {
    using Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Implements discovery processor as adapter on top of onboarding api.
    /// </summary>
    public sealed class OnboardingServicesApiAdapter : IDiscoveryResultProcessor {

        /// <summary>
        /// Create service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        public OnboardingServicesApiAdapter(IOnboardingServiceApi client, ISerializer serializer) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public async Task ProcessDiscoveryResultsAsync(string discovererId,
            DiscoveryResultModel result, IEnumerable<DiscoveryEventModel> events) {
            await _client.ProcessDiscoveryResultsAsync(discovererId,
                new DiscoveryResultListApiModel {
                    Result = _serializer.Map<DiscoveryResultApiModel>(result),
                    Events = _serializer.Map<List<DiscoveryEventApiModel>>(events)
                });
        }

        private readonly ISerializer _serializer;
        private readonly IOnboardingServiceApi _client;
    }
}
