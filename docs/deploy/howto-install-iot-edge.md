# How to Setup a Industrial IoT Edge Gateway

[Home](readme.md)

## Pre-configured Industrial IoT Edge Gateways

The industrial assets (machines and systems) are connected to Azure through modules running on an [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) industrial gateway.

You can purchase industrial gateways compatible with IoT Edge. Please see our [Azure Device Catalog](https://catalog.azureiotsolutions.com/alldevices?filters={"3":["2","9"],"18":["1"]}) for a selection of industrial-grade gateways. Alternatively, you can setup a local VM.

## Manual Industrial IoT Edge Gateway Installation

### Create an IoT Edge Instance and Install the IoT Edge Runtime

You can also manually [create an IoT Edge instance for an IoT Hub](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-register-device) and install the IoT Edge runtime following the [IoT Edge setup documentation](https://docs.microsoft.com/en-us/azure/iot-edge/). The IoT Edge Runtime can be installed on [Linux](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-linux) or [Windows](https://docs.microsoft.com/en-us/azure/iot-edge/iot-edge-for-linux-on-windows).

### Install the Industrial Modules

The Azure Industrial IoT deployment script will setup IoT Edge Layered Deployments for each Industrial Module. These Layered Deployments will be automatically applied to any IoT Edge instance that contains the following Device Twin JSON tags.

1. Go to the [Azure Portal page](http://portal.azure.com) and select your IoT Hub

2. Open the Device Twin configuration JSON under IoT Edge -> [your IoT Edge device] -> Device Twin

3. Insert the following `tags`:

- For Linux, set the "os" property to "Linux":

```json
...
},
"version": 1,
"tags": {
    "__type__": "iiotedge",
    "os": "Linux"
},
"properties":
...
```

- For Windows (EFLOW), set the "os" property to "Windows":

```json
...
},
"version": 1,
"tags": {
    "__type__": "iiotedge",
    "os": "Windows"
},
"properties":
...
```

The tags can also be created as part of an Azure Device Provisioning (DPS) enrollment. An example of the latter can be found in `/deploy/scripts/dps-enroll.ps1`.

### Unmanaged Industrial IoT Edge

Layered deployments will make sure that your edge devices will always contain the modules that work with your platform deployment. This includes keeping module versions aligned with the platform version.

However, sometimes it is desirable to not have layered deployments manage your Gateway fleet. Instead you might want to manage the content of the edge gateway yourself. To prevent the platform from creating layered deployments on your IoT Edge Gateway, but still have the Gateway participate in the Platform, you can define a `unmanaged` tag in addition to the tags above, like so:

```json
...
"tags": {
    "__type__": "iiotedge",
    // ...
    "unmanaged": true
}
...
```

This will cause that no modules are automatically deployed and thus you must deploy all modules using a module deployment manifest via [Az](howto-deploy-modules-az.md), or [Portal](howto-deploy-modules-portal.md).

### Temporarily continue deploying out of support 1.1 LTS modules to an 1.1 IoT Edge device

To continue deploying the 1.1 LTS modules to a 1.1 LTS IoT Edge gateway device until you are able to upgrade the device to 1.4, add a tag to your gateway device's twin with the name `use_1_1_LTS` and remove it once you have upgraded your edge gateway to 1.4 LTS. This operation can be automated using the az CLI. It should be done ahead of deploying the 2.8.4 release to Azure to avoid outages.

```json
...
"tags": {
    "__type__": "iiotedge",
    // ...
    "use_1_1_LTS": true
}
...
```

> IMPORTANT: Setting the tag to `false` or any other value has no effect.  Once you upgrade your IoT Edge device to 1.4 you must remove the tag to ensure the 1.4 modules are deployed to it. 

### Module Versions

By default, the same Docker container image version tag from mcr.microsoft.com is deployed that corresponds to the corresponding micro-service's version.

If you need to point to a different Docker container registry or image version tag, you can configure the source using environment variables `PCS_DOCKER_SERVER`, `PCS_DOCKER_USER`, `PCS_DOCKER_PASSWORD`, `PCS_IMAGES_NAMESPACE` and `PCS_IMAGES_TAG`, for example in your .env file (which can also be set during deployment), then restart the edge management or all-in-one service.

## Troubleshooting

To troubleshoot your IoT Edge installation follow the official [IoT Edge troubleshooting guide](https://docs.microsoft.com/en-us/azure/iot-edge/troubleshoot)

### Host network

When device discovery operations fail on Linux gateways (where the discovery module by default is attached to the host network) make sure to validate the host network is available:

```bash
docker network ls
    NETWORK ID          NAME                DRIVER              SCOPE
    beceb3bd61c4        azure-iot-edge      bridge              local
    97eccb2b9f82        bridge              bridge              local
    758d949c5343        host                host                local
    72fb3597ef74        none                null                local
```

## Other Options

- [Deploy Industrial IoT modules to your Gateway using the Azure Portal and Marketplace](howto-deploy-modules-portal.md)
- [Deploy Industrial IoT modules using Az](howto-deploy-modules-az.md)
- [Learn about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)
