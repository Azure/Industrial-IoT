// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment {

    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.KeyVault.Fluent.Models;
    using Microsoft.Azure.Management.Storage.Fluent.Models;
    using Microsoft.Azure.Management.IotHub.Models;
    using Microsoft.Azure.Management.EventHub.Fluent.Models;
    using Microsoft.Azure.Management.Network.Fluent.Models;
    using Microsoft.Identity.Client;
    using Microsoft.Graph;

    class DeploymentExecutor : IDisposable {

        public static readonly string OWNER_KEY = "owner";
        public static readonly string APPLICATION_KEY = "application";
        public static readonly string APPLICATION_IIOT = "industrial-iot";
        public static readonly string VERSION_KEY = "version";
        public static readonly string VERSION_IIOT = "2.5.1";

        public static readonly string ENV_FILE_PATH = @".env";

        private List<string> _defaultTagsList;
        private Dictionary<string, string> _defaultTagsDict;

        private readonly Configuration.IConfigurationProvider _configurationProvider;

        private AzureEnvironment _azureEnvironment;
        private IAccount _account;
        private string _tenantName;
        private Guid _tenantId;
        private ISubscription _subscription;
        private AzureCredentials _azureCredentials;
        private string _applicationName;
        private IResourceGroup _resourceGroup;

        private AuthenticationManager _authenticationManager;
        private Infrastructure.AzureResourceManager _azureResourceManager;

        // Resource management clients
        private RestClient _restClient;

        private Infrastructure.MicrosoftGraphServiceClient _msGraphServiceClient;
        private Infrastructure.ResourceMgmtClient _resourceMgmtClient;
        private Infrastructure.KeyVaultMgmtClient _keyVaultManagementClient;
        private Infrastructure.StorageMgmtClient _storageManagementClient;
        private Infrastructure.IotHubMgmtClient _iotHubManagementClient;
        private Infrastructure.CosmosDBMgmtClient _cosmosDBManagementClient;
        private Infrastructure.ServiceBusMgmtClient _serviceBusManagementClient;
        private Infrastructure.EventHubMgmtClient _eventHubManagementClient;
        private Infrastructure.OperationalInsightsMgmtClient _operationalInsightsManagementClient;
        private Infrastructure.ApplicationInsightsMgmtClient _applicationInsightsManagementClient;
        private Infrastructure.WebSiteMgmtClient _webSiteManagementClient;
        private Infrastructure.NetworkMgmtClient _networkManagementClient;
        private Infrastructure.AuthorizationMgmtClient _authorizationManagementClient;
        private Infrastructure.AksMgmtClient _aksManagementClient;
        private Infrastructure.SignalRMgmtClient _signalRManagementClient;

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
        private Application _serviceApplication;
        private ServicePrincipal _serviceApplicationSP;

        private Application _clientApplication;
        private ServicePrincipal _clientApplicationSP;

        private Application _aksApplication;
        private ServicePrincipal _aksApplicationSP;
        private string _aksApplicationPasswordCredentialRbacSecret;

        private const string WEB_APP_CN = "webapp.services.net"; // ToDo: Assign meaningfull value.
        private X509Certificate2 _webAppX509Certificate;

        private const string AKS_CLUSTER_CN = "aks.cluster.net"; // ToDo: Assign meaningfull value.
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
            _tenantName = _configurationProvider.GetTenant();

            _authenticationManager = new AuthenticationManager(_azureEnvironment, _tenantName);
            await _authenticationManager
                .AuthenticateAsync(cancellationToken);

            _account = _authenticationManager.GetAccount();
            _tenantId = _authenticationManager.GetTenantId();
            //_azureCredentials = _authenticationManager
            //    .GetAzureCredentialsAsync(cancellationToken)
            //    .Result;
            _azureCredentials = _authenticationManager.GetDelegatingAzureCredentials();

            _defaultTagsList = new List<string> {
                _account.Username,
                APPLICATION_IIOT,
                VERSION_IIOT
            };

            _defaultTagsDict = new Dictionary<string, string> {
                { OWNER_KEY, _account.Username },
                { APPLICATION_KEY, APPLICATION_IIOT },
                { VERSION_KEY, VERSION_IIOT }
            };
        }

        public void GetApplicationName() {
            // Select application name.
            _applicationName = _configurationProvider.GetApplicationName();
        }

        public async Task InitializeResourceGroupSelectionAsync(
            CancellationToken cancellationToken = default
        ) {
            _azureResourceManager = new Infrastructure.AzureResourceManager(_azureCredentials);

            // Select subscription to use.
            var subscriptionsList = _azureResourceManager.GetSubscriptions();
            _subscription = _configurationProvider.SelectSubscription(subscriptionsList);

            _azureResourceManager.Init(_subscription);

            // Select existing ResourceGroup or create a new one.
            var defaultResourceGroupName = _applicationName;

            var useExisting = _configurationProvider.CheckIfUseExistingResourceGroup();

            if (useExisting) {
                var resourceGroups = _azureResourceManager.GetResourceGroups();
                _resourceGroup = _configurationProvider.SelectExistingResourceGroup(resourceGroups);
            }
            else {
                var region = _configurationProvider
                    .SelectResourceGroupRegion(
                        Infrastructure.AzureResourceManager.FunctionalRegions
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

                if (checkIfResourceGroupExists(defaultResourceGroupName)) {
                    defaultResourceGroupName = null;
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
            // Microsoft Graph 
            var microsoftGraphTokenProvider = _authenticationManager
                .GenerateDelegatingTokenProvider(
                    _authenticationManager.AcquireMicrosoftGraphTokenAsync
                );

            _msGraphServiceClient = new Infrastructure.MicrosoftGraphServiceClient(
                _tenantId,
                microsoftGraphTokenProvider,
                cancellationToken
            );

            // Create generic RestClient for services
            _restClient = RestClient
                .Configure()
                .WithEnvironment(_azureEnvironment)
                .WithCredentials(_azureCredentials)
                //.WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .Build();

            var subscriptionId = _subscription.SubscriptionId;

            _resourceMgmtClient = new Infrastructure.ResourceMgmtClient(subscriptionId, _restClient);
            _keyVaultManagementClient = new Infrastructure.KeyVaultMgmtClient(subscriptionId, _restClient);
            _storageManagementClient = new Infrastructure.StorageMgmtClient(subscriptionId, _restClient);
            _iotHubManagementClient = new Infrastructure.IotHubMgmtClient(subscriptionId, _restClient);
            _cosmosDBManagementClient = new Infrastructure.CosmosDBMgmtClient(subscriptionId, _restClient);
            _serviceBusManagementClient = new Infrastructure.ServiceBusMgmtClient(subscriptionId, _restClient);
            _eventHubManagementClient = new Infrastructure.EventHubMgmtClient(subscriptionId, _restClient);
            _operationalInsightsManagementClient = new Infrastructure.OperationalInsightsMgmtClient(subscriptionId, _restClient);
            _applicationInsightsManagementClient = new Infrastructure.ApplicationInsightsMgmtClient(subscriptionId, _restClient);
            _webSiteManagementClient = new Infrastructure.WebSiteMgmtClient(subscriptionId, _restClient);
            _networkManagementClient = new Infrastructure.NetworkMgmtClient(subscriptionId, _restClient);
            _authorizationManagementClient = new Infrastructure.AuthorizationMgmtClient(subscriptionId, _restClient);
            _aksManagementClient = new Infrastructure.AksMgmtClient(subscriptionId, _restClient);
            _signalRManagementClient = new Infrastructure.SignalRMgmtClient(subscriptionId, _restClient);
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

            // KeyVault names
            _keyVaultName = await _keyVaultManagementClient
                .GenerateAvailableNameAsync(cancellationToken);

            // Storage Account names
            _storageAccountName = await _storageManagementClient
                .GenerateAvailableNameAsync(cancellationToken);

            // IoT hub names
            _iotHubName = await _iotHubManagementClient
                .GenerateAvailableNameAsync(cancellationToken);

            // CosmosDB names
            _cosmosDBAccountName = await _cosmosDBManagementClient
                .GenerateAvailableNameAsync(cancellationToken);

            // Service Bus Namespace names
            _serviceBusNamespaceName = await _serviceBusManagementClient
                .GenerateAvailableNamespaceNameAsync(cancellationToken);

            // Event Hub Namespace names
            _eventHubNamespaceName = await _eventHubManagementClient
                .GenerateAvailableNamespaceNameAsync(cancellationToken);
            _eventHubName = Infrastructure.EventHubMgmtClient.GenerateEventHubName();

            // Operational Insights workspace name.
            _operationalInsightsWorkspaceName = Infrastructure.OperationalInsightsMgmtClient.GenerateWorkspaceName();

            // Application Insights name.
            _applicationInsightsName = Infrastructure.ApplicationInsightsMgmtClient.GenerateName();

            // AppService Plan name
            _appServicePlanName = Infrastructure.WebSiteMgmtClient.GenerateAppServicePlanName(_applicationName);
            _azureWebsiteName = _applicationName;

            // Networking names
            _networkSecurityGroupName = Infrastructure.NetworkMgmtClient.GenerateNetworkSecurityGroupName();
            //_routTableName = Infrastructure.NetworkMgmtClient.GenerateRoutTableName();
            _virtualNetworkName = Infrastructure.NetworkMgmtClient.GenerateVirtualNetworkName();
            //_networkInterfaceName = Infrastructure.NetworkMgmtClient.GenerateNetworkInterfaceName();
            //_publicIPAddressName = Infrastructure.NetworkMgmtClient.GeneratePublicIPAddressName();
            //_domainNameLabel = SdkContext.RandomResourceName(_applicationName + "-", 5);

            // AKS cluster name
            _aksClusterName = Infrastructure.AksMgmtClient.GenerateName();

            // SignalR name
            _signalRName = await _signalRManagementClient
                .GenerateAvailableNameAsync(_resourceGroup, cancellationToken);
        }

        public async Task RegisterApplicationsAsync(
            CancellationToken cancellationToken = default
        ) {
            // Service Application /////////////////////////////////////////////
            // Register service application

            Log.Information("Creating service application registration...");

            _serviceApplication = await _msGraphServiceClient
                .RegisterServiceApplicationAsync(
                    _servicesApplicationName,
                    _defaultTagsList,
                    cancellationToken
                );

            // Find service principal for service application
            _serviceApplicationSP = await _msGraphServiceClient
                .GetServicePrincipalAsync(_serviceApplication, cancellationToken);

            // Try to add current user as app owner for service application, if it is not owner already
            await _msGraphServiceClient
                .AddMeAsApplicationOwnerAsync(_serviceApplication, cancellationToken);

            // Client Application //////////////////////////////////////////////
            // Register client application

            Log.Information("Creating client application registration...");

            _clientApplication = await _msGraphServiceClient
                .RegisterClientApplicationAsync(
                    _serviceApplication,
                    _clientsApplicationName,
                    _azureWebsiteName,
                    _defaultTagsList,
                    cancellationToken
                );

            // Find service principal for client application
            _clientApplicationSP = await _msGraphServiceClient
                .GetServicePrincipalAsync(_clientApplication, cancellationToken);

            // Try to add current user as app owner for client application, if it is not owner already
            await _msGraphServiceClient
                .AddMeAsApplicationOwnerAsync(_clientApplication, cancellationToken);

            // Update service application to include client applicatoin as knownClientApplications
            await _msGraphServiceClient
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

            // Try to add current user as app owner for aks application, if it is not owner already
            await _msGraphServiceClient
                .AddMeAsApplicationOwnerAsync(_aksApplication, cancellationToken);
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

            var me = _msGraphServiceClient.Me(cancellationToken);

            var keyVaultParameters = _keyVaultManagementClient
                .GetCreationParameters(
                    _tenantId,
                    _resourceGroup,
                    _serviceApplicationSP,
                    me
                );

            keyVault = await _keyVaultManagementClient
                .CreateAsync(
                    _resourceGroup,
                    _keyVaultName,
                    keyVaultParameters,
                    cancellationToken
                );

            // Add certificates to KeyVault
            var keyVaultAuthenticationCallback = new Infrastructure.IIoTKeyVaultClient.AuthenticationCallback(
                async (authority, resource, scope) => {
                    // Fetch AccessToken from cache.
                    var authenticationResult = await _authenticationManager
                        .AcquireKeyVaultTokenAsync(cancellationToken);

                    return authenticationResult.AccessToken;
                }
            );

            using (var iiotKeyVaultClient = new Infrastructure.IIoTKeyVaultClient(keyVaultAuthenticationCallback, keyVault)) {
                await iiotKeyVaultClient.CreateCertificateAsync(
                    Infrastructure.IIoTKeyVaultClient.WEB_APP_CERT_NAME,
                    WEB_APP_CN,
                    _defaultTagsDict,
                    cancellationToken
                );

                _webAppX509Certificate = await iiotKeyVaultClient.GetSecretAsync(
                    Infrastructure.IIoTKeyVaultClient.WEB_APP_CERT_NAME,
                    cancellationToken
                );

                await iiotKeyVaultClient.CreateCertificateAsync(
                    Infrastructure.IIoTKeyVaultClient.AKS_CLUSTER_CERT_NAME,
                    AKS_CLUSTER_CN,
                    _defaultTagsDict,
                    cancellationToken
                );

                _aksClusterX509Certificate = await iiotKeyVaultClient.GetCertificateAsync(
                    Infrastructure.IIoTKeyVaultClient.AKS_CLUSTER_CERT_NAME,
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
                    Infrastructure.StorageMgmtClient.STORAGE_ACCOUNT_IOT_HUB_CONTAINER_NAME,
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
                    Infrastructure.IotHubMgmtClient.IOT_HUB_EVENT_HUB_PARTITIONS_COUNT,
                    storageAccountConectionString,
                    Infrastructure.StorageMgmtClient.STORAGE_ACCOUNT_IOT_HUB_CONTAINER_NAME,
                    _defaultTagsDict,
                    cancellationToken
                );

            await _iotHubManagementClient
                .CreateEventHubConsumerGroupAsync(
                    _resourceGroup,
                    iotHub,
                    Infrastructure.IotHubMgmtClient.IOT_HUB_EVENT_HUB_ONBOARDING_ENDPOINT_NAME,
                    Infrastructure.IotHubMgmtClient.IOT_HUB_EVENT_HUB_ONBOARDING_CONSUMER_GROUP_NAME,
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
            var remoteEndpoint = "";

            var webSiteCreationTask = _webSiteManagementClient
                .CreateSiteAsync(
                    _resourceGroup,
                    appServicePlan,
                    _azureWebsiteName,
                    remoteEndpoint,
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
                    Infrastructure.IotHubMgmtClient.IOT_HUB_OWNER_KEY_NAME,
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
                Infrastructure.IotHubMgmtClient.IOT_HUB_EVENT_HUB_ONBOARDING_CONSUMER_GROUP_NAME,
                Infrastructure.IotHubMgmtClient.IOT_HUB_EVENT_HUB_PARTITIONS_COUNT,
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

            iiotK8SClient.CreateIIoTNamespaceAsync().Wait();
            iiotK8SClient.CreateIIoTEnvSecretAsync(iiotEnvironment.Dict).Wait();
            iiotK8SClient.DeployIIoTServicesAsync().Wait();
            iiotK8SClient
                .CreateNGINXDefaultSSLCertificateSecretAsync(
                    webAppPemCertificate,
                    webAppPemPrivateKey
                )
                .Wait();

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
            void disposeIfNotNull(IDisposable disposable) {
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
