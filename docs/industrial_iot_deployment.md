# Microsoft.Azure.IIoT.Deployment

## Table Of Contents

* [Running Microsoft.Azure.IIoT.Deployment](#running-microsoft.azure.iiot.deployment)
* [Deployed Resources](#deployed-resources)
* [Missing And Planned Features](#missing-and-planned-features)
* [Known Issues](#known-issues)
* [Resources](#resources)

`Microsoft.Azure.IIoT.Deployment` is a command line application for deploying Industrial IoT solution. It takes care of deploying Azure infrastructure resources and microservices of Industrial IoT solution.

This replaces [services/deploy/deploy.ps1](../services/deploy/deploy.ps1) script, which similarly deployed Azure infrastructure resources and microservices. Main difference from infrastructure perspective is that `Microsoft.Azure.IIoT.Deployment` deploys microservices to an AKS cluster, while `deploy.ps1` uses a VM for that.

## Running Microsoft.Azure.IIoT.Deployment

`Microsoft.Azure.IIoT.Deployment` requires permissions to create azure resources on users behalf and as such it requires admin consent within the Azure tenant. This entails a few additional manual steps before the application can run properly. Those steps are required after first run which will fail if the admin of Azure Active Directory has not already consented to use of the Enterprise App within the directory. **5**th step below describes the error that you might get if you are lacking admin consent and [Granting Admin Consent](#granting-admin-consent) walks you through steps to mitigate that problem. Please note, that you will still need to run the application once before granting the consent and that first run will fail.

We are working on simplifying this flow, but as of right now these are the steps to run `Microsoft.Azure.IIoT.Deployment`:

1. Run `Microsoft.Azure.IIoT.Deployment` application from command line:

    ``` bash
    ./Microsoft.Azure.IIoT.Deployment
    ```

    On Linux and MacOS you might need to assign execute permission to the application before you can run it. To do that run the following command first:

    ``` bash
    chmod +x Microsoft.Azure.IIoT.Deployment
    ```

2. The application will ask you to choose your Azure environment, please select one. Those are either Azure Global Cloud or government-specific independent deployments of Microsoft Azure.

    ``` bash
    Please select Azure environment to use:

    Available Azure environments:
    0: AzureGlobalCloud
    1: AzureChinaCloud
    2: AzureUSGovernment
    3: AzureGermanCloud
    Select Azure environment:
    0
    ```

3. The application will ask you to provide your Tenant Id. This is the same as Directory Id. To find out your Directory Id:

    * Go to Azure Portal
    * Then go to **Azure Active Directory** service
    * In Azure Active Directory service page, on the left side find **Properties** under **Manage** group. Copy Directory Id from `Properties` page.

    Then provide Tenant Id to the application:

    ``` bash
    Please provide your TenantId:
    XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
    ```

4. Now the application will ask you to login to Azure. During the first run, login flow will also ask your consent to provide permissions to `AzureIndustrialIoTDeployment` application. If you are not an admin in your Azure account, you would be asked to request consent from an admin.

    * On Windows, an interactive flow will launch a web browser and ask you to login to Azure.

    * On Linux and MacOS, a device login flow will be used which will ask you to manually open a web browser, go to Azure device login page and login with the code provided by the application. This can be done on a different machine if the one in question does not have a web browser. It will look something like this:

        ``` bash
        To sign in, use a web browser to open the page https://microsoft.com/devicelogin and enter the code XXXXXXXXX to authenticate.
        ```

5. At this point you might receive an error indicating that the user or administrator has not consented to use of the application, it will look like this:

    ``` bash
    [10:46:35 ERR] Failed to deploy Industrial IoT solution.
    System.AggregateException: One or more errors occurred. (AADSTS65001: The user or administrator has not consented to use the application with ID 'fb2ca262-60d8-4167-ac33-1998d6d5c50b' named 'AzureIndustrialIoTIAI'. Send an interactive authorization request for this user and resource.
    Trace ID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
    Correlation ID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
    Timestamp: 2019-11-11 09:46:35Z)
    ...
    ```

    If you encounter this go to [Granting Admin Consent](#granting-admin-consent) to resolve this issue and then try to run the application again.

6. Provide a name for the AAD applications that will be registered. You will be asked for one name, but 3 applications will be registered:

    * one with `-services` suffix added to provided name
    * one with `-clients` suffix added to provided name
    * one with `-aks` suffix added to provided name

    ``` bash
    Please provide a name for the AAD application to register:
    kkTestApp51
    ```

7. Select a Subscription within your Azure Account that will be used for creating Azure resources. If just one Subscription exists in your Account, then it will be listed and application will proceed further, otherwise you have to select Subscription from the list of available Subscriptions:

    ``` bash
    The following subscription will be used:
    SubscriptionId: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX, DisplayName: Visual Studio Enterprise
    ```

8. Select Resource Group. You would be presented with options of either using an existing Resource Group or
creating a new one. If you choose to use an existing one, you will be presented with a list of all Resource
Groups within the Subscription to choose from. Otherwise, you would be asked to select a Region for the new
Resource Group and provide a name for it. Output for the latter case is below.

    > **Note:** If the application encounters an error during execution, it will ask you whether to perform a
    cleanup or not. Cleanup works by deleting registered Applications and the Resource Group. This means
    if an existing Resource Group has been selected for the deployment and an error occurs then selecting to
    perform cleanup will also trigger deletion of all resources previously present in the Resource Group. More
    details can be found in [Resource Cleanup](#resource-cleanup).

    ``` bash
    Do you want to create a new ResourceGroup or use an existing one ? Please select N[new] or E[existing]
    n
    Please select region where resource group will be created.

    Available regions:
    0: eastus2
    1: westus2
    2: northeurope
    3: westeurope
    4: canadacentral
    5: centralindia
    6: southeastasia
    Select a region:
    3
    Select resource group name, press Enter to use 'kkTestApp51':

    [13:36:20 INF] Creating Resource Group: kkTestApp51 ...
    [13:36:21 INF] Created Resource Group: kkTestApp51
    ```

9. Now the application will perform application registration, Azure resource deployment and deployment of microservices to AKS cluster. This will take around 15 to 20 minutes depending on Azure Region. Along the way, it will log what Azure resources are created. Sample log looks like this:

    ``` bash
    [13:36:21 INF] Registering resource providers ...
    [13:36:24 INF] Registered resource providers
    [13:36:25 INF] Creating service application registration...
    [13:36:29 INF] Creating client application registration...
    [13:36:36 INF] Creating AKS application registration...
    [13:36:38 INF] Creating Azure Network Security Group: iiotservices-nsg ...
    [13:36:41 INF] Created Azure Network Security Group: iiotservices-nsg
    [13:36:41 INF] Creating Azure Virtual Network: iiotservices-vnet ...
    [13:36:45 INF] Created Azure Virtual Network: iiotservices-vnet
    [13:36:46 WRN] ServicePrincipal creation has not propagated correcty. Waiting for 120 seconds before retry.
    [13:38:48 INF] Creating Azure KeyVault: keyvault-50242 ...
    [13:39:20 INF] Created Azure KeyVault: keyvault-50242
    [13:39:20 INF] Adding certificate to Azure KeyVault: webAppCert ...
    [13:39:21 INF] Added certificate to Azure KeyVault: webAppCert
    [13:39:30 INF] Adding certificate to Azure KeyVault: aksClusterCert ...
    [13:39:30 INF] Added certificate to Azure KeyVault: aksClusterCert
    [13:39:44 INF] Creating Azure Operational Insights Workspace: workspace-72124 ...
    [13:39:44 INF] Creating Azure Application Insights Component: appinsights-85593 ...
    [13:39:58 INF] Created Azure Application Insights Component: appinsights-85593
    [13:40:20 INF] Created Azure Operational Insights Workspace: workspace-72124
    [13:40:20 INF] Creating Azure AKS cluster: akscluster-39852 ...
    [13:40:20 INF] Creating Azure Storage Account: storage96291 ...
    [13:40:38 INF] Created Azure Storage Account: storage96291
    [13:40:39 INF] Creating Blob Container: iothub-default ...
    [13:40:39 INF] Created Blob Container: iothub-default
    [13:40:39 INF] Creating Azure IoT Hub: iothub-13998 ...
    [13:42:12 INF] Created Azure IoT Hub: iothub-13998
    [13:42:28 INF] Creating Azure CosmosDB Account: cosmosdb-98554 ...
    [13:42:28 INF] Creating Azure Service Bus Namespace: sb-32960 ...
    [13:42:28 INF] Creating Azure Event Hub Namespace: eventhubnamespace-87937 ...
    [13:43:34 INF] Created Azure Service Bus Namespace: sb-32960
    [13:43:35 INF] Created Azure Event Hub Namespace: eventhubnamespace-87937
    [13:43:35 INF] Creating Azure Event Hub: eventhub-65619 ...
    [13:43:38 INF] Created Azure Event Hub: eventhub-65619
    [13:43:38 INF] Creating Azure AppService Plan: kktestapp51-42681 ...
    [13:43:47 INF] Created Azure AppService Plan: kktestapp51-42681
    [13:43:47 INF] Creating Azure AppService: kkTestApp51 ...
    [13:47:48 INF] Created Azure AppService: kkTestApp51
    [13:48:28 INF] Created Azure AKS cluster: akscluster-39852
    [13:49:07 INF] Created Azure CosmosDB Account: cosmosdb-98554
    [13:49:10 INF] Deploying Industrial IoT microservices to Azure AKS cluster ...
    [13:49:11 INF] Deployed Industrial IoT microservices to Azure AKS cluster
    ```

10. After that you would be provided with an option to save connection details of deployed resources to '.env' file. If you choose to do so, please store the file is a secure location afterwards since it contains sensitive data such as connection string to the resources and some secrets.

    ``` bash
    [13:49:11 INF] Deployed Industrial IoT microservices to Azure AKS cluster
    Do you want to save connection details of deployed resources to '.env' file ? Please select Y[yes] or N[no]
    y
    [13:49:17 INF] Writing environment to file: '.env' ...
    ```

11. At this point the application should have successfully deployed Industrial IoT solution. One last thing left to do is to manually lower the scale of created CosmosDB containers. This is not a functional issue, but the default size of created containers will cost ~60$ per day. Check [CosmosDB Scale](#cosmosdb-scale) to learn more and lower the cost.

### Granting Admin Consent

To grant admin consent you have to be **admin** in the Azure account. Here are the steps to do this:

* Go to Azure Portal
* Then go to **Enterprise Applications** service
* Find `AzureIndustrialIoTDeployment` in the list of applications and select it
* On the left side find **Permissions** under **Security** group and select it. It will show list of permissions currently granted to `AzureIndustrialIoTDeployment`.
* Click on `Grant admin consent for <your-directory-name>` button to grant consent for the application to manage Azure resources. It will show you a prompt with some additional permissions that `AzureIndustrialIoTDeployment` has requested.

### Resource Cleanup

`Microsoft.Azure.IIoT.Deployment` will prompt for resource cleanup if it encounters an error during
deployment of Industrial IoT platform. Cleanup works by deleting registered Applications and the
Resource Group that has been selected for deployment. This means, that one should be cautious with
cleanup if an existing Resource Group has been selected for the deployment, since the cleanup will
trigger deletion of **all** resources within the Resource Group, even the ones that have been present
before execution of `Microsoft.Azure.IIoT.Deployment`. We will introduce more granular cleanup in the
next iterations of the application.

Cleanup prompt looks like this:

```bash
Do you want to delete registered Applications and the Resource Group ? Please select Y[yes] or N[no]
y
[11:24:48 INF] Initiated deletion of Resource Group: IAIDeployment
[11:24:48 INF] Deleting application: IAIDeployment-services ...
[11:24:57 INF] Deleted application: IAIDeployment-services
[11:24:58 INF] Deleting application: IAIDeployment-clients ...
[11:25:03 INF] Deleted application: IAIDeployment-clients
[11:25:03 INF] Deleting application: IAIDeployment-aks ...
[11:25:04 INF] Deleted application: IAIDeployment-aks
```

## Deployed Resources

### AKS

All of microservices of Industrial IoT solution are deployed to an AKS Kubernetes cluster. The deployment creates `industrial-iot` namespace where all microservices are running. To provide connection details for all Azure resources created by `Microsoft.Azure.IIoT.Deployment`, we created `industrial-iot-env` secret, which is then consumed by deployments.

To see YAML files of all Kubernetes resources that are created by the application check [deploy/src/Microsoft.Azure.IIoT.Deployment/Resources/aks/](../deploy/src/Microsoft.Azure.IIoT.Deployment/Resources/aks/) directory.

To see state of microservices you can check Kubernetes Dashboard. Follow this tutorial on how to access it:

* [Access the Kubernetes web dashboard in Azure Kubernetes Service (AKS)](https://docs.microsoft.com/azure/aks/kubernetes-dashboard)

## Missing And Planned Features

### Connectivity To AKS

Currently we are missing the connection from App Services to deployed microservices which are running in AKS cluster. This is required if you want to access APIs exposed by microservices for debugging or exploratory purposes. The connectivity will be added in the next iteration of the application. Currently it is possible to manually setup connection.

You will need to have [Azure CLI](https://docs.microsoft.com/cli/azure/?view=azure-cli-latest) installed for next steps.

* [Install the Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli?view=azure-cli-latest)

Setup connection between App Service and AKS cluster:

1. Install [kubectl](https://kubernetes.io/docs/reference/kubectl/overview/) on you local machine:

    * To install `kubectl` locally, use the [az aks install-cli](https://docs.microsoft.com/cli/azure/aks?view=azure-cli-latest#az-aks-install-cli) command:

        ```bash
        az aks install-cli
        ```

    * Or follow steps on [Kubernetes documentation](https://kubernetes.io/docs/tasks/tools/install-kubectl/).

2. Setup connection between `kubectl` and AKS Kubernetes cluster:

    * To configure `kubectl` to connect to your Kubernetes cluster, use the [az aks get-credentials](https://docs.microsoft.com/cli/azure/aks?view=azure-cli-latest#az-aks-get-credentials) command:

        ``` bash
        az aks get-credentials --resource-group myResourceGroup --name myAKSCluster
        ```

3. Install [Helm](https://helm.sh/) on you local machine:

    * Follow [Helm documentation](https://helm.sh/docs/using_helm/#install-helm).

4. Setup connection between Helm and AKS Kubernetes cluster and perform helm init. Please follow steps in the tutorial:

    * [Install applications with Helm in Azure Kubernetes Service (AKS)](https://docs.microsoft.com/azure/aks/kubernetes-helm)

5. Create NGINX Ingress controller. Please follow the steps in the tutorial below but with changes listed under it:

    [Create an HTTPS ingress controller and use your own TLS certificates on Azure Kubernetes Service (AKS)](https://docs.microsoft.com/azure/aks/ingress-own-tls)

    1. Instead of using `ingress-basic` namespace use `industrial-iot`. So you don't have to create a new namespace.
    2. Instead of manually **generating a TLS certificate** (step 2) and **creating a Kubernetes Secret for the certificate** (step 3) use an existing `web-app` secret in `industrial-iot` namespace as [default ssl certificate](https://kubernetes.github.io/ingress-nginx/user-guide/tls/#default-ssl-certificate).
    `web-app` secret will already be created by `Microsoft.Azure.IIoT.Deployment` application. So the `helm install` command in **Create an ingress controller** (step 1) should look like this:

        ```bash
        # Use Helm to deploy an NGINX ingress controller
        helm install stable/nginx-ingress \
            --namespace industrial-iot \
            --set controller.replicaCount=2 \
            --set controller.nodeSelector."beta\.kubernetes\.io/os"=linux \
            --set defaultBackend.nodeSelector."beta\.kubernetes\.io/os"=linux \
            --set controller.extraArgs.default-ssl-certificate=industrial-iot/web-app
        ```

    3. Skip **Run demo applications** (step 4).
    4. For step 5, **Create an ingress route**, use [30_industrial_iot_ingress.yaml](../deploy/src/Microsoft.Azure.IIoT.Deployment/Resources/aks/30_industrial_iot_ingress.yaml)
    file or the snippet bellow. Stop after this step.

        ```yaml
        apiVersion: extensions/v1beta1
        kind: Ingress
        metadata:
          name: industrial-iot-ingress
          namespace: industrial-iot
          annotations:
            kubernetes.io/ingress.class: nginx
            nginx.ingress.kubernetes.io/rewrite-target: /$1
          labels:
            app.kubernetes.io/name: industrial-iot-ingress
            app.kubernetes.io/part-of: industrial-iot
            app.kubernetes.io/version: 2.5.1
            app.kubernetes.io/managed-by: Microsoft.Azure.IIoT.Deployment
        spec:
          rules:
          - http:
              paths:
              - path: /registry/(.*)
                backend:
                  serviceName: registry-service
                  servicePort: 9042
              - path: /twin/(.*)
                backend:
                  serviceName: twin-service
                  servicePort: 9041
              - path: /history/(.*)
                backend:
                  serviceName: history-service
                  servicePort: 9043
              - path: /ua/(.*)
                backend:
                  serviceName: gateway-service
                  servicePort: 9040
              - path: /vault/(.*)
                backend:
                  serviceName: vault-service
                  servicePort: 9044
        ```

    After completing above steps you should be able to see a Load Balancer and a Public IP Address being created in the Resource Group managed by AKS cluster.
    When creating an AKS cluster, Azure will create a new Resource Group which will contain managed Azure resources that are required to run a Kubernetes cluster, such as VMs, Network Interfaces ans so on.
    Name of the Resource Groups managed by your AKS cluster should look something like this:

    * MC_\<resource_group_name\>\_\<aks-cluster-name\>\_\<region\>

    Get IP address of Public IP Address service, we will need it for next step.

6. Set up connection between App Service and Public IP Address

    * Go to Azure Portal.
    * Go to **Resource Groups** service.
    * Find and select Resource Group that was used for deployment.
    * Within the Resource Group find instance of **App Service** and select it. You can also find the name of App Service in logs of the deployment.
    * Under **Settings** group select **Configuration**.
    * Find **REMOTE_ENDPOINT** setting and edit its value to point to IP address of Public IP Address service that was created in the previous step. Do not forget to have `https://` prefix before the address. Save your change.
    * Go to **Overview** of you App Service, find **Restart** button on top and restart the service instance.

Now you should be able to access APIs of microservices through URL of App Service. The URL is available in overview of App Service. You can try to access OPC Registry Service by appending `/registry/` to the URL. It should look something like this:

* https://kktestapp51.azurewebsites.net/registry/

Where kktestapp51 is the name of App Service.

### Non-admin Users

Currently only admin users in Azure directory can run `Microsoft.Azure.IIoT.Deployment` application. We will add support of non-admin users in next iterations.

### Configuring Azure Resources

Currently `Microsoft.Azure.IIoT.Deployment` application deploys predefined Azure resources. In the next iterations we will provide a way to re-use existing Azure resources and specifying you own definitions of resources.

## Known Issues

### CosmosDB Scale

`Microsoft.Azure.IIoT.Deployment` deploys version `2.5.1` of Industrial IoT platform which includes deployment of CosmosDB databases and containers within it. The solution creates 2 CosmosDB databases with 3 containers which are used by microservices:

* `iiot_opc` database, which contains the following containers:
  * `iiot_opc-indices`
  * `iiot_opc-requests`
* `OpcVault` database, which contains the following container:
  * `AppsAndCertRequests`

By default, the containers are created with [throughput allocation of 10000 RU/s](https://docs.microsoft.com/azure/cosmos-db/request-units) which each costs around 20$ daily. The next version of the platform will create containers with lower throughput allocation, but currently you have to manually lower the cost of throughput allocation. To do that follow these steps:

1. Go to Azure Portal.
2. Go to **Resource Groups**.
3. Find the Resource Groups that has been selected during the deployment and select it.
4. Within the Resource Group, find and select instance of CosmosDB that has been deployed. Deployment logs will contain the name of ConsmosBD, which will be something like `cosmosdb-98554`. Last 5 characters will be different for each deployment.
5. On the left side find **Scale** under **Containers** group and select it.
6. Then for each container listed above change the value of **Throughput** from 10000 to 400 and Save it.

## Resources

### AKS Docs

* [Access the Kubernetes web dashboard in Azure Kubernetes Service (AKS)](https://docs.microsoft.com/azure/aks/kubernetes-dashboard)
* [Install applications with Helm in Azure Kubernetes Service (AKS)](https://docs.microsoft.com/azure/aks/kubernetes-helm)
* [Create an HTTPS ingress controller and use your own TLS certificates on Azure Kubernetes Service (AKS)](https://docs.microsoft.com/azure/aks/ingress-own-tls)

### CosmosDB Docs

* [Request Units in Azure Cosmos DB](https://docs.microsoft.com/azure/cosmos-db/request-units)
