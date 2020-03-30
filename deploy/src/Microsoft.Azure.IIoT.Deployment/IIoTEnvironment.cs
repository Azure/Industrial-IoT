// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
    using Microsoft.Azure.Management.AppService.Fluent.Models;
    using Microsoft.Azure.Management.EventHub.Fluent.Models;
    using Microsoft.Azure.Management.IotHub.Models;
    using Microsoft.Azure.Management.KeyVault.Fluent.Models;
    using Microsoft.Azure.Management.OperationalInsights.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.Storage.Fluent.Models;

    using Microsoft.Graph;
    using Serilog;

    class IIoTEnvironment {
        ////////////////////----------2.6.104----------////////////////////
        // IoT Hub
        public readonly string PCS_IOTHUB_CONNSTRING;
        public readonly string PCS_IOTHUB_EVENTHUBENDPOINT;
        public readonly string PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_EVENTS;
        public readonly string PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_TELEMETRY;

        // Cosmos DB
        public readonly string PCS_COSMOSDB_CONNSTRING;

        // Storage Account
        public readonly string PCS_STORAGE_CONNSTRING;
        public readonly string PCS_STORAGE_CONTAINER_DATAPROTECTION;

        // ADLS Gen2 Storage Account
        public readonly string PCS_ADLSG2_CONNSTRING;
        public readonly string PCS_ADLSG2_CONTAINER_CDM;
        public readonly string PCS_ADLSG2_CONTAINER_CDM_ROOTFOLDER;

        // Event Hub Namespace
        public readonly string PCS_EVENTHUB_CONNSTRING;
        public readonly string PCS_EVENTHUB_NAME;
        public readonly string PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_CDM;
        public readonly string PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_UX;

        // Service Bus
        public readonly string PCS_SERVICEBUS_CONNSTRING;

        // Key Vault
        public readonly string PCS_KEYVAULT_URL;
        public readonly string PCS_KEYVAULT_KEY_DATAPROTECTION;
        public readonly string PCS_KEYVAULT_APPID;
        public readonly string PCS_KEYVAULT_SECRET;

        // Application Insights
        public readonly string PCS_APPINSIGHTS_INSTRUMENTATIONKEY;

        // Service URLs
        public readonly string PCS_SERVICE_URL;
        public readonly string PCS_TWIN_REGISTRY_URL;
        public readonly string PCS_TWIN_SERVICE_URL;
        public readonly string PCS_HISTORY_SERVICE_URL;
        public readonly string PCS_VAULT_SERVICE_URL;
        public readonly string PCS_ONBOARDING_SERVICE_URL;
        public readonly string PCS_PUBLISHER_SERVICE_URL;
        public readonly string PCS_JOBS_SERVICE_URL;
        public readonly string PCS_JOB_ORCHESTRATOR_SERVICE_URL;
        public readonly string PCS_CONFIGURATION_SERVICE_URL;

        // SignalR
        public readonly string PCS_SIGNALR_CONNSTRING;

        // Authentication
        public readonly string PCS_AUTH_REQUIRED;
        public readonly string PCS_AUTH_TENANT;
        public readonly string PCS_AUTH_INSTANCE;
        public readonly string PCS_AUTH_ISSUER;
        public readonly string PCS_AUTH_HTTPSREDIRECTPORT;
        public readonly string PCS_AUTH_AUDIENCE;
        public readonly string PCS_AUTH_CLIENT_APPID;
        public readonly string PCS_AUTH_CLIENT_SECRET;
        public readonly string PCS_AUTH_SERVICE_APPID;
        public readonly string PCS_AUTH_SERVICE_SECRET;

        // CORS Whitelist
        public readonly string PCS_CORS_WHITELIST;

        // Service URL path bases
        public readonly string PCS_TWIN_REGISTRY_SERVICE_PATH_BASE;
        public readonly string PCS_TWIN_SERVICE_PATH_BASE;
        public readonly string PCS_HISTORY_SERVICE_PATH_BASE;
        public readonly string PCS_GATEWAY_SERVICE_PATH_BASE;
        public readonly string PCS_VAULT_SERVICE_PATH_BASE;
        public readonly string PCS_ONBOARDING_SERVICE_PATH_BASE;
        public readonly string PCS_PUBLISHER_SERVICE_PATH_BASE;
        public readonly string PCS_CONFIGURATION_SERVICE_PATH_BASE;
        public readonly string PCS_EDGE_MANAGER_SERVICE_PATH_BASE;
        public readonly string PCS_FRONTEND_APP_SERVICE_PATH_BASE;
        public readonly string PCS_JOB_ORCHESTRATOR_SERVICE_PATH_BASE;
        public readonly string PCS_JOBS_SERVICE_PATH_BASE;

        // AspNetCore
        public readonly string ASPNETCORE_FORWARDEDHEADERS_ENABLED;
        public readonly string ASPNETCORE_FORWARDEDHEADERS_FORWARDLIMIT;

        // OpenAPI
        public readonly string PCS_OPENAPI_SERVER_HOST;

        ////////////////////----------2.5.2----------////////////////////
        public readonly string _HUB_CS; //deprecated

        //public readonly string PCS_IOTHUB_CONNSTRING;
        public readonly string PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING; //deprecated
        public readonly string PCS_TELEMETRY_DOCUMENTDB_CONNSTRING; //deprecated
        public readonly string PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING; //deprecated

        public readonly string PCS_IOTHUBREACT_ACCESS_CONNSTRING; //deprecated
        public readonly string PCS_IOTHUBREACT_HUB_NAME; //deprecated
        public readonly string PCS_IOTHUBREACT_HUB_ENDPOINT; //deprecated
        public readonly string PCS_IOTHUBREACT_HUB_CONSUMERGROUP; //deprecated
        public readonly string PCS_IOTHUBREACT_HUB_PARTITIONS; //deprecated
        public readonly string PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT; //deprecated
        public readonly string PCS_IOTHUBREACT_AZUREBLOB_KEY; //deprecated
        public readonly string PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX; //deprecated

        public readonly string PCS_ASA_DATA_AZUREBLOB_ACCOUNT; //deprecated
        public readonly string PCS_ASA_DATA_AZUREBLOB_KEY; //deprecated
        public readonly string PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX; //deprecated

        //public readonly string PCS_EVENTHUB_CONNSTRING;
        //public readonly string PCS_EVENTHUB_NAME;
        //public readonly string PCS_SERVICEBUS_CONNSTRING;
        //public readonly string PCS_KEYVAULT_URL;

        public readonly string PCS_WORKSPACE_NAME; //deprecated
        public readonly string PCS_APPINSIGHTS_NAME; //deprecated
        
        //public readonly string PCS_APPINSIGHTS_INSTRUMENTATIONKEY;
        //public readonly string PCS_SERVICE_URL;
        //public readonly string PCS_SIGNALR_CONNSTRING;

        //public readonly string PCS_AUTH_HTTPSREDIRECTPORT;
        //public readonly string PCS_AUTH_REQUIRED;
        //public readonly string PCS_AUTH_AUDIENCE;
        //public readonly string PCS_AUTH_ISSUER;

        public readonly string PCS_WEBUI_AUTH_AAD_APPID; //deprecated
        public readonly string PCS_WEBUI_AUTH_AAD_AUTHORITY; //deprecated
        public readonly string PCS_WEBUI_AUTH_AAD_TENANT; //deprecated

        //public readonly string PCS_CORS_WHITELIST;

        public readonly string REACT_APP_PCS_AUTH_REQUIRED; //deprecated
        public readonly string REACT_APP_PCS_AUTH_AUDIENCE; //deprecated
        public readonly string REACT_APP_PCS_AUTH_ISSUER; //deprecated
        public readonly string REACT_APP_PCS_WEBUI_AUTH_AAD_APPID; //deprecated
        public readonly string REACT_APP_PCS_WEBUI_AUTH_AAD_AUTHORITY; //deprecated
        public readonly string REACT_APP_PCS_WEBUI_AUTH_AAD_TENANT; //deprecated

        public readonly Dictionary<string, string> Dict;

        public IIoTEnvironment(
            AzureEnvironment azureEnvironment,
            Guid tenantId,
            // IoT Hub
            IotHubDescription iotHub,
            string iotHubOwnerConnectionString,
            string iotHubEventHubEventsEndpointName,
            EventHubConsumerGroupInfo iotHubEventHubEventsConsumerGroup,
            EventHubConsumerGroupInfo iotHubEventHubTelemetryConsumerGroup,
            // Cosmos DB
            string cosmosDBAccountConnectionString,
            // Storage Account
            StorageAccountInner storageAccount,
            StorageAccountKey storageAccountKey,
            string storageAccountConectionString,
            string storageAccountContainerDataprotection,
            // ADLS Gen2 Storage Account
            string adlsConectionString,
            string adlsContainerCdm,
            string adlsContainerCdmRootFolder,
            // Event Hub Namespace
            EventhubInner eventHub,
            string eventHubConnectionString,
            ConsumerGroupInner telemetryCdm,
            ConsumerGroupInner telemetryUx,
            // Service Bus
            string serviceBusConnectionString,
            // SignalR
            string signalRConnectionString,
            // Key Vault
            VaultInner keyVault,
            string dataprotectionKeyName,
            // Operational Insights Workspace
            Workspace operationalInsightsWorkspace,
            // Application Insights
            ApplicationInsightsComponent applicationInsightsComponent,
            SiteInner webSite,
            Application serviceApplication,
            string serviceApplicationSecret,
            Application clientApplication,
            string clientApplicationSecret

        ) {
            ////////////////////----------2.6.104----------////////////////////
            // IoT Hub
            PCS_IOTHUB_CONNSTRING = iotHubOwnerConnectionString;
            PCS_IOTHUB_EVENTHUBENDPOINT = iotHub.Properties.EventHubEndpoints[iotHubEventHubEventsEndpointName].Endpoint;
            PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_EVENTS = iotHubEventHubEventsConsumerGroup.Name;
            PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_TELEMETRY = iotHubEventHubTelemetryConsumerGroup.Name;

            // Cosmos DB
            PCS_COSMOSDB_CONNSTRING = cosmosDBAccountConnectionString;

            // Storage Account
            PCS_STORAGE_CONNSTRING = storageAccountConectionString;
            PCS_STORAGE_CONTAINER_DATAPROTECTION = storageAccountContainerDataprotection;

            // ADLS Gen2 Storage Account
            PCS_ADLSG2_CONNSTRING = adlsConectionString;
            PCS_ADLSG2_CONTAINER_CDM = adlsContainerCdm;
            PCS_ADLSG2_CONTAINER_CDM_ROOTFOLDER = adlsContainerCdmRootFolder;

            // Event Hub Namespace
            PCS_EVENTHUB_CONNSTRING = eventHubConnectionString;
            PCS_EVENTHUB_NAME = eventHub.Name;
            PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_CDM = telemetryCdm.Name;
            PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_UX = telemetryUx.Name;

            // Service Bus
            PCS_SERVICEBUS_CONNSTRING = serviceBusConnectionString;

            // Key Vault
            PCS_KEYVAULT_URL = keyVault.Properties.VaultUri;
            PCS_KEYVAULT_KEY_DATAPROTECTION = dataprotectionKeyName;
            PCS_KEYVAULT_APPID = serviceApplication.AppId;
            PCS_KEYVAULT_SECRET = serviceApplicationSecret;

            // Application Insights
            PCS_APPINSIGHTS_INSTRUMENTATIONKEY = applicationInsightsComponent.InstrumentationKey;

            // Service URLs
            PCS_SERVICE_URL = $"https://{webSite.HostNames[0]}";

            var iiotNamespace = "industrial-iot";

            PCS_TWIN_REGISTRY_URL = $"http://{iiotNamespace}.{"registry"}:{9042}";
            PCS_TWIN_SERVICE_URL = $"http://{iiotNamespace}.{"twin"}:{9041}";
            PCS_HISTORY_SERVICE_URL = $"http://{iiotNamespace}.{"history"}:{9043}";
            PCS_VAULT_SERVICE_URL = $"http://{iiotNamespace}.{"vault"}:{9044}";
            PCS_ONBOARDING_SERVICE_URL = $"http://{iiotNamespace}.{"onboarding"}:{9060}";
            PCS_PUBLISHER_SERVICE_URL = $"http://{iiotNamespace}.{"publisher"}:{9045}";
            PCS_JOBS_SERVICE_URL = $"http://{iiotNamespace}.{"publisher-jobs"}:{9046}";
            // NOTE: PCS_JOB_ORCHESTRATOR_SERVICE_URL should be externally accessible URL.
            PCS_JOB_ORCHESTRATOR_SERVICE_URL = $"https://{webSite.HostNames[0]}/edge/jobs";
            PCS_CONFIGURATION_SERVICE_URL = $"http://{iiotNamespace}.{"configuration"}:{9050}";

            // SignalR
            PCS_SIGNALR_CONNSTRING = signalRConnectionString;

            // Authentication
            PCS_AUTH_REQUIRED = $"{true}";
            PCS_AUTH_TENANT = $"{tenantId}";
            // ToDo: Check value of PCS_AUTH_INSTANCE.
            //PCS_AUTH_INSTANCE = "https://login.microsoftonline.com/";
            PCS_AUTH_INSTANCE = azureEnvironment.AuthenticationEndpoint;
            PCS_AUTH_ISSUER = $"https://sts.windows.net/{tenantId}/";
            PCS_AUTH_HTTPSREDIRECTPORT = $"{0}";
            PCS_AUTH_AUDIENCE = serviceApplication.IdentifierUris.First();
            PCS_AUTH_CLIENT_APPID = clientApplication.AppId;
            PCS_AUTH_CLIENT_SECRET = clientApplicationSecret;
            PCS_AUTH_SERVICE_APPID = serviceApplication.AppId;
            PCS_AUTH_SERVICE_SECRET = serviceApplicationSecret;

            // CORS Whitelist
            PCS_CORS_WHITELIST = "*";

            // Service URL path bases
            PCS_TWIN_REGISTRY_SERVICE_PATH_BASE = "/registry";
            PCS_TWIN_SERVICE_PATH_BASE = "/twin";
            PCS_HISTORY_SERVICE_PATH_BASE = "/history";
            PCS_GATEWAY_SERVICE_PATH_BASE = "/ua";
            PCS_VAULT_SERVICE_PATH_BASE = "/vault";
            PCS_ONBOARDING_SERVICE_PATH_BASE = "/onboarding";
            PCS_PUBLISHER_SERVICE_PATH_BASE = "/publisher";
            PCS_CONFIGURATION_SERVICE_PATH_BASE = "/configuration";
            PCS_EDGE_MANAGER_SERVICE_PATH_BASE = "/edge/manage";
            PCS_FRONTEND_APP_SERVICE_PATH_BASE = "/frontend";
            PCS_JOB_ORCHESTRATOR_SERVICE_PATH_BASE = "/edge/jobs";
            PCS_JOBS_SERVICE_PATH_BASE = "/jobs";

            // AspNetCore
            ASPNETCORE_FORWARDEDHEADERS_ENABLED = $"{true}";
            ASPNETCORE_FORWARDEDHEADERS_FORWARDLIMIT = $"{1}";

            // OpenAPI
            PCS_OPENAPI_SERVER_HOST = "";

            ////////////////////----------2.5.2----------////////////////////
            _HUB_CS = iotHubOwnerConnectionString;

            //PCS_IOTHUB_CONNSTRING = iotHubOwnerConnectionString; // duplicate
            PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING = cosmosDBAccountConnectionString;
            PCS_TELEMETRY_DOCUMENTDB_CONNSTRING = cosmosDBAccountConnectionString; // duplicate
            PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING = cosmosDBAccountConnectionString; // duplicate

            PCS_IOTHUBREACT_ACCESS_CONNSTRING = iotHubOwnerConnectionString; // duplicate
            PCS_IOTHUBREACT_HUB_NAME = iotHub.Name;
            PCS_IOTHUBREACT_HUB_ENDPOINT = iotHub.Properties.EventHubEndpoints[iotHubEventHubEventsEndpointName].Endpoint;
            PCS_IOTHUBREACT_HUB_CONSUMERGROUP = iotHubEventHubEventsConsumerGroup.Name;
            PCS_IOTHUBREACT_HUB_PARTITIONS = $"{iotHub.Properties.EventHubEndpoints[iotHubEventHubEventsEndpointName].PartitionCount.Value}";
            PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT = storageAccount.Name;
            PCS_IOTHUBREACT_AZUREBLOB_KEY = storageAccountKey.Value;
            PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX = azureEnvironment.StorageEndpointSuffix;

            PCS_ASA_DATA_AZUREBLOB_ACCOUNT = PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT; // duplicate
            PCS_ASA_DATA_AZUREBLOB_KEY = PCS_IOTHUBREACT_AZUREBLOB_KEY; // duplicate
            PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX = PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX; // duplicate

            //PCS_EVENTHUB_CONNSTRING = eventHubConnectionString;
            //PCS_EVENTHUB_NAME = eventHub.Name;
            //PCS_SERVICEBUS_CONNSTRING = serviceBusConnectionString;
            //PCS_KEYVAULT_URL = keyVault.Properties.VaultUri;

            PCS_WORKSPACE_NAME = operationalInsightsWorkspace.Name;
            PCS_APPINSIGHTS_NAME = applicationInsightsComponent.Name;
            
            //PCS_APPINSIGHTS_INSTRUMENTATIONKEY = applicationInsightsComponent.InstrumentationKey;
            //PCS_SERVICE_URL = $"https://{webSite.HostNames[0]}";
            //PCS_SIGNALR_CONNSTRING = signalRConnectionString;

            //PCS_AUTH_HTTPSREDIRECTPORT = "0";
            //PCS_AUTH_REQUIRED = "true";
            //PCS_AUTH_AUDIENCE = serviceApplication.IdentifierUris.First();
            //PCS_AUTH_ISSUER = $"https://sts.windows.net/{tenantId.ToString()}/";

            PCS_WEBUI_AUTH_AAD_APPID = clientApplication.AppId;
            PCS_WEBUI_AUTH_AAD_AUTHORITY = azureEnvironment.AuthenticationEndpoint;
            PCS_WEBUI_AUTH_AAD_TENANT = tenantId.ToString();

            //PCS_CORS_WHITELIST = "*";

            REACT_APP_PCS_AUTH_REQUIRED = PCS_AUTH_REQUIRED; // duplicate
            REACT_APP_PCS_AUTH_AUDIENCE = PCS_AUTH_AUDIENCE; // duplicate
            REACT_APP_PCS_AUTH_ISSUER = PCS_AUTH_ISSUER; // duplicate
            REACT_APP_PCS_WEBUI_AUTH_AAD_APPID = PCS_WEBUI_AUTH_AAD_APPID; // duplicate
            REACT_APP_PCS_WEBUI_AUTH_AAD_AUTHORITY = PCS_WEBUI_AUTH_AAD_AUTHORITY; // duplicate
            REACT_APP_PCS_WEBUI_AUTH_AAD_TENANT = PCS_WEBUI_AUTH_AAD_TENANT; // duplicate

            Dict = new Dictionary<string, string> {
                ////////////////////----------2.6.104----------////////////////////
                // IoT Hub
                { $"{nameof(PCS_IOTHUB_CONNSTRING)}", PCS_IOTHUB_CONNSTRING },
                { $"{nameof(PCS_IOTHUB_EVENTHUBENDPOINT)}", PCS_IOTHUB_EVENTHUBENDPOINT },
                { $"{nameof(PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_EVENTS)}", PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_EVENTS },
                { $"{nameof(PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_TELEMETRY)}", PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_TELEMETRY },

                // Cosmos DB
                { $"{nameof(PCS_COSMOSDB_CONNSTRING)}", PCS_COSMOSDB_CONNSTRING },

                // Storage Account
                { $"{nameof(PCS_STORAGE_CONNSTRING)}", PCS_STORAGE_CONNSTRING },
                { $"{nameof(PCS_STORAGE_CONTAINER_DATAPROTECTION)}", PCS_STORAGE_CONTAINER_DATAPROTECTION },

                // ADLS Gen2 Storage Account
                { $"{nameof(PCS_ADLSG2_CONNSTRING)}", PCS_ADLSG2_CONNSTRING },
                { $"{nameof(PCS_ADLSG2_CONTAINER_CDM)}", PCS_ADLSG2_CONTAINER_CDM },
                { $"{nameof(PCS_ADLSG2_CONTAINER_CDM_ROOTFOLDER)}", PCS_ADLSG2_CONTAINER_CDM_ROOTFOLDER },

                // Event Hub Namespace
                { $"{nameof(PCS_EVENTHUB_CONNSTRING)}", PCS_EVENTHUB_CONNSTRING },
                { $"{nameof(PCS_EVENTHUB_NAME)}", PCS_EVENTHUB_NAME },
                { $"{nameof(PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_CDM)}", PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_CDM },
                { $"{nameof(PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_UX)}", PCS_EVENTHUB_CONSUMERGROUP_TELEMETRY_UX },

                // Service Bus
                { $"{nameof(PCS_SERVICEBUS_CONNSTRING)}", PCS_SERVICEBUS_CONNSTRING },

                // Key Vault
                { $"{nameof(PCS_KEYVAULT_URL)}", PCS_KEYVAULT_URL },
                { $"{nameof(PCS_KEYVAULT_KEY_DATAPROTECTION)}", PCS_KEYVAULT_KEY_DATAPROTECTION },
                { $"{nameof(PCS_KEYVAULT_APPID)}", PCS_KEYVAULT_APPID },
                { $"{nameof(PCS_KEYVAULT_SECRET)}", PCS_KEYVAULT_SECRET },

                // Application Insights
                { $"{nameof(PCS_APPINSIGHTS_INSTRUMENTATIONKEY)}", PCS_APPINSIGHTS_INSTRUMENTATIONKEY },

                // Service URLs
                { $"{nameof(PCS_SERVICE_URL)}", PCS_SERVICE_URL },
                { $"{nameof(PCS_TWIN_REGISTRY_URL)}", PCS_TWIN_REGISTRY_URL },
                { $"{nameof(PCS_TWIN_SERVICE_URL)}", PCS_TWIN_SERVICE_URL },
                { $"{nameof(PCS_HISTORY_SERVICE_URL)}", PCS_HISTORY_SERVICE_URL },
                { $"{nameof(PCS_VAULT_SERVICE_URL)}", PCS_VAULT_SERVICE_URL },
                { $"{nameof(PCS_ONBOARDING_SERVICE_URL)}", PCS_ONBOARDING_SERVICE_URL },
                { $"{nameof(PCS_PUBLISHER_SERVICE_URL)}", PCS_PUBLISHER_SERVICE_URL },
                { $"{nameof(PCS_JOBS_SERVICE_URL)}", PCS_JOBS_SERVICE_URL },
                { $"{nameof(PCS_JOB_ORCHESTRATOR_SERVICE_URL)}", PCS_JOB_ORCHESTRATOR_SERVICE_URL },
                { $"{nameof(PCS_CONFIGURATION_SERVICE_URL)}", PCS_CONFIGURATION_SERVICE_URL },

                // SignalR
                { $"{nameof(PCS_SIGNALR_CONNSTRING)}", PCS_SIGNALR_CONNSTRING },

                // Authentication
                { $"{nameof(PCS_AUTH_REQUIRED)}", PCS_AUTH_REQUIRED },
                { $"{nameof(PCS_AUTH_TENANT)}", PCS_AUTH_TENANT },
                { $"{nameof(PCS_AUTH_INSTANCE)}", PCS_AUTH_INSTANCE },
                { $"{nameof(PCS_AUTH_ISSUER)}", PCS_AUTH_ISSUER },
                { $"{nameof(PCS_AUTH_HTTPSREDIRECTPORT)}", PCS_AUTH_HTTPSREDIRECTPORT },
                { $"{nameof(PCS_AUTH_AUDIENCE)}", PCS_AUTH_AUDIENCE },
                { $"{nameof(PCS_AUTH_CLIENT_APPID)}", PCS_AUTH_CLIENT_APPID },
                { $"{nameof(PCS_AUTH_CLIENT_SECRET)}", PCS_AUTH_CLIENT_SECRET },
                { $"{nameof(PCS_AUTH_SERVICE_APPID)}", PCS_AUTH_SERVICE_APPID },
                { $"{nameof(PCS_AUTH_SERVICE_SECRET)}", PCS_AUTH_SERVICE_SECRET },

                // CORS Whitelist
                { $"{nameof(PCS_CORS_WHITELIST)}", PCS_CORS_WHITELIST },

                // Service URL path bases
                { $"{nameof(PCS_TWIN_REGISTRY_SERVICE_PATH_BASE)}", PCS_TWIN_REGISTRY_SERVICE_PATH_BASE },
                { $"{nameof(PCS_TWIN_SERVICE_PATH_BASE)}", PCS_TWIN_SERVICE_PATH_BASE },
                { $"{nameof(PCS_HISTORY_SERVICE_PATH_BASE)}", PCS_HISTORY_SERVICE_PATH_BASE },
                { $"{nameof(PCS_GATEWAY_SERVICE_PATH_BASE)}", PCS_GATEWAY_SERVICE_PATH_BASE },
                { $"{nameof(PCS_VAULT_SERVICE_PATH_BASE)}", PCS_VAULT_SERVICE_PATH_BASE },
                { $"{nameof(PCS_ONBOARDING_SERVICE_PATH_BASE)}", PCS_ONBOARDING_SERVICE_PATH_BASE },
                { $"{nameof(PCS_PUBLISHER_SERVICE_PATH_BASE)}", PCS_PUBLISHER_SERVICE_PATH_BASE },
                { $"{nameof(PCS_CONFIGURATION_SERVICE_PATH_BASE)}", PCS_CONFIGURATION_SERVICE_PATH_BASE },
                { $"{nameof(PCS_EDGE_MANAGER_SERVICE_PATH_BASE)}", PCS_EDGE_MANAGER_SERVICE_PATH_BASE },
                { $"{nameof(PCS_FRONTEND_APP_SERVICE_PATH_BASE)}", PCS_FRONTEND_APP_SERVICE_PATH_BASE },
                { $"{nameof(PCS_JOB_ORCHESTRATOR_SERVICE_PATH_BASE)}", PCS_JOB_ORCHESTRATOR_SERVICE_PATH_BASE },
                { $"{nameof(PCS_JOBS_SERVICE_PATH_BASE)}", PCS_JOBS_SERVICE_PATH_BASE },

                // AspNetCore
                { $"{nameof(ASPNETCORE_FORWARDEDHEADERS_ENABLED)}", ASPNETCORE_FORWARDEDHEADERS_ENABLED },
                { $"{nameof(ASPNETCORE_FORWARDEDHEADERS_FORWARDLIMIT)}", ASPNETCORE_FORWARDEDHEADERS_FORWARDLIMIT },

                // OpenAPI
                { $"{nameof(PCS_OPENAPI_SERVER_HOST)}", PCS_OPENAPI_SERVER_HOST },

                ////////////////////----------2.5.2----------////////////////////
                { "_HUB_CS", _HUB_CS },
                //{ "PCS_IOTHUB_CONNSTRING", PCS_IOTHUB_CONNSTRING },
                { "PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING", PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING },
                { "PCS_TELEMETRY_DOCUMENTDB_CONNSTRING", PCS_TELEMETRY_DOCUMENTDB_CONNSTRING },
                { "PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING", PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING },
                { "PCS_IOTHUBREACT_ACCESS_CONNSTRING", PCS_IOTHUBREACT_ACCESS_CONNSTRING },
                { "PCS_IOTHUBREACT_HUB_NAME", PCS_IOTHUBREACT_HUB_NAME },
                { "PCS_IOTHUBREACT_HUB_ENDPOINT", PCS_IOTHUBREACT_HUB_ENDPOINT },
                { "PCS_IOTHUBREACT_HUB_CONSUMERGROUP", PCS_IOTHUBREACT_HUB_CONSUMERGROUP },
                { "PCS_IOTHUBREACT_HUB_PARTITIONS", PCS_IOTHUBREACT_HUB_PARTITIONS },
                { "PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT", PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT },
                { "PCS_IOTHUBREACT_AZUREBLOB_KEY", PCS_IOTHUBREACT_AZUREBLOB_KEY },
                { "PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX", PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX },
                { "PCS_ASA_DATA_AZUREBLOB_ACCOUNT", PCS_ASA_DATA_AZUREBLOB_ACCOUNT },
                { "PCS_ASA_DATA_AZUREBLOB_KEY", PCS_ASA_DATA_AZUREBLOB_KEY },
                { "PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX", PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX },
                //{ "PCS_EVENTHUB_CONNSTRING", PCS_EVENTHUB_CONNSTRING },
                //{ "PCS_EVENTHUB_NAME", PCS_EVENTHUB_NAME },
                //{ "PCS_SERVICEBUS_CONNSTRING", PCS_SERVICEBUS_CONNSTRING },
                //{ "PCS_KEYVAULT_URL", PCS_KEYVAULT_URL },
                { "PCS_WORKSPACE_NAME", PCS_WORKSPACE_NAME },
                { "PCS_APPINSIGHTS_NAME", PCS_APPINSIGHTS_NAME },
                //{ "PCS_APPINSIGHTS_INSTRUMENTATIONKEY", PCS_APPINSIGHTS_INSTRUMENTATIONKEY },
                //{ "PCS_SERVICE_URL", PCS_SERVICE_URL },
                //{ "PCS_SIGNALR_CONNSTRING", PCS_SIGNALR_CONNSTRING },
                //{ "PCS_AUTH_HTTPSREDIRECTPORT", PCS_AUTH_HTTPSREDIRECTPORT },
                //{ "PCS_AUTH_REQUIRED", PCS_AUTH_REQUIRED },
                //{ "PCS_AUTH_AUDIENCE", PCS_AUTH_AUDIENCE },
                //{ "PCS_AUTH_ISSUER", PCS_AUTH_ISSUER },
                { "PCS_WEBUI_AUTH_AAD_APPID", PCS_WEBUI_AUTH_AAD_APPID },
                { "PCS_WEBUI_AUTH_AAD_AUTHORITY", PCS_WEBUI_AUTH_AAD_AUTHORITY },
                { "PCS_WEBUI_AUTH_AAD_TENANT", PCS_WEBUI_AUTH_AAD_TENANT },
                //{ "PCS_CORS_WHITELIST", PCS_CORS_WHITELIST },
                { "REACT_APP_PCS_AUTH_REQUIRED", REACT_APP_PCS_AUTH_REQUIRED },
                { "REACT_APP_PCS_AUTH_AUDIENCE", REACT_APP_PCS_AUTH_AUDIENCE },
                { "REACT_APP_PCS_AUTH_ISSUER", REACT_APP_PCS_AUTH_ISSUER },
                { "REACT_APP_PCS_WEBUI_AUTH_AAD_APPID", REACT_APP_PCS_WEBUI_AUTH_AAD_APPID },
                { "REACT_APP_PCS_WEBUI_AUTH_AAD_AUTHORITY", REACT_APP_PCS_WEBUI_AUTH_AAD_AUTHORITY },
                { "REACT_APP_PCS_WEBUI_AUTH_AAD_TENANT", REACT_APP_PCS_WEBUI_AUTH_AAD_TENANT }
            };
        }

        public void WriteToFile(string path) {
            try {
                var iiotEnvVarLines = Dict
                    .Select(kvp => $"{kvp.Key}={kvp.Value}")
                    .ToList();

                Log.Information($"Writing environment to file: '{path}' ...");

                System.IO.File.WriteAllLines(path, iiotEnvVarLines);
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to write environment to file: '{path}'");
                throw;
            }
        }
    }
}
