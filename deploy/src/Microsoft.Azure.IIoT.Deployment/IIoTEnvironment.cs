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
    using Microsoft.Azure.Management.OperationalInsights.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;

    using Microsoft.Graph;
    using Serilog;

    class IIoTEnvironment {
        // IoT Hub
        public readonly string PCS_IOTHUB_CONNSTRING;
        public readonly string PCS_IOTHUB_EVENTHUBENDPOINT;
        public readonly string PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_EVENTS;
        public readonly string PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_TELEMETRY;
        public readonly string PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_ONBOARDING;

        // Cosmos DB
        public readonly string PCS_COSMOSDB_CONNSTRING;

        // Storage Account
        public readonly string PCS_STORAGE_CONNSTRING;
        public readonly string PCS_STORAGE_CONTAINER_DATAPROTECTION;

        // Event Hub Namespace
        public readonly string PCS_EVENTHUB_CONNSTRING;
        public readonly string PCS_EVENTHUB_NAME;
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

        // Log Analytics Workspace
        public readonly string PCS_WORKSPACE_ID;
        public readonly string PCS_WORKSPACE_KEY;
        public readonly string PCS_SUBSCRIPTION_ID;
        public readonly string PCS_RESOURCE_GROUP;

        // Service URLs
        public readonly string PCS_SERVICE_URL;

        // Service URLs that will be consumed by microservices.
        public readonly string PCS_TWIN_REGISTRY_URL;
        public readonly string PCS_TWIN_SERVICE_URL;
        public readonly string PCS_HISTORY_SERVICE_URL;
        public readonly string PCS_PUBLISHER_SERVICE_URL;
        public readonly string PCS_PUBLISHER_ORCHESTRATOR_SERVICE_URL;
        public readonly string PCS_EVENTS_SERVICE_URL;

        // SignalR
        public readonly string PCS_SIGNALR_CONNSTRING;
        public readonly string PCS_SIGNALR_MODE;

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
        public readonly string PCS_PUBLISHER_SERVICE_PATH_BASE;
        public readonly string PCS_PUBLISHER_ORCHESTRATOR_SERVICE_PATH_BASE;
        public readonly string PCS_EVENTS_SERVICE_PATH_BASE;
        public readonly string PCS_FRONTEND_APP_SERVICE_PATH_BASE;

        // AspNetCore
        public readonly string ASPNETCORE_FORWARDEDHEADERS_ENABLED;
        public readonly string ASPNETCORE_FORWARDEDHEADERS_FORWARDLIMIT;

        // OpenAPI
        public readonly string PCS_OPENAPI_SERVER_HOST;

        public readonly Dictionary<string, string> Dict;

        public IIoTEnvironment(
            AzureEnvironment azureEnvironment,
            Guid tenantId,
            string subscriptionId,
            string resourceGroupName,
            // IoT Hub
            IotHubDescription iotHub,
            string iotHubOwnerConnectionString,
            string iotHubEventHubEventsEndpointName,
            EventHubConsumerGroupInfo iotHubEventHubConsumerGroupEvents,
            EventHubConsumerGroupInfo iotHubEventHubConsumerGroupTelemetry,
            EventHubConsumerGroupInfo iotHubEventHubConsumerGroupOnboarding,
            // Cosmos DB
            string cosmosDBAccountConnectionString,
            // Storage Account
            string storageAccountConectionString,
            string storageAccountContainerDataprotection,
            // Event Hub Namespace
            EventhubInner eventHub,
            string eventHubConnectionString,
            ConsumerGroupInner telemetryUx,
            // Service Bus
            string serviceBusConnectionString,
            // SignalR
            string signalRConnectionString,
            string signalRServiceMode,
            // Key Vault
            VaultInner keyVault,
            string dataprotectionKeyName,
            // Application Insights
            ApplicationInsightsComponent applicationInsightsComponent,
            // Log Analytics Workspace
            Workspace workspace,
            string workspaceKey,
            // Service URL
            string serviceURL,
            // App Registrations
            Application serviceApplication,
            string serviceApplicationSecret,
            Application clientApplication,
            string clientApplicationSecret

        ) {
            // IoT Hub
            PCS_IOTHUB_CONNSTRING = iotHubOwnerConnectionString;
            PCS_IOTHUB_EVENTHUBENDPOINT = iotHub.Properties.EventHubEndpoints[iotHubEventHubEventsEndpointName].Endpoint;
            PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_EVENTS = iotHubEventHubConsumerGroupEvents.Name;
            PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_TELEMETRY = iotHubEventHubConsumerGroupTelemetry.Name;
            PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_ONBOARDING = iotHubEventHubConsumerGroupOnboarding.Name;

            // Cosmos DB
            PCS_COSMOSDB_CONNSTRING = cosmosDBAccountConnectionString;

            // Storage Account
            PCS_STORAGE_CONNSTRING = storageAccountConectionString;
            PCS_STORAGE_CONTAINER_DATAPROTECTION = storageAccountContainerDataprotection;

            // Event Hub Namespace
            PCS_EVENTHUB_CONNSTRING = eventHubConnectionString;
            PCS_EVENTHUB_NAME = eventHub.Name;
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

            // Log Analytics Workspace
            PCS_WORKSPACE_ID = workspace.CustomerId;
            PCS_WORKSPACE_KEY = workspaceKey;
            PCS_SUBSCRIPTION_ID = subscriptionId;
            PCS_RESOURCE_GROUP = resourceGroupName;

            // Service URLs
            PCS_SERVICE_URL = serviceURL;

            // Service URLs that will be consumed by microservices.
            PCS_TWIN_REGISTRY_URL = $"{serviceURL}/registry/";
            PCS_TWIN_SERVICE_URL = $"{serviceURL}/twin/";
            PCS_HISTORY_SERVICE_URL = $"{serviceURL}/history/";
            PCS_PUBLISHER_SERVICE_URL = $"{serviceURL}/publisher/";
            PCS_PUBLISHER_ORCHESTRATOR_SERVICE_URL = $"{serviceURL}/edge/publisher/";
            PCS_EVENTS_SERVICE_URL = $"{serviceURL}/events/";

            // SignalR
            PCS_SIGNALR_CONNSTRING = signalRConnectionString;
            PCS_SIGNALR_MODE = signalRServiceMode;

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
            PCS_PUBLISHER_SERVICE_PATH_BASE = "/publisher";
            PCS_PUBLISHER_ORCHESTRATOR_SERVICE_PATH_BASE = "/edge/publisher";
            PCS_EVENTS_SERVICE_PATH_BASE = "/events";
            PCS_FRONTEND_APP_SERVICE_PATH_BASE = "/frontend";

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
                { $"{nameof(PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_ONBOARDING)}", PCS_IOTHUB_EVENTHUB_CONSUMER_GROUP_ONBOARDING },

                // Cosmos DB
                { $"{nameof(PCS_COSMOSDB_CONNSTRING)}", PCS_COSMOSDB_CONNSTRING },

                // Storage Account
                { $"{nameof(PCS_STORAGE_CONNSTRING)}", PCS_STORAGE_CONNSTRING },
                { $"{nameof(PCS_STORAGE_CONTAINER_DATAPROTECTION)}", PCS_STORAGE_CONTAINER_DATAPROTECTION },

                // Event Hub Namespace
                { $"{nameof(PCS_EVENTHUB_CONNSTRING)}", PCS_EVENTHUB_CONNSTRING },
                { $"{nameof(PCS_EVENTHUB_NAME)}", PCS_EVENTHUB_NAME },
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

                // Log Analytics Workspace
                { $"{nameof(PCS_WORKSPACE_ID)}", PCS_WORKSPACE_ID },
                { $"{nameof(PCS_WORKSPACE_KEY)}", PCS_WORKSPACE_KEY },
                { $"{nameof(PCS_SUBSCRIPTION_ID)}", PCS_SUBSCRIPTION_ID },
                { $"{nameof(PCS_RESOURCE_GROUP)}", PCS_RESOURCE_GROUP },

                // Service URLs
                { $"{nameof(PCS_SERVICE_URL)}", PCS_SERVICE_URL },

                // Service URLs that will be consumed by microservices.
                { $"{nameof(PCS_TWIN_REGISTRY_URL)}", PCS_TWIN_REGISTRY_URL },
                { $"{nameof(PCS_TWIN_SERVICE_URL)}", PCS_TWIN_SERVICE_URL },
                { $"{nameof(PCS_HISTORY_SERVICE_URL)}", PCS_HISTORY_SERVICE_URL },
                { $"{nameof(PCS_PUBLISHER_SERVICE_URL)}", PCS_PUBLISHER_SERVICE_URL },
                { $"{nameof(PCS_PUBLISHER_ORCHESTRATOR_SERVICE_URL)}", PCS_PUBLISHER_ORCHESTRATOR_SERVICE_URL },
                { $"{nameof(PCS_EVENTS_SERVICE_URL)}", PCS_EVENTS_SERVICE_URL },

                // SignalR
                { $"{nameof(PCS_SIGNALR_CONNSTRING)}", PCS_SIGNALR_CONNSTRING },
                { $"{nameof(PCS_SIGNALR_MODE)}", PCS_SIGNALR_MODE },

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
                { $"{nameof(PCS_PUBLISHER_SERVICE_PATH_BASE)}", PCS_PUBLISHER_SERVICE_PATH_BASE },
                { $"{nameof(PCS_PUBLISHER_ORCHESTRATOR_SERVICE_PATH_BASE)}", PCS_PUBLISHER_ORCHESTRATOR_SERVICE_PATH_BASE },
                { $"{nameof(PCS_EVENTS_SERVICE_PATH_BASE)}", PCS_EVENTS_SERVICE_PATH_BASE },
                { $"{nameof(PCS_FRONTEND_APP_SERVICE_PATH_BASE)}", PCS_FRONTEND_APP_SERVICE_PATH_BASE },

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
