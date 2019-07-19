// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Serilog;
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
            if (string.IsNullOrEmpty(request.RegistrationId)) {
                request.RegistrationId = Guid.NewGuid().ToString();
            }
            await DiscoverAsync(new DiscoveryRequestModel {
                Configuration = new DiscoveryConfigModel {
                    ActivationFilter = request.ActivationFilter,
                    Callbacks = request.Callback == null ? null :
                        new List<CallbackModel> { request.Callback },
                    DiscoveryUrls = new List<string> { request.DiscoveryUrl },
                    Locales = request.Locales
                },
                Id = request.RegistrationId,
                Context = request.Context
            });
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _events.PublishAsync(request);
        }

        private readonly IEventBus _events;
    }
}
