
# Azure Industrial IoT Gateway Installer

[Home](readme.md)

The IoT Edge Runtime and the Industrial IoT Edge Modules are automatically installed with the Azure Industrial IoT Gateway Installer.

## Run the Industrial IoT Gateway Installer

From your edge gateway, run the Industrial IoT Gateway Installer for Windows directly from the Web from [here](https://github.com/Azure/Industrial-IoT-Gateway-Installer/raw/master/Releases/Windows/setup.exe)

For Linux download following folder in your gateway device from [here](https://github.com/Azure/Industrial-IoT-Gateway-Installer/raw/master/Releases/Linux.zip) and unzip it. Then go to the extracted folder and make the console application file executable e.g.:

   ```bash
   chmod 777 ./IoTEdgeInstallerConsoleApp
   ```

Then run the application to start the gateway installation and provide the required parameters

   ```bash
   sudo ./IoTEdgeInstallerConsoleApp
   ```

The Industrial IoT Gateway Installer will make sure all prerequisits are met.

Apart from a name for your gateway, you will be asked to provide the IoT Hub Owner Connection String for the IoT Hub your gateway should communicate with. The person in your organisation responsible for Azure cloud administration can provide this string to you. It is available in the [Azure Portal](http://portal.azure.com) under the "Shared access policies" tab of the corresponding IoT Hub page.

The installation will run completely automatically and a restart may be required several times during installation, depending on prerequisits that need to be installed. Simply rerun the Industrial IoT Gateway Installer after restarts.

## Next steps

- [Deploy and monitor Edge modules at scale](https://docs.microsoft.com/azure/iot-edge/how-to-deploy-monitor)
- [Learn more about Azure IoT Edge for Visual Studio Code](https://github.com/microsoft/vscode-azure-iot-edge)
