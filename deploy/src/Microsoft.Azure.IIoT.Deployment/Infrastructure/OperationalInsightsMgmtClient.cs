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

    class OperationalInsightsMgmtClient : IDisposable
    {

        public const string DEFAULT_NAME_PREFIX = "workspace-";

        private readonly OperationalInsightsManagementClient _operationalInsightsManagementClient;

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

        public static string GenerateWorkspaceName(
            string prefix = DEFAULT_NAME_PREFIX,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(prefix, suffixLen);
        }

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

                    Sku = new Sku {
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

        public void Dispose() {
            if (null != _operationalInsightsManagementClient) {
                _operationalInsightsManagementClient.Dispose();
            }
        }
    }
}
