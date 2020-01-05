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

    using Microsoft.Azure.Management.ApplicationInsights.Management;
    using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Serilog;

    class ApplicationInsightsMgmtClient : IDisposable
    {
        public const string DEFAULT_NAME_PREFIX = "appinsights-";

        public const string APPLICATION_INSIGHTS_COMPONENT_KIND = "web";
        public const string APPLICATION_INSIGHTS_COMPONENT_APPLICATION_TYPE = "web";

        private readonly ApplicationInsightsManagementClient _applicationInsightsManagementClient;
        public ApplicationInsightsMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            // We need to initialize new RestClient so that we
            // extract RootHttpHandler and DelegatingHandlers out of it.
            var applicationInsightsRestClient = RestClient
                .Configure()
                .WithEnvironment(restClient.Environment)
                .WithCredentials(restClient.Credentials)
                //.WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .Build();

            _applicationInsightsManagementClient = new ApplicationInsightsManagementClient(
                restClient.Credentials,
                applicationInsightsRestClient.RootHttpHandler,
                applicationInsightsRestClient.Handlers.ToArray()
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

        public async Task<ApplicationInsightsComponent> CreateApplicationInsightsComponentAsync(
            IResourceGroup resourceGroup,
            string applicationInsightsName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Creating Azure Application Insights Component: {applicationInsightsName} ...");

                var applicationInsightsComponentParameters = new ApplicationInsightsComponent() {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    Kind = APPLICATION_INSIGHTS_COMPONENT_KIND,
                    ApplicationType = APPLICATION_INSIGHTS_COMPONENT_APPLICATION_TYPE
                };

                applicationInsightsComponentParameters.Validate();

                var applicationInsightsComponent = await _applicationInsightsManagementClient
                    .Components
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        applicationInsightsName,
                        applicationInsightsComponentParameters,
                        cancellationToken
                    );

                var applicationInsightsComponentBillingFeaturesParameters = new ApplicationInsightsComponentBillingFeatures() {
                    CurrentBillingFeatures = new List<string> { "Basic" },

                    // ResetTime is get only, so not available through object initializers
                    DataVolumeCap = new ApplicationInsightsComponentDataVolumeCap(
                        cap: 100.0,
                        resetTime: 24,
                        warningThreshold: 90
                    )
                };

                var applicationInsightsComponentBillingFeatures = await _applicationInsightsManagementClient
                    .ComponentCurrentBillingFeatures
                    .UpdateAsync(
                        resourceGroup.Name,
                        applicationInsightsName,
                        applicationInsightsComponentBillingFeaturesParameters,
                        cancellationToken
                    );

                Log.Information($"Created Azure Application Insights Component: {applicationInsightsName}");

                return applicationInsightsComponent;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure Application Insights Component: {applicationInsightsName}");
                throw;
            }
        }

        public void Dispose() {
            if (null != _applicationInsightsManagementClient) {
                _applicationInsightsManagementClient.Dispose();
            }
        }
    }
}
