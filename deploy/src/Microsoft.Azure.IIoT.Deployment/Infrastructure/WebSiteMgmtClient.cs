// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.AppService.Fluent;
    using Microsoft.Azure.Management.AppService.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Serilog;

    class WebSiteMgmtClient : IDisposable {

        public const string PROXY_REPO_URL = "https://github.com/Azure/reverse-proxy-dotnet.git";
        public const string PROXY_BRANCH = "master";

        // Environment variables for proxy application.
        public const string PROXY_ENV_REMOTE_ENDPOINT = "REMOTE_ENDPOINT";
        public const string PROXY_ENV_REMOTE_ENDPOINT_SSL_THUMBPRINT = "REMOTE_ENDPOINT_SSL_THUMBPRINT";

        private readonly WebSiteManagementClient _webSiteManagementClient;

        public WebSiteMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            _webSiteManagementClient = new WebSiteManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };
        }

        public static string GenerateAppServicePlanName(
            string applicationName,
            int suffixLen = 5
        ) {
            return SdkContext.RandomResourceName(applicationName + "-", suffixLen);
        }

        public async Task<AppServicePlanInner> CreateAppServicePlanAsync(
            IResourceGroup resourceGroup,
            string appServicePlanName,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags = tags ?? new Dictionary<string, string>();

                Log.Information($"Creating Azure AppService Plan: {appServicePlanName} ...");

                var appServicePlanParameters = new AppServicePlanInner {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    Sku = new SkuDescription {
                        Name = "S1",
                        Capacity = 0
                    }
                };

                appServicePlanParameters.Validate();

                var appServicePlan = await _webSiteManagementClient
                    .AppServicePlans
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        appServicePlanName,
                        appServicePlanParameters,
                        cancellationToken
                    );

                Log.Information($"Created Azure AppService Plan: {appServicePlanName}");

                return appServicePlan;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure AppService Plan: {appServicePlanName}");
                throw;
            }
        }

        public async Task<SiteInner> CreateSiteAsync(
            IResourceGroup resourceGroup,
            AppServicePlanInner appServicePlan,
            string azureWebsiteName,
            string remoteEndpoint,
            X509Certificate2 webAppX509Certificate,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags = tags ?? new Dictionary<string, string>();

                Log.Information($"Creating Azure AppService: {azureWebsiteName} ...");

                var webSiteParameters = new SiteInner {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    Enabled = true,
                    HttpsOnly = true, // Will redirect HTTP traffic to HTTPS.
                    ClientAffinityEnabled = false,
                    ServerFarmId = appServicePlan.Id,
                    SiteConfig = new SiteConfig {
                        AppSettings = new List<NameValuePair> {
                            new NameValuePair{
                                Name = PROXY_ENV_REMOTE_ENDPOINT,
                                // NOTE: This should be PublicIP address exposed by Ingress.
                                Value = remoteEndpoint
                            },
                            new NameValuePair{
                                Name = PROXY_ENV_REMOTE_ENDPOINT_SSL_THUMBPRINT,
                                // NOTE: this certificate should be added to Ingress as default certificate.
                                Value = webAppX509Certificate.Thumbprint
                            }
                        },

                        // Coming from Microsoft.Web/sites/config resource
                        NumberOfWorkers = 1,
                        RequestTracingEnabled = true,
                        HttpLoggingEnabled = true,
                        DetailedErrorLoggingEnabled = true,
                        AlwaysOn = true,
                        MinTlsVersion = SupportedTlsVersions.OneFullStopTwo
                    }
                };

                webSiteParameters.Validate();

                var webSite = await _webSiteManagementClient
                    .WebApps
                    .CreateOrUpdateAsync(
                        resourceGroup.Name,
                        azureWebsiteName,
                        webSiteParameters,
                        cancellationToken
                    );

                var siteSourceControlRequest = new SiteSourceControlInner() {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    RepoUrl = PROXY_REPO_URL,
                    Branch = PROXY_BRANCH,
                    IsManualIntegration = true
                };

                siteSourceControlRequest.Validate();

                var siteSourceControl = await _webSiteManagementClient
                    .WebApps
                    .CreateOrUpdateSourceControlAsync(
                        resourceGroup.Name,
                        azureWebsiteName,
                        siteSourceControlRequest,
                        cancellationToken
                    );

                webSite = await _webSiteManagementClient
                    .WebApps
                    .GetAsync(
                        resourceGroup.Name,
                        azureWebsiteName,
                        cancellationToken
                    );

                Log.Information($"Created Azure AppService: {azureWebsiteName}");

                return webSite;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure AppService: {azureWebsiteName}");
                throw;
            }
        }

        public void Dispose() {
            if (null != _webSiteManagementClient) {
                _webSiteManagementClient.Dispose();
            }
        }
    }
}
