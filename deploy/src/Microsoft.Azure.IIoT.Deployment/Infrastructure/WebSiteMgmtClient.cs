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
                tags ??= new Dictionary<string, string>();

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
                tags ??= new Dictionary<string, string>();

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
                                // NOTE: This should be Public IP address exposed by Ingress.
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

                Log.Information($"Created Azure AppService: {azureWebsiteName}");

                return webSite;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to create Azure AppService: {azureWebsiteName}");
                throw;
            }
        }

        /// <summary>
        /// Get AppService by its name.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="azureWebsiteName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SiteInner> GetSiteAsync(
            IResourceGroup resourceGroup,
            string azureWebsiteName,
            CancellationToken cancellationToken = default
        ) {
            try {
                var webSite = await _webSiteManagementClient
                    .WebApps
                    .GetAsync(
                        resourceGroup.Name,
                        azureWebsiteName,
                        cancellationToken
                    );

                return webSite;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to get Azure AppService: {azureWebsiteName}");
                throw;
            }
        }

        /// <summary>
        /// Deploy lightweight reverse proxy to App Service.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="webSite"></param>
        /// <param name="tags"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SiteSourceControlInner> DeployProxyAsync(
            IResourceGroup resourceGroup,
            SiteInner webSite,
            IDictionary<string, string> tags = null,
            CancellationToken cancellationToken = default
        ) {
            try {
                tags ??= new Dictionary<string, string>();

                Log.Information($"Deploying proxy service to AppService: {webSite.Name} ...");

                var siteSourceControlDefinition = new SiteSourceControlInner() {
                    Location = resourceGroup.RegionName,
                    Tags = tags,

                    RepoUrl = PROXY_REPO_URL,
                    Branch = PROXY_BRANCH,
                    IsManualIntegration = true
                };

                siteSourceControlDefinition.Validate();

                var siteSourceControl = await _webSiteManagementClient
                    .WebApps
                    .CreateOrUpdateSourceControlAsync(
                        resourceGroup.Name,
                        webSite.Name,
                        siteSourceControlDefinition,
                        cancellationToken
                    );

                Log.Information($"Deployed proxy service to AppService: {webSite.Name}");

                return siteSourceControl;
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed deploy proxy service to AppService: {webSite.Name}");
                throw;
            }
        }

        /// <summary>
        /// Update application settings of App Service.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="webSite"></param>
        /// <param name="remoteEndpoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UpdateSiteApplicationSettingsAsync(
            IResourceGroup resourceGroup,
            SiteInner webSite,
            string remoteEndpoint,
            X509Certificate2 webAppX509Certificate,
            CancellationToken cancellationToken = default
        ) {
            try {
                var remoteEndpointSettings = new StringDictionaryInner {
                    Location = webSite.Location,

                    Properties = new Dictionary<string, string> {
                        { PROXY_ENV_REMOTE_ENDPOINT, remoteEndpoint },
                        { PROXY_ENV_REMOTE_ENDPOINT_SSL_THUMBPRINT, webAppX509Certificate.Thumbprint }
                    }
                };

                remoteEndpointSettings.Validate();

                // Note: UpdateApplicationSettingsAsync(...) replaces existing
                // settings with new ones.
                var applicationSettings = await _webSiteManagementClient
                    .WebApps
                    .UpdateApplicationSettingsAsync(
                        resourceGroup.Name,
                        webSite.Name,
                        remoteEndpointSettings,
                        cancellationToken
                    );
            }
            catch (Exception ex) {
                Log.Error(ex, $"Failed to update {PROXY_ENV_REMOTE_ENDPOINT} setting of Azure AppService: {webSite.Name}");
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
