// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Client;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Validate endpoint through proxy or jobs.
    /// </summary>
    public sealed class OpcUaCompositeValidator : IOpcUaValidationServices {

        /// <summary>
        /// Create endpoint validator
        /// </summary>
        /// <param name="jobs"></param>
        /// <param name="proxy"></param>
        /// <param name="logger"></param>
        public OpcUaCompositeValidator(IIoTHubJobServices jobs, IOpcUaClient proxy,
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create composite of edge micro service + proxy control
            _edge = new OpcUaTwinValidator(jobs, logger);
            _proxy = new OpcUaValidationServices(proxy, logger);
        }

        /// <summary>
        /// Validate endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public async Task<ApplicationModel> ValidateEndpointAsync(EndpointModel endpoint) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            try {
                return await _edge.ValidateEndpointAsync(endpoint);
            }
            catch (Exception e) {
                _logger.Error("Failed validation call on all edge services. Trying proxy",
                    () => e);
            }
            return await _proxy.ValidateEndpointAsync(endpoint);
        }

        /// <summary>
        /// Discover application
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        public async Task<ApplicationModel> DiscoverApplicationAsync(Uri discoveryUrl) {
            if (discoveryUrl == null) {
                throw new ArgumentNullException(nameof(discoveryUrl));
            }
            try {
                return await _edge.DiscoverApplicationAsync(discoveryUrl);
            }
            catch (Exception e) {
                _logger.Error("Failed validation call on all edge services. Trying proxy",
                    () => e);
            }
            return await _proxy.DiscoverApplicationAsync(discoveryUrl);
        }

        private readonly OpcUaTwinValidator _edge;
        private readonly OpcUaValidationServices _proxy;
        private readonly ILogger _logger;
    }
}
