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

        public readonly string _HUB_CS;

        public readonly string PCS_IOTHUB_CONNSTRING;
        public readonly string PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING;
        public readonly string PCS_TELEMETRY_DOCUMENTDB_CONNSTRING;
        public readonly string PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING;

        public readonly string PCS_IOTHUBREACT_ACCESS_CONNSTRING;
        public readonly string PCS_IOTHUBREACT_HUB_NAME;
        public readonly string PCS_IOTHUBREACT_HUB_ENDPOINT;
        public readonly string PCS_IOTHUBREACT_HUB_CONSUMERGROUP;
        public readonly string PCS_IOTHUBREACT_HUB_PARTITIONS;
        public readonly string PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT;
        public readonly string PCS_IOTHUBREACT_AZUREBLOB_KEY;
        public readonly string PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX;

        public readonly string PCS_ASA_DATA_AZUREBLOB_ACCOUNT;
        public readonly string PCS_ASA_DATA_AZUREBLOB_KEY;
        public readonly string PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX;

        public readonly string PCS_EVENTHUB_CONNSTRING;
        public readonly string PCS_EVENTHUB_NAME;
        public readonly string PCS_SERVICEBUS_CONNSTRING;
        public readonly string PCS_KEYVAULT_URL;
        public readonly string PCS_WORKSPACE_NAME;
        public readonly string PCS_APPINSIGHTS_NAME;
        public readonly string PCS_APPINSIGHTS_INSTRUMENTATIONKEY;
        public readonly string PCS_SERVICE_URL;
        public readonly string PCS_SIGNALR_CONNSTRING;

        public readonly string PCS_AUTH_HTTPSREDIRECTPORT;
        public readonly string PCS_AUTH_REQUIRED;
        public readonly string PCS_AUTH_AUDIENCE;
        public readonly string PCS_AUTH_ISSUER;

        public readonly string PCS_WEBUI_AUTH_AAD_APPID;
        public readonly string PCS_WEBUI_AUTH_AAD_AUTHORITY;
        public readonly string PCS_WEBUI_AUTH_AAD_TENANT;

        public readonly string PCS_CORS_WHITELIST;

        public readonly string REACT_APP_PCS_AUTH_REQUIRED;
        public readonly string REACT_APP_PCS_AUTH_AUDIENCE;
        public readonly string REACT_APP_PCS_AUTH_ISSUER;
        public readonly string REACT_APP_PCS_WEBUI_AUTH_AAD_APPID;
        public readonly string REACT_APP_PCS_WEBUI_AUTH_AAD_AUTHORITY;
        public readonly string REACT_APP_PCS_WEBUI_AUTH_AAD_TENANT;

        public readonly Dictionary<string, string> Dict;

        public IIoTEnvironment(
            AzureEnvironment azureEnvironment,
            Guid tenantId,
            IotHubDescription iotHub,
            string iotHubOwnerConnectionString,
            string iotHubOnboardingConsumerGroupName,
            int iotHubEventHubEndpointsPartitionsCount,
            string cosmosDBAccountConnectionString,
            StorageAccountInner storageAccount,
            StorageAccountKey storageAccountKey,
            EventhubInner eventHub,
            string eventHubConnectionString,
            string serviceBusConnectionString,
            string signalRConnectionString,
            VaultInner keyVault,
            Workspace operationalInsightsWorkspace,
            ApplicationInsightsComponent applicationInsightsComponent,
            SiteInner webSite,
            Application serviceApplication,
            Application clientApplication

        ) {
            _HUB_CS = iotHubOwnerConnectionString;

            PCS_IOTHUB_CONNSTRING = iotHubOwnerConnectionString; // duplicate
            PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING = cosmosDBAccountConnectionString;
            PCS_TELEMETRY_DOCUMENTDB_CONNSTRING = cosmosDBAccountConnectionString; // duplicate
            PCS_TELEMETRYAGENT_DOCUMENTDB_CONNSTRING = cosmosDBAccountConnectionString; // duplicate

            PCS_IOTHUBREACT_ACCESS_CONNSTRING = iotHubOwnerConnectionString; // duplicate
            PCS_IOTHUBREACT_HUB_NAME = iotHub.Name;
            PCS_IOTHUBREACT_HUB_ENDPOINT = iotHub.Properties.EventHubEndpoints["events"].Endpoint;
            PCS_IOTHUBREACT_HUB_CONSUMERGROUP = iotHubOnboardingConsumerGroupName;
            PCS_IOTHUBREACT_HUB_PARTITIONS = $"{iotHubEventHubEndpointsPartitionsCount}";
            PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT = storageAccount.Name;
            PCS_IOTHUBREACT_AZUREBLOB_KEY = storageAccountKey.Value;
            PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX = azureEnvironment.StorageEndpointSuffix;

            PCS_ASA_DATA_AZUREBLOB_ACCOUNT = PCS_IOTHUBREACT_AZUREBLOB_ACCOUNT; // duplicate
            PCS_ASA_DATA_AZUREBLOB_KEY = PCS_IOTHUBREACT_AZUREBLOB_KEY; // duplicate
            PCS_ASA_DATA_AZUREBLOB_ENDPOINT_SUFFIX = PCS_IOTHUBREACT_AZUREBLOB_ENDPOINT_SUFFIX; // duplicate

            PCS_EVENTHUB_CONNSTRING = eventHubConnectionString;
            PCS_EVENTHUB_NAME = eventHub.Name;
            PCS_SERVICEBUS_CONNSTRING = serviceBusConnectionString;
            PCS_KEYVAULT_URL = keyVault.Properties.VaultUri;
            PCS_WORKSPACE_NAME = operationalInsightsWorkspace.Name;
            PCS_APPINSIGHTS_NAME = applicationInsightsComponent.Name;
            PCS_APPINSIGHTS_INSTRUMENTATIONKEY = applicationInsightsComponent.InstrumentationKey;
            PCS_SERVICE_URL = $"https://{webSite.HostNames[0]}";
            PCS_SIGNALR_CONNSTRING = signalRConnectionString;

            PCS_AUTH_HTTPSREDIRECTPORT = "0";
            PCS_AUTH_REQUIRED = "true";
            PCS_AUTH_AUDIENCE = serviceApplication.IdentifierUris.First();
            PCS_AUTH_ISSUER = $"https://sts.windows.net/{tenantId.ToString()}/";

            PCS_WEBUI_AUTH_AAD_APPID = clientApplication.AppId;
            PCS_WEBUI_AUTH_AAD_AUTHORITY = azureEnvironment.AuthenticationEndpoint;
            PCS_WEBUI_AUTH_AAD_TENANT = tenantId.ToString();

            PCS_CORS_WHITELIST = "*";

            REACT_APP_PCS_AUTH_REQUIRED = PCS_AUTH_REQUIRED; // duplicate
            REACT_APP_PCS_AUTH_AUDIENCE = PCS_AUTH_AUDIENCE; // duplicate
            REACT_APP_PCS_AUTH_ISSUER = PCS_AUTH_ISSUER; // duplicate
            REACT_APP_PCS_WEBUI_AUTH_AAD_APPID = PCS_WEBUI_AUTH_AAD_APPID; // duplicate
            REACT_APP_PCS_WEBUI_AUTH_AAD_AUTHORITY = PCS_WEBUI_AUTH_AAD_AUTHORITY; // duplicate
            REACT_APP_PCS_WEBUI_AUTH_AAD_TENANT = PCS_WEBUI_AUTH_AAD_TENANT; // duplicate

            Dict = new Dictionary<string, string> {
                { "_HUB_CS", _HUB_CS },
                { RuntimeVariable.PCS_IOTHUB_CONNSTRING, PCS_IOTHUB_CONNSTRING },
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
                { "PCS_EVENTHUB_CONNSTRING", PCS_EVENTHUB_CONNSTRING },
                { "PCS_EVENTHUB_NAME", PCS_EVENTHUB_NAME },
                { "PCS_SERVICEBUS_CONNSTRING", PCS_SERVICEBUS_CONNSTRING },
                { "PCS_KEYVAULT_URL", PCS_KEYVAULT_URL },
                { "PCS_WORKSPACE_NAME", PCS_WORKSPACE_NAME },
                { "PCS_APPINSIGHTS_NAME", PCS_APPINSIGHTS_NAME },
                { "PCS_APPINSIGHTS_INSTRUMENTATIONKEY", PCS_APPINSIGHTS_INSTRUMENTATIONKEY },
                { "PCS_SERVICE_URL", PCS_SERVICE_URL },
                { "PCS_SIGNALR_CONNSTRING", PCS_SIGNALR_CONNSTRING },
                { "PCS_AUTH_HTTPSREDIRECTPORT", PCS_AUTH_HTTPSREDIRECTPORT },
                { "PCS_AUTH_REQUIRED", PCS_AUTH_REQUIRED },
                { "PCS_AUTH_AUDIENCE", PCS_AUTH_AUDIENCE },
                { "PCS_AUTH_ISSUER", PCS_AUTH_ISSUER },
                { "PCS_WEBUI_AUTH_AAD_APPID", PCS_WEBUI_AUTH_AAD_APPID },
                { "PCS_WEBUI_AUTH_AAD_AUTHORITY", PCS_WEBUI_AUTH_AAD_AUTHORITY },
                { "PCS_WEBUI_AUTH_AAD_TENANT", PCS_WEBUI_AUTH_AAD_TENANT },
                { "PCS_CORS_WHITELIST", PCS_CORS_WHITELIST },
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
