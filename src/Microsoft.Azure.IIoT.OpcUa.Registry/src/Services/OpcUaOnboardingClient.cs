// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Encoder.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Onboarding client triggers registry onboarding in the onboarding agent.
    /// </summary>
    public sealed class OpcUaOnboardingClient : IOpcUaOnboardingServices {

        /// <summary>
        /// Create onboarding client
        /// </summary>
        /// <param name="iothub"></param>
        public OpcUaOnboardingClient(IIoTHubTwinServices iothub,
            IIoTHubMessagingServices events, ILogger logger) {

            _iothub = iothub ?? throw new ArgumentNullException(nameof(iothub));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            OpcUaOnboardingHelper.EnsureOnboarderIdExists(_iothub).Wait();
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
                        new List <CallbackModel> { request.Callback },
                    DiscoveryUrls = new List<string> { request.DiscoveryUrl }
                },
                Id = request.RegistrationId,
            });
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _events.SendAsync(OpcUaOnboardingHelper.kId, new DeviceMessageModel {
                Properties = new Dictionary<string, string> {
                    ["ContentType"] = ContentTypes.DiscoveryRequest,
                    ["ContentEncoding"] = ContentEncodings.Json,
                    [SystemProperties.ContentType] = ContentTypes.DiscoveryRequest,
                    [SystemProperties.ContentEncoding] = ContentEncodings.Json
                },
                Payload = JToken.FromObject(request)
            });
        }

        private readonly IIoTHubTwinServices _iothub;
        private readonly IIoTHubMessagingServices _events;
        private readonly ILogger _logger;
    }
}
