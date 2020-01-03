// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Runtime {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class TwinConfig : ApiConfigBase, ITwinConfig {

        /// <summary>
        /// Twin configuration
        /// </summary>
        private const string kOpcUaTwinServiceUrlKey = "OpcTwinServiceUrl";
        private const string kOpcUaTwinServiceIdKey = "OpcTwinServiceResourceId";

        /// <summary>OPC twin service endpoint url</summary>
        public string OpcUaTwinServiceUrl => GetStringOrDefault(
            kOpcUaTwinServiceUrlKey, GetStringOrDefault(
                "PCS_TWIN_SERVICE_URL", GetDefaultUrl("9041", "twin")));
        /// <summary>OPC twin service audience</summary>
        public string OpcUaTwinServiceResourceId => GetStringOrDefault(
            kOpcUaTwinServiceIdKey, GetStringOrDefault("OPC_TWIN_APP_ID",
                GetStringOrDefault("PCS_AUTH_AUDIENCE", null)));

        /// <inheritdoc/>
        public TwinConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
