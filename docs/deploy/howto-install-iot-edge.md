# How to Install Azure IoT Edge for Industrial Scenarios

[Home](readme.md)

The industrial assets (machines and systems) are connected to Azure through modules running on an [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) gateway. This article explains how to setup an IoT Edge Gateway for industrial scenarios.

## IoT Edge Runtime

You can purchase preconfigured IoT Edge gateways, please see our [Azure Device Catalog](https://catalog.azureiotsolutions.com/alldevices?filters={%223%22:[%222%22,%229%22],%2218%22:[%221%22]}) for a selection of industrial-grade gateways. Alternatively, you can setup an on-prem VM.

Then, run our [Industrial Gateway Installer](https://docs.microsoft.com/en-us/azure/quickstart-gateway-installer.md) on your gateway to automatically install the IoT Edge Runtime and Industrial Modules. Alternatively, you can manually install the IoT Edge runtime following the Azure IoT Edge [documentation](https://docs.microsoft.com/en-us/azure/iot-edge/). You can install the runtime on [Linux](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-linux) or [Windows](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-windows).

### Manually Enable Deployment of the Industrial IoT Modules on your IoT Edge Gateway

The deployment script will setup layered deployments for each IoT Edge Module. These layered deployments will be automatically applied to any gateway with the following Device Twin JSON tags:

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

If your container runtime is Windows Containers set the os property to Windows:

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

Assign these tags to your IoT Edge Gateway’s Device Twin [when you register the new IoT Edge gateway in IoT Hub](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-register-device). The Device Twin configuration JSON can be found in the Azure Portal under IoT Hub -> IoT Edge -> < your IoT Edge device > -> Device Twin.

> These tags can also be created as part of a Azure Device Provisioning (DPS) enrollment.  An example of the latter can be found in `/deploy/scripts/dps-enroll.ps1`.

### Module Versions

By default, the same Docker container image version tag from mcr.microsoft.com is deployed that corresponds to the corresponding micro-service's version.

If you need to point to a different Docker container registry or image version tag, you can configure the source using environment variables `PCS_DOCKER_SERVER`, `PCS_DOCKER_USER`, `PCS_DOCKER_PASSWORD`, `PCS_IMAGES_NAMESPACE` and `PCS_IMAGES_TAG`, for example in your .env file (which can also be set during deployment), then restart the edge management or all-in-one service.

## Windows Networking Configuration

When running the Industrial IoT Edge modules in host (transparent) network, the container must be on the transparent host network and will require IP addresses assignment if no DNS server is avialable on that network.

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

## Next steps

- [Deploy Industrial IoT modules to your Gateway using the Azure Portal and Marketplace](howto-deploy-modules-portal.md)
- [Deploy Industrial IoT modules using Az](howto-deploy-modules-az.md)
- [Learn about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)
