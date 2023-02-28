# Deploying OPC Publisher and Industrial IoT services

[Home](../readme.md)

## Deploy OPC Publisher

This article explains how to deploy the OPC Publisher module to [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) using the Azure Portal and Marketplace.

Before you begin, make sure you followed the [instructions to set up a IoT Edge device](howto-install-iot-edge.md) and have a running IoT Edge Gateway.

### Using Azure CLI

1. Obtain the IoT Hub name and device id of the [installed IoT Edge](howto-install-iot-edge.md) Gateway.

1. Install the [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli).  You must have at least `v2.0.24`, which you can verify with `az --version`.

1. Add the [IoT Edge Extension](https://github.com/Azure/azure-iot-cli-extension/) with the following commands:

    ```bash
    az extension add --name azure-cli-iot-ext
    ```

To deploy all required modules using Az...  

1. Save the [deployment manifest](deployment-manifest.md) into a `deployment.json` file.  

1. Use the following command to apply the configuration to an IoT Edge device:

   ```bash
   az iot edge set-modules --device-id [device id] --hub-name [hub name] --content ./deployment.json
   ```

   The `device id` parameter is case-sensitive. The content parameter points to the deployment manifest file that you saved.
    ![az iot edge set-modules output](https://docs.microsoft.com/azure/iot-edge/media/how-to-deploy-cli/set-modules.png)

1. Once you've deployed modules to your device, you can view all of them with the following command:

   ```bash
   az iot hub module-identity list --device-id [device id] --hub-name [hub name]
   ```

   The device id parameter is case-sensitive. ![az iot hub module-identity list output](https://docs.microsoft.com/azure/iot-edge/media/how-to-deploy-cli/list-modules.png)

More information about az and IoT Edge can be found [here](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-monitor-cli).

### Using the Azure Portal

To deploy all required modules to the Gateway using the Azure Portal...

1. Sign in to the [Azure portal](https://portal.azure.com/) and navigate to the IoT Hub deployed earlier.

   > If you deploy using [the deployment script](#optional-deploying-the-industrial-iot-platform) then a simple way to locate your IoT Hub is to find the resource group variable in your `.env` file.  This resource group contains the IoT Hub.

2. Select **IoT Edge** from the left-hand menu.

3. Click on the ID of the target device from the list of devices.

4. Select **Set Modules**.

5. In the **Deployment modules** section of the page, select **Add** and **IoT Edge Module.**

6. In the **IoT Edge Custom Module** dialog use `discovery` as name for the module, then specify the container *image URI* as

   ```bash
   mcr.microsoft.com/iotedge/discovery:2.8.4
   ```

   As *create options* use the following JSON:

   ```json
   {"NetworkingConfig":{"EndpointsConfig":{"host":{}}},"HostConfig":{"NetworkMode":"host","CapAdd":["NET_ADMIN"],
   "CapDrop":["CHOWN", "SETUID"]}}
   ```

   Fill out the optional fields if necessary. For more information about container create options, restart policy, and desired status see [EdgeAgent desired properties](https://docs.microsoft.com/azure/iot-edge/module-edgeagent-edgehub#edgeagent-desired-properties). For more information about the module twin see [Define or update desired properties](https://docs.microsoft.com/azure/iot-edge/module-composition#define-or-update-desired-properties).

7. Select **Save** and repeat step **5**.

8. In the **IoT Edge Custom Module** dialog use `twin` as name for the module, then specify the container *image URI* as

   ```bash
   mcr.microsoft.com/iotedge/opc-twin:2.8.4
   ```

   Leave the *create options* empty.

9. Select **Save** and repeat step **5**.

10. In the IoT Edge Custom Module dialog, use `publisher` as name for the module and the container *image URI* as

    ```bash
    mcr.microsoft.com/iotedge/opc-publisher:2.8.4
    ```

    Leave the *create options* empty.

11. Select **Save** and then **Next** to continue to the routes section.

12. In the routes tab, paste the following

    ```json
    {
      "routes": {
        "twinToUpstream": "FROM /messages/modules/twin/* INTO $upstream",
        "discoveryToUpstream": "FROM /messages/modules/discovery/* INTO $upstream",
        "publisherToUpstream": "FROM /messages/modules/publisher/* INTO $upstream",
        "leafToUpstream": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO $upstream"
      }
    }
    ```

    and select **Next**

13. Review your deployment information and manifest.  It should look like this [deployment manifest](deployment-manifest.md).  Select **Submit**.

14. Once you've deployed modules to your device, you can view all of them in the **Device details** page of the portal. This page displays the name of each deployed module, as well as useful information like the deployment status and exit code.

15. Add your own or other modules from the Azure Marketplace using the steps above.

For more in depth information check out [the Azure IoT Edge Portal documentation](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-portal).

## (Optional) Deploy the Industrial IoT Platform

The simplest way to get started is to deploy the [Azure Industrial IoT OPC Publisher, Services and Simulation demonstrator using the deployment script](howto-deploy-all-in-one.md).

Unless you decide otherwise, it also deploys 2 simulated Edge Gateways and assets.

> The all in one hosting option is intended as a quick start solution. For production deployments that require staging, rollback, scaling and resilience you should deploy the platform into Kubernetes as explained [here](howto-deploy-aks.md).

The deployment script allows to select which set of components to deploy using deployment types. The dependencies and deployments types are explained below.

- Minimum dependencies:

  - 1 [IoT Hub](https://azure.microsoft.com/services/iot-hub/) to communicate with the edge and ingress raw OPC UA telemetry data
  - 1 [Key Vault](https://azure.microsoft.com/services/key-vault/), Premium SKU (to manage secrets and certificates)
  - 1 [Blob Storage](https://azure.microsoft.com/services/storage/) V2, Standard LRS SKU (for event hub checkpointing)
  - App Service Plan, 1 [App Service](https://azure.microsoft.com/services/app-service/), B1 SKU for hosting the cloud micro-services [all-in-one](../services/all-in-one.md)
  - App Service Plan (shared with microservices), 1 [App Service](https://azure.microsoft.com/services/app-service/) for hosting the Industrial IoT Engineering Tool cloud application

- Simulation:

  - 1 [Device Provisioning Service](https://docs.microsoft.com/azure/iot-dps/), S1 SKU (used for deploying and provisioning the simulation gateways)
  - [Virtual machine](https://azure.microsoft.com/services/virtual-machines/), Virtual network, IoT Edge used for a factory simulation to show the capabilities of the platform and to generate sample telemetry. By default, 4 [Virtual Machines](https://azure.microsoft.com/services/virtual-machines/), 2 B2 SKU (1 Linux IoT Edge gateway and 1 Windows IoT Edge gateway) and 2 B1 SKU (factory simulation).

- [Azure Kubernetes Service](howto-deploy-aks.md) should be used to host the cloud microservices

The types of deployments are the following:

- `minimum`: Minimum dependencies
- `local`: Minimum and Standard dependencies
- `services`: `local` and Microservices
- `simulation`: Minimum dependencies and Simulation components
- `app`: `services` and UI components
- `all` (default): all the above components

## Other hosting and deployment methods

Alternative options to deploy the platform services include:

- Deploying Azure Industrial IoT Platform to [Azure Kubernetes Service (AKS)](howto-deploy-aks.md) as production solution.
- Deploying Azure Industrial IoT Platform microservices into an existing Kubernetes cluster [using Helm](howto-deploy-helm.md).
- Deploying [Azure Kubernetes Service (AKS) cluster on top of Azure Industrial IoT Platform created by deployment script and adding Azure Industrial IoT components into the cluster](howto-add-aks-to-ps1.md).
- For development and testing purposes, you can also [deploy only the Microservices dependencies in Azure](howto-deploy-local.md) and [run Microservices locally](howto-run-microservices-locally.md).
