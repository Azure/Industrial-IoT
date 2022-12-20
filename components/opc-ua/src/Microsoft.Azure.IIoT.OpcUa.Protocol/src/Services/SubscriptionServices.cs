// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription services implementation
    /// </summary>
    public partial class SubscriptionServices : ISubscriptionManager {

        /// <summary>
        /// Create subscription manager
        /// </summary>
        public SubscriptionServices(ISessionManager sessionManager,
            IVariantEncoderFactory codec,
            IClientServicesConfig clientConfig,
            ILogger logger) {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _clientConfig = clientConfig ?? throw new ArgumentNullException(nameof(codec));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public Task<ISubscription> GetOrCreateSubscriptionAsync(SubscriptionModel subscriptionModel) {
            if (string.IsNullOrEmpty(subscriptionModel?.Id)) {
                throw new ArgumentNullException(nameof(subscriptionModel));
            }
            var sub = new SubscriptionWrapper(this, subscriptionModel, _logger);
            _sessionManager.RegisterSubscription(sub);
            return Task.FromResult<ISubscription>(sub);
        }

        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;
        private readonly IVariantEncoderFactory _codec;
        private readonly IClientServicesConfig _clientConfig;
    }
}
