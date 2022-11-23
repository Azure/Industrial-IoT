// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deploy.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Azure Log Analytics Workspace configuration
    /// </summary>
    public class LogWorkspaceConfig : ConfigBase, ILogWorkspaceConfig {

        private const string kWorkspaceId = "Docker:WorkspaceId";
        private const string kWorkspaceKey = "Docker:WorkspaceKey";
        private const string kSubscriptionId = "SubscriptionId";
        private const string kResourceGroupName = "ResourceGroupName";
        private const string kIoTHubConnectionString = "IoTHubConnectionString";

        /// <inheritdoc/>
        public string LogWorkspaceId => GetStringOrDefault(kWorkspaceId,
            () => GetStringOrDefault(PcsVariable.PCS_WORKSPACE_ID));

        /// <inheritdoc/>
        public string LogWorkspaceKey => GetStringOrDefault(kWorkspaceKey,
            () => GetStringOrDefault(PcsVariable.PCS_WORKSPACE_KEY));

        /// <inheritdoc/>
        public string IoTHubResourceId {
            get {
                string subscriptionId = GetStringOrDefault(kSubscriptionId, () => GetStringOrDefault(PcsVariable.PCS_SUBSCRIPTION_ID));
                string resourceGroup = GetStringOrDefault(kResourceGroupName, () => GetStringOrDefault(PcsVariable.PCS_RESOURCE_GROUP));
                if (string.IsNullOrEmpty(resourceGroup) || string.IsNullOrEmpty(subscriptionId)) {
                    return string.Empty;
                }
                else {
                    return "/subscriptions/" + subscriptionId + "/resourceGroups/" + resourceGroup +
                            "/providers/Microsoft.Devices/IotHubs/" +
                            ConnectionString.Parse(GetStringOrDefault(kIoTHubConnectionString,
                                    () => GetStringOrDefault(PcsVariable.PCS_IOTHUB_CONNSTRING))).HubName;
                }
            }
        }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public LogWorkspaceConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
