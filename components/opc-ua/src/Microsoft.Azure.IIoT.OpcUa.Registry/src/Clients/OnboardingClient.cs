// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Onboarding client triggers registry onboarding in the jobs agent.
    /// </summary>
    public sealed class OnboardingClient : IOnboardingServices {

        /// <summary>
        /// Create onboarding client
        /// </summary>
        /// <param name="events"></param>
        public OnboardingClient(IEventBus events) {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(ServerRegistrationRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.DiscoveryUrl == null) {
                throw new ArgumentNullException(nameof(request.DiscoveryUrl));
            }
            if (string.IsNullOrEmpty(request.Id)) {
                request.Id = Guid.NewGuid().ToString();
            }
            await DiscoverAsync(new DiscoveryRequestModel {
                Configuration = new DiscoveryConfigModel {
                    ActivationFilter = request.ActivationFilter.Clone(),
                    DiscoveryUrls = new List<string> { request.DiscoveryUrl },
                },
                Id = request.Id,
                Context = request.Context.Clone()
            });
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _events.PublishAsync(request);
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _events.PublishAsync(request);
        }

        private readonly IEventBus _events;
    }
}
