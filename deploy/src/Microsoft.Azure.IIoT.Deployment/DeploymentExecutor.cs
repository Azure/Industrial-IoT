// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {

    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Infrastructure;

    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.KeyVault.Fluent.Models;
    using Microsoft.Azure.Management.Storage.Fluent.Models;
    using Microsoft.Azure.Management.IotHub.Models;
    using Microsoft.Azure.Management.EventHub.Fluent.Models;
    using Microsoft.Azure.Management.Network.Fluent.Models;
    using Microsoft.Graph;

    class DeploymentExecutor : IDisposable {

        public static readonly string ENV_FILE_PATH = @".env";

        private List<string> _defaultTagsList;
        private Dictionary<string, string> _defaultTagsDict;

        private readonly Configuration.IConfigurationProvider _configurationProvider;

        private AzureEnvironment _azureEnvironment;
        private Guid _tenantId;
        private ISubscription _subscription;
        private string _applicationName;
        private IResourceGroup _resourceGroup;

        private IAuthenticationManager _authenticationManager;
        private AzureResourceManager _azureResourceManager;

        // Resource management clients
        private RestClient _restClient;

        private MicrosoftGraphServiceClient _msGraphServiceClient;
        private ResourceMgmtClient _resourceMgmtClient;
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
        private string _servicesApplicationName;
        private string _clientsApplicationName;
        private string _aksApplicationName;
        private string _keyVaultName;
        private string _storageAccountName;
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

        private Application _serviceApplication;
        private ServicePrincipal _serviceApplicationSP;

        private Application _clientApplication;
        private ServicePrincipal _clientApplicationSP;

        private Application _aksApplication;
        private ServicePrincipal _aksApplicationSP;
        private string _aksApplicationPasswordCredentialRbacSecret;

        private const string kWEB_APP_CN = "webapp.services.net"; // ToDo: Assign meaningfull value.
        private X509Certificate2 _webAppX509Certificate;

        private const string kAKS_CLUSTER_CN = "aks.cluster.net"; // ToDo: Assign meaningfull value.
        private X509Certificate2 _aksClusterX509Certificate;

        private string _aksKubeConfig;

        public DeploymentExecutor(
            Configuration.IConfigurationProvider configurationProvider
        ) {
            _configurationProvider = configurationProvider;
        }

        public async Task InitializeAuthenticationAsync(
            CancellationToken cancellationToken = default
        ) {
            // Select Azure environment
            _azureEnvironment = _configurationProvider.SelectEnvironment(AzureEnvironment.KnownEnvironments);

            // ToDo: Figure out how to sign-in without tenantId.
            _tenantId = _configurationProvider.GetTenantId();

            _authenticationManager = new AuthenticationManager(_azureEnvironment, _tenantId);
            await _authenticationManager
                .AuthenticateAsync(cancellationToken);

            var account = _authenticationManager.GetAccount();
            InitializeDefaultTags(account?.Username);
        }

        /// <summary>
        /// Initialize default tags (list and dictionary) that will be added to
        /// resources created by the application.
        /// </summary>
        /// <param name="owner"></param>
        private void InitializeDefaultTags(string owner = null) {
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

        public void GetApplicationName() {
            // Select application name.
            _applicationName = _configurationProvider.GetApplicationName();
        }

        public async Task InitializeResourceGroupSelectionAsync(
            CancellationToken cancellationToken = default
        ) {
            //var azureCredentials = await _authenticationManager
            //    .GetAzureCredentialsAsync(cancellationToken);
            var azureCredentials = _authenticationManager
                .GetDelegatingAzureCredentials();

            _azureResourceManager = new AzureResourceManager(azureCredentials);

            // Select subscription to use.
            var subscriptionsList = _azureResourceManager.GetSubscriptions();
            _subscription = _configurationProvider.SelectSubscription(subscriptionsList);

            _azureResourceManager.Init(_subscription);

            // Select existing ResourceGroup or create a new one.
            var useExisting = _configurationProvider.CheckIfUseExistingResourceGroup();

            if (useExisting) {
                var resourceGroups = _azureResourceManager.GetResourceGroups();
                _resourceGroup = _configurationProvider.SelectExistingResourceGroup(resourceGroups);
            }
            else {
                var region = _configurationProvider
                    .SelectResourceGroupRegion(
                        AzureResourceManager.FunctionalRegions
                    );

                bool checkIfResourceGroupExists(string _resourceGroupName) {
                    var _resourceGroupExists = _azureResourceManager
                        .CheckIfResourceGroupExistsAsync(
                            _resourceGroupName,
                            cancellationToken
                        )
                        .Result;

                    return _resourceGroupExists;
                }

                string defaultResourceGroupName = null;

                if (!checkIfResourceGroupExists(_applicationName)) {
                    defaultResourceGroupName = _applicationName;
                }

                var newResourceGroupName = _configurationProvider
                    .SelectResourceGroupName(
                        checkIfResourceGroupExists,
                        defaultResourceGroupName
                    );

                _resourceGroup = await _azureResourceManager
                    .CreateResourceGroupAsync(
                        region,
                        newResourceGroupName,
                        _defaultTagsDict,
                        cancellationToken
                    );
            }
        }

        public async Task BeginDeleteResourceGroupAsync(
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

        public void InitializeResourceManagementClients(
            CancellationToken cancellationToken = default
        ) {
            //var azureCredentials = await _authenticationManager
            //    .GetAzureCredentialsAsync(cancellationToken);
            var azureCredentials = _authenticationManager
                .GetDelegatingAzureCredentials();

            // Microsoft Graph
            var microsoftGraphTokenCredentials = _authenticationManager
                .GetMicrosoftGraphDelegatingTokenCredentials();

            _msGraphServiceClient = new MicrosoftGraphServiceClient(
                _tenantId,
                microsoftGraphTokenCredentials,
                cancellationToken
            );

            // Create generic RestClient for services
            _restClient = RestClient
                .Configure()
                .WithEnvironment(_azureEnvironment)
                .WithCredentials(azureCredentials)
                //.WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .Build();

            var subscriptionId = _subscription.SubscriptionId;

            _resourceMgmtClient = new ResourceMgmtClient(subscriptionId, _restClient);
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

        public async Task RegisterResourceProvidersAsync(
            CancellationToken cancellationToken = default
        ) {
            await _resourceMgmtClient.RegisterRequiredResourceProvidersAsync(cancellationToken);
        }

        public async Task GenerateResourceNamesAsync(
            CancellationToken cancellationToken = default
        ) {
            _servicesApplicationName = _applicationName + "-services";
            _clientsApplicationName = _applicationName + "-clients";
            _aksApplicationName = _applicationName + "-aks";

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
                _storageAccountName = await _storageManagementClient
                    .GenerateAvailableNameAsync(cancellationToken);
            }
            catch (Microsoft.Rest.Azure.CloudException) {
                Log.Warning(notAvailableApiFormat, "Storage Account");
                _storageAccountName = StorageMgmtClient.GenerateStorageAccountName();
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

        public async Task RegisterApplicationsAsync(
            CancellationToken cancellationToken = default
        ) {
            _owner = await _msGraphServiceClient
                .GetMeAsync(cancellationToken);

            // Service Application /////////////////////////////////////////////
            // Register service application

            Log.Information("Creating service application registration...");

            _serviceApplication = await _msGraphServiceClient
                .RegisterServiceApplicationAsync(
                    _servicesApplicationName,
                    _owner,
                    _defaultTagsList,
                    cancellationToken
                );

            // Find service principal for service application
            _serviceApplicationSP = await _msGraphServiceClient
                .GetServicePrincipalAsync(_serviceApplication, cancellationToken);

            // Add current user or service principal as app owner for service
            // application, if it is not owner already.
            await _msGraphServiceClient
                .AddAsApplicationOwnerAsync(
                    _serviceApplication,
                    _owner,
                    cancellationToken
                );

            // Client Application //////////////////////////////////////////////
            // Register client application

            Log.Information("Creating client application registration...");

            _clientApplication = await _msGraphServiceClient
                .RegisterClientApplicationAsync(
                    _serviceApplication,
                    _clientsApplicationName,
                    _defaultTagsList,
                    cancellationToken
                );

            // Find service principal for client application
            _clientApplicationSP = await _msGraphServiceClient
                .GetServicePrincipalAsync(_clientApplication, cancellationToken);

            // Add current user or service principal as app owner for client
            // application, if it is not owner already.
            await _msGraphServiceClient
                .AddAsApplicationOwnerAsync(
                    _clientApplication,
                    _owner,
                    cancellationToken
                );

            // Update service application to include client applicatoin as knownClientApplications
            _serviceApplication = await _msGraphServiceClient
                .AddAsKnownClientApplicationAsync(
                    _serviceApplication,
                    _clientApplication,
                    cancellationToken
                );

            // Grant admin consent for service application "user_impersonation" API permissions of client applicatoin
            // Grant admin consent for Microsoft Graph "User.Read" API permissions of client applicatoin
            await _msGraphServiceClient
                .GrantAdminConsentToClientApplicationAsync(
                    _serviceApplicationSP,
                    _clientApplicationSP,
                    cancellationToken
                );

            // App Registration for AKS ////////////////////////////////////////
            // Register aks application

            Log.Information("Creating AKS application registration...");

            var registrationResult = await _msGraphServiceClient
                .RegisterAKSApplicationAsync(
                    _aksApplicationName,
                    _defaultTagsList,
                    cancellationToken
                );

            _aksApplication = registrationResult.Item1;
            _aksApplicationPasswordCredentialRbacSecret = registrationResult.Item2;

            // Find service principal for aks application
            _aksApplicationSP = await _msGraphServiceClient
                .GetServicePrincipalAsync(_aksApplication, cancellationToken);

            // Add current user or service principal as app owner for aks
            // application, if it is not owner already.
            await _msGraphServiceClient
                .AddAsApplicationOwnerAsync(
                    _aksApplication,
                    _owner,
                    cancellationToken
                );
        }

        public async Task DeleteApplicationsAsync(
            CancellationToken cancellationToken = default
        ) {
            // Service Application
            if (null != _serviceApplicationSP) {
                try {
                    await _msGraphServiceClient
                        .DeleteServicePrincipalAsync(
                            _serviceApplicationSP,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning("Ignoring failure to delete ServicePrincipal of Service Application");
                }
            }

            if (null != _serviceApplication) {
                try {
                    await _msGraphServiceClient
                        .DeleteApplicationAsync(
                            _serviceApplication,
                            cancellationToken
                        );
                } catch (Exception) {
                    Log.Warning("Ignoring failure to delete Service Application");
                }
            }

            // Client Application
            if (null != _clientApplicationSP) {
                try {
                    await _msGraphServiceClient
                        .DeleteServicePrincipalAsync(
                            _clientApplicationSP,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning("Ignoring failure to delete ServicePrincipal of Client Application");
                }
            }

            if (null != _clientApplication) {
                try {
                    await _msGraphServiceClient
                        .DeleteApplicationAsync(
                            _clientApplication,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning("Ignoring failure to delete Client Application");
                }
            }

            // AKS Application
            if (null != _aksApplicationSP) {
                try {
                    await _msGraphServiceClient
                        .DeleteServicePrincipalAsync(
                            _aksApplicationSP,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning("Ignoring failure to delete ServicePrincipal of AKS Application");
                }
            }

            if (null != _aksApplication) {
                try {
                    await _msGraphServiceClient
                        .DeleteApplicationAsync(
                            _aksApplication,
                            cancellationToken
                        );
                }
                catch (Exception) {
                    Log.Warning("Ignoring failure to delete AKS Application");
                }
            }
        }

        public async Task CleanupIfAskedAsync(
            CancellationToken cancellationToken = default
        ) {
            if (null == _resourceGroup
                && null == _serviceApplicationSP
                && null == _serviceApplication
                && null == _clientApplicationSP
                && null == _clientApplication
                && null == _aksApplicationSP
                && null == _aksApplication
            ) {
                Log.Information("Nothing to cleanup");
                return;
            }

            var performCleanup = _configurationProvider.CheckIfPerformCleanup();

            if (performCleanup) {
                await BeginDeleteResourceGroupAsync(cancellationToken);
                await DeleteApplicationsAsync(cancellationToken);
            }
        }

        public async Task CreateAzureResourcesAsync(
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

            // Assign Service Principal of AKS Application "Network Contributor" IAM role for Virtual Network and its Subnet.
            await _authorizationManagementClient
                .AssignNetworkContributorRoleForResourceAsync(
                    _aksApplicationSP,
                    virtualNetwork.Id
                );

            await _authorizationManagementClient
                .AssignNetworkContributorRoleForResourceAsync(
                    _aksApplicationSP,
                    virtualNetworkAksSubnet.Id
                );

            // Create Azure KeyVault
            VaultInner keyVault;

            var keyVaultParameters = _keyVaultManagementClient
                .GetCreationParameters(
                    _tenantId,
                    _resourceGroup,
                    _serviceApplicationSP,
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
                _aksApplication,
                _aksApplicationPasswordCredentialRbacSecret,
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
            StorageAccountInner storageAccount;
            string storageAccountConectionString;

            storageAccount = await _storageManagementClient
                .CreateStorageAccountAsync(
                    _resourceGroup,
                    _storageAccountName,
                    _defaultTagsDict,
                    cancellationToken
                );

            storageAccountConectionString = await _storageManagementClient
                .GetStorageAccountConectionStringAsync(
                    _resourceGroup,
                    storageAccount,
                    cancellationToken
                );

            await _storageManagementClient
                .CreateBlobContainerAsync(
                    _resourceGroup,
                    storageAccount,
                    StorageMgmtClient.STORAGE_ACCOUNT_IOT_HUB_CONTAINER_NAME,
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
                    storageAccountConectionString,
                    StorageMgmtClient.STORAGE_ACCOUNT_IOT_HUB_CONTAINER_NAME,
                    _defaultTagsDict,
                    cancellationToken
                );

            await _iotHubManagementClient
                .CreateEventHubConsumerGroupAsync(
                    _resourceGroup,
                    iotHub,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_ONBOARDING_ENDPOINT_NAME,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_ONBOARDING_CONSUMER_GROUP_NAME,
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
                    storageAccount,
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
                _azureEnvironment,
                _tenantId,
                iotHub,
                iotHubOwnerConnectionString,
                IotHubMgmtClient.IOT_HUB_EVENT_HUB_ONBOARDING_CONSUMER_GROUP_NAME,
                IotHubMgmtClient.IOT_HUB_EVENT_HUB_PARTITIONS_COUNT,
                cosmosDBAccountConnectionString,
                storageAccount,
                storageAccountKey,
                eventHub,
                eventHubNamespaceConnectionString,
                serviceBusNamespaceConnectionString,
                signalRConnectionString,
                keyVault,
                operationalInsightsWorkspace,
                applicationInsightsComponent,
                webSite,
                _serviceApplication,
                _clientApplication
            );

            // Deploy IIoT services to AKS cluster

            // Generate default SSL certificate for NGINX Ingress
            var webAppPemCertificate = X509CertificateHelper.GetPemCertificate(_webAppX509Certificate);
            //var webAppPemPublicKey = X509CertificateHelper.GetPemPublicKey(webAppX509Certificate);
            var webAppPemPrivateKey = X509CertificateHelper.GetPemPrivateKey(_webAppX509Certificate);

            // Get KubeConfig
            var aksCluster = aksClusterCreationTask.Result;
            _aksKubeConfig = await _aksManagementClient
                .GetClusterAdminCredentialsAsync(
                    _resourceGroup,
                    aksCluster.Name,
                    cancellationToken
                );

            var iiotK8SClient = new IIoTK8SClient(_aksKubeConfig);

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

            // After we have proxy deployed to App Service, we will update 
            // client application to have redirect URIs for App Service.
            var redirectUris = new List<string> {
                $"https://{webSite.DefaultHostName}/",
                $"https://{webSite.DefaultHostName}/registry/",
                $"https://{webSite.DefaultHostName}/twin/",
                $"https://{webSite.DefaultHostName}/history/",
                $"https://{webSite.DefaultHostName}/ua/",
                $"https://{webSite.DefaultHostName}/vault/"
            };

            _clientApplication = await _msGraphServiceClient
                .UpdateRedirectUrisAsync(
                    _clientApplication,
                    redirectUris,
                    cancellationToken
                );

            // Check if we want to save environment to .env file
            var saveEnvFile = _configurationProvider.CheckIfSaveEnvFile();

            try {
                if (saveEnvFile) {
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
            disposeIfNotNull(_resourceMgmtClient);
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
