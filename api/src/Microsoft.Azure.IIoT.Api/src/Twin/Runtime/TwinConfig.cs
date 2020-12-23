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

        /// <summary>OPC twin service endpoint url</summary>
        public string OpcUaTwinServiceUrl => GetStringOrDefault(
            kOpcUaTwinServiceUrlKey,
            () => GetStringOrDefault(PcsVariable.PCS_TWIN_SERVICE_URL,
                () => GetDefaultUrl("9041", "twin")));

        /// <inheritdoc/>
        public TwinConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
