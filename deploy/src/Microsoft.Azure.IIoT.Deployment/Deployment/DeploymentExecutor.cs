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
    using System.IO;
    using System.Text;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    using Authentication;
    using Infrastructure;
    using Configuration;
    using Microsoft.Azure.Management.ContainerService.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
    using Microsoft.Azure.Management.KeyVault.Fluent.Models;
    using Microsoft.Azure.Management.Storage.Fluent.Models;
    using Microsoft.Azure.Management.IotHub.Models;
    using Microsoft.Azure.Management.EventHub.Fluent.Models;
    using Microsoft.Azure.Management.Network.Fluent.Models;
    using Microsoft.Graph;
    using global::Azure.Storage.Blobs;

    class DeploymentExecutor : IDisposable {

        public const string ENV_FILE_PATH = @".env";

        private List<string> _defaultTagsList;
        private Dictionary<string, string> _defaultTagsDict;

        private readonly IConfigurationProvider _configurationProvider;

        private AuthenticationConfiguration _authConf;
        private ISubscription _subscription;
        private string _applicationName;
        private string _applicationUrl;
        private IResourceGroup _resourceGroup;

        private IAuthenticationManager _authenticationManager;
        private AzureResourceManager _azureResourceManager;

        // Resource management clients
        private RestClient _restClient;

        private ApplicationsManager _applicationsManager;
        private ResourceMgmtClient _resourceManagementClient;
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
        private ComputeMgmtClient _computeManagementClient;

        // Resource names
        private string _keyVaultName;
        private string _storageAccountGen2Name;
        private string _iotHubName;
        private string _cosmosDBAccountName;
        private string _serviceBusNamespaceName;
        private string _eventHubNamespaceName;
        private string _eventHubName;
        private string _operationalInsightsWorkspaceName;
        private string _applicationInsightsName;
        private string _networkSecurityGroupName;
        private string _virtualNetworkName;
        private string _aksClusterName;
        private string _signalRName;

        // Resources
        private DirectoryObject _owner;
        private bool _ownedApplications = false;

        private const string kAKS_CLUSTER_CN = "aks.cluster.net"; // ToDo: Assign meaningfull value.
        private X509Certificate2 _aksClusterX509Certificate;

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
                OutputApplicationRegistrationDefinitionSettings();

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
                    _authConf
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
            var helmSettings = _configurationProvider.GetHelmSettings();
            var versionIIoT = string.IsNullOrEmpty(helmSettings?.ImageTag)
                    ? HelmSettings._defaultImageTag
                    : helmSettings?.ImageTag;

            var valueIotSuiteType =
                $"{Resources.IIoTDeploymentTags.VALUE_APPLICATION_IIOT}-" +
                $"{versionIIoT}-" +
                $"{Resources.IIoTDeploymentTags.VALUE_IOT_SUITE_TYPE_SUF}";

            _defaultTagsList = new List<string> {
                Resources.IIoTDeploymentTags.VALUE_APPLICATION_IIOT,
                versionIIoT,
                Resources.IIoTDeploymentTags.VALUE_MANAGED_BY_IIOT,
                valueIotSuiteType
            };

            _defaultTagsDict = new Dictionary<string, string> {
                { Resources.IIoTDeploymentTags.KEY_APPLICATION, Resources.IIoTDeploymentTags.VALUE_APPLICATION_IIOT },
                { Resources.IIoTDeploymentTags.KEY_VERSION, versionIIoT},
                { Resources.IIoTDeploymentTags.KEY_MANAGED_BY, Resources.IIoTDeploymentTags.VALUE_MANAGED_BY_IIOT},
                { Resources.IIoTDeploymentTags.KEY_IOT_SUITE_TYPE, valueIotSuiteType}
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

                if (!string.IsNullOrWhiteSpace(me.Mail)) {
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
                var resourceGroups = await _azureResourceManager
                    .GetResourceGroupsAsync(cancellationToken);

                resourceGroups = resourceGroups
                    .OrderBy(resourceGroup => resourceGroup.Name.ToLower())
                    .ToList();

                _resourceGroup = _configurationProvider
                    .GetExistingResourceGroup(resourceGroups);
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

            _resourceManagementClient = new ResourceMgmtClient(_subscription.SubscriptionId, _restClient);
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
            _computeManagementClient = new ComputeMgmtClient(subscriptionId, _restClient);
        }

        protected async Task RegisterResourceProvidersAsync(
            CancellationToken cancellationToken = default
        ) {
            await _resourceManagementClient.RegisterRequiredResourceProvidersAsync(cancellationToken);
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
                _applicationUrl = _configurationProvider.GetApplicationUrl();

                if (!string.IsNullOrEmpty(_applicationUrl)) {
                    await _applicationsManager
                        .UpdateClientApplicationRedirectUrisAsync(
                            _applicationUrl,
                            cancellationToken
                        );
                }
                else {
                    Log.Information("Client application redirectUris will not " +
                        "be configured since ApplicationUrl is not provided.");
                }
            }
            else if (RunMode.Full == runMode) {
                // _applicationUrl will be set up by CreateAzureResourcesAsync() call;
                await _applicationsManager
                    .UpdateClientApplicationRedirectUrisAsync(
                        _applicationUrl,
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
            if (_applicationsManager != null) {
                await _applicationsManager.DeleteApplicationsAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Output application registration definitions as
        /// ApplicationRegistrationDefinitionSettings to console.
        /// </summary>
        protected void OutputApplicationRegistrationDefinitionSettings() {
            var appRegDef = _applicationsManager
                .ToApplicationRegistrationDefinition();
            var appRegDefSettings = ApplicationRegistrationDefinitionSettings
                .FromApplicationRegistrationDefinition(appRegDef);

            var jsonString = JsonConvert
                .SerializeObject(
                    appRegDefSettings,
                    Formatting.Indented,
                    new JsonConverter[] { new StringEnumConverter() }
                );

            Log.Information("Use details bellow as ApplicationRegistration " +
                "for resource deployment of Industrial IoT solution.");
            Console.WriteLine(jsonString);
        }

        /// <summary>
        /// Output jumpbox credentials to console.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        protected void OutputJumpboxCredentials(
            string username,
            string password
        ) {
            // We will only check for null
            if (username is null) {
                throw new ArgumentNullException(nameof(username));
            }
            if (password is null) {
                throw new ArgumentNullException(nameof(password));
            }

            Console.WriteLine(
                $"Use the following credentials to login to jumpbox:\n" +
                $"\n" +
                $"Username: {username}\n" +
                $"Password: {password}\n" +
                $"\n"
            );
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

            // Networking names
            _networkSecurityGroupName = NetworkMgmtClient.GenerateNetworkSecurityGroupName();
            _virtualNetworkName = NetworkMgmtClient.GenerateVirtualNetworkName();

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
            SubnetInner virtualNetworkVmSubnet;
            //PublicIPAddressInner publicIPAddress;
            //NetworkInterfaceInner networkInterface;

            var networkingDeploymentParameters = new Dictionary<string, object> {
                {"nsgName", _networkSecurityGroupName},
                {"vnetName", _virtualNetworkName},
                {"subnetAKSName", NetworkMgmtClient.VIRTUAL_NETWORK_AKS_SUBNET_NAME},
                {"subnetVMName", NetworkMgmtClient.VIRTUAL_NETWORK_VM_SUBNET_NAME}
            };

            var networkingDeployment = await _resourceManagementClient
                .CreateResourceGroupDeploymentAsync(
                    _resourceGroup,
                    "networking",
                    Resources.ArmTemplates.networking,
                    networkingDeploymentParameters,
                    DeploymentMode.Incremental,
                    _defaultTagsDict,
                    cancellationToken
                );

            var networkingDeploymentOutput = ResourceMgmtClient
                .ExtractDeploymentOutput(networkingDeployment);

            networkSecurityGroup = await _networkManagementClient
                .GetNetworkSecurityGroupAsync(
                    _resourceGroup,
                    _networkSecurityGroupName,
                    cancellationToken
                );

            virtualNetwork = await _networkManagementClient
                .GetVirtualNetworkAsync(
                    _resourceGroup,
                    _virtualNetworkName,
                    cancellationToken
                );

            virtualNetworkAksSubnet = virtualNetwork
                .Subnets
                .Where(subnet => subnet.Name == NetworkMgmtClient.VIRTUAL_NETWORK_AKS_SUBNET_NAME)
                .First();

            virtualNetworkVmSubnet = virtualNetwork
                .Subnets
                .Where(subnet => subnet.Name == NetworkMgmtClient.VIRTUAL_NETWORK_VM_SUBNET_NAME)
                .First();

            // Create Azure KeyVault
            var keyVaultParameters = _keyVaultManagementClient
                .GetCreationParameters(
                    _authConf.TenantId,
                    _resourceGroup,
                    _applicationsManager.GetServiceApplicationSP(),
                    _owner,
                    _defaultTagsDict
                );

            var keyVault = await _keyVaultManagementClient
                .CreateAsync(
                    _resourceGroup,
                    _keyVaultName,
                    keyVaultParameters,
                    cancellationToken
                );

            // Add required elements to KeyVault
            var setupKeyVaultTask = SetupKeyVaultAsync(keyVault, cancellationToken);

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
            // We have to wait for setupKeyVaultTask to finish before using _aksClusterX509Certificate.
            await setupKeyVaultTask;
            var operationalInsightsWorkspace = await operationalInsightsWorkspaceCreationTask;

            var kubernetesVersions = await _azureResourceManager
                .ListKubernetesVersionsAsync(_resourceGroup.Region, cancellationToken);
            var kubernetesVersion = AksMgmtClient.SelectLatestPatchVersion(
                AksMgmtClient.KUBERNETES_VERSION_MAJ_MIN,
                kubernetesVersions.ToList()
            );

            // Take higher version between the received latest and KUBERNETES_VERSION_FALLBACK.
            kubernetesVersion = AksMgmtClient.SelectLatestPatchVersion(
                AksMgmtClient.KUBERNETES_VERSION_MAJ_MIN,
                new List<string> { kubernetesVersion, AksMgmtClient.KUBERNETES_VERSION_FALLBACK }
            );

            Log.Information($"Kubernetes version {kubernetesVersion} will be used in AKS.");

            var clusterDefinition = _aksManagementClient.GetClusterDefinition(
                kubernetesVersion,
                _resourceGroup,
                _applicationsManager.GetAKSApplication(),
                _applicationsManager.GetAKSApplicationSecret(),
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
            StorageAccountInner storageAccountGen2;
            string storageAccountGen2ConectionString;
            BlobContainerInner iotHubBlobContainer;
            BlobContainerInner dataprotectionBlobContainer;
            BlobContainerInner deploymentScriptsBlobContainer;

            storageAccountGen2 = await _storageManagementClient
                .CreateStorageAccountGen2Async(
                    _resourceGroup,
                    _storageAccountGen2Name,
                    false,
                    _defaultTagsDict,
                    cancellationToken
                );

            storageAccountGen2ConectionString = await _storageManagementClient
                .GetStorageAccountConectionStringAsync(
                    _resourceGroup,
                    storageAccountGen2,
                    cancellationToken
                );

            // Create Blob container for IoT Hub storage.
            iotHubBlobContainer = await _storageManagementClient
                .CreateBlobContainerAsync(
                    _resourceGroup,
                    storageAccountGen2,
                    StorageMgmtClient.STORAGE_ACCOUNT_IOT_HUB_CONTAINER_NAME,
                    PublicAccess.None,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create Blob container for dataprotection feature.
            dataprotectionBlobContainer = await _storageManagementClient
                .CreateBlobContainerAsync(
                    _resourceGroup,
                    storageAccountGen2,
                    StorageMgmtClient.STORAGE_ACCOUNT_DATAPROTECTION_CONTAINER_NAME,
                    PublicAccess.None,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create Blob container for deployment scripts.
            deploymentScriptsBlobContainer = await _storageManagementClient
                .CreateBlobContainerAsync(
                    _resourceGroup,
                    storageAccountGen2,
                    StorageMgmtClient.STORAGE_ACCOUNT_DEPLOYMENT_SCRIPTS_CONTAINER_NAME,
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
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_RETENTION_TIME_IN_DAYS,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_PARTITIONS_COUNT,
                    storageAccountGen2ConectionString,
                    iotHubBlobContainer.Name,
                    _defaultTagsDict,
                    cancellationToken
                );

            // Create "events" consumer group.
            var iotHubEventHubCGEvents = await _iotHubManagementClient
                .CreateEventHubConsumerGroupAsync(
                    _resourceGroup,
                    iotHub,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_EVENTS_ENDPOINT_NAME,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_CONSUMER_GROUP_EVENTS_NAME,
                    cancellationToken
                );

            // Create "telemetry" consumer group.
            var iotHubEventHubCGTelemetry = await _iotHubManagementClient
                .CreateEventHubConsumerGroupAsync(
                    _resourceGroup,
                    iotHub,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_EVENTS_ENDPOINT_NAME,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_CONSUMER_GROUP_TELEMETRY_NAME,
                    cancellationToken
                );

            // Create "onboarding" consumer group.
            var iotHubEventHubCGOnboarding = await _iotHubManagementClient
                .CreateEventHubConsumerGroupAsync(
                    _resourceGroup,
                    iotHub,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_EVENTS_ENDPOINT_NAME,
                    IotHubMgmtClient.IOT_HUB_EVENT_HUB_CONSUMER_GROUP_ONBOARDING_NAME,
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
                    EventHubMgmtClient.DEFAULT_MESSAGE_RETENTION_IN_DAYS,
                    EventHubMgmtClient.DEFUALT_PARTITION_COUNT,
                    _defaultTagsDict,
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

            // SignalR
            var signalRServiceMode = SignalRMgmtClient.ServiceMode.Default;
            var signalRCreationTask = _signalRManagementClient
                .CreateAsync(
                    _resourceGroup,
                    _signalRName,
                    signalRServiceMode,
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

            var cosmosDBAccount = await cosmosDBAccountCreationTask;
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

            var serviceBusNamespace = await serviceBusNamespaceCreationTask;
            var serviceBusNamespaceConnectionString = await _serviceBusManagementClient
                .GetServiceBusNamespaceConnectionStringAsync(
                    _resourceGroup,
                    serviceBusNamespace,
                    cancellationToken
                );

            var signalR = await signalRCreationTask;
            var signalRConnectionString = await _signalRManagementClient
                .GetConnectionStringAsync(
                    _resourceGroup,
                    signalR,
                    cancellationToken
                );

            var applicationInsightsComponent = await applicationInsightsComponentCreationTask;

            var operationalInsightsWorkspaceKeys = await _operationalInsightsManagementClient
                .GetSharedKeysAsync(_resourceGroup, operationalInsightsWorkspace, cancellationToken);

            // Wat for Public IP of AKS before creating IIoTEnvironment
            var aksCluster = await aksClusterCreationTask;

            // Create a PublicIP address in AKS node resource group
            var aksPublicIpName = "aks-public-ip";
            var aksPublicIpDomainNameLabel = _applicationName.ToLower();

            var aksPublicIp = await CreateAksPublicIPAsync(
                aksCluster,
                aksPublicIpName,
                aksPublicIpDomainNameLabel,
                cancellationToken
            );

            var serviceURL = $"https://{aksPublicIp.DnsSettings.Fqdn}";

            var iiotEnvironment = new IIoTEnvironment(
                _authConf.AzureEnvironment,
                _authConf.TenantId,
                _subscription.SubscriptionId,
                _resourceGroup.Name,
                // IoT Hub
                iotHub,
                iotHubOwnerConnectionString,
                IotHubMgmtClient.IOT_HUB_EVENT_HUB_EVENTS_ENDPOINT_NAME,
                iotHubEventHubCGEvents,
                iotHubEventHubCGTelemetry,
                iotHubEventHubCGOnboarding,
                // Cosmos DB
                cosmosDBAccountConnectionString,
                // Storage Account
                storageAccountGen2ConectionString,
                dataprotectionBlobContainer.Name,
                // Event Hub Namespace
                eventHub,
                eventHubNamespaceConnectionString,
                telemetryUx,
                // Service Bus
                serviceBusNamespaceConnectionString,
                // SignalR
                signalRConnectionString,
                signalRServiceMode.Value,
                // Key Vault
                keyVault,
                IIoTKeyVaultClient.DATAPROTECTION_KEY_NAME,
                // Application Insights
                applicationInsightsComponent,
                // Log Analytics Workspace
                operationalInsightsWorkspace,
                operationalInsightsWorkspaceKeys.PrimarySharedKey,
                // Service URL
                serviceURL,
                // App Registrations
                _applicationsManager.GetServiceApplication(),
                _applicationsManager.GetServiceApplicationSecret(),
                _applicationsManager.GetClientApplication(),
                _applicationsManager.GetClientApplicationSecret()
            );

            // We will push iiotEnvironment to KeyVault so that Azure IIoT
            // components can consume configuration from there.
            var keyVaultConfCreationTask = PushIIoTEnvironmentToKeyVaultAsync(
                keyVault, iiotEnvironment, cancellationToken);

            // Create and setup a jumpbox for AKS.

            // First we need to upload jumpbox setup script and YAML files that
            // will be used by it to Blob Container.
            const string jumpboxShBlobName = "jumpbox.sh";
            var jumpboxShBlobUri = await UploadBlobAsync(
                storageAccountGen2ConectionString,
                deploymentScriptsBlobContainer.Name,
                jumpboxShBlobName,
                Resources.Scripts.JumpboxSh,
                cancellationToken
            );

            const string omsAgentConfBlobName = "04_oms_agent_configmap.yaml";
            var omsAgentConfBlobUri = await UploadBlobAsync(
                storageAccountGen2ConectionString,
                deploymentScriptsBlobContainer.Name,
                omsAgentConfBlobName,
                Resources.IIoTK8SResources._04_oms_agent_configmap,
                cancellationToken
            );

            const string clusterIssuerBlobName = "90_letsencrypt_cluster_issuer.yaml";
            var clusterIssuerBlobUri = await UploadBlobAsync(
                storageAccountGen2ConectionString,
                deploymentScriptsBlobContainer.Name,
                clusterIssuerBlobName,
                Resources.IIoTK8SResources._90_letsencrypt_cluster_issuer,
                cancellationToken
            );

            // Wait a bit before starting the deployment.
            const int millisecondsDelay = 30 * 1000;
            await Task.Delay(millisecondsDelay);

            const string jumpboxPublicIpName = "jumpbox-ip";
            const string jumpboxNetworkInterfaceName = "jumpbox-networkInterface";
            const string jumpboxVirtualMachineName = "jumpbox-vm";

            var jumpboxUsername = "jumpboxuser";
            var jumpboxPassword = Guid.NewGuid().ToString();

            var aksRoleType = "AzureKubernetesServiceClusterAdminRole";
            var aksRoleGuid = Guid.NewGuid();
            var storageRoleType = "StorageBlobDataReader";
            var storageRoleGuid = Guid.NewGuid();

            var helmSettings = _configurationProvider.GetHelmSettings();
            // azure-industrial-iot Helm chart details
            var helmRepoUrl = string.IsNullOrEmpty(helmSettings?.RepoUrl)
                ? HelmSettings._defaultRepoUrl
                : helmSettings?.RepoUrl;
            var helmChartVersion = string.IsNullOrEmpty(helmSettings?.ChartVersion)
                ? HelmSettings._defaultChartVersion
                : helmSettings?.ChartVersion;
            // azure-industrial-iot Helm chart values
            var aiiotImageTag = string.IsNullOrEmpty(helmSettings?.ImageTag)
                ? HelmSettings._defaultImageTag
                : helmSettings?.ImageTag;
            var aiiotImageNamespace = helmSettings?.ImageNamespace ?? HelmSettings._defaultImageNamespace;
            var aiiotContainerRegistryServer = string.IsNullOrEmpty(helmSettings?.ContainerRegistryServer)
                ? HelmSettings._defaultContainerRegistryServer
                : helmSettings?.ContainerRegistryServer;
            var aiiotContainerRegistryUsername = helmSettings?.ContainerRegistryUsername ?? HelmSettings._defaultContainerRegistryUsername;
            var aiiotContainerRegistryPassword = helmSettings?.ContainerRegistryPassword ?? HelmSettings._defaultContainerRegistryPassword;

            Log.Information("Helm Settings: tag=" + aiiotImageTag + ", namespace=" + aiiotImageNamespace + ", server=" + aiiotContainerRegistryServer + ", username=" + aiiotContainerRegistryUsername);

            var aiiotTenantId = _authConf.TenantId.ToString();
            var aiiotKeyVaultUri = keyVault.Properties.VaultUri;
            var aiiotServicesAppId = _applicationsManager.GetServiceApplication().AppId;
            var aiiotServicesAppSecret = _applicationsManager.GetServiceApplicationSecret();

            var jumpboxDeploymentParameters = new Dictionary<string, object> {
                {"nsgId", networkSecurityGroup.Id},
                {"subnetId", virtualNetworkVmSubnet.Id},
                {"aksClusterName", aksCluster.Name},
                {"aksPublicIpAddress", aksPublicIp.IpAddress},
                {"aksPublicIpDnsLabel", aksPublicIp.DnsSettings.DomainNameLabel},
                {"publicIpName", jumpboxPublicIpName},
                {"networkInterfaceName", jumpboxNetworkInterfaceName},
                {"virtualMachineName", jumpboxVirtualMachineName},
                {"virtualMachineUsername", jumpboxUsername},
                {"virtualMachinePassword", jumpboxPassword},
                {"aksBuiltInRoleType", aksRoleType},
                {"aksRbacGuid", aksRoleGuid.ToString()},
                {"storageBuiltInRoleType", storageRoleType},
                {"storageRbacGuid", storageRoleGuid.ToString()},
            };

            Log.Information("Deploying jumpbox VM resources.");

            var jumpboxDeployment = await _resourceManagementClient
                .CreateResourceGroupDeploymentAsync(
                    _resourceGroup,
                    "jumpbox-vm",
                    Resources.ArmTemplates.jumpbox_vm,
                    jumpboxDeploymentParameters,
                    DeploymentMode.Incremental,
                    _defaultTagsDict,
                    cancellationToken
                );

            var jumpboxDeploymentOutput = ResourceMgmtClient
                .ExtractDeploymentOutput(jumpboxDeployment);

            // We will wait a minute for role assignments to be applied.
            await Task.Delay(60_000, cancellationToken);

            var jumpboxSetupDeploymentParameters = new Dictionary<string, object> {
                {"aksClusterName", aksCluster.Name},
                {"aksPublicIpAddress", aksPublicIp.IpAddress},
                {"aksPublicIpDnsLabel", aksPublicIp.DnsSettings.DomainNameLabel},
                {"virtualMachineName", jumpboxVirtualMachineName},
                {"aksBuiltInRoleType", aksRoleType},
                {"scriptFileUris", new List<string> {
                    jumpboxShBlobUri,
                    omsAgentConfBlobUri,
                    clusterIssuerBlobUri
                    }
                },
                // azure-industrial-iot Helm chart details
                {"helmRepoUrl", helmRepoUrl},
                {"helmChartVersion", helmChartVersion},
                // azure-industrial-iot Helm chart values
                {"aiiotImageTag", aiiotImageTag},
                {"aiiotImageNamespace", aiiotImageNamespace},
                {"aiiotContainerRegistryServer", aiiotContainerRegistryServer},
                {"aiiotContainerRegistryUsername", aiiotContainerRegistryUsername},
                {"aiiotContainerRegistryPassword", aiiotContainerRegistryPassword},
                {"aiiotTenantId", aiiotTenantId},
                {"aiiotKeyVaultUri", aiiotKeyVaultUri},
                {"aiiotServicesAppId", aiiotServicesAppId},
                {"aiiotServicesAppSecret", aiiotServicesAppSecret},
                {"aiiotServicesHostname", aksPublicIp.DnsSettings.Fqdn}
            };

            Log.Information("Installing Helm charts from jumpbox VM.");

            var jumpboxSetupDeployment = await _resourceManagementClient
                .CreateResourceGroupDeploymentAsync(
                    _resourceGroup,
                    "jumpbox-vm-setup",
                    Resources.ArmTemplates.jumpbox_vm_setup,
                    jumpboxSetupDeploymentParameters,
                    DeploymentMode.Incremental,
                    _defaultTagsDict,
                    cancellationToken
                );


            // Output jumpbox credentials so that users can login into it.
            OutputJumpboxCredentials(jumpboxUsername, jumpboxPassword);

            // Stop jumpbox.
            var jumpboxVirtualMachine = await _computeManagementClient
                .GetVMAsync(
                    _resourceGroup,
                    jumpboxVirtualMachineName,
                    cancellationToken
                );
            await _computeManagementClient
                .BeginDeallocateVMAsync(
                    _resourceGroup,
                    jumpboxVirtualMachine,
                    cancellationToken
                );

            // After we have endpoint for accessing Azure IIoT microservices we
            // will update client application to have Redirect URIs.
            // This will be performed in UpdateClientApplicationRedirectUrisAsync() call.
            _applicationUrl = aksPublicIp.DnsSettings.Fqdn;

            // Waiting for unfinished tasks.
            await keyVaultConfCreationTask;

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

        /// <summary>
        /// Add the following elements to KeyVault:
        ///     - Certificate for SSH access of AKS
        ///     - Dataprotection key
        /// </summary>
        /// <param name="keyVault"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task SetupKeyVaultAsync(
            VaultInner keyVault,
            CancellationToken cancellationToken = default
        ) {
            if (keyVault is null) {
                throw new ArgumentNullException(nameof(keyVault));
            }

            var keyVaultAuthenticationCallback = new IIoTKeyVaultClient.AuthenticationCallback(
                async (authority, resource, scope) => {
                    // Fetch AccessToken from cache.
                    var authenticationResult = await _authenticationManager
                        .AcquireKeyVaultAuthenticationResultAsync(cancellationToken);

                    return authenticationResult.AccessToken;
                }
            );

            using (var iiotKeyVaultClient = new IIoTKeyVaultClient(keyVaultAuthenticationCallback, keyVault)) {
                // This will be SSH certificate for accessing AKS cluster.
                await iiotKeyVaultClient.CreateCertificateAsync(
                    IIoTKeyVaultClient.AKS_CLUSTER_CERT_NAME,
                    kAKS_CLUSTER_CN,
                    _defaultTagsDict,
                    cancellationToken
                );

                var aksClusterX509CertificateGetTask = iiotKeyVaultClient.GetCertificateAsync(
                    IIoTKeyVaultClient.AKS_CLUSTER_CERT_NAME,
                    cancellationToken
                );

                // Create the key that will be used for dataprotection feature.
                await iiotKeyVaultClient.CreateDataprotectionKeyAsync(
                    IIoTKeyVaultClient.DATAPROTECTION_KEY_NAME,
                    _defaultTagsDict,
                    cancellationToken
                );

                _aksClusterX509Certificate = await aksClusterX509CertificateGetTask;
            }
        }

        /// <summary>
        /// Push elements of IIoTEnvironment to KeyVault.
        /// </summary>
        /// <param name="keyVault"></param>
        /// <param name="iioTEnvironment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task PushIIoTEnvironmentToKeyVaultAsync(
            VaultInner keyVault,
            IIoTEnvironment iioTEnvironment,
            CancellationToken cancellationToken = default
        ) {
            if (keyVault is null) {
                throw new ArgumentNullException(nameof(keyVault));
            }
            if (iioTEnvironment is null) {
                throw new ArgumentNullException(nameof(iioTEnvironment));
            }

            const string keyK = "key";
            const string valueK = "value";

            const string deploymentName = "configuration";

            try {
                var configurationList = new List<Dictionary<string, string>>();
                foreach (var kvp in iioTEnvironment.Dict) {
                    configurationList.Add(new Dictionary<string, string> {
                        { keyK,  kvp.Key },
                        { valueK, kvp.Value }
                    });
                }

                var parameters = new Dictionary<string, object> {
                    {"keyVaultName", keyVault.Name},
                    {"configuration", configurationList}
                };

                Log.Information("Pushing IIoT configuration parameters to Key Vault...");
                await _resourceManagementClient
                    .CreateResourceGroupDeploymentAsync(
                        _resourceGroup,
                        deploymentName,
                        Resources.ArmTemplates.configuration,
                        parameters,
                        DeploymentMode.Incremental,
                        _defaultTagsDict,
                        cancellationToken
                    );

                Log.Information("Pushed IIoT configuration parameters to Key Vault.");
            }
            catch (Exception) {
                Log.Information("Failed to push IIoT configuration parameters to Key Vault.");
                throw;
            }
        }

        /// <summary>
        /// Create blob with given content. Returns blob's URL.
        /// </summary>
        /// <param name="storageAccountConectionString"></param>
        /// <param name="blobContainer"></param>
        /// <param name="blobName"></param>
        /// <param name="blobContent"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task<string> UploadBlobAsync(
            string storageAccountConectionString,
            string blobContainerName,
            string blobName,
            string blobContent,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(storageAccountConectionString)) {
                throw new ArgumentNullException(nameof(storageAccountConectionString));
            }
            if (string.IsNullOrWhiteSpace(blobContainerName)) {
                throw new ArgumentNullException(nameof(blobContainerName));
            }
            if (string.IsNullOrWhiteSpace(blobName)) {
                throw new ArgumentNullException(nameof(blobName));
            }
            // blobContent can be an empty string, but it should not be null.
            if (blobContent is null) {
                throw new ArgumentNullException(nameof(blobContent));
            }

            Log.Debug("Uploading data to Azure Blob ...");
            var blobServiceClient = new BlobServiceClient(storageAccountConectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(blobContent));
            await blobClient.UploadAsync(memoryStream, cancellationToken);

            Log.Debug("Uploaded data to Azure Blob.");
            return blobClient.Uri.ToString();
        }

        /// <summary>
        /// Get Public IP SKU of the same kind.
        /// </summary>
        /// <param name="loadBalancerSku"></param>
        /// <returns></returns>
        protected static string ToPublicIpSku(
            Management.ContainerService.Fluent.Models.LoadBalancerSku loadBalancerSku
        ) {
            if (loadBalancerSku == Management.ContainerService.Fluent.Models.LoadBalancerSku.Basic) {
                return "Basic";
            } else if (loadBalancerSku == Management.ContainerService.Fluent.Models.LoadBalancerSku.Standard) {
                return "Standard";
            } else {
                throw new ArgumentException($"Unknows LoadBalancerSku: {loadBalancerSku}");
            }
        }

        /// <summary>
        /// Create a Public IP resource in AKS node resource group.
        /// </summary>
        /// <param name="aksCluster"></param>
        /// <param name="publicIpName"></param>
        /// <param name="publicIpDomainNameLabel"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task<PublicIPAddressInner> CreateAksPublicIPAsync(
            ManagedClusterInner aksCluster,
            string publicIpName,
            string publicIpDomainNameLabel,
            CancellationToken cancellationToken = default
        ) {
            if (aksCluster is null) {
                throw new ArgumentNullException(nameof(aksCluster));
            }
            if (string.IsNullOrWhiteSpace(publicIpName)) {
                throw new ArgumentNullException(nameof(publicIpName));
            }
            if (string.IsNullOrWhiteSpace(publicIpDomainNameLabel)) {
                throw new ArgumentNullException(nameof(publicIpDomainNameLabel));
            }

            Log.Information("Creating Azure Public IP for AKS cluster ...");

            // We will create a PublicIP address in AKS node resource group.
            var aksNodeResourceGroup = await _azureResourceManager
                .GetResourceGroupAsync(
                    aksCluster.NodeResourceGroup,
                    cancellationToken
                );

            // Public IP resource should have the same SKU as AKS cluster Load Balancer.
            var aksPublicIpSku = ToPublicIpSku(aksCluster.NetworkProfile.LoadBalancerSku);

            var aksPublicIpDeploymentParameters = new Dictionary<string, object> {
                {"publicIpName", publicIpName},
                {"publicIpSku", aksPublicIpSku},
                {"publicIpDomainNameLabel", publicIpDomainNameLabel},
            };

            const string aksPublicIpDeploymentName = "aks-public-ip";
            var aksPublicIpDeployment = await _resourceManagementClient
                .CreateResourceGroupDeploymentAsync(
                    aksNodeResourceGroup,
                    aksPublicIpDeploymentName,
                    Resources.ArmTemplates.aks_public_ip,
                    aksPublicIpDeploymentParameters,
                    DeploymentMode.Incremental,
                    _defaultTagsDict,
                    cancellationToken
                );

            var aksPublicIp = await _networkManagementClient
                .GetPublicIPAddressAsync(
                    aksNodeResourceGroup,
                    publicIpName,
                    cancellationToken
                );

            Log.Information("Created Azure Public IP for AKS cluster.");
            return aksPublicIp;
        }

        public void Dispose() {
            static void disposeIfNotNull(IDisposable disposable) {
                if (null != disposable) {
                    disposable.Dispose();
                }
            };

            // Certificates
            disposeIfNotNull(_aksClusterX509Certificate);

            // Resource management classes
            disposeIfNotNull(_resourceManagementClient);
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
            disposeIfNotNull(_computeManagementClient);
        }
    }
}
