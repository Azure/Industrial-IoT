# How to Setup a Local Gateway for Industrial Scenarios

[Home](readme.md)

The industrial assets (machines and systems) are connected to Azure through modules running on an [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) industrial gateway.

You can purchase industrial gateways compatible with IoT Edge. Please see our [Azure Device Catalog](https://catalog.azureiotsolutions.com/alldevices?filters={"3":["2","9"],"18":["1"]}) for a selection of industrial-grade gateways. Alternatively, you can setup a local VM.

## Automatic Industrial IoT Gateway Installation

Run the [Industrial IoT Gateway Installer](quickstart-gateway-installer.md) from your gateway to automatically install the IoT Edge Runtime and Industrial Modules.

## Manual Industrial IoT Gateway Installation

### Create an IoT Edge Instance and Install the IoT Edge Runtime

You can also manually [create an IoT Edge instance for an IoT Hub](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-register-device) and install the IoT Edge runtime following the [IoT Edge setup documentation](https://docs.microsoft.com/en-us/azure/iot-edge/). The IoT Edge Runtime can be installed on [Linux](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-linux) or [Windows](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-windows).

### Install the Industrial Modules

The Azure Industrial IoT deployment script will setup IoT Edge Layered Deployments for each Industrial Module. These Layered Deployments will be automatically applied to any IoT Edge instance with the following Device Twin JSON tags. To enable this for your IoT Edge gateway, add these tags through the [Azure Portal page](http://portal.azure.com) of the IoT Hub your gateway should communicate with:

The Device Twin configuration JSON can be found in the Azure Portal under IoT Hub -> IoT Edge -> [your IoT Edge device] -> Device Twin.

If your gateway uses Linux as an OS (with Linux Containers), set the "os" property to "Linux":

```json
...
},
"version": 1,
"tags": {
    "__type__": "iiotedge",
    "os": "Linux"
}
"properties":
...
```

If your gateway uses Windows as an OS (with Linux or Windows Containers), set the "os" property to "Windows":

```json
...
},
"version": 1,
"tags": {
    "__type__": "iiotedge",
    "os": "Windows"
}
"properties":
...
```

These tags can also be created as part of an Azure Device Provisioning (DPS) enrollment.  An example of the latter can be found in `/deploy/scripts/dps-enroll.ps1`.

### Unmanaged Industrial IoT Edge

Layered deployments will make sure that your edge devices will always contain the modules that work with your platform deployment.  This includes keeping module versions aligned with the platform version.

However, sometimes it is desirable to not have layered deployments manage your Gateway fleet.  Instead you might want to manage the content of the edge gateway yourself.  To prevent the platform from creating layered deployments on your IoT Edge Gateway, but still have the Gateway participate in the Platform, you can define a `unmanaged` tag in addition to the tags above, like so:

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

### Module Versions

By default, the same Docker container image version tag from mcr.microsoft.com is deployed that corresponds to the corresponding micro-service's version.

If you need to point to a different Docker container registry or image version tag, you can configure the source using environment variables `PCS_DOCKER_SERVER`, `PCS_DOCKER_USER`, `PCS_DOCKER_PASSWORD`, `PCS_IMAGES_NAMESPACE` and `PCS_IMAGES_TAG`, for example in your .env file (which can also be set during deployment), then restart the edge management or all-in-one service.

## Special Cases for Windows Networking Configuration

When running the Industrial IoT Edge modules in host (transparent) network mode, the container must be on the transparent host network and will require IP addresses assignment if no DNS server is avialable on that network.

- Ensure Hyper-V is enabled in the host OS
- Create a new virtual switch named **host** and attach it to a network containing the industrial assets you want to connect to (e.g. "Ethernet 2").

    ```bash
    New-VMSwitch -name host -NetAdapterName "<Adapter Name>" -AllowManagementOS $true
    ```

- To make sure the container is assigned an IP address it can either obtain:

    1. A Dynamic IP address from a local DHCP server accessible from the host's network interface associated to the container's **host** network  

    2. A Static IP address assigned on the container create options statement

        In order to allow static IP address assignment on a Windows container, the docker network requires to be created having the the subnet specified identical to the host's interface

        ```bash
        docker -H npipe:////.//pipe//iotedge_moby_engine network create -d transparent -o com.docker.network.windowsshim.interface="Ethernet 2" -o com.docker.network.windowsshim.networkname=host --subnet=192.168.30.0/24 --gateway=192.168.30.1 host
        ```

## Troubleshooting

To troubleshoot your IoT Edge installation follow the official [IoT Edge troubleshooting guide](https://docs.microsoft.com/en-us/azure/iot-edge/troubleshoot)

When device discovery operations fail also make sure to validate:

### Host network

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

## Other Options

- [Deploy Industrial IoT modules to your Gateway using the Azure Portal and Marketplace](howto-deploy-modules-portal.md)
- [Deploy Industrial IoT modules using Az](howto-deploy-modules-az.md)
- [Learn about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)
