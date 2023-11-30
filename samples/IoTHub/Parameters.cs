// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using CommandLine;
using Microsoft.Azure.Devices;
using System.Text.Json;

namespace InvokeDeviceMethod;

/// <summary>
/// Command line parameters
/// </summary>
internal sealed class Parameters
{
    [Option(
        'c',
        "IoTHubOwnerConnectionString",
        Required = false,
        HelpText = "The IoT hub connection string. This is available under the 'Shared access policies' in the Azure portal." +
        "\nDefaults to value of environment variable 'IoTHubOwnerConnectionString'.")]
    public string? IoTHubOwnerConnectionString { get; set; } = Environment.GetEnvironmentVariable("IoTHubOwnerConnectionString");

    [Option(
        'e',
        "EdgeHubConnectionString",
        Required = false,
        HelpText = "The edge hub connection string that OPC Publisher is using mainly for the module and device id values." +
        "\nDefaults to value of environment variable 'EdgeHubConnectionString'.")]
    public string? EdgeHubConnectionString { get; set; } = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

    internal static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static void ValidateConnectionStrings(string? hubConnectionString, string? edgeConnectionString,
        out string deviceId, out string moduleId)
    {
        deviceId = string.Empty;
        moduleId = string.Empty;

        try
        {
            _ = IotHubConnectionStringBuilder.Create(hubConnectionString);
        }
        catch (Exception)
        {
            Console.WriteLine("An IoT hub connection string needs to be specified, " +
                "please set the environment variable \"IoTHubOwnerConnectionString\" " +
                "or pass in \"-c | --IoTHubOwnerConnectionString\" through command line.");
            Environment.Exit(1);
        }
        try
        {
            var ehc = IotHubConnectionStringBuilder.Create(edgeConnectionString);
            deviceId = ehc.DeviceId;
            moduleId = ehc.ModuleId;
        }
        catch (Exception)
        {
            Console.WriteLine("An Edge hub connection string needs to be specified, " +
                "please set the environment variable \"EdgeHubConnectionString\" " +
                "or pass in \"-e | --EdgeHubConnectionString\" through command line.");
            Environment.Exit(1);
        }
    }
}
