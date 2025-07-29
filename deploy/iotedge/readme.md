# IoT Edge deployment scripts <!-- omit in toc -->

> DO NOT USE FOR PRODUCTION SYSTEMS. The scripts here are intended for development and testing purposes only.

## Table Of Contents <!-- omit in toc -->

- [Deploy IoT Edge simulation to Azure](#deploy-iot-edge-simulation-to-azure)
- [Azure IoT Edge EFLOW](#azure-iot-edge-eflow)
  - [Deploying a debug OPC Publisher](#deploying-a-debug-opc-publisher)
- [IoTEdgeHubDev](#iotedgehubdev)
- [Azure VM based IoT Edge](#azure-vm-based-iot-edge)

## Deploy IoT Edge simulation to Azure

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2FIndustrial-IoT%2Fmain%2Fdeploy%2Fiotedge%2Fazuredeploy.json)

## Azure IoT Edge EFLOW

To simplify setting up a development IoT Edge *on Windows*, run the `eflow-setup.ps1` script in a powershell session with Administrator privileges.

The script was tested on Windows 11. It will install and configure [Azure IoT Edge EFLOW](https://learn.microsoft.com/azure/iot-edge/quickstart) against an existing IoT Hub. The IoT Hub name can be provided on the command line or selected.  Use the `-Tenant` and `-Subscription` parameters of the powershell script to narrow which IoT Hub is selected.

The script will also deploy an initial set up modules (OPC PLC and OPC Publisher) sharing a common shared volume. This setup is similar to the [docker-compose](../docker/), but not intended for production deployments.

> Once the modules in `eflow-setup.json` are deployed the OPC Publisher will start to produce data that is uploaded to IoT Hub. This data can be significant and will accrue charges.

Make sure to stop the VM when you do not need it anymore (Run `powershell Stop-EflowVm` - Administrator privileges needed).  If you want to deploy a naked IOT Edge, re-provision the IoT Edge with the `-NoModules` switch.

By default the shared folder is persisted on the Host OS in the `C:\Shared` folder. If you like to choose a different folder, supply the full path using the `-SharedFolderPath` script parameter.

> The module PKI is persisted into the shared folder path. It contains keys which are secrets, make sure to guard access to the folder and properly delete the content when you are done with your development tasks.

The script also provides an option to `-ProvisioningOnly` an existing EFLOW installation against a different IOT Hub. The device ID will always be the name of the VM, which should be set as `%HOSTNAME%_EFLOW`. You can find and manage this device in the portal or via AZ CLI.  If the IoT Edge VM has been provisioned before, the script will prompt to confirm whether to re-provision the VM.

### Deploying a debug OPC Publisher

The script can also set up the Azure IoT Edge EFLOW device to support [debugging](https://aka.ms/iotedge-eflow-debugging). Use the `-DebuggingSupport` parameter to do so.

The script will print the docker command line that can be used from the host to access the docker daemon on the guest. Furthermore, if `-NoModules` is not set the script will build a debug container image of OPC Publisher from the current branch and deploy it into the IoT Edge instance instead of deploying the `latest` released container of OPC Publisher. The debug image can then be debugged using the previously mentioned instructions.

## IoTEdgeHubDev

In addition the folder contains a script to run OPC Publisher and OPC PLC in `IoTEdgeHubDev` tool. The script does not install, and only configures the `IoTEdgeHubDev` tool.  Also, volume mapping support on Windows is only provided in version 0.14.3 or higher. For this reason the deployed manifest has file mapping turned off.

> Note: The IoTEdgeHubDev tool is in maintenance mode and is not supported anymore. It is recommended to use the earlier mechanism to create a simulation IoT Edge.

## Azure VM based IoT Edge

If you are looking to setup a Azure IoT Edge VM in Azure (Windows or Linux), take a look at the deployment scripts [here](../scripts/).
