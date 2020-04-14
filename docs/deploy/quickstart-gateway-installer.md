
# Azure Industrial IoT Gateway Installer

[Home](readme.md)

The IoT Edge Runtime and the Industrial IoT Edge Modules are automatically installed with the Azure Industrial IoT Gateway Installer.

## Run the Industrial IoT Gateway Installer

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

   ```bash
   sudo snap install dotnet-sdk --classic
   sudo snap alias dotnet-sdk.dotnet dotnet
   ```

The Industrial IoT Gateway Installer will make sure all prerequisits are met.

Apart from a name for your gateway, you will be asked to provide the IoT Hub Owner Connection String for the IoT Hub your gateway should communicate with. The person in your organisation responsible for Azure cloud administration can provide this string to you. It is available in the [Azure Portal](http://portal.azure.com) under the "Shared access policies" tab of the corresponding IoT Hub page.

The installation will run completely automatically and a restart may be required several times during installation, depending on prerequisits that need to be installed. Simply rerun the Industrial IoT Gateway Installer after restarts.

## Next steps

- [Deploy and monitor Edge modules at scale](https://docs.microsoft.com/azure/iot-edge/how-to-deploy-monitor)
- [Learn more about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)
