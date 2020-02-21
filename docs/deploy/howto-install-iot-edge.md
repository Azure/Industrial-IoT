# Install Azure IoT Edge Runtime

[Home](readme.md)

The machines and factory equipment is connected to Azure through modules running inside a [Azure IoT Edge](https://azure.microsoft.com/services/iot-edge/) gateway.  This article shows you how to set up an IoT Edge Gateway on the Edge network.

## IoT Edge Gateway

To connect your own equipment obtain a preconfigured IoT Edge gateway.  If you do not have a gateway with IoT Edge pre-installed, you can install the IoT Edge runtime following the Azure IoT Edge [documentation](https://docs.microsoft.com/en-us/azure/iot-edge/).  For example you can install the runtime on [Linux](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-linux) or [Windows](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-windows).

To support network scanning and equipment discovery, the discovery module should best run in docker host network mode. To enable host network mode on Windows follow the instructions [below](#Windows-Networking-Configuration).

## Deploy the Industrial IoT Workloads to the Gateway

If you have not done so yet, [deploy](readme.md) the Industrial IoT platform or at a minimum the required [dependencies](../services/dependencies.md).  
> If you **only** want to deploy the cloud dependencies, start the [Edge Management](../services/edgemanager.md) Microservice or the [all-in-one](../services/all-in-one.md) service locally to ensure IoT Hub is configured to auto deploy the Industrial IoT Edge modules.

When the platform starts up it will set up layered deployments for each required module.  These layered deployment configurations will be automatically applied to any gateway with the following Device Twin tags:

```json
"tags": {
    "__type__": "iiotedge",
    "os": "Linux"    
}
```

If your container runtime is Windows Containers set the `os` property to `Windows`:

```json
"tags": {
    "__type__": "iiotedge",
    "os": "Windows"
}
```

Assign these tags to the IoT Edge gateway device's twin [when you register the new IoT Edge gateway in IoT Hub](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-register-device).  

> These tags can also be created as part of a Azure Device Provisioning (DPS) enrollment.  An example of the latter can be found in `/deploy/scripts/dps-enroll.ps1`.

### Workload versions

By default the same version tag from mcr.microsoft.com is deployed that corresponds to the service's version.

If you need to point to a different docker container registry or image version tag, you can configure the source using  environment variables `PCS_DOCKER_SERVER`, `PCS_DOCKER_USER`, `PCS_DOCKER_PASSWORD`, `PCS_IMAGES_NAMESPACE` and `PCS_IMAGES_TAG`, for example in your `.env` file (which can also be set during deployment), then restart the edge management or all-in-one service.

## Windows Networking Configuration

When running the industrial IoT Edge modules in host (transparent) network, the container must be on the transparent host network and might require IP addresses assignment.

- Ensure Hyper-V must be active  
- Create a new virtual switch named host having attached to an external network interface (e.g. "Ethernet 2").

    ```bash
    New-VMSwitch -name host -NetAdapterName "<Adapter Name>" -AllowManagementOS $true
    ```

- To make sure the container is assigned an IP address it can either obtain a:

    1. Dynamic IP address from a local DHCP server accessible from the host's network interface associated to the container's 'host' network, or a.  

    2. Static IP address assigned on the container create options statement
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
