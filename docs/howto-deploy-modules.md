# Deploy OPC Twin and OPC Publisher Edge Modules

[Home](readme.md)

This article explains how to deploy the Industrial IoT Edge modules to [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/).

There are several options to deploy modules to your [Azure IoT Edge](https://azure.microsoft.com/en-us/services/iot-edge/) Gateway, among them

- [Deploying from Azure Portal's IoT Edge blade](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-portal)
- [Deploying using AZ CLI](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-monitor-cli)

## Deployment manifest

All modules are deployed using a deployment manifest.  An example manifest to deploy both [OPC Publisher](publisher.md) and [OPC Twin](twin.md) is shown below.

```json
{
  "modulesContent": {
    "$edgeAgent": {
      "properties.desired": {
        "schemaVersion": "1.0",
        "runtime": {
          "type": "docker",
          "settings": {
            "minDockerVersion": "v1.25",
            "loggingOptions": "",
            "registryCredentials": {}
          }
        },
        "systemModules": {
          "edgeAgent": {
            "type": "docker",
            "settings": {
              "image": "mcr.microsoft.com/azureiotedge-agent:1.0",
              "createOptions": ""
            }
          },
          "edgeHub": {
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/azureiotedge-hub:1.0",
              "createOptions": "{\"HostConfig\":{\"PortBindings\":{\"5671/tcp\":[{\"HostPort\":\"5671\"}], \"8883/tcp\":[{\"HostPort\":\"8883\"}],\"443/tcp\":[{\"HostPort\":\"443\"}]}}}"
            }
          }
        },
        "modules": {
          "opctwin": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-twin:latest",
              "createOptions": "{\"Hostname\":\"opctwin\",\"Cmd\":[\"PkiRootPath=\\\\mount\\\\pki\",\"AutoAccept=true\"],\"NetworkingConfig\":{\"EndpointsConfig\":{\"host\":{}}},\"HostConfig\":{\"NetworkMode\":\"host\",\"CapAdd\":[\"NET_ADMIN\"],\"Mounts\":[{\"Type\":\"bind\",\"Source\":\"C:\\\\IoTEdgeMapping\",\"Target\":\"C:\\\\mount\"}]}}"               }
          },
          "opcpublisher": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-publisher:latest",
              "createOptions": "{\"Hostname\":\"opcpublisher\",\"Cmd\":[\"PkiRootPath=\\\\mount\\\\pki\",\"AutoAccept=true\"],\"HostConfig\":{\"Mounts\":[{\"Type\":\"bind\",\"Source\":\"C:\\\\IoTEdgeMapping\",\"Target\":\"C:\\\\mount\"}]}}"            }
          }
        }
      }
    },
    "$edgeHub": {
      "properties.desired": {
        "schemaVersion": "1.0",
        "routes": {
          "opctwinToIoTHub": "FROM /messages/modules/opctwin/* INTO $upstream",
          "opcpublisherToIoTHub": "FROM /messages/modules/opcpublisher/* INTO $upstream"
        },
        "storeAndForwardConfiguration": {
          "timeToLiveSecs": 7200
        }
      }
    }
  }
}
```

## Deploying from Azure Portal

The easiest way to deploy the modules to an Azure IoT Edge gateway device is through the Azure Portal.  

### Prerequisites

1. Deploy the OPC Twin [dependencies](services/dependencies.md) and obtained the resulting `.env` file. Note the deployed `hub name` of the `PCS_IOTHUBREACT_HUB_NAME` variable in the `.env` file.

2. For a windows Windows IoT Edge runtime deoplyment:
	* Hyper-V must be active  
	* Create a new virtual switch named host having attached to an external network interface (e.g. "Ethernet 2").
	```bash
	New-VMSwitch -name host -NetAdapterName "<Adapter Name>" -AllowManagementOS $true
	```

3. Register and start a [Linux](https://docs.microsoft.com/azure/iot-edge/how-to-install-iot-edge-linux) or [Windows](https://docs.microsoft.com/azure/iot-edge/how-to-install-iot-edge-windows) IoT Edge gateway and note its `device id`.

4. Validate the presence of `host` network in the docker instance 
	
	Linux containers:
	```bash
	docker network ls
		NETWORK ID          NAME                DRIVER              SCOPE
		beceb3bd61c4        azure-iot-edge      bridge              local
		97eccb2b9f82        bridge              bridge              local
		758d949c5343        host                host                local
		72fb3597ef74        none                null                local
	```
	
	Windows containers:
	```bash
	docker -H npipe:////.//pipe//iotedge_moby_engine network ls
		NETWORK ID          NAME                DRIVER              SCOPE
		8e0ea888dbd4        host                transparent         local
		f3390c998f90        nat                 nat                 local
		6750449db22d        none                null                local
	```
	
	When running the industrial iot edge modules in host (transparent) network, the containers will require IP addresses assignment. There are 2 possibilitues : 
	* dynamic IP address from a local DHCP server accessible from the host's network interface associated to the container's 'host' network.  
	* static IP address assigned on the container create options statement
	
		Windows Static IP Example:		
		In order to allow static IP address assignment on a container, the docker network requires to be created having the the subnet speciffied identical to the host's interface
				
		```bash 
		docker -H npipe:////.//pipe//iotedge_moby_engine network create -d transparent -o com.docker.network.windowsshim.interface="Ethernet 2" -o com.docker.network.windowsshim.networkname=host --subnet=192.168.30.0/24 --gateway=192.168.30.1 host
		```
		Furthermore, the container's create option will look like:
		
		```json 
		"createOptions": "{\"Hostname\":\"opctwin\",\"Cmd\":[\"PkiRootPath=\\\\mount\\\\pki\",\"AutoAccept=true\"],\"NetworkingConfig\":{\"EndpointsConfig\":{\"host\":{\"IPAMConfig\":{\"IPv4Address\":\"192.168.30.100\"}}}},\"HostConfig\":{\"NetworkMode\":\"host\",\"CapAdd\":[\"NET_ADMIN\"],\"Mounts\":[{\"Type\":\"bind\",\"Source\":\"C:\\\\IoTEdgeMapping\",\"Target\":\"C:\\\\mount\"}]}}"
		```
		


### Deploy to Edge device

1. Sign in to the [Azure portal](https://portal.azure.com/) and navigate to your IoT hub.

2. Select **IoT Edge** from the left-hand menu.

3. Click on the ID of the target device from the list of devices.

4. Select **Set Modules**.

5. In the **Deployment modules** section of the page, select **Add** and **IoT Edge Module.**

6. In the **IoT Edge Custom Module** dialog use `opctwin` as name for the module, then specify the container *image URI* as

   ```bash
   mcr.microsoft.com/iotedge/opc-twin:latest
   ```

   As *create options* use the following JSON:

   ```json
   {"NetworkingConfig":{"EndpointsConfig":{"host":{}}},"HostConfig":{"NetworkMode":"host","CapAdd":["NET_ADMIN"]}}
   ```

   Fill out the optional fields if necessary. For more information about container create options, restart policy, and desired status see [EdgeAgent desired properties](https://docs.microsoft.com/azure/iot-edge/module-edgeagent-edgehub#edgeagent-desired-properties). For more information about the module twin see [Define or update desired properties](https://docs.microsoft.com/azure/iot-edge/module-composition#define-or-update-desired-properties).

7. Select **Save** and repeat step **5**.  

8. In the IoT Edge Custom Module dialog, use `opcpublisher` as name for the module and the container *image URI* as

   ```bash
   mcr.microsoft.com/iotedge/opc-publisher:latest
   ```

   As *create options* use the following JSON:

   ```json
   {"Hostname":"publisher","Cmd":["publisher","--aa"]}
   ```

9. Select **Save** and then **Next** to continue to the routes section.

10. In the routes tab, paste the following

    ```json
    {
      "routes": {
        "opctwinToIoTHub": "FROM /messages/modules/opctwin/* INTO $upstream",
        "opcpublisherToIoTHub": "FROM /messages/modules/opcpublisher/* INTO $upstream"
      }
    }
    ```

    and select **Next**

11. Review your deployment information and manifest.  It should look like the above [deployment manifest](#Deployment-manifest).  Select **Submit**.

12. Once you've deployed modules to your device, you can view all of them in the **Device details** page of the portal. This page displays the name of each deployed module, as well as useful information like the deployment status and exit code.

## Deploying using AZ CLI

### Prerequisites

1. Ensure you have the prerequisites for [deploying modules using Azure portal](#Deploying-from-Azure-Portal).

2. Install the [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli).  You must have at least `v2.0.24`, which you can verify with `az --version`.

3. Add the [IoT Edge Extension](https://github.com/Azure/azure-iot-cli-extension/) with the following commands:

    ```bash
    az extension add --name azure-cli-iot-ext
    ```

### Quick start

1. Save the above [deployment manifest](#Deployment-manifest) into a `deployment.json` file.  

2. Use the following command to apply the configuration to an IoT Edge device:

   ```bash
   az iot edge set-modules --device-id [device id] --hub-name [hub name] --content ./deployment.json
   ```

   The `device id` parameter is case-sensitive. The content parameter points to the deployment manifest file that you saved.
    ![az iot edge set-modules output](https://docs.microsoft.com/azure/iot-edge/media/how-to-deploy-cli/set-modules.png)

3. Once you've deployed modules to your device, you can view all of them with the following command:

   ```bash
   az iot hub module-identity list --device-id [device id] --hub-name [hub name]
   ```

   The device id parameter is case-sensitive. ![az iot hub module-identity list output](https://docs.microsoft.com/azure/iot-edge/media/how-to-deploy-cli/list-modules.png)

## Next steps

* [Deploy and monitor Edge modules at scale](https://docs.microsoft.com/azure/iot-edge/how-to-deploy-monitor)
* [Learn more about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)
