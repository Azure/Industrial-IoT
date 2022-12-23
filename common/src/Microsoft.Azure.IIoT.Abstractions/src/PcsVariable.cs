// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT {

    /// <summary>
    /// Common runtime environment variables
    /// </summary>
    public static class PcsVariable {

        /// <summary> Service name </summary>
        public const string PCS_SERVICE_NAME =
            "PCS_SERVICE_NAME";
        /// <summary> Root user id </summary>
        public const string PCS_ROOT_USERID =
            "PCS_ROOT_USERID";
        /// <summary> Root password </summary>
        public const string PCS_ROOT_PASSWORD =
            "PCS_ROOT_PASSWORD";
        /// <summary> Iot hub connection string </summary>
        public const string PCS_IOTHUB_CONNSTRING =
            "PCS_IOTHUB_CONNSTRING";
        /// <summary> Iot hub event hub endpoint </summary>
        public const string PCS_IOTHUB_EVENTHUBENDPOINT =
            "PCS_IOTHUB_EVENTHUBENDPOINT";
        /// <summary> Iot hub event hub Telemetry Consumer Group </summary>
        public const string PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_TELEMETRY =
            "PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_TELEMETRY";
        /// <summary> Iot hub event hub Events Consumer Group </summary>
        public const string PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_EVENTS =
            "PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_EVENTS";
        /// <summary> Iot hub event hub Onboarder Consumer Group </summary>
        public const string PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_ONBOARDING =
            "PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_ONBOARDING";
        /// <summary> Cosmos db connection string </summary>
        public const string PCS_COSMOSDB_CONNSTRING =
            "PCS_COSMOSDB_CONNSTRING";
        /// <summary> Dps connection string </summary>
        public const string PCS_DPS_CONNSTRING =
            "PCS_DPS_CONNSTRING";
        /// <summary> Dps idscope </summary>
        public const string PCS_DPS_IDSCOPE =
            "PCS_DPS_IDSCOPE";
        /// <summary> storage connection string </summary>
        public const string PCS_STORAGE_CONNSTRING =
            "PCS_STORAGE_CONNSTRING";
        /// <summary> Blob Storage Container that holds encrypted keys </summary>
        public const string PCS_STORAGE_CONTAINER_DATAPROTECTION =
            "PCS_STORAGE_CONTAINER_DATAPROTECTION";
        /// <summary> SignalR connection string </summary>
        public const string PCS_SIGNALR_CONNSTRING =
            "PCS_SIGNALR_CONNSTRING";
        /// <summary> SignalR service mode </summary>
        public const string PCS_SIGNALR_MODE =
            "PCS_SIGNALR_MODE";
        /// <summary> Secondary event hub connection string </summary>
        public const string PCS_EVENTHUB_CONNSTRING =
            "PCS_EVENTHUB_CONNSTRING";
        /// <summary> Event hub name </summary>
        public const string PCS_EVENTHUB_NAME =
            "PCS_EVENTHUB_NAME";
        /// <summary> Event hub consumer group telemetryux </summary>
        public const string PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_UX =
            "PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_UX";
        /// <summary> Service bus connection string </summary>
        public const string PCS_SERVICEBUS_CONNSTRING =
            "PCS_SERVICEBUS_CONNSTRING";
        /// <summary> Instrumentation key </summary>
        public const string PCS_APPINSIGHTS_INSTRUMENTATIONKEY =
            "PCS_APPINSIGHTS_INSTRUMENTATIONKEY";
        /// <summary> LogLevel for Application Insights </summary>
        public const string PCS_APPINSIGHTS_LOGLEVEL =
            "PCS_APPINSIGHTS_LOGLEVEL";
        /// <summary> Keyvault client application id </summary>
        public const string PCS_KEYVAULT_APPID =
            "PCS_KEYVAULT_APPID";
        /// <summary> Keyvault client application secret </summary>
        public const string PCS_KEYVAULT_SECRET =
            "PCS_KEYVAULT_SECRET";
        /// <summary> Keyvault url </summary>
        public const string PCS_KEYVAULT_URL =
            "PCS_KEYVAULT_URL";
        /// <summary> Keyvault is hsm </summary>
        public const string PCS_KEYVAULT_ISHSM =
            "PCS_KEYVAULT_ISHSM";
        /// <summary> Key (in KeyVault) to be used for encription of keys </summary>
        public const string PCS_KEYVAULT_KEY_DATAPROTECTION =
            "PCS_KEYVAULT_KEY_DATAPROTECTION";
        /// <summary> Determines whether configuratoin should be loaded from KeyVault </summary>
        public const string PCS_KEYVAULT_CONFIG_ENABLED =
            "PCS_KEYVAULT_CONFIG_ENABLED";
        /// <summary> Managed service identity application id </summary>
        public const string PCS_MSI_APPID =
            "PCS_MSI_APPID";
        /// <summary> Managed service identity tenant </summary>
        public const string PCS_MSI_TENANT =
            "PCS_MSI_TENANT";
        /// <summary> Aad Auth tenant </summary>
        public const string PCS_AUTH_TENANT =
            "PCS_AUTH_TENANT";
        /// <summary> Aad Instance </summary>
        public const string PCS_AAD_INSTANCE =
            "PCS_AUTH_INSTANCE";
        /// <summary> Aad Trusted Issuer </summary>
        public const string PCS_AAD_ISSUER =
            "PCS_AUTH_ISSUER";
        /// <summary> Aad valid audience or null if disabled </summary>
        public const string PCS_AAD_AUDIENCE =
            "PCS_AUTH_AUDIENCE";
        /// <summary> Service application id </summary>
        public const string PCS_AAD_SERVICE_APPID =
            "PCS_AUTH_SERVICE_APPID";
        /// <summary> Service secret </summary>
        public const string PCS_AAD_SERVICE_SECRET =
            "PCS_AUTH_SERVICE_SECRET";
        /// <summary> Built in Auth server disabled </summary>
        public const string PCS_AUTH_SERVICE_DISABLED =
            "PCS_AUTH_SERVICE_DISABLED";
        /// <summary> Built in Auth server trusted issuer </summary>
        public const string PCS_AUTH_SERVICE_ISSUER =
            "PCS_AUTH_SERVICE_ISSUER";
        /// <summary> Built in Auth server service application id </summary>
        public const string PCS_AUTH_SERVICE_SERVICE_APPID =
            "PCS_AUTH_SERVICE_SERVICE_APPID";
        /// <summary> Built in Auth server service secret </summary>
        public const string PCS_AUTH_SERVICE_SERVICE_SECRET =
            "PCS_AUTH_SERVICE_SERVICE_SECRET";
        /// <summary> Client application id </summary>
        public const string PCS_AAD_CONFIDENTIAL_CLIENT_APPID =
            "PCS_AUTH_CLIENT_APPID";
        /// <summary> Client application secret </summary>
        public const string PCS_AAD_CONFIDENTIAL_CLIENT_SECRET =
            "PCS_AUTH_CLIENT_SECRET";
        /// <summary> Client application id </summary>
        public const string PCS_AAD_PUBLIC_CLIENT_APPID =
            "PCS_AUTH_PUBLIC_CLIENT_APPID";
        /// <summary> Built in Auth server service application id </summary>
        public const string PCS_AUTH_SERVICE_CLIENT_APPID =
            "PCS_AUTH_SERVICE_CLIENT_APPID";
        /// <summary> Built in Auth server service secret </summary>
        public const string PCS_AUTH_SERVICE_CLIENT_SECRET =
            "PCS_AUTH_SERVICE_CLIENT_SECRET";
        /// <summary> Whether to use role based access </summary>
        public const string PCS_AUTH_ROLES =
            "PCS_AUTH_ROLES";
        /// <summary> Auth required </summary>
        public const string PCS_AUTH_REQUIRED =
            "PCS_AUTH_REQUIRED";
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
        /// <summary> Auth service endpoint url </summary>
        public const string PCS_AUTH_SERVICE_URL =
            "PCS_AUTH_SERVICE_URL";
        /// <summary> User servíce endpoint url </summary>
        public const string PCS_USERS_SERVICE_URL =
            "PCS_USERS_SERVICE_URL";
        /// <summary> OPC twin service endpoint url </summary>
        public const string PCS_TWIN_SERVICE_URL =
            "PCS_TWIN_SERVICE_URL";
        /// <summary> OPC registry service endpoint url </summary>
        public const string PCS_TWIN_REGISTRY_URL =
            "PCS_TWIN_REGISTRY_URL";
        /// <summary> OPC vault service endpoint url </summary>
        public const string PCS_VAULT_SERVICE_URL =
            "PCS_VAULT_SERVICE_URL";
        /// <summary> OPC publisher service endpoint url </summary>
        public const string PCS_PUBLISHER_SERVICE_URL =
            "PCS_PUBLISHER_SERVICE_URL";
        /// <summary> Publisher orchestrator service endpoint url </summary>
        public const string PCS_PUBLISHER_ORCHESTRATOR_SERVICE_URL =
            "PCS_PUBLISHER_ORCHESTRATOR_SERVICE_URL";
        /// <summary> OPC history service endpoint url </summary>
        public const string PCS_HISTORY_SERVICE_URL =
            "PCS_HISTORY_SERVICE_URL";
        /// <summary> Events service endpoint url </summary>
        public const string PCS_EVENTS_SERVICE_URL =
            "PCS_EVENTS_SERVICE_URL";
        /// <summary> CORS whitelist </summary>
        public const string PCS_CORS_WHITELIST =
            "PCS_CORS_WHITELIST";
        /// <summary> Whether openapi should be enabled (Swagger) </summary>
        public const string PCS_OPENAPI_ENABLED =
            "PCS_OPENAPI_ENABLED";
        /// <summary> Whether create v2 openapi json </summary>
        public const string PCS_OPENAPI_USE_V2 =
            "PCS_OPENAPI_USE_V2";
        /// <summary> The AAD application id for the openapi UI client </summary>
        public const string PCS_OPENAPI_APPID =
            "PCS_OPENAPI_APPID";
        /// <summary> AAD Client / Application secret for openapi </summary>
        public const string PCS_OPENAPI_APP_SECRET =
            "PCS_OPENAPI_APP_SECRET";
        /// <summary> Server host for openapi </summary>
        public const string PCS_OPENAPI_SERVER_HOST =
            "PCS_OPENAPI_SERVER_HOST";
        /// <summary> Service URL path base </summary>
        public const string PCS_SERVICE_PATH_BASE =
            "PCS_SERVICE_PATH_BASE";
        /// <summary> Service URL path base for OPC twin service </summary>
        public const string PCS_TWIN_SERVICE_PATH_BASE =
            "PCS_TWIN_SERVICE_PATH_BASE";
        /// <summary> Service URL path base for OPC registry service </summary>
        public const string PCS_TWIN_REGISTRY_SERVICE_PATH_BASE =
            "PCS_TWIN_REGISTRY_SERVICE_PATH_BASE";
        /// <summary> Service URL path base for OPC vault service </summary>
        public const string PCS_VAULT_SERVICE_PATH_BASE =
            "PCS_VAULT_SERVICE_PATH_BASE";
        /// <summary> Service URL path base for OPC publisher service </summary>
        public const string PCS_PUBLISHER_SERVICE_PATH_BASE =
            "PCS_PUBLISHER_SERVICE_PATH_BASE";
        /// <summary> Service URL path base for OPC history service </summary>
        public const string PCS_HISTORY_SERVICE_PATH_BASE =
            "PCS_HISTORY_SERVICE_PATH_BASE";
        /// <summary> Service URL path base for gateway service </summary>
        public const string PCS_GATEWAY_SERVICE_PATH_BASE =
            "PCS_GATEWAY_SERVICE_PATH_BASE";
        /// <summary> Service URL path base for publisher orchestrator service </summary>
        public const string PCS_PUBLISHER_ORCHESTRATOR_SERVICE_PATH_BASE =
            "PCS_PUBLISHER_ORCHESTRATOR_SERVICE_PATH_BASE";
        /// <summary> Service URL path base for frontend app </summary>
        public const string PCS_FRONTEND_APP_SERVICE_PATH_BASE =
            "PCS_FRONTEND_APP_SERVICE_PATH_BASE";
        /// <summary> Service URL path base for events service </summary>
        public const string PCS_EVENTS_SERVICE_PATH_BASE =
            "PCS_EVENTS_SERVICE_PATH_BASE";
        /// <summary> Service URL path base for auth service </summary>
        public const string PCS_AUTH_SERVICE_PATH_BASE =
            "PCS_AUTH_SERVICE_PATH_BASE";
        /// <summary> Service URL path base for user servíce </summary>
        public const string PCS_USERS_SERVICE_PATH_BASE =
            "PCS_USERS_SERVICE_PATH_BASE";
        /// <summary> Deployment Level </summary>
        public const string PCS_DEPLOYMENT_LEVEL =
            "PCS_DEPLOYMENT_LEVEL";
        /// <summary> Log Analytics workspace Id </summary>
        public const string PCS_WORKSPACE_ID =
            "PCS_WORKSPACE_ID";
        /// <summary> Log Analytics workspace key </summary>
        public const string PCS_WORKSPACE_KEY =
            "PCS_WORKSPACE_KEY";
        /// <summary> URL path base for TSI query </summary>
        public const string PCS_TSI_URL =
            "PCS_TSI_URL";
        /// <summary> Log Analytics workbook id </summary>
        public const string PCS_WORKBOOK_ID =
            "PCS_WORKBOOK_ID";
        /// <summary> Subscription id </summary>
        public const string PCS_SUBSCRIPTION_ID =
            "PCS_SUBSCRIPTION_ID";
        /// <summary> Resource group </summary>
        public const string PCS_RESOURCE_GROUP =
            "PCS_RESOURCE_GROUP";
        /// <summary> Log file path environment variable </summary>
        public const string PCS_LOGS_PATH =
            "PCS_LOGS_PATH";
        /// <summary> The maximum size of the (IoT D2C) message outgress queue </summary>
        public const string PCS_DEFAULT_PUBLISH_MAX_OUTGRESS_MESSAGES =
            "PCS_DEFAULT_PUBLISH_MAX_OUTGRESS_MESSAGES";
    }
}
