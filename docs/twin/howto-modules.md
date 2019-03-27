# Deploy OPC Twin and OPC Publisher Edge Modules

This article explains how to deploy the Industrial IoT Edge modules to [Azure IoT Edge](https://azure.microsoft.com/en-us/services/iot-edge/). 

## Deployment manifest

All modules are deployed using a deployment manifest.  An example manifest to deploy both [OPC Publisher](https://github.com/Azure/iot-edge-opc-publisher) and [OPC Twin](module.md) is shown below, but can also be found in the root of the [OPC Twin Module repository](https://github.com/Azure/azure-iiot-opc-twin-module).

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
            "restartPolicy": "never",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-twin:latest",
              "createOptions": "{\"HostConfig\": { \"NetworkMode\": \"host\", \"CapAdd\": [\"NET_ADMIN\"] } }"
            }
          },
          "opcpublisher": {
            "version": "2.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "never",
            "settings": {
              "image": "mcr.microsoft.com/iotedge/opc-publisher:latest",
              "createOptions": "{\"Hostname\": \"publisher\", \"Cmd\": [ \"publisher\", \"--pf=./pn.json\", \"--di=60\", \"--to\", \"--aa\", \"--si=0\", \"--ms=0\" ], \"ExposedPorts\": { \"62222/tcp\": {} }, \"HostConfig\": { \"PortBindings\": { \"62222/tcp\": [{ \"HostPort\": \"62222\" }] } } }"
            }
          }
        }
      }
    },
    "$edgeHub": {
      "properties.desired": {
        "schemaVersion": "1.0",
        "routes": {
          "opctwinToIoTHub": "FROM /messages/modules/opctwin/outputs/* INTO $upstream",
          "opcpublisherToIoTHub": "FROM /messages/modules/opcpublisher/outputs/* INTO $upstream"
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

1. Deploy the OPC UA Device management [dependencies](dependencies.md) and obtained the resulting `.env` file. Note the deployed `hub name` of the `PCS_IOTHUBREACT_HUB_NAME` variable in the `.env` file.

2. Register and start a [Linux](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-linux) or [Windows](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-windows) IoT Edge gateway and note its `device id`.

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
   {"HostConfig":{"NetworkMode":"host","CapAdd":["NET_ADMIN"]}}
   ```

   Fill out the optional fields if necessary. For more information about container create options, restart policy, and desired status see [EdgeAgent desired properties](https://docs.microsoft.com/en-us/azure/iot-edge/module-edgeagent-edgehub#edgeagent-desired-properties). For more information about the module twin see [Define or update desired properties](https://docs.microsoft.com/en-us/azure/iot-edge/module-composition#define-or-update-desired-properties).

7. Select **Save** and repeat step **5**.  

8. In the IoT Edge Custom Module dialog, use `opcpublisher` as name for the module and the container *image URI* as 

   ```bash
   mcr.microsoft.com/iotedge/opc-publisher:latest
   ```

   As *create options* use the following JSON:

   ```json
   {"Hostname":"publisher","Cmd":["publisher","--pf=./pn.json","--di=60","--to","--aa","--si=0","--ms=0"],"ExposedPorts":{"62222/tcp":{}},"HostConfig":{"PortBindings":{"62222/tcp":[{"HostPort":"62222"}] }}}
   ```

9. Select **Save** and then **Next** to continue to the routes section.

10. In the routes tab, paste the following 

    ```json
    {
      "routes": {
        "opctwinToIoTHub": "FROM /messages/modules/opctwin/outputs/* INTO $upstream",
        "opcpublisherToIoTHub": "FROM /messages/modules/opcpublisher/outputs/* INTO $upstream"
      }
    }
    ```

    and select **Next**

11. Review your deployment information and manifest.  It should look like the above [deployment manifest](#Deployment-manifest).  Select **Submit**.

12. Once you've deployed modules to your device, you can view all of them in the **Device details** page of the portal. This page displays the name of each deployed module, as well as useful information like the deployment status and exit code.

## Deploying using AZ CLI

### Prerequisites

1. Ensure you have the prerequisites for [deploying modules using Azure portal](#Deploying-from-Azure-Portal).

2. Install the latest version of the [Azure command line interface (AZ)](https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest) from [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest).

### Quick start

1. Save the above [deployment manifest](#Deployment-manifest) into a `deployment.json` file.  

2. Use the following command to apply the configuration to an IoT Edge device:

   ```bash
   az iot edge set-modules --device-id [device id] --hub-name [hub name] --content ./deployment.json
   ```

   The `device id` parameter is case-sensitive. The content parameter points to the deployment manifest file that you saved. 
    ![az iot edge set-modules output](https://docs.microsoft.com/en-us/azure/iot-edge/media/how-to-deploy-cli/set-modules.png)

3. Once you've deployed modules to your device, you can view all of them with the following command: 

   ```bash
   az iot hub module-identity list --device-id [device id] --hub-name [hub name]
   ```

   The device id parameter is case-sensitive. ![az iot hub module-identity list output](https://docs.microsoft.com/en-us/azure/iot-edge/media/how-to-deploy-cli/list-modules.png)

## Run and debug locally

For trouble shooting and debugging it is useful to run the Edge modules locally using the [IoT Edge Development Simulator](https://github.com/Azure/iotedgehubdev).  It provides a local development experience with a simulator for creating, developing, testing, running, and debugging Azure IoT Edge modules and solutions using the same bits/code that are used in production.

### Prerequisites

1. Deploy the OPC UA Device management [dependencies](dependencies.md).

2. Install [Docker CE (18.02.0+)](https://www.docker.com/community-edition) on [Windows](https://docs.docker.com/docker-for-windows/install/), [macOS](https://docs.docker.com/docker-for-mac/install/) or [Linux](https://docs.docker.com/install/linux/docker-ce/ubuntu/#install-docker-ce).

3. Install [Docker Compose (1.20.0+)](https://docs.docker.com/compose/install/#install-compose) (Only required for **Linux**. Compose has already been included in Windows/macOS Docker CE installation)

4. Install [Python (2.7/3.5+) and Pip](https://www.python.org/)

5. Install iotedgehubdev by running below command in your terminal

   ```bash
   pip install --upgrade iotedgehubdev
   ```

   **Note**: Please install `iotedgehubdev` to **root** on Linux/macOS (*Don't use '--user' option in the 'pip install' command*).

**Please make sure there is no Azure IoT Edge runtime running on the same machine with iotedgehubdev since they require the same ports.**

### Quick start

1. Follow the instructions to [create a Edge Device in the Azure Portal](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-register-device-portal).  Copy the edge device connection string.

2. Setup the simulator using the edge connection string.

    ```bash
    iotedgehubdev setup -c <edge-device-connection-string>
    ```

3. Copy above manifest into a `deployment.json` file in the same folder.  Start the deployment in the simulator using

    ```bash
    iotedgehubdev start -d deployment.json
    ```

4. Stop the simulator using

   ```bash
   iotedgehubdev stop
   ```

### Advanced options

1. Start the module with specific input(s)

    ```bash
    iotedgehubdev start -i <module-inputs>
    ```

    For example: `iotedgehubdev start -i "input1,input2"`

2. Output the module credential environment variables

    ```bash
    iotedgehubdev modulecred
    ```

3. Send message to the module through RESTful API. 

    For example:
    `curl --header "Content-Type: application/json" --request POST --data '{"inputName": "input1","data": "hello world"}' http://localhost:53000/api/v1/messages`

## Next steps

- [Deploy and monitor OPC UA Device Management modules at scale](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-monitor)
- [Learn more about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)