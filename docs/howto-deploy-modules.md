# Deploy Industrial IoT Edge Modules

[Home](readme.md)

This article explains how to deploy the Industrial IoT Edge modules to [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/).

There are several options to deploy modules to your [Azure IoT Edge](https://azure.microsoft.com/en-us/services/iot-edge/) Gateway, among them

- [Deploying from Azure Portal's IoT Edge blade](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-portal)
- [Deploying using AZ CLI](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-monitor-cli)

## Prerequisites

1. [Deploy](howto-deploy-microservices.md) the Industrial IoT platform or at a minimum the required [dependencies](services/dependencies.md) and obtained the resulting `.env` file. Note the resource group name in the `.env` file to find the IoT Hub resource in the portal view.  

2. For a windows Windows IoT Edge runtime deployment:
    - Hyper-V must be active  
    - Create a new virtual switch named host having attached to an external network interface (e.g. "Ethernet 2").
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

    When running the industrial IoT Edge modules in host (transparent) network, the containers will require IP addresses assignment. There are 2 possibilities: 
    - dynamic IP address from a local DHCP server accessible from the host's network interface associated to the container's 'host' network.  
    - static IP address assigned on the container create options statement

    Windows Static IP Example:
    In order to allow static IP address assignment on a container, the docker network requires to be created having the the subnet specified identical to the host's interface

    ```bash
    docker -H npipe:////.//pipe//iotedge_moby_engine network create -d transparent -o com.docker.network.windowsshim.interface="Ethernet 2" -o com.docker.network.windowsshim.networkname=host --subnet=192.168.30.0/24 --gateway=192.168.30.1 host
    ```
    Furthermore, the container's create option will look like:

    ```json
    "createOptions": "{\"Hostname\":\"opctwin\",\"NetworkingConfig\":{\"EndpointsConfig\":{\"host\":{\"IPAMConfig\":{\"IPv4Address\":\"192.168.30.100\"}}}},\"HostConfig\":{\"NetworkMode\":\"host\",\"CapAdd\":[\"NET_ADMIN\"]}}"
    ```

## Deploy using Industrial IoT Edge Management service

> If you have **only** deployed the cloud dependencies, start the [Edge Management](services/edgemanager.md) Microservice or the [all-in-one](services/all-in-one.md) service locally to ensure IoT Hub is configured to auto deploy the Industrial IoT Edge modules.   

The service will set up layered deployments for each required module.   These layered deployment configurations will be automatically applied to any gateway with the following Device Twin tags:

```JSON
"tags" = {
    "__type__" = "iiotedge"
    "os" = "Windows" // or "Linux"
}
```

You can assign these tags to the IoT Edge device's twin [when you register the IoT Edge device](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-register-device).  

It can also be created as part of a Azure Device Provisioning (DPS) enrollment.  An example of the latter can be found in `/deploy/scripts/dps-enroll.ps1`.

By default the "latest" tag from mcr.microsoft.com is deployed.   This corresponds to the latest stable release.  

> If you need to point to a different docker container registry, you can configure the source using  environment variables `PCS_DOCKER_SERVER`, `PCS_DOCKER_USER`, `PCS_DOCKER_PASSWORD`, `PCS_IMAGE_NAMESPACE`, for example in your `.env` file (which can also be set during deployment) and restart the edge management or all-in-one service. 

## Individual module deployment using Azure Portal

The easiest way to deploy the modules to an Azure IoT Edge gateway device is through the Azure Portal.  

1. Sign in to the [Azure portal](https://portal.azure.com/) and navigate to your IoT hub.

2. Select **IoT Edge** from the left-hand menu.

3. Click on the ID of the target device from the list of devices.

4. Select **Set Modules**.

5. In the **Deployment modules** section of the page, select **Add** and **IoT Edge Module.**

6. In the **IoT Edge Custom Module** dialog use `discovery` as name for the module, then specify the container *image URI* as

   ```bash
   mcr.microsoft.com/iotedge/discovery:latest
   ```

   As *create options* use the following JSON:

   ```json
   {"NetworkingConfig":{"EndpointsConfig":{"host":{}}},"HostConfig":{"NetworkMode":"host","CapAdd":["NET_ADMIN"]}}
   ```

   Fill out the optional fields if necessary. For more information about container create options, restart policy, and desired status see [EdgeAgent desired properties](https://docs.microsoft.com/azure/iot-edge/module-edgeagent-edgehub#edgeagent-desired-properties). For more information about the module twin see [Define or update desired properties](https://docs.microsoft.com/azure/iot-edge/module-composition#define-or-update-desired-properties).

7. Select **Save** and repeat step **5**.  

8. In the **IoT Edge Custom Module** dialog use `opctwin` as name for the module, then specify the container *image URI* as

   ```bash
   mcr.microsoft.com/iotedge/opc-twin:latest
   ```

   Leave the *create options* empty.

9. Select **Save** and repeat step **5**.  

10. In the IoT Edge Custom Module dialog, use `opcpublisher` as name for the module and the container *image URI* as

   ```bash
   mcr.microsoft.com/iotedge/opc-publisher:latest
   ```

   Leave the *create options* empty.

11. Select **Save** and then **Next** to continue to the routes section.

12. In the routes tab, paste the following

    ```json
    {
      "routes": {
        "opctwinToIoTHub": "FROM /messages/modules/opctwin/* INTO $upstream",
        "opcpublisherToIoTHub": "FROM /messages/modules/opcpublisher/* INTO $upstream",
        "discoveryToIoTHub": "FROM /messages/modules/discovery/* INTO $upstream"
      }
    }
    ```

    and select **Next**

13. Review your deployment information and manifest.  It should look like the above [deployment manifest](#Deployment-manifest).  Select **Submit**.

14. Once you've deployed modules to your device, you can view all of them in the **Device details** page of the portal. This page displays the name of each deployed module, as well as useful information like the deployment status and exit code.

## Deployment manifest

All modules are deployed using a deployment manifest.  

### Linux

An example manifest to deploy [Discovery](modules/discovery.md) module, [OPC Publisher](modules/publisher.md) and [OPC Twin](modules/twin.md) to a Linux IoT Edge gateway is shown below.

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
          "discovery": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/discovery:latest",
              "createOptions": "{\"Hostname\":\"opctwin\",\"NetworkingConfig\":{\"EndpointsConfig\":{\"host\":{}}},\"HostConfig\":{\"NetworkMode\":\"host\",\"CapAdd\":[\"NET_ADMIN\"]}}"
            }
          },
          "opctwin": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-twin:latest"
            }
          },
          "opcpublisher": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-publisher:latest"
            }
          }
        }
      }
    },
    "$edgeHub": {
      "properties.desired": {
        "schemaVersion": "1.0",
        "routes": {
          "opctwinToIoTHub": "FROM /messages/modules/opctwin/* INTO $upstream",
          "discoveryToIoTHub": "FROM /messages/modules/discovery/* INTO $upstream",
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

### Windows

An example manifest to deploy to Windows IoT Edge gateway is shown below.

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
          "discovery": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-twin:latest",
              "createOptions": "{\"Hostname\":\"opctwin\",\"HostConfig\":{\"CapAdd\":[\"NET_ADMIN\"]}}"
            }
          },
          "opctwin": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-twin:latest"
            }
          },
          "opcpublisher": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-publisher:latest"
            }
          }
        }
      }
    },
    "$edgeHub": {
      "properties.desired": {
        "schemaVersion": "1.0",
        "routes": {
          "opctwinToIoTHub": "FROM /messages/modules/opctwin/* INTO $upstream",
          "discoveryToIoTHub": "FROM /messages/modules/discovery/* INTO $upstream",
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

- [Deploy and monitor Edge modules at scale](https://docs.microsoft.com/azure/iot-edge/how-to-deploy-monitor)
- [Learn more about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)
