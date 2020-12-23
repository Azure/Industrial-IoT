# Deploy Industrial IoT Edge Modules using Azure CLI

[Home](readme.md)

This article explains how to deploy the Industrial IoT Edge modules to [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) using the Azure Portal and Marketplace.

Before you begin,

1. Follow the [instructions to set up a IoT Edge device](howto-install-iot-edge.md).

2. Obtain the IoT Hub name and device id of the installed IoT Edge Gateway.

3. Install the [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli).  You must have at least `v2.0.24`, which you can verify with `az --version`.

4. Add the [IoT Edge Extension](https://github.com/Azure/azure-iot-cli-extension/) with the following commands:

    ```bash
    az extension add --name azure-cli-iot-ext
    ```

To deploy all required modules using Az...  

1. Save the [deployment manifest](deployment-manifest.md) into a `deployment.json` file.  

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

More information about az and IoT Edge can be found [here](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-monitor-cli).

## Next steps

- [Deploy and monitor Edge modules at scale](https://docs.microsoft.com/azure/iot-edge/how-to-deploy-monitor)
- [Learn more about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)
