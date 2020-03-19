
# Install IoT Edge automatically with Azure Industrial IoT Gateway Installer

IoT Edge Runtime and the modules can also be deployed automatically using the Azure Industrial IoT Gateway Installer.

To use the Azure Industrial IoT Gateway Installer to setup IoT Edge and the Industrial IoT Edge modules follow the following steps:

## Prerequisites

- Download Azure CLI

    1. On the Azure CLI Wizard read and accept the terms in the License Agreement by checking "I accept the terms in the License Agreement.
    2. Select "Install" to install Azure CLI.
    3. Click "Next".
    4. After Azure CLI is installed, click the Finish button to exit the Setup Wizard.

- Activate Hyper-V

    1. Right click on the Windows button and select ‘Apps and Features’.
    2. Select Programs and Features on the right under related settings.
    3. Select Turn Windows Features on or off.
    4. Select Hyper-V and click OK.
    5. When the installation has completed you are prompted to restart your computer.

## Download the Gateway Installer

1. Download and run the Gateway Installer for Windows from [here](https://github.com/Azure/Industrial-IoT-Gateway-Installer/raw/master/Releases/Windows/setup.exe) and for Linux from [here](https://github.com/Azure/Industrial-IoT-Gateway-Installer/raw/master/Releases/Linux.zip).

2. Select "Install".

3. If Azure CLI hasn’t been downloaded yet, you will be asked to download it.

4. The Industrial IoT Gateway Installer will check the requirements for the installation.

5. In the next step, you will be asked to install Azure IoT Edge on your computer. To do so

    1. Select your IoT Hub.
    2. Name your IoT Edge device.
    3. Optionally, select a local network interface to host IoT Edge.
    4. Check "install industrial modules (OPC Twin & OPC Publisher)" to install them.

6. To install them click "Install".

## Next steps

Get started with discovering your assets by deploying the
> [!div class="nextstepaction"]
> [Azure Industrial IoT Platform using the deployment script](../deploy/howto-deploy-all-in-one.md)