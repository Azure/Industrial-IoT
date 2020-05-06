// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
    using Microsoft.Azure.Management.EventHub.Fluent.Models;
    using Microsoft.Azure.Management.IotHub.Models;
    using Microsoft.Azure.Management.KeyVault.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;

    using Microsoft.Graph;
    using Serilog;

    class IIoTEnvironment {
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

        // AKS internal service URLs
        public readonly string PCS_TWIN_REGISTRY_URL_INTERNAL;
        public readonly string PCS_TWIN_SERVICE_URL_INTERNAL;
        public readonly string PCS_HISTORY_SERVICE_URL_INTERNAL;
        public readonly string PCS_VAULT_SERVICE_URL_INTERNAL;
        public readonly string PCS_ONBOARDING_SERVICE_URL_INTERNAL;
        public readonly string PCS_PUBLISHER_SERVICE_URL_INTERNAL;
        public readonly string PCS_JOBS_SERVICE_URL_INTERNAL;
        public readonly string PCS_JOB_ORCHESTRATOR_SERVICE_URL_INTERNAL;
        public readonly string PCS_CONFIGURATION_SERVICE_URL_INTERNAL;

        // Externally accessible service URLs
        public readonly string PCS_TWIN_REGISTRY_URL_EXTERNAL;
        public readonly string PCS_TWIN_SERVICE_URL_EXTERNAL;
        public readonly string PCS_HISTORY_SERVICE_URL_EXTERNAL;
        public readonly string PCS_VAULT_SERVICE_URL_EXTERNAL;
        public readonly string PCS_ONBOARDING_SERVICE_URL_EXTERNAL;
        public readonly string PCS_PUBLISHER_SERVICE_URL_EXTERNAL;
        public readonly string PCS_JOBS_SERVICE_URL_EXTERNAL;
        public readonly string PCS_JOB_ORCHESTRATOR_SERVICE_URL_EXTERNAL;
        public readonly string PCS_CONFIGURATION_SERVICE_URL_EXTERNAL;

        // Service URLs that will be consumed by microservices.
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
            // Application Insights
            ApplicationInsightsComponent applicationInsightsComponent,
            string serviceURL,
            Application serviceApplication,
            string serviceApplicationSecret,
            Application clientApplication,
            string clientApplicationSecret

        ) {
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
            PCS_SERVICE_URL = serviceURL;

            var iiotNamespace = "industrial-iot";

            // AKS internal service URLs
            PCS_TWIN_REGISTRY_URL_INTERNAL = $"http://{"registry-service"}.{iiotNamespace}:{9042}";
            PCS_TWIN_SERVICE_URL_INTERNAL = $"http://{"twin-service"}.{iiotNamespace}:{9041}";
            PCS_HISTORY_SERVICE_URL_INTERNAL = $"http://{"history-service"}.{iiotNamespace}:{9043}";
            PCS_VAULT_SERVICE_URL_INTERNAL = $"http://{"vault-service"}.{iiotNamespace}:{9044}";
            PCS_ONBOARDING_SERVICE_URL_INTERNAL = $"http://{"onboarding-service"}.{iiotNamespace}:{9060}";
            PCS_PUBLISHER_SERVICE_URL_INTERNAL = $"http://{"publisher-service"}.{iiotNamespace}:{9045}";
            PCS_JOBS_SERVICE_URL_INTERNAL = $"http://{"publisher-jobs-service"}.{iiotNamespace}:{9046}";
            PCS_JOB_ORCHESTRATOR_SERVICE_URL_INTERNAL = $"http://{"edge-jobs-service"}.{iiotNamespace}:{9051}";
            PCS_CONFIGURATION_SERVICE_URL_INTERNAL = $"http://{"configuration-service"}.{iiotNamespace}:{9050}";

            // Externally accessible service URLs
            serviceURL = serviceURL.TrimEnd('/');
            PCS_TWIN_REGISTRY_URL_EXTERNAL = $"{serviceURL}/registry/";
            PCS_TWIN_SERVICE_URL_EXTERNAL = $"{serviceURL}/twin/";
            PCS_HISTORY_SERVICE_URL_EXTERNAL = $"{serviceURL}/history/";
            PCS_VAULT_SERVICE_URL_EXTERNAL = $"{serviceURL}/vault/";
            PCS_ONBOARDING_SERVICE_URL_EXTERNAL = $"{serviceURL}/onboarding/";
            PCS_PUBLISHER_SERVICE_URL_EXTERNAL = $"{serviceURL}/publisher/";
            PCS_JOBS_SERVICE_URL_EXTERNAL = $"{serviceURL}/jobs/";
            PCS_JOB_ORCHESTRATOR_SERVICE_URL_EXTERNAL = $"{serviceURL}/edge/jobs/";
            PCS_CONFIGURATION_SERVICE_URL_EXTERNAL = $"{serviceURL}/configuration/";

            // Service URLs that will be consumed by microservices.
            PCS_TWIN_REGISTRY_URL = PCS_TWIN_REGISTRY_URL_INTERNAL;
            PCS_TWIN_SERVICE_URL = PCS_TWIN_SERVICE_URL_INTERNAL;
            PCS_HISTORY_SERVICE_URL = PCS_HISTORY_SERVICE_URL_INTERNAL;
            PCS_VAULT_SERVICE_URL = PCS_VAULT_SERVICE_URL_INTERNAL;
            PCS_ONBOARDING_SERVICE_URL = PCS_ONBOARDING_SERVICE_URL_INTERNAL;
            PCS_PUBLISHER_SERVICE_URL = PCS_PUBLISHER_SERVICE_URL_INTERNAL;
            PCS_JOBS_SERVICE_URL = PCS_JOBS_SERVICE_URL_INTERNAL;
            // NOTE: PCS_JOB_ORCHESTRATOR_SERVICE_URL should be externally accessible URL.
            PCS_JOB_ORCHESTRATOR_SERVICE_URL = PCS_JOB_ORCHESTRATOR_SERVICE_URL_EXTERNAL;
            PCS_CONFIGURATION_SERVICE_URL = PCS_CONFIGURATION_SERVICE_URL_INTERNAL;

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

            Dict = new Dictionary<string, string> {
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
            };
        }

        /// <summary>
        /// Write environment variables into a file. Service URL variables for microservices
        /// will point to externally accessible URLs.
        /// </summary>
        /// <param name="path"></param>
        public void WriteToFile(string path) {
            try {
                // We will create a new dictionary where service URLs would be externally
                // accessible ones. That is required so that client applications consuming
                // the file can run outside of AKS cluster.
                var extDict = Dict
                    .ToDictionary(
                        entry => entry.Key.Clone(),
                        entry => entry.Value.Clone()
                     );

                extDict[nameof(PCS_TWIN_REGISTRY_URL)] = PCS_TWIN_REGISTRY_URL_EXTERNAL;
                extDict[nameof(PCS_TWIN_SERVICE_URL)] = PCS_TWIN_SERVICE_URL_EXTERNAL;
                extDict[nameof(PCS_HISTORY_SERVICE_URL)] = PCS_HISTORY_SERVICE_URL_EXTERNAL;
                extDict[nameof(PCS_VAULT_SERVICE_URL)] = PCS_VAULT_SERVICE_URL_EXTERNAL;
                extDict[nameof(PCS_ONBOARDING_SERVICE_URL)] = PCS_ONBOARDING_SERVICE_URL_EXTERNAL;
                extDict[nameof(PCS_PUBLISHER_SERVICE_URL)] = PCS_PUBLISHER_SERVICE_URL_EXTERNAL;
                extDict[nameof(PCS_JOBS_SERVICE_URL)] = PCS_JOBS_SERVICE_URL_EXTERNAL;
                extDict[nameof(PCS_JOB_ORCHESTRATOR_SERVICE_URL)] = PCS_JOB_ORCHESTRATOR_SERVICE_URL_EXTERNAL;
                extDict[nameof(PCS_CONFIGURATION_SERVICE_URL)] = PCS_CONFIGURATION_SERVICE_URL_EXTERNAL;

                var iiotEnvVarLines = extDict
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
