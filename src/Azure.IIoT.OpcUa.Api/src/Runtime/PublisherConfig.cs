// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Publisher.Runtime {
    using Azure.IIoT.OpcUa.Api;
    using Azure.IIoT.OpcUa.Api.Runtime;
    using Microsoft.Azure.IIoT;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class PublisherConfig : ApiConfigBase, IServiceApiConfig {

        /// <summary>
        /// Publisher configuration
        /// </summary>
        private const string kServiceUrlKey = "ServiceUrl";

        /// <summary>Service endpoint url</summary>
        public string ServiceUrl => GetStringOrDefault(kServiceUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_PUBLISHER_SERVICE_URL,
                () => GetDefaultUrl("9045", "publisher")));

        /// <inheritdoc/>
        public PublisherConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
