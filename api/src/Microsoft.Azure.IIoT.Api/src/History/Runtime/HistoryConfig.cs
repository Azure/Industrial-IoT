// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Runtime {
    using Microsoft.Azure.IIoT.Api.Runtime;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration - wraps a configuration root
    /// </summary>
    public class HistoryConfig : ApiConfigBase, IHistoryConfig {

        /// <summary>
        /// History configuration
        /// </summary>
        private const string kOpcUaHistoryServiceUrlKey = "OpcHistoryServiceUrl";
        private const string kOpcUaHistoryServiceIdKey = "OpcHistoryServiceResourceId";

        /// <summary>OPC history service endpoint url</summary>
        public string OpcUaHistoryServiceUrl => GetStringOrDefault(
            kOpcUaHistoryServiceUrlKey, GetStringOrDefault(
                "PCS_HISTORY_SERVICE_URL", GetDefaultUrl("9043", "history")));
        /// <summary>OPC vault audience</summary>
        public string OpcUaHistoryServiceResourceId => GetStringOrDefault(
            kOpcUaHistoryServiceIdKey, GetStringOrDefault("OPC_HISTORY_APP_ID",
                GetStringOrDefault("PCS_AUTH_AUDIENCE", null)));

        /// <inheritdoc/>
        public HistoryConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
