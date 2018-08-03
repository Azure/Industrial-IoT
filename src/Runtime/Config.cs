// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Runtime
{
    /// <summary>Web service configuration</summary>
    public class Config : ConfigData
    {
        // web service config
        private const string ApplicationKey = "GdsVault:";
        private const string PortKey = ApplicationKey + "webservice_port";

        // services config
        private const string KeyVaultKey = "keyvault:";
        private const string KeyVaultApiUrlKey = KeyVaultKey + "serviceuri";
        private const string KeyVaultApiTimeoutKey = KeyVaultKey + "timeout";
        private const string CosmosDBKey = "cosmosdb:";
        private const string CosmosDBEndpointKey = CosmosDBKey + "endpoint";
        private const string CosmosDBTokenKey = CosmosDBKey + "token";

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfigurationRoot configuration) :
            base(Uptime.ProcessId, ServiceInfo.ID, configuration)
        {
        }

        private static string MapRelativePath(string path)
        {
            if (path.StartsWith(".")) return AppContext.BaseDirectory + Path.DirectorySeparatorChar + path;
            return path;
        }

        /// <summary>Service layer configuration</summary>
        public IServicesConfig ServicesConfig =>
            new ServicesConfig
            {
                KeyVaultApiUrl = this.GetString(KeyVaultApiUrlKey),
                KeyVaultApiTimeout = this.GetInt(KeyVaultApiTimeoutKey),
                CosmosDBEndpoint = this.GetString(CosmosDBEndpointKey),
                CosmosDBToken = this.GetString(CosmosDBTokenKey)
            };
    }
}

