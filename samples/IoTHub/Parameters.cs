// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices;
using System;

namespace InvokeDeviceMethod
{
    /// <summary>
    /// Command line parameters for the InvokeDeviceMethod sample
    /// </summary>
    internal sealed class Parameters
    {
        [Option(
            'c',
            "HubConnectionString",
            Required = false,
            HelpText = "The IoT hub connection string. This is available under the 'Shared access policies' in the Azure portal." +
            "\nDefaults to value of environment variable IOTHUB_CONNECTION_STRING.")]
        public string? HubConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");

        [Option(
            'd',
            "DeviceId",
            Required = false,
            HelpText = "The Id of the device to receive the direct method." +
            "\nDefaults to 'MyDotnetDevice'.")]
        public string DeviceId { get; set; } = "MyDotnetDevice";

        public static void ValidateConnectionString(string? hubConnectionString)
        {
            try
            {
                _ = IotHubConnectionStringBuilder.Create(hubConnectionString);
            }
            catch (Exception)
            {
                Console.WriteLine("An IoT hub connection string needs to be specified, " +
                    "please set the environment variable \"IOTHUB_DEVICE_CONNECTION_STRING\" " +
                    "or pass in \"-c | --DeviceConnectionString\" through command line.");
                Environment.Exit(1);
            }
        }
    }
}
