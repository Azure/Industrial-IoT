// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration services uses the address space and services of a
    /// connected server to configure the publisher. The configuration
    /// services allow interactive expansion of published nodes.
    /// </summary>
    public sealed class ConfigurationServices : IConfigurationServices
    {
        /// <summary>
        /// Create configuration services
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public ConfigurationServices(IPublisherConfiguration configuration,
            IOpcUaClientManager<ConnectionModel> client, ILogger<ConfigurationServices> logger)
        {
            _configuration = configuration;
            _client = client;
            _logger = logger;
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<PublishedNodesEntryModel>> ExpandAsync(
            PublishedNodeExpansionRequestModel request, bool noUpdate, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        internal sealed class ConfigurationBrowser
        {

        }

        private readonly IPublisherConfiguration _configuration;
        private readonly IOpcUaClientManager<ConnectionModel> _client;
        private readonly ILogger<ConfigurationServices> _logger;
    }
}
