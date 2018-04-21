// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Exceptions;
    using Microsoft.Azure.IIoT.OpcTwin.Services.External;
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using Microsoft.Azure.IIoT.OpcTwin.Services.Client;
    using Microsoft.Azure.IIoT.Common.Diagnostics;
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
        public async Task<ApplicationRegistrationModel> ValidateEndpointAsync(
            EndpointModel endpoint) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            try {
                return await _edge.ValidateEndpointAsync(endpoint);
            }
            catch (Exception e) {
                FilterException(e);
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
        public async Task<ApplicationRegistrationModel> DiscoverApplicationAsync(
            Uri discoveryUrl) {
            if (discoveryUrl == null) {
                throw new ArgumentNullException(nameof(discoveryUrl));
            }
            try {
                return await _edge.DiscoverApplicationAsync(discoveryUrl);
            }
            catch (Exception e) {
                FilterException(e);
                _logger.Error("Failed validation call on all edge services. Trying proxy",
                    () => e);
            }
            return await _proxy.DiscoverApplicationAsync(discoveryUrl);
        }

        /// <summary>
        /// Filter exceptions that should not be continued but thrown
        /// </summary>
        /// <param name="exception"></param>
        private static void FilterException(Exception exception) {
            switch (exception) {
                case ArgumentException ae:
                case MethodCallException mce:
                case TimeoutException toe:
                    throw exception;
            }
        }

        private readonly OpcUaTwinValidator _edge;
        private readonly OpcUaValidationServices _proxy;
        private readonly ILogger _logger;
    }
}
