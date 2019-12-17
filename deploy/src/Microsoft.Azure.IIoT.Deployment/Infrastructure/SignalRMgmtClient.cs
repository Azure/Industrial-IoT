// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.SignalR;
    using Microsoft.Azure.Management.SignalR.Models;

    using Serilog;

    class SignalRMgmtClient : IDisposable
    {
        public const string DEFAULT_NAME_PREFIX = "signalr-";
        public const int NUM_OF_MAX_NAME_AVAILABILITY_CHECKS = 5;

        private readonly SignalRManagementClient _signalRManagementClient;

        public SignalRMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            // We need to initialize new RestClient so that we
            // extract RootHttpHandler and DelegatingHandlers out of it.
            var signalRRestClient = RestClient
                .Configure()
                .WithEnvironment(restClient.Environment)
                .WithCredentials(restClient.Credentials)
                //.WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .Build();

            _signalRManagementClient = new SignalRManagementClient(
                signalRRestClient.Credentials,
                signalRRestClient.RootHttpHandler,
                signalRRestClient.Handlers.ToArray()
            ) {
                SubscriptionId = subscriptionId
            };
        }

        public static string GenerateName(
            string prefix = DEFAULT_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        public async Task<SignalRResource> CreateAsync(
            IResourceGroup resourceGroup,
            string signalRName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Creating SignalR Service: {signalRName} ...");

                // Constructor of SignalRFeature will set Flag to "ServiceMode".
                // Bug report for adding feature to explicitly set the Flag to "ServiceMode":
                // https://github.com/Azure/azure-sdk-for-net/issues/8806
                var serviceModeFeature = new SignalRFeature {
                    Value = "Default"
                };

                var signalRCreateParameters = new SignalRCreateParameters() {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    Sku = new ResourceSku {
                        Name = "Standard_S1",
                        Tier = "Standard",
                        Capacity = 1,
                    },
                    Properties = new SignalRCreateOrUpdateProperties {
                        HostNamePrefix = signalRName,
                        Features = new List<SignalRFeature> {
                           serviceModeFeature
                        }
                    }
                };

                signalRCreateParameters.Validate();

                var signalR = await _signalRManagementClient
                    .SignalR
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        signalRName,
                        signalRCreateParameters,
                        cancellationToken
                    );

                Log.Information($"Created SignalR Service: {signalRName}");

                return signalR;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed ot create SignalR Service: {signalRName}");
                throw;
            }
        }

        public async Task<SignalRResource> GetAsync(
            IResourceGroup resourceGroup,
            string signalRName,
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Information($"Getting SignalR Service: {signalRName} ...");

                var signalR = await _signalRManagementClient
                    .SignalR
                    .GetAsync(
                        resourceGroup.Name,
                        signalRName,
                        cancellationToken
                    );

                Log.Information($"Got SignalR Service: {signalRName}");

                return signalR;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to get SignalR Service: {signalRName}");
                throw;
            }
        }

        public async Task DeleteAsync(
            IResourceGroup resourceGroup,
            SignalRResource signalR,
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Debug($"Deleting SignalR Service: {signalR.Name} ...");

                await _signalRManagementClient
                    .SignalR
                    .DeleteAsync(
                        resourceGroup.Name,
                        signalR.Name,
                        cancellationToken
                    );

                Log.Debug($"Deleted SignalR Service: {signalR.Name}");
            }
            catch(Exception ex) {
                Log.Error(ex, $"Failed to delete SignalR Service: {signalR.Name}");
                throw;
            }
        }

        /// <summary>
        /// Checks whether given SignalR name is available.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="signalRName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if name is available, False otherwise.</returns>
        /// <exception cref="Microsoft.Rest.Azure.CloudException"></exception>
        public async Task<bool> CheckNameAvailabilityAsync(
            IResourceGroup resourceGroup,
            string signalRName,
            CancellationToken cancellationToken = default
        ) {
            try {
                var parameters = new NameAvailabilityParameters {
                    Type = "Microsoft.SignalRService/SignalR",
                    Name = signalRName
                };

                var nameAvailability = await _signalRManagementClient
                    .SignalR
                    .CheckNameAvailabilityAsync(
                        resourceGroup.RegionName,
                        parameters,
                        cancellationToken
                    );

                if (nameAvailability.NameAvailable.HasValue) {
                    return nameAvailability.NameAvailable.Value;
                }
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                // Will be thrown if there is no registered resource provider
                // found for specified location and/or api version to perform
                // name availability check.
                throw;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to check SignalR Service name availability for {signalRName}");
                throw;
            }

            throw new Exception($"Failed to check SignalR Service name availability for {signalRName}");
        }

        /// <summary>
        /// Tries to generate SignalR name that is available.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>An available name for SignalR.</returns>
        /// <exception cref="Microsoft.Rest.Azure.CloudException"></exception>
        public async Task<string> GenerateAvailableNameAsync(
            IResourceGroup resourceGroup,
            CancellationToken cancellationToken = default
        ) {
            try {
                for (var numOfChecks = 0; numOfChecks < NUM_OF_MAX_NAME_AVAILABILITY_CHECKS; ++numOfChecks) {
                    var signalRName = GenerateName();
                    var nameAvailable = await CheckNameAvailabilityAsync(
                            resourceGroup,
                            signalRName,
                            cancellationToken
                        );

                    if (nameAvailable) {
                        return signalRName;
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
                Log.Error(ex, "Failed to generate unique SignalR service name");
                throw;
            }

            var errorMessage = $"Failed to generate unique SignalR service name " +
                $"after {NUM_OF_MAX_NAME_AVAILABILITY_CHECKS} retries";
            
            Log.Error(errorMessage);
            throw new Exception(errorMessage);
        }

        public async Task<string> GetConnectionStringAsync(
            IResourceGroup resourceGroup,
            SignalRResource signalR,
            CancellationToken cancellationToken = default
        ) {
            try {
                var keys = await _signalRManagementClient
                    .SignalR
                    .ListKeysAsync(
                        resourceGroup.Name,
                        signalR.Name,
                        cancellationToken
                    );

                return keys.PrimaryConnectionString;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to retrieve connection string for SignalR Service: {signalR.Name}");
                throw;
            }
        }

        public void Dispose() {
            if (null != _signalRManagementClient) {
                _signalRManagementClient.Dispose();
            }
        }
    }
}
