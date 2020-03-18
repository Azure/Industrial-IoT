// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.CosmosDB.Fluent;
    using Microsoft.Azure.Management.CosmosDB.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Serilog;

    class CosmosDBMgmtClient : IDisposable {

        public const string DEFAULT_COSMOS_DB_ACCOUNT_NAME_PREFIX = "cosmosDB-";
        public const int NUM_OF_MAX_NAME_AVAILABILITY_CHECKS = 5;

        private const string kCOSMOS_DB_ACCOUNT_CONNECTION_STRING_FORMAT = "AccountEndpoint={0};AccountKey={1};";

        private readonly CosmosDBManagementClient _cosmosDBManagementClient;

        public CosmosDBMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            _cosmosDBManagementClient = new CosmosDBManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };
        }

        public static string GenerateCosmosDBAccountName(
            string prefix = DEFAULT_COSMOS_DB_ACCOUNT_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        /// <summary>
        /// Checks whether given CosmosDB account name is available.
        /// </summary>
        /// <param name="cosmosDBAccountName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if name is available, False otherwise.</returns>
        /// <exception cref="Microsoft.Rest.Azure.CloudException"></exception>
        public async Task<bool> CheckNameAvailabilityAsync(
            string cosmosDBAccountName,
            CancellationToken cancellationToken = default
        ) {
            try {
                var nameExists = await _cosmosDBManagementClient
                    .DatabaseAccounts
                    .CheckNameExistsAsync(
                        cosmosDBAccountName,
                        cancellationToken
                    );

                return !nameExists;
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                // Will be thrown if there is no registered resource provider
                // found for specified location and/or api version to perform
                // name availability check.
                throw;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to check CosmosDB Account name availability for {cosmosDBAccountName}");
                throw;
            }
        }

        /// <summary>
        /// Tries to generate CosmosDB account name that is available.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>An available name for CosmosDB account.</returns>
        /// <exception cref="Microsoft.Rest.Azure.CloudException"></exception>
        public async Task<string> GenerateAvailableNameAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                for (var numOfChecks = 0; numOfChecks < NUM_OF_MAX_NAME_AVAILABILITY_CHECKS; ++numOfChecks) {
                    var cosmosDBAccountName = GenerateCosmosDBAccountName();
                    var nameAvailable = await CheckNameAvailabilityAsync(
                            cosmosDBAccountName,
                            cancellationToken
                        );

                    if (nameAvailable) {
                        return cosmosDBAccountName;
                    }
                }
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                // Will be thrown if there is no registered resource provider
                // found for specified location and/or api version to perform
                // name availability check.
                throw;
            }
            catch (Exception ex) {
                Log.Error(ex, "Failed to generate unique CosmosDB Account name");
                throw;
            }

            var errorMessage = $"Failed to generate unique CosmosDB Account name " +
                $"after {NUM_OF_MAX_NAME_AVAILABILITY_CHECKS} retries";

            Log.Error(errorMessage);
            throw new Exception(errorMessage);
        }

        public async Task<DatabaseAccountGetResultsInner> CreateDatabaseAccountAsync(
            IResourceGroup resourceGroup,
            string cosmosDBAccountName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Creating Azure CosmosDB Account: {cosmosDBAccountName} ...");

                var databaseAccountParameters = new DatabaseAccountCreateUpdateParameters {
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
            DatabaseAccountGetResultsInner cosmosDBAccount,
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
                kCOSMOS_DB_ACCOUNT_CONNECTION_STRING_FORMAT,
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
