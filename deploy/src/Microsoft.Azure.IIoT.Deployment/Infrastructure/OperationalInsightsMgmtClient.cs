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

    using Microsoft.Azure.Management.OperationalInsights;
    using Microsoft.Azure.Management.OperationalInsights.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Serilog;

    /// <summary>
    /// Log Analytics workspaces management client.
    /// </summary>
    class OperationalInsightsMgmtClient : IDisposable
    {

        public const string DEFAULT_NAME_PREFIX = "workspace-";

        private readonly OperationalInsightsManagementClient _operationalInsightsManagementClient;

        /// <summary>
        /// Constructor of Log Analytics workspaces management client.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="restClient"></param>
        public OperationalInsightsMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            // We need to initialize new RestClient so that we
            // extract RootHttpHandler and DelegatingHandlers out of it.
            var operationalInsightsRestClient = RestClient
                .Configure()
                .WithEnvironment(restClient.Environment)
                .WithCredentials(restClient.Credentials)
                //.WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .Build();

            _operationalInsightsManagementClient = new OperationalInsightsManagementClient(
                restClient.Credentials,
                operationalInsightsRestClient.RootHttpHandler,
                operationalInsightsRestClient.Handlers.ToArray()
            ) {
                SubscriptionId = subscriptionId
            };
        }

        /// <summary>
        /// Generate Log Analytics workspaces name with given prefix and length of random suffix.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="suffixLen"></param>
        /// <returns></returns>
        public static string GenerateWorkspaceName(
            string prefix = DEFAULT_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

        /// <summary>
        /// Create Log Analytics workspaces.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="operationalInsightsWorkspaceName"></param>
        /// <param name="tags"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Workspace> CreateOperationalInsightsWorkspaceAsync(
            IResourceGroup resourceGroup,
            string operationalInsightsWorkspaceName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Creating Azure Operational Insights Workspace: {operationalInsightsWorkspaceName} ...");

                var workspaceParameters = new Workspace {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    Sku = new WorkspaceSku {
                        Name = "PerGB2018"
                    }
                };

                workspaceParameters.Validate();

                var operationalInsightsWorkspace = await _operationalInsightsManagementClient
                    .Workspaces
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        operationalInsightsWorkspaceName,
                        workspaceParameters,
                        cancellationToken
                    );

                Log.Information($"Created Azure Operational Insights Workspace: {operationalInsightsWorkspaceName}");

                return operationalInsightsWorkspace;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure Operational Insights Workspace: {operationalInsightsWorkspaceName}");
                throw;
            }
        }

        /// <summary>
        /// Get shared keys for Log Analytics workspaces.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="workspace"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SharedKeys> GetSharedKeysAsync(
            IResourceGroup resourceGroup,
            Workspace workspace,
            CancellationToken cancellationToken = default
        ) {
            var sharedKeys = await _operationalInsightsManagementClient.SharedKeys
                .GetSharedKeysAsync(
                    resourceGroup.Name,
                    workspace.Name,
                    cancellationToken
                );

            return sharedKeys;
        }

        public void Dispose() {
            if (null != _operationalInsightsManagementClient) {
                _operationalInsightsManagementClient.Dispose();
            }
        }
    }
}
