// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.CosmosDB.Fluent;
    using Microsoft.Azure.Management.CosmosDB.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Serilog;

    class CosmosDBMgmtClient : IDisposable {

        public const string DEFAULT_COSMOS_DB_ACCOUNT_NAME_PREFIX = "cosmosDB-";

        private const string COSMOS_DB_ACCOUNT_CONNECTION_STRING_FORMAT = "AccountEndpoint={0};AccountKey={1};";

        private readonly CosmosDB _cosmosDBManagementClient;

        public CosmosDBMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            _cosmosDBManagementClient = new CosmosDB(restClient) {
                SubscriptionId = subscriptionId
            };
        }

        public static string GenerateCosmosDBAccountName(
            string prefix = DEFAULT_COSMOS_DB_ACCOUNT_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        public async Task<DatabaseAccountInner> CreateDatabaseAccountAsync(
            IResourceGroup resourceGroup,
            string cosmosDBAccountName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                if (null == tags) {
                    tags = new Dictionary<string, string> { };
                }

                Log.Information($"Creating Azure CosmosDB Account: {cosmosDBAccountName} ...");

                var databaseAccountParameters = new DatabaseAccountCreateUpdateParametersInner {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    //DatabaseAccountOfferType = "Standard",
                    Kind = DatabaseAccountKind.GlobalDocumentDB,

                    ConsistencyPolicy = new ConsistencyPolicy {
                        DefaultConsistencyLevel = DefaultConsistencyLevel.Strong,
                        MaxStalenessPrefix = 10,
                        MaxIntervalInSeconds = 5
                    },
                    Locations = new List<Location> {
                        new Location {
                            LocationName = resourceGroup.RegionName,
                            FailoverPriority = 0,
                            IsZoneRedundant = false
                        }
                    }
                };

                databaseAccountParameters.Validate();

                var cosmosDBAccount = await _cosmosDBManagementClient
                    .DatabaseAccounts
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        cosmosDBAccountName,
                        databaseAccountParameters,
                        cancellationToken
                    );

                Log.Information($"Created Azure CosmosDB Account: {cosmosDBAccountName}");

                return cosmosDBAccount;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure CosmosDB Account: {cosmosDBAccountName}");
                throw;
            }
        }

        public async Task<string> GetCosmosDBAccountConnectionStringAsync(
            IResourceGroup resourceGroup,
            DatabaseAccountInner cosmosDBAccount,
            CancellationToken cancellationToken = default
        ) {
            var cosmosDBAccountKeys = await _cosmosDBManagementClient
                .DatabaseAccounts
                .ListKeysAsync(
                    resourceGroup.Name,
                    cosmosDBAccount.Name,
                    cancellationToken
                );

            var cosmosDBAccountConnectionString = string.Format(
                COSMOS_DB_ACCOUNT_CONNECTION_STRING_FORMAT,
                cosmosDBAccount.DocumentEndpoint,
                cosmosDBAccountKeys.PrimaryMasterKey
            );

            return cosmosDBAccountConnectionString;
        }

        public void Dispose() {
            if (null != _cosmosDBManagementClient) {
                _cosmosDBManagementClient.Dispose();
            }
        }
    }
}
