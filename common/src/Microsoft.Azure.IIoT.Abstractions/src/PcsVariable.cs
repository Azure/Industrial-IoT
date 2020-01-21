// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT {

    /// <summary>
    /// Common runtime environment variables
    /// </summary>
    public static class PcsVariable {

        /// <summary> Iot hub connection string </summary>
        public const string PCS_IOTHUB_CONNSTRING =
            "PCS_IOTHUB_CONNSTRING";
        /// <summary> Iot hub event hub endpoint</summary>
        public const string PCS_IOTHUB_EVENTHUBENDPOINT =
            "PCS_IOTHUB_EVENTHUBENDPOINT";
        /// <summary> Cosmos db connection string </summary>
        public const string PCS_COSMOSDB_CONNSTRING =
            "PCS_COSMOSDB_CONNSTRING";
        /// <summary> Dps connection string </summary>
        public const string PCS_DPS_CONNSTRING =
            "PCS_DPS_CONNSTRING";
        /// <summary> Dps idscope</summary>
        public const string PCS_DPS_IDSCOPE =
            "PCS_DPS_IDSCOPE";
        /// <summary> datalake account </summary>
        public const string PCS_ADLSG2_ACCOUNT =
            "PCS_ADLSG2_ACCOUNT";
        /// <summary> storage connection string </summary>
        public const string PCS_STORAGE_CONNSTRING =
            "PCS_STORAGE_CONNSTRING";
        /// <summary> SignalR connection string </summary>
        public const string PCS_SIGNALR_CONNSTRING =
            "PCS_SIGNALR_CONNSTRING";
        /// <summary> Secondary event hub connection string </summary>
        public const string PCS_EVENTHUB_CONNSTRING =
            "PCS_EVENTHUB_CONNSTRING";
        /// <summary> Event hub name </summary>
        public const string PCS_EVENTHUB_NAME =
            "PCS_EVENTHUB_NAME";
        /// <summary> Service bus connection string</summary>
        public const string PCS_SERVICEBUS_CONNSTRING =
            "PCS_SERVICEBUS_CONNSTRING";
        /// <summary> Workspace name </summary>
        public const string PCS_WORKSPACE_NAME =
            "PCS_WORKSPACE_NAME";
        /// <summary> Instrumentation key </summary>
        public const string PCS_APPINSIGHTS_INSTRUMENTATIONKEY =
            "PCS_APPINSIGHTS_INSTRUMENTATIONKEY";
        /// <summary> Keyvault client application id </summary>
        public const string PCS_KEYVAULT_APPID =
            "PCS_KEYVAULT_APPID";
        /// <summary> Keyvault client application secret </summary>
        public const string PCS_KEYVAULT_SECRET =
            "PCS_KEYVAULT_SECRET";
        /// <summary> Keyvault url </summary>
        public const string PCS_KEYVAULT_URL =
            "PCS_KEYVAULT_URL";
        /// <summary> Auth tenant </summary>
        public const string PCS_AUTH_TENANT =
            "PCS_AUTH_TENANT";
        /// <summary> Instance </summary>
        public const string PCS_AUTH_INSTANCE =
            "PCS_AUTH_INSTANCE";
        /// <summary> Trusted Issuer </summary>
        public const string PCS_AUTH_ISSUER =
            "PCS_AUTH_ISSUER";
        /// <summary> Audience </summary>
        public const string PCS_AUTH_AUDIENCE =
            "PCS_AUTH_AUDIENCE";
        /// <summary> Client application id </summary>
        public const string PCS_AUTH_CLIENT_APPID =
            "PCS_AUTH_CLIENT_APPID";
        /// <summary> Client application secret </summary>
        public const string PCS_AUTH_CLIENT_SECRET =
            "PCS_AUTH_CLIENT_SECRET";
        /// <summary> Service application id </summary>
        public const string PCS_AUTH_SERVICE_APPID =
            "PCS_AUTH_SERVICE_APPID";
        /// <summary> Service secret </summary>
        public const string PCS_AUTH_SERVICE_SECRET =
            "PCS_AUTH_SERVICE_SECRET";
        /// <summary> Docker server </summary>
        public const string PCS_DOCKER_SERVER =
            "PCS_DOCKER_SERVER";
        /// <summary> Docker user name </summary>
        public const string PCS_DOCKER_USER =
            "PCS_DOCKER_USER";
        /// <summary> Docker password </summary>
        public const string PCS_DOCKER_PASSWORD =
            "PCS_DOCKER_PASSWORD";
        /// <summary> Optional images namespace </summary>
        public const string PCS_IMAGES_NAMESPACE =
            "PCS_IMAGES_NAMESPACE";
        /// <summary> Images tag </summary>
        public const string PCS_IMAGES_TAG =
            "PCS_IMAGES_TAG";
        /// <summary> Service url </summary>
        public const string PCS_SERVICE_URL =
            "PCS_SERVICE_URL";
    }
}
