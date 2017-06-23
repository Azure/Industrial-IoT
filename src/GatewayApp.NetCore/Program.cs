// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IoTHubCredentialTools;
using Microsoft.Azure.Devices.Gateway;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GatewayApp.NetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // check for OSX, which we don't support
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                throw new NotSupportedException("OSX is not supported by the Gateway App on .Net Core");
            }

            // patch IoT Hub module DLL name
            string gatewayConfigFile = "gatewayconfig.json";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("Target system is Linux.");
                File.WriteAllText(gatewayConfigFile, File.ReadAllText(gatewayConfigFile).Replace("iothub.dll", "libiothub.so"));
            }
            else
            {
                Console.WriteLine("Target system is Windows.");
                File.WriteAllText(gatewayConfigFile, File.ReadAllText(gatewayConfigFile).Replace("libiothub.so", "iothub.dll"));
            }
            Console.WriteLine(RuntimeInformation.OSDescription);

            // print target system info
            if (IsX64Process())
            {
                Console.WriteLine("Target system is 64-bit.");
            }
            else
            {
                Console.WriteLine("Target system is 32-bit.");
                throw new Exception("32-bit systems are currently not supported.");
            }

            // check if we got command line arguments to patch our gateway config file and register ourselves with IoT Hub
            if ((args.Length > 0) && !string.IsNullOrEmpty(args[0]))
            {
                string applicationName = args[0];
                File.WriteAllText(gatewayConfigFile, File.ReadAllText(gatewayConfigFile).Replace("<ReplaceWithYourApplicationName>", applicationName));
                Console.WriteLine("Gateway config file patched with application name: " + applicationName);

                // check if we also received an owner connection string to register ourselves with IoT Hub
                if ((args.Length > 1) && !string.IsNullOrEmpty(args[1]))
                {
                    string ownerConnectionString = args[1];

                    Console.WriteLine("Attemping to register ourselves with IoT Hub using owner connection string: " + ownerConnectionString);
                    string deviceConnectionString = IoTHubRegistration.RegisterDeviceWithIoTHub(applicationName, ownerConnectionString);
                    if (!string.IsNullOrEmpty(deviceConnectionString))
                    {
                        SecureIoTHubToken.Write(applicationName, deviceConnectionString);
                    }
                    else
                    {
                        Console.WriteLine("Could not register ourselves with IoT Hub using owner connection string: " + ownerConnectionString);
                    }
                }
                else
                {
                    Console.WriteLine("IoT Hub owner connection string not passed as argument, registration with IoT Hub abandoned.");
                }

                // try to read connection string from secure store and patch gateway config file
                Console.WriteLine("Attemping to read connection string from secure store with certificate name: " + applicationName);
                string connectionString = SecureIoTHubToken.Read(applicationName);
                if (!string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("Attemping to configure publisher with connection string: " + connectionString);
                    string[] parsedConnectionString = IoTHubRegistration.ParseConnectionString(connectionString, true);
                    if ((parsedConnectionString != null) && (parsedConnectionString.Length == 3))
                    {
                        string _IoTHubName = parsedConnectionString[0];
                        if (_IoTHubName.Contains("."))
                        {
                            _IoTHubName = _IoTHubName.Substring(0, _IoTHubName.IndexOf('.'));
                        }
                        File.WriteAllText(gatewayConfigFile, File.ReadAllText(gatewayConfigFile).Replace("<ReplaceWithYourIoTHubName>", _IoTHubName));
                        Console.WriteLine("Gateway config file patched with IoT Hub name: " + _IoTHubName);
                    }
                    else
                    {
                        throw new Exception("Could not parse persisted device connection string!");
                    }
                }
                else
                {
                    Console.WriteLine("Device connection string not found in secure store.");
                }
            }
            else
            {
                Console.WriteLine("Application name not passed as argument, patching gateway config file abandoned");
            }

            IntPtr gateway = GatewayInterop.CreateFromJson(gatewayConfigFile);
            if (gateway != IntPtr.Zero)
            {
                Console.WriteLine(".NET Core Gateway is running. Press enter to quit.");
                Console.ReadLine();
                GatewayInterop.Destroy(gateway);
            }
            else
            {
                Console.WriteLine(".NET Core Gateway failed to initialize. Please make sure you have published the GatewayApp.NetCore app to make sure the depend DLLs are available!");
            }
        }

        private static bool IsX64Process()
        {
            return (IntPtr.Size == 8);
        }
    }
}
