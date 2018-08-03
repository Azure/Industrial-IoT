// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Onboarding client triggers registry onboarding in the onboarding agent.
    /// </summary>
    public sealed class OpcUaOnboardingTrigger : IOpcUaOnboardingServices {

        /// <summary>
        /// Create onboarding services
        /// </summary>
        /// <param name="iothub"></param>
        public OpcUaOnboardingTrigger(IIoTHubTwinServices iothub,
            IIoTHubMessagingServices events, ILogger logger) {

            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            OpcUaOnboarderHelper.EnsureOnboarderIdExists(_iothub).Wait();
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

            await _events.SendAsync(OpcUaOnboarderHelper.kId, new DeviceMessageModel {
                Properties = new Dictionary<string, string> {
                    [SystemPropertyNames.ContentType] =
                        "application/x-registration-v1-json",
                    [SystemPropertyNames.ContentEncoding] =
                        "application/json",
                    ["caller-id"] =
                        "onboarding-trigger",
                },
                Payload = JToken.FromObject(request)
            }.YieldReturn());
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly IIoTHubMessagingServices _events;
        private readonly ILogger _logger;
    }
}
