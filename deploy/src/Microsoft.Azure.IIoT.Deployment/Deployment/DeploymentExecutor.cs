// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Deployment {

    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    using Authentication;
    using Infrastructure;
    using Configuration;

    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.KeyVault.Fluent.Models;
    using Microsoft.Azure.Management.Storage.Fluent.Models;
    using Microsoft.Azure.Management.IotHub.Models;
    using Microsoft.Azure.Management.EventHub.Fluent.Models;
    using Microsoft.Azure.Management.Network.Fluent.Models;
    using Microsoft.Graph;

    class DeploymentExecutor : IDisposable {

        public const string ENV_FILE_PATH = @".env";

        private List<string> _defaultTagsList;
        private Dictionary<string, string> _defaultTagsDict;

        private readonly IConfigurationProvider _configurationProvider;

        private AuthenticationConfiguration _authConf;
        private ISubscription _subscription;
        private string _applicationName;
        private string _applicationURL;
        private IResourceGroup _resourceGroup;

        private IAuthenticationManager _authenticationManager;
        private AzureResourceManager _azureResourceManager;

        // Resource management clients
        private RestClient _restClient;

        private ApplicationsManager _applicationsManager;
        private KeyVaultMgmtClient _keyVaultManagementClient;
        private StorageMgmtClient _storageManagementClient;
        private IotHubMgmtClient _iotHubManagementClient;
        private CosmosDBMgmtClient _cosmosDBManagementClient;
        private ServiceBusMgmtClient _serviceBusManagementClient;
        private EventHubMgmtClient _eventHubManagementClient;
        private OperationalInsightsMgmtClient _operationalInsightsManagementClient;
        private ApplicationInsightsMgmtClient _applicationInsightsManagementClient;
        private WebSiteMgmtClient _webSiteManagementClient;
        private NetworkMgmtClient _networkManagementClient;
        private AuthorizationMgmtClient _authorizationManagementClient;
        private AksMgmtClient _aksManagementClient;
        private SignalRMgmtClient _signalRManagementClient;

        // Resource names
        private string _keyVaultName;
        private string _storageAccountGen1Name;
        private string _storageAccountGen2Name;
        private string _iotHubName;
        private string _cosmosDBAccountName;
        private string _serviceBusNamespaceName;
        private string _eventHubNamespaceName;
        private string _eventHubName;
        private string _operationalInsightsWorkspaceName;
        private string _applicationInsightsName;
        private string _appServicePlanName;
        private string _azureWebsiteName;
        private string _networkSecurityGroupName;
        //private string _routTableName;
        private string _virtualNetworkName;
        //private string _networkInterfaceName;
        //private string _publicIPAddressName;
        //private string _domainNameLabel;
        private string _aksClusterName;
        private string _signalRName;

        // Resources
        private DirectoryObject _owner;
        private bool _ownedApplications = false;

        private const string kWEB_APP_CN = "webapp.services.net"; // ToDo: Assign meaningfull value.
        private X509Certificate2 _webAppX509Certificate;

        private const string kAKS_CLUSTER_CN = "aks.cluster.net"; // ToDo: Assign meaningfull value.
        private X509Certificate2 _aksClusterX509Certificate;

        private KeyBundle _dataprotectionKey;

        public DeploymentExecutor(
            IConfigurationProvider configurationProvider
        ) {
            _configurationProvider = configurationProvider;
        }

        public async Task RunAsync(
            CancellationToken cancellationToken = default
        ) {
            var runMode = _configurationProvider.GetRunMode();

            switch (runMode) {
                case RunMode.Full:
                    await RunFullDeploymentAsync(cancellationToken);
                    break;
                case RunMode.ApplicationRegistration:
                    await RunApplicationRegistrationOnlyAsync(cancellationToken);
                    break;
                case RunMode.ResourceDeployment:
                    await RunResourceDeploymentOnlyAsync(cancellationToken);
                    break;
                default:
                    throw new Exception($"Unknown RunMode: {runMode}");
            }
        }

        protected async Task RunFullDeploymentAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Information("Starting full deployment of Industrial IoT solution.");

                await AuthenticateAsync(cancellationToken);
                await GetSubscriptionAsync(cancellationToken);
                GetApplicationName();

                InitializeResourceManagementClients();

                await InitializeResourceGroupSelectionAsync(cancellationToken);
                await RegisterResourceProvidersAsync(cancellationToken);
                await SetupApplicationsAsync(cancellationToken);
                await GenerateAzureResourceNamesAsync(cancellationToken);
                await CreateAzureResourcesAsync(cancellationToken);
                await UpdateClientApplicationRedirectUrisAsync(cancellationToken);

                Log.Information("Done.");
            }
            catch (Exception ex) {
                Log.Error(ex, "Failed to deploy Industrial IoT solution.");

                if (_configurationProvider.IfPerformCleanup()) {
                    await BeginDeleteResourceGroupAsync(cancellationToken);
                    await DeleteApplicationsAsync(cancellationToken);
                }

                throw;
            }
        }

        protected async Task RunApplicationRegistrationOnlyAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Information("Starting application registration for Industrial IoT solution.");

                await AuthenticateAsync(cancellationToken);
                await GetSubscriptionAsync(cancellationToken);
                GetApplicationName();

                InitializeResourceManagementClients();

                await SetupApplicationsAsync(cancellationToken);
                await UpdateClientApplicationRedirectUrisAsync(cancellationToken);
                OutputApplicationRegistrationDefinition();

                Log.Information("Done.");
            }
            catch (Exception ex) {
                Log.Error(ex, "Failed to register applications for Industrial IoT solution.");

                if (_configurationProvider.IfPerformCleanup()) {
                    await DeleteApplicationsAsync(cancellationToken);
                }

                throw;
            }
        }

        protected async Task RunResourceDeploymentOnlyAsync(
            CancellationToken cancellationToken = default
        ) {
            try {
                Log.Information("Starting resource deployment of Industrial IoT solution.");

                await AuthenticateAsync(cancellationToken);
                await GetSubscriptionAsync(cancellationToken);
                GetApplicationName();

                InitializeResourceManagementClients();

                await InitializeResourceGroupSelectionAsync(cancellationToken);
                await RegisterResourceProvidersAsync(cancellationToken);
                await SetupApplicationsAsync(cancellationToken);
                await GenerateAzureResourceNamesAsync(cancellationToken);
                await CreateAzureResourcesAsync(cancellationToken);

                Log.Information("Done.");
            }
            catch (Exception ex) {
                Log.Error(ex, "Failed to deploy resources of Industrial IoT solution.");

                if (_configurationProvider.IfPerformCleanup()) {
                    await BeginDeleteResourceGroupAsync(cancellationToken);
                }

                throw;
            }
        }

        protected async Task AuthenticateAsync(
            CancellationToken cancellationToken = default
        ) {
            // ToDo: Figure out how to sign-in without TenantId.
            _authConf = _configurationProvider
                .GetAuthenticationConfiguration(
                    AzureEnvironment.KnownEnvironments
                );

            _authenticationManager = AuthenticationManagerFactory
                .GetAuthenticationManager(
                    _authConf.AzureEnvironment,
                    _authConf.TenantId,
                    _authConf.ClientId,
                    _authConf.ClientSecret
                );

            await _authenticationManager
                .AuthenticateAsync(cancellationToken);
        }

        /// <summary>
        /// Initialize default tags (list and dictionary) that will be added to
        /// resources created by the application.
        /// </summary>
        /// <param name="owner"></param>
        protected void InitializeDefaultTags(string owner = null) {
            _defaultTagsList = new List<string> {
                Resources.IIoTDeploymentTags.VALUE_APPLICATION_IIOT,
                Resources.IIoTDeploymentTags.VALUE_VERSION_IIOT,
                Resources.IIoTDeploymentTags.VALUE_MANAGED_BY_IIOT
            };

            _defaultTagsDict = new Dictionary<string, string> {
                { Resources.IIoTDeploymentTags.KEY_APPLICATION, Resources.IIoTDeploymentTags.VALUE_APPLICATION_IIOT },
                { Resources.IIoTDeploymentTags.KEY_VERSION, Resources.IIoTDeploymentTags.VALUE_VERSION_IIOT},
                { Resources.IIoTDeploymentTags.KEY_MANAGED_BY, Resources.IIoTDeploymentTags.VALUE_MANAGED_BY_IIOT}
            };

            if (null != owner) {
                _defaultTagsList.Add(owner);
                _defaultTagsDict.Add(Resources.IIoTDeploymentTags.KEY_OWNER, owner);
            }
        }

        protected void GetApplicationName() {
            _applicationName = _configurationProvider.GetApplicationName();
        }

        protected async Task SetOwnerAsync(
            CancellationToken cancellationToken = default
        ) {
            // Initialization of MicrosoftGraphServiceClient
            var microsoftGraphTokenCredentials = _authenticationManager
                .GetMicrosoftGraphDelegatingTokenCredentials();

            var msGraphServiceClient = new MicrosoftGraphServiceClient(
                microsoftGraphTokenCredentials,
                cancellationToken
            );

            if (_authenticationManager.IsUserAuthenticationFlow()) {
                // If this is user authentication flow then authenticated user
                // will be used as owner of the deployment.
                var me = await msGraphServiceClient
                    .GetMeAsync(cancellationToken);

                _owner = me;

                if (null != me.Mail) {
                    InitializeDefaultTags(me.Mail);
                }
                else {
                    var account = _authenticationManager.GetAccount();
                    InitializeDefaultTags(account?.Username);
                }
            }
            else {
                // If this is not user authentication flow then service principal
                // of the application will be used as owner of the deployment.
                var ownerSP = await msGraphServiceClient
                    .GetServicePrincipalByAppIdAsync(
                        _authConf.ClientId.ToString(),
                        cancellationToken
                    );

                _owner = ownerSP;

                InitializeDefaultTags(ownerSP.DisplayName);
            }
        }

        protected async Task GetSubscriptionAsync(
            CancellationToken cancellationToken = default
        ) {
            // Initialization of MicrosoftGraphServiceClient
            var microsoftGraphTokenCredentials = _authenticationManager
                .GetMicrosoftGraphDelegatingTokenCredentials();

            _applicationsManager = new ApplicationsManager(
                _authConf.TenantId,
                microsoftGraphTokenCredentials,
                cancellationToken
            );

            await SetOwnerAsync(cancellationToken);

            // Initialization of AzureResourceManager
            var azureCredentials = _authenticationManager
                .GetDelegatingAzureCredentials();

            _azureResourceManager = new AzureResourceManager(azureCredentials);

            // Select subscription to use.
            var subscriptionsList = _azureResourceManager.GetSubscriptions();
            _subscription = _configurationProvider.GetSubscription(subscriptionsList);
            _azureResourceManager.Init(_subscription);
        }

        protected async Task InitializeResourceGroupSelectionAsync(
            CancellationToken cancellationToken = default
        ) {
            // Select existing ResourceGroup or create a new one.
            if (_configurationProvider.IfUseExistingResourceGroup()) {
                var resourceGroups = _azureResourceManager.GetResourceGroups();
                _resourceGroup = _configurationProvider.GetExistingResourceGroup(resourceGroups);
            }
            else {
                bool ifResourceGroupExists(string _resourceGroupName) {
                    var _resourceGroupExists = _azureResourceManager
                        .CheckIfResourceGroupExistsAsync(
                            _resourceGroupName,
                            cancellationToken
                        )
                        .Result;

                    return _resourceGroupExists;
                }

                var defaultResourceGroupName =
                    ifResourceGroupExists(_applicationName) ? null : _applicationName;

                var newRGParams = _configurationProvider.GetNewResourceGroup(
                        AzureResourceManager.FunctionalRegions,
                        ifResourceGroupExists,
                        defaultResourceGroupName
                    );

                _resourceGroup = await _azureResourceManager
                    .CreateResourceGroupAsync(
                        newRGParams.Item1,
                        newRGParams.Item2,
                        _defaultTagsDict,
                        cancellationToken
                    );
            }
        }

        protected async Task BeginDeleteResourceGroupAsync(
            CancellationToken cancellationToken = default
        ) {
            if (null != _resourceGroup) {
                try {
                    await _azureResourceManager
                        .BeginDeleteResourceGroupAsync(
                            _resourceGroup,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning($"Ignoring failure to delete Resource Group: {_resourceGroup.Name}.");
                }
            }

        }

        protected void InitializeResourceManagementClients() {
            var azureCredentials = _authenticationManager
                .GetDelegatingAzureCredentials();

            // Create generic RestClient for services
            _restClient = RestClient
                .Configure()
                .WithEnvironment(_authConf.AzureEnvironment)
                .WithCredentials(azureCredentials)
                //.WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .Build();

            var subscriptionId = _subscription.SubscriptionId;

            _keyVaultManagementClient = new KeyVaultMgmtClient(subscriptionId, _restClient);
            _storageManagementClient = new StorageMgmtClient(subscriptionId, _restClient);
            _iotHubManagementClient = new IotHubMgmtClient(subscriptionId, _restClient);
            _cosmosDBManagementClient = new CosmosDBMgmtClient(subscriptionId, _restClient);
            _serviceBusManagementClient = new ServiceBusMgmtClient(subscriptionId, _restClient);
            _eventHubManagementClient = new EventHubMgmtClient(subscriptionId, _restClient);
            _operationalInsightsManagementClient = new OperationalInsightsMgmtClient(subscriptionId, _restClient);
            _applicationInsightsManagementClient = new ApplicationInsightsMgmtClient(subscriptionId, _restClient);
            _webSiteManagementClient = new WebSiteMgmtClient(subscriptionId, _restClient);
            _networkManagementClient = new NetworkMgmtClient(subscriptionId, _restClient);
            _authorizationManagementClient = new AuthorizationMgmtClient(subscriptionId, _restClient);
            _aksManagementClient = new AksMgmtClient(subscriptionId, _restClient);
            _signalRManagementClient = new SignalRMgmtClient(subscriptionId, _restClient);
        }

        protected async Task RegisterResourceProvidersAsync(
            CancellationToken cancellationToken = default
        ) {
            using var resourceMgmtClient = new ResourceMgmtClient(_subscription.SubscriptionId, _restClient);
            await resourceMgmtClient.RegisterRequiredResourceProvidersAsync(cancellationToken);
        }

        protected async Task SetupApplicationsAsync(
            CancellationToken cancellationToken = default
        ) {
            var runMode = _configurationProvider.GetRunMode();

            var applicationRegistrationDefinition = _configurationProvider
                .GetApplicationRegistrationDefinition();

            if (RunMode.Full == runMode
                || RunMode.ApplicationRegistration == runMode
            ) {
                if (null != applicationRegistrationDefinition) {
                    // Load definitions of existing applications.
                    _applicationsManager.Load(applicationRegistrationDefinition);
                }
                else {
                    // Create new applications.
                    await _applicationsManager
                        .RegisterApplicationsAsync(
                            _applicationName,
                            _owner,
                            _defaultTagsList,
                            cancellationToken
                        );

                    // Remeber that we have registered applications.
                    // Required for UpdateClientApplicationRedirectUrisAsync().
                    _ownedApplications = true;

                    // Assign Service Principal of AKS Application
                    // "Network Contributor" IAM role for Subscription.
                    await _authorizationManagementClient
                        .AssignNetworkContributorRoleForSubscriptionAsync(
                            _applicationsManager.GetAKSApplicationSP(),
                            cancellationToken
                        );

                    // ToDo: Remove role assignment after telemetryCdmProcessor uses connection string.
                    // Assign Service Principal of Service Application
                    // "Storage Blob Data Contributor" IAM role for Subscription.
                    await _authorizationManagementClient
                        .AssignStorageBlobDataContributorRoleForSubscriptionAsync(
                            _applicationsManager.GetServiceApplicationSP(),
                            cancellationToken
                        );
                }
            }
            else if (RunMode.ResourceDeployment == runMode) {
                if (null != applicationRegistrationDefinition) {
                    // Load definitions of existing applications.
                    _applicationsManager.Load(applicationRegistrationDefinition);
                }
                else {
                    // In ResourceDeployment mode, we assume that the application
                    // does not have enouh permissions to create new Applications
                    // and Service Principals. So ApplicationRegistration must be configured.
                    throw new Exception("ApplicationRegistration must be configured.");
                }
            }
        }

        protected async Task UpdateClientApplicationRedirectUrisAsync(
            CancellationToken cancellationToken = default
        ) {
            // Check whether applications have been created or loaded.
            if (!_ownedApplications) {
                Log.Information("Client application definition has been loaded through " +
                    "configuration, so update of RedirectUris will not be performed.");
                return;
            }

            var runMode = _configurationProvider.GetRunMode();

            if (RunMode.ApplicationRegistration == runMode) {
                _applicationURL = _configurationProvider.GetApplicationURL();

                if (!string.IsNullOrEmpty(_applicationURL)) {
                    await _applicationsManager
                        .UpdateClientApplicationRedirectUrisAsync(
                            _applicationURL,
                            cancellationToken
                        );
                }
                else {
                    Log.Information("Client application redirectUris will not " +
                        "be configured since ApplicationURL is not provided.");
                }
            }
            else if (RunMode.Full == runMode) {
                // _applicationURL will be set up by CreateAzureResourcesAsync() call;
                await _applicationsManager
                    .UpdateClientApplicationRedirectUrisAsync(
                        _applicationURL,
                        cancellationToken
                    );
            } else {
                // RunMode.ResourceDeployment == runMode
                // In this mode we assum that the deployment application does not have
                // enough permissions to change redirectUris of registered application.
                throw new Exception($"UpdateClientApplicationRedirectUrisAsync() method " +
                    $"cannot be called in {RunMode.ResourceDeployment} run mode.");
            }
        }

        protected async Task DeleteApplicationsAsync(
            CancellationToken cancellationToken = default
        ) {
            await _applicationsManager.DeleteApplicationsAsync(cancellationToken);
        }

        protected void OutputApplicationRegistrationDefinition() {
            var appRegDef = new ApplicationRegistrationDefinitionSettings(
                new ApplicationSettings(_applicationsManager.GetServiceApplication()),
                new ServicePrincipalSettings(_applicationsManager.GetServiceApplicationSP()),
                new ApplicationSettings(_applicationsManager.GetClientApplication()),
                new ServicePrincipalSettings(_applicationsManager.GetClientApplicationSP()),
                new ApplicationSettings(_applicationsManager.GetAKSApplication()),
                new ServicePrincipalSettings(_applicationsManager.GetAKSApplicationSP()),
                _applicationsManager.GetAKSApplicationRbacSecret()
            );

            var jsonString = JsonConvert
                .SerializeObject(
                    appRegDef,
                    Formatting.Indented,
                    new JsonConverter[] { new StringEnumConverter() }
                );

            Log.Information("Use details bellow as ApplicationRegistration " +
                "for resource deployment of Industrial IoT solution.");
            Console.WriteLine(jsonString);
        }

        protected async Task GenerateAzureResourceNamesAsync(
            CancellationToken cancellationToken = default
        ) {
            // It might happen that there is no registered resource provider
            // found for specified location and/or api version to perform name
            // availability check. In these cases, we will silently ignore
            // errors and just use generated random names.
            const string notAvailableApiFormat = "Name availability APIs are not available for '{0}'";

            // KeyVault names
            try {
                _keyVaultName = await _keyVaultManagementClient
                    .GenerateAvailableNameAsync(cancellationToken);
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                Log.Warning(notAvailableApiFormat, "KeyVault");
                _keyVaultName = KeyVaultMgmtClient.GenerateName();
            }

            // Storage Account names
            try {
                _storageAccountGen1Name = await _storageManagementClient
                    .GenerateAvailableNameAsync(cancellationToken);
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                Log.Warning(notAvailableApiFormat, "Storage Account");
                _storageAccountGen1Name = StorageMgmtClient.GenerateStorageAccountName();
            }

            // Storage Account Gen2 name
            try {
                _storageAccountGen2Name = await _storageManagementClient
                    .GenerateAvailableNameAsync(cancellationToken);
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                Log.Warning(notAvailableApiFormat, "Storage Account");
                _storageAccountGen2Name = StorageMgmtClient.GenerateStorageAccountName();
            }

            // IoT hub names
            try {
                _iotHubName = await _iotHubManagementClient
                    .GenerateAvailableNameAsync(cancellationToken);
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                Log.Warning(notAvailableApiFormat, "IoT Hub");
                _iotHubName = IotHubMgmtClient.GenerateIotHubName();
            }

            // CosmosDB names
            try {
                _cosmosDBAccountName = await _cosmosDBManagementClient
                    .GenerateAvailableNameAsync(cancellationToken);
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                Log.Warning(notAvailableApiFormat, "CosmosDB Account");
                _cosmosDBAccountName = CosmosDBMgmtClient.GenerateCosmosDBAccountName();
            }

            // Service Bus Namespace names
            try {
                _serviceBusNamespaceName = await _serviceBusManagementClient
                    .GenerateAvailableNamespaceNameAsync(cancellationToken);
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                Log.Warning(notAvailableApiFormat, "ServiceBus Namespace");
                _serviceBusNamespaceName = ServiceBusMgmtClient.GenerateNamespaceName();
            }

            // Event Hub Namespace names
            try {
                _eventHubNamespaceName = await _eventHubManagementClient
                    .GenerateAvailableNamespaceNameAsync(cancellationToken);
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                Log.Warning(notAvailableApiFormat, "EventHub Namespace");
                _eventHubNamespaceName = EventHubMgmtClient.GenerateEventHubNamespaceName();
            }

            _eventHubName = EventHubMgmtClient.GenerateEventHubName();

            // Operational Insights workspace name.
            _operationalInsightsWorkspaceName = OperationalInsightsMgmtClient.GenerateWorkspaceName();

            // Application Insights name.
            _applicationInsightsName = ApplicationInsightsMgmtClient.GenerateName();

            // AppService Plan name
            _appServicePlanName = WebSiteMgmtClient.GenerateAppServicePlanName(_applicationName);
            _azureWebsiteName = _applicationName;

            // Networking names
            _networkSecurityGroupName = NetworkMgmtClient.GenerateNetworkSecurityGroupName();
            //_routTableName = NetworkMgmtClient.GenerateRoutTableName();
            _virtualNetworkName = NetworkMgmtClient.GenerateVirtualNetworkName();
            //_networkInterfaceName = NetworkMgmtClient.GenerateNetworkInterfaceName();
            //_publicIPAddressName = NetworkMgmtClient.GeneratePublicIPAddressName();
            //_domainNameLabel = SdkContext.RandomResourceName(_applicationName + "-", 5);

            // AKS cluster name
            _aksClusterName = AksMgmtClient.GenerateName();

            // SignalR name
            try {
                _signalRName = await _signalRManagementClient
                    .GenerateAvailableNameAsync(_resourceGroup, cancellationToken);
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                Log.Warning(notAvailableApiFormat, "SignalR");
                _signalRName = SignalRMgmtClient.GenerateName();
            }
        }

        protected async Task CreateAzureResourcesAsync(
            CancellationToken cancellationToken = default
        ) {
            // Create Virtual Network
            NetworkSecurityGroupInner networkSecurityGroup;
            //RouteTableInner routeTable;
            VirtualNetworkInner virtualNetwork;
            SubnetInner virtualNetworkAksSubnet;
            //PublicIPAddressInner publicIPAddress;
            //NetworkInterfaceInner networkInterface;

            networkSecurityGroup = await _networkManagementClient
                .CreateNetworkSecurityGroupAsync(
                    _resourceGroup,
                    _networkSecurityGroupName,
                    _defaultTagsDict,
                    cancellationToken
                );

            //routeTable = _networkManagementClient
            //    .CreateRouteTableAsync(
            //        _resourceGroup,
            //        _routTableName,
            //        networkInterfacePrivateIPAddress,
            //        _defaultTagsDict,
            //        cancellationToken    
            //    ).Result;

            virtualNetwork = await _networkManagementClient
                .CreateVirtualNetworkAsync(
                    _resourceGroup,
                    networkSecurityGroup,
                    _virtualNetworkName,
                    null,
                    _defaultTagsDict,
                    cancellationToken
                );

            virtualNetworkAksSubnet = _networkManagementClient.GetAksSubnet(virtualNetwork);

            //publicIPAddress = _networkManagementClient
            //    .CreatePublicIPAddressAsync(
            //        _resourceGroup,
            //        _publicIPAddressName,
            //        _domainNameLabel,
            //        _defaultTagsDict,
            //        cancellationToken
            //    )
            //    .Result;

            //networkInterface = _networkManagementClient
            //    .CreateNetworkInterfaceAsync(
            //        _resourceGroup,
            //        networkSecurityGroup,
            //        virtualNetworkAksSubnet,
            //        _networkInterfaceName,
            //        networkInterfacePrivateIPAddress,
            //        _defaultTagsDict,
            //        cancellationToken
            //    )
            //    .Result;

            // Create Azure KeyVault
            VaultInner keyVault;

            var keyVaultParameters = _keyVaultManagementClient
                .GetCreationParameters(
                    _authConf.TenantId,
                    _resourceGroup,
                    _applicationsManager.GetServiceApplicationSP(),
                    _owner
                );

            keyVault = await _keyVaultManagementClient
                .CreateAsync(
                    _resourceGroup,
                    _keyVaultName,
                    keyVaultParameters,
                    cancellationToken
                );

            // Add certificates to KeyVault
            var keyVaultAuthenticationCallback = new IIoTKeyVaultClient.AuthenticationCallback(
                async (authority, resource, scope) => {
                    // Fetch AccessToken from cache.
                    var authenticationResult = await _authenticationManager
                        .AcquireKeyVaultAuthenticationResultAsync(cancellationToken);

                    return authenticationResult.AccessToken;
                }
            );

            using (var iiotKeyVaultClient = new IIoTKeyVaultClient(keyVaultAuthenticationCallback, keyVault)) {
                await iiotKeyVaultClient.CreateCertificateAsync(
                    IIoTKeyVaultClient.WEB_APP_CERT_NAME,
                    kWEB_APP_CN,
                    _defaultTagsDict,
                    cancellationToken
                );

                _webAppX509Certificate = await iiotKeyVaultClient.GetSecretAsync(
                    IIoTKeyVaultClient.WEB_APP_CERT_NAME,
                    cancellationToken
                );

                await iiotKeyVaultClient.CreateCertificateAsync(
                    IIoTKeyVaultClient.AKS_CLUSTER_CERT_NAME,
                    kAKS_CLUSTER_CN,
                    _defaultTagsDict,
                    cancellationToken
                );

                _aksClusterX509Certificate = await iiotKeyVaultClient.GetCertificateAsync(
                    IIoTKeyVaultClient.AKS_CLUSTER_CERT_NAME,
                    cancellationToken
                );

                // Create the key that will be used for dataprotection feature.
                _dataprotectionKey = await iiotKeyVaultClient.CreateDataprotectionKeyAsync(
                    IIoTKeyVaultClient.DATAPROTECTION_KEY_NAME,
                    _defaultTagsDict,
                    cancellationToken
                );
            }

            // Create Operational Insights workspace.
            var operationalInsightsWorkspaceCreationTask = _operationalInsightsManagementClient
                .CreateOperationalInsightsWorkspaceAsync(
                    _resourceGroup,
                    _operationalInsightsWorkspaceName,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create Application Insights components.
            var applicationInsightsComponentCreationTask = _applicationInsightsManagementClient
                .CreateApplicationInsightsComponentAsync(
                    _resourceGroup,
                    _applicationInsightsName,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create AKS cluster
            var operationalInsightsWorkspace = operationalInsightsWorkspaceCreationTask.Result;

            var clusterDefinition = _aksManagementClient.GetClusterDefinition(
                _resourceGroup,
                _applicationsManager.GetAKSApplication(),
                _applicationsManager.GetAKSApplicationRbacSecret(),
                _aksClusterName,
                _aksClusterX509Certificate,
                virtualNetworkAksSubnet,
                operationalInsightsWorkspace,
                _defaultTagsDict
            );

            var aksClusterCreationTask = _aksManagementClient
                .CreateClusterAsync(
                    _resourceGroup,
                    _aksClusterName,
                    clusterDefinition,
                    cancellationToken
                );

            // Create Storage Account
            StorageAccountInner storageAccountGen1;
            string storageAccountGen1ConectionString;
            BlobContainerInner iotHubBlobContainer;
            BlobContainerInner dataprotectionBlobContainer;

            storageAccountGen1 = await _storageManagementClient
                .CreateStorageAccountGen1Async(
                    _resourceGroup,
                    _storageAccountGen1Name,
                    _defaultTagsDict,
                    cancellationToken
                );

            storageAccountGen1ConectionString = await _storageManagementClient
                .GetStorageAccountConectionStringAsync(
                    _resourceGroup,
                    storageAccountGen1,
                    cancellationToken
                );

            // Create Blob container for IoT Hub storage.
            iotHubBlobContainer= await _storageManagementClient
                .CreateBlobContainerAsync(
                    _resourceGroup,
                    storageAccountGen1,
                    StorageMgmtClient.STORAGE_ACCOUNT_IOT_HUB_CONTAINER_NAME,
                    PublicAccess.None,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create Blob container for dataprotection feature.
            dataprotectionBlobContainer = await _storageManagementClient
                .CreateBlobContainerAsync(
                    _resourceGroup,
                    storageAccountGen1,
                    StorageMgmtClient.STORAGE_ACCOUNT_DATAPROTECTION_CONTAINER_NAME,
                    PublicAccess.None,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create Storage Account Gen2
            StorageAccountInner storageAccountGen2;
            BlobContainerInner powerbiContainer;

            storageAccountGen2 = await _storageManagementClient
                .CreateStorageAccountGen2Async(
                    _resourceGroup,
                    _storageAccountGen2Name,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create Blob container for IoT Hub storage.
            powerbiContainer = await _storageManagementClient
                .CreateBlobContainerAsync(
                    _resourceGroup,
                    storageAccountGen2,
                    StorageMgmtClient.STORAGE_ACCOUNT_POWERBI_CONTAINER_NAME,
                    PublicAccess.None,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create IoT Hub
            IotHubDescription iotHub;

            iotHub = await _iotHubManagementClient
                .CreateIotHubAsync(
                    _resourceGroup,
                    _iotHubName,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_PARTITIONS_COUNT,
                    storageAccountGen1ConectionString,
                    StorageMgmtClient.STORAGE_ACCOUNT_IOT_HUB_CONTAINER_NAME,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create "events" consumer group.
            await _iotHubManagementClient
                .CreateEventHubConsumerGroupAsync(
                    _resourceGroup,
                    iotHub,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_EVENTS_ENDPOINT_NAME,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_EVENTS_CONSUMER_GROUP_NAME,
                    cancellationToken
                );

            // Create "telemetry" consumer group.
            await _iotHubManagementClient
                .CreateEventHubConsumerGroupAsync(
                    _resourceGroup,
                    iotHub,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_EVENTS_ENDPOINT_NAME,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_TELEMETRY_CONSUMER_GROUP_NAME,
                    cancellationToken
                );

            // Create CosmosDB account
            var cosmosDBAccountCreationTask = _cosmosDBManagementClient
                .CreateDatabaseAccountAsync(
                    _resourceGroup,
                    _cosmosDBAccountName,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create Service Bus Namespace
            var serviceBusNamespaceCreationTask = _serviceBusManagementClient
                .CreateServiceBusNamespaceAsync(
                    _resourceGroup,
                    _serviceBusNamespaceName,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create Azure Event Hub Namespace and Azure Event Hub
            EHNamespaceInner eventHubNamespace;
            EventhubInner eventHub;
            ConsumerGroupInner telemetryCdm;
            ConsumerGroupInner telemetryUx;

            // Create Azure Event Hub Namespace
            eventHubNamespace = await _eventHubManagementClient
                .CreateEventHubNamespaceAsync(
                    _resourceGroup,
                    _eventHubNamespaceName,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create Azure Event Hub
            eventHub = await _eventHubManagementClient
                .CreateEventHubAsync(
                    _resourceGroup,
                    eventHubNamespace,
                    _eventHubName,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create "telemetry_cdm" consumer group.
            telemetryCdm = await _eventHubManagementClient
                .CreateConsumerGroupAsync(
                    _resourceGroup,
                    eventHubNamespace,
                    eventHub,
                    EventHubMgmtClient.EVENT_HUB_CONSUMER_GROUP_TELEMETRY_CDM,
                    cancellationToken
                );

            // Create "telemetry_ux" consumer group.
            telemetryUx = await _eventHubManagementClient
                .CreateConsumerGroupAsync(
                    _resourceGroup,
                    eventHubNamespace,
                    eventHub,
                    EventHubMgmtClient.EVENT_HUB_CONSUMER_GROUP_TELEMETRY_UX,
                    cancellationToken
                );

            // Create AppService Plan to host the Application Gateway Web App
            var appServicePlan = await _webSiteManagementClient
                .CreateAppServicePlanAsync(
                    _resourceGroup,
                    _appServicePlanName,
                    _defaultTagsDict,
                    cancellationToken
                );

            // This will point to PublicIP address of Ingress.
            var emptyRemoteEndpoint = "";

            var webSiteCreationTask = _webSiteManagementClient
                .CreateSiteAsync(
                    _resourceGroup,
                    appServicePlan,
                    _azureWebsiteName,
                    emptyRemoteEndpoint,
                    _webAppX509Certificate,
                    _defaultTagsDict,
                    cancellationToken
                );

            // SignalR
            var signalRCreationTask = _signalRManagementClient
                .CreateAsync(
                    _resourceGroup,
                    _signalRName,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Collect all necessary environment variables for IIoT services.
            var iotHubOwnerConnectionString = await _iotHubManagementClient
                .GetIotHubConnectionStringAsync(
                    _resourceGroup,
                    iotHub,
                    IotHubMgmtClient.IOT_HUB_OWNER_KEY_NAME,
                    cancellationToken
                );

            var cosmosDBAccount = cosmosDBAccountCreationTask.Result;
            var cosmosDBAccountConnectionString = await _cosmosDBManagementClient
                .GetCosmosDBAccountConnectionStringAsync(
                    _resourceGroup,
                    cosmosDBAccount,
                    cancellationToken
                );

            var eventHubNamespaceConnectionString = await _eventHubManagementClient
                .GetEventHubNamespaceConnectionStringAsync(
                    _resourceGroup,
                    eventHubNamespace,
                    cancellationToken
                );

            var serviceBusNamespace = serviceBusNamespaceCreationTask.Result;
            var serviceBusNamespaceConnectionString = await _serviceBusManagementClient
                .GetServiceBusNamespaceConnectionStringAsync(
                    _resourceGroup,
                    serviceBusNamespace,
                    cancellationToken
                );

            var storageAccountKey = await _storageManagementClient
                .GetStorageAccountKeyAsync(
                    _resourceGroup,
                    storageAccountGen1,
                    cancellationToken
                );

            var signalR = signalRCreationTask.Result;
            var signalRConnectionString = await _signalRManagementClient
                .GetConnectionStringAsync(
                    _resourceGroup,
                    signalR,
                    cancellationToken
                );

            var applicationInsightsComponent = applicationInsightsComponentCreationTask.Result;
            var webSite = webSiteCreationTask.Result;

            var iiotEnvironment = new IIoTEnvironment(
                _authConf.AzureEnvironment,
                _authConf.TenantId,
                iotHub,
                iotHubOwnerConnectionString,
                IotHubMgmtClient.IOT_HUB_EVENT_HUB_EVENTS_ENDPOINT_NAME,
                IotHubMgmtClient.IOT_HUB_EVENT_HUB_EVENTS_CONSUMER_GROUP_NAME,
                cosmosDBAccountConnectionString,
                storageAccountGen1,
                storageAccountKey,
                eventHub,
                eventHubNamespaceConnectionString,
                serviceBusNamespaceConnectionString,
                signalRConnectionString,
                keyVault,
                operationalInsightsWorkspace,
                applicationInsightsComponent,
                webSite,
                _applicationsManager.GetServiceApplication(),
                _applicationsManager.GetClientApplication()
            );

            // Deploy IIoT services to AKS cluster

            // Generate default SSL certificate for NGINX Ingress
            var webAppPemCertificate = X509CertificateHelper.GetPemCertificate(_webAppX509Certificate);
            //var webAppPemPublicKey = X509CertificateHelper.GetPemPublicKey(webAppX509Certificate);
            var webAppPemPrivateKey = X509CertificateHelper.GetPemPrivateKey(_webAppX509Certificate);

            // Get KubeConfig
            var aksCluster = aksClusterCreationTask.Result;
            var aksKubeConfig = await _aksManagementClient
                .GetClusterAdminCredentialsAsync(
                    _resourceGroup,
                    aksCluster.Name,
                    cancellationToken
                );

            var iiotK8SClient = new IIoTK8SClient(aksKubeConfig);

            // enable scraping of Prometheus metrics
            iiotK8SClient.EnablePrometheusMetricsScrapingAsync(cancellationToken).Wait();

            // industrial-iot namespace
            iiotK8SClient.CreateIIoTNamespaceAsync(cancellationToken).Wait();
            iiotK8SClient.SetupIIoTServiceAccountAsync(cancellationToken).Wait();
            iiotK8SClient.CreateIIoTEnvSecretAsync(iiotEnvironment.Dict, cancellationToken).Wait();
            iiotK8SClient.DeployIIoTServicesAsync(cancellationToken).Wait();

            // We will add default SSL certificate for Ingress
            // NGINX controler to industrial-iot namespace
            iiotK8SClient
                .CreateNGINXDefaultSSLCertificateSecretAsync(
                    webAppPemCertificate,
                    webAppPemPrivateKey,
                    cancellationToken
                )
                .Wait();

            // ingress-nginx namespace
            iiotK8SClient.CreateNGINXNamespaceAsync(cancellationToken).Wait();
            iiotK8SClient.SetupNGINXServiceAccountAsync(cancellationToken).Wait();
            iiotK8SClient.DeployNGINXIngressControllerAsync(cancellationToken).Wait();

            // After we have NGINX Ingress controller we can create Ingress
            // for our Industrial IoT services and wait for IP address of
            // its LoadBalancer.
            var iiotIngress = await iiotK8SClient.CreateIIoTIngressAsync(cancellationToken);
            var iiotIngressIPAddresses = await iiotK8SClient.WaitForIngressIPAsync(iiotIngress, cancellationToken);
            var iiotIngressIPAdress = iiotIngressIPAddresses.FirstOrDefault().Ip;

            // Update remote endpoint and certificate thumbprint application settings
            // of App Servise.
            var iiotIngressRemoteEndpoint = $"https://{iiotIngressIPAdress}";
            await _webSiteManagementClient
                .UpdateSiteApplicationSettingsAsync(
                    _resourceGroup,
                    webSite,
                    iiotIngressRemoteEndpoint,
                    _webAppX509Certificate,
                    cancellationToken
                );

            // Deploy reverse proxy to App Service. It will consume values of remote
            // endpoint and certificate thumbprint application settings of App Service.
            var proxySiteSourceControl = await _webSiteManagementClient
                .DeployProxyAsync(
                    _resourceGroup,
                    webSite,
                    _defaultTagsDict,
                    cancellationToken
                );

            // After we have deployed proxy to App Service, we will update 
            // client application to have redirect URIs for App Service.
            // This will be performed in UpdateClientApplicationRedirectUrisAsync() call.
            _applicationURL = webSite.DefaultHostName;

            // Check if we want to save environment to .env file
            try {
                if (_configurationProvider.IfSaveEnvFile()) {
                    iiotEnvironment.WriteToFile(ENV_FILE_PATH);
                }
            }
            catch (Exception) {
                Log.Warning("Skipping environment file generation.");
            }
        }

        public void Dispose() {
            static void disposeIfNotNull(IDisposable disposable) {
                if (null != disposable) {
                    disposable.Dispose();
                }
            };

            // Certificates
            disposeIfNotNull(_webAppX509Certificate);
            disposeIfNotNull(_aksClusterX509Certificate);

            // Resource management classes
            disposeIfNotNull(_keyVaultManagementClient);
            disposeIfNotNull(_storageManagementClient);
            disposeIfNotNull(_iotHubManagementClient);
            disposeIfNotNull(_cosmosDBManagementClient);
            disposeIfNotNull(_serviceBusManagementClient);
            disposeIfNotNull(_eventHubManagementClient);
            disposeIfNotNull(_operationalInsightsManagementClient);
            disposeIfNotNull(_applicationInsightsManagementClient);
            disposeIfNotNull(_webSiteManagementClient);
            disposeIfNotNull(_networkManagementClient);
            disposeIfNotNull(_authorizationManagementClient);
            disposeIfNotNull(_aksManagementClient);
            disposeIfNotNull(_signalRManagementClient);
        }
    }
}
