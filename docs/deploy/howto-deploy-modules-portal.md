# Deploy Industrial IoT Edge Modules using the Azure Portal

[Home](readme.md)

This article explains how to deploy the Industrial IoT Edge modules to [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) using the Azure Portal and Marketplace.

Before you begin, make sure you followed the [instructions to set up a IoT Edge device](howto-install-iot-edge.md) and have a running IoT Edge Gateway.

To deploy all required modules to the Gateway using the Azure Portal...

1. Sign in to the [Azure portal](https://portal.azure.com/) and navigate to the IoT Hub deployed earlier.

   > A simple way to locate your IoT Hub is to find the resource group variable in your `.env` file.  This resource group contains the IoT Hub.

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

## Next steps

- [Deploy and monitor Edge modules at scale](https://docs.microsoft.com/azure/iot-edge/how-to-deploy-monitor)
- [Learn more about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)
