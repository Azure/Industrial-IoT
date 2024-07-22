# Netcap Diagnostic Tool

> **This tool is provided as a sample, as-is and without any support and warrenty. When used in production please be aware that it is invasive and should be used with care.**

The netcap tool is designed to help diagnose interop issues with OPC UA servers that OPC Publisher connects to and which the normal logs of OPC Publisher fail to diagnose. 
To support diagnosis, Netcap collects network traces remotely from a OPC Publisher deployment and then makes the traces available for download and analysis. It is implemented as a sidecar to OPC Publisher that must be running in the same network as OPC Publisher (and see the same endpoints).

The Netcap tool has three modes of operation:

1. **Install Mode**: Allows installation and uninstallation of Netcap on a remote IoT Edge device as side car of a running OPC Publisher module. Installation provisions a storage account and container registry in the same resource group as the IoT Hub the device is connected to, then builds and deploys Netcap to the IoT Edge device. If installed with an output path (`-o`) it will also download the capture bundles during the capture session and remove the netcap module from the IoT Edege device once capture is cancelled. 
1. **Remote Mode**: Netcap connects to the OPC Publisher and starts a capture session. The capture session runs for a duration of 10 minutes after which a capture bundle is uploaded. This happens as long as the Netcap module is deployed.
1. **Uninstall Mode**: Removes the Netcap module from the IoT Edge device. This leaves the storage account and container registry in place.

Intallation and uninstallation require administrator rights to the subscription that includes the IoT Hub OPC Publisher is connected to. Rights must include access to the IoT Hub as well as the permission to create storage and container registry resources.
If you do not have permissions to create resources in the subscription, you must build Netcap and deploy it manually.
During installation the netcap tool is built inside the provisioned container registry from the sources found in the main branch of this repository. If you want to use a different branch, you must update the code in the source folder.

The capture bundle contains the network traces and the published nodes .json file from OPC Publisher retrieved during the capture session. The capture bundle is stored in the storage account provisioned during installation. 
The bundle contains sensitive data and must be handled with care, e.g. in addition to the OPC Publisher configuration it also contains key/token material that can be used in Wireshark 4.3 to decrypt secure channel communication inside the network traces.
