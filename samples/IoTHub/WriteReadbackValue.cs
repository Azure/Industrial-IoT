// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Text.Json;
using CommandLine;
using InvokeDeviceMethod;
using Microsoft.Azure.Devices;

Parameters? parameters = null;
ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
    .WithParsed(parsedParams => parameters = parsedParams)
    .WithNotParsed(errors => Environment.Exit(1));

// This sample accepts the service connection string as a parameter, if present.
Parameters.ValidateConnectionStrings(parameters?.IoTHubOwnerConnectionString, parameters?.EdgeHubConnectionString,
    out var deviceId, out var moduleId);

// Create a ServiceClient to communicate with service-facing endpoint on your hub.
using var serviceClient = ServiceClient.CreateFromConnectionString(parameters!.IoTHubOwnerConnectionString);

//
// Perform a couple write and readback operations:
//

var originalValue = await ReadSlowNumberOfUpdatesValueAsync().ConfigureAwait(false);
Console.WriteLine("Original value is: " + originalValue);

await WriteSlowNumberOfUpdatesValueAsync(33).ConfigureAwait(false);
var updatedValue = await ReadSlowNumberOfUpdatesValueAsync().ConfigureAwait(false);
Console.WriteLine("Value updated to : " + updatedValue);

await WriteSlowNumberOfUpdatesValueAsync(44).ConfigureAwait(false);
updatedValue = await ReadSlowNumberOfUpdatesValueAsync().ConfigureAwait(false);
Console.WriteLine("Value updated to : " + updatedValue);

await WriteSlowNumberOfUpdatesValueAsync(originalValue).ConfigureAwait(false);
originalValue = await ReadSlowNumberOfUpdatesValueAsync().ConfigureAwait(false);
Console.WriteLine("Now reset back to: " + originalValue);

//
// Helpers functions
//

// Read value of the slow number of updates configuration node
async ValueTask<int> ReadSlowNumberOfUpdatesValueAsync()
{
    var methodInvocation = new CloudToDeviceMethod("ValueRead")
    {
        ResponseTimeout = TimeSpan.FromSeconds(30),
    };
    methodInvocation.SetPayloadJson(JsonSerializer.Serialize(new
    {
        connection = new
        {
            endpoint = new
            {
                url = "opc.tcp://opcplc:50000",
                securityMode = "SignAndEncrypt"
            }
        },
        request = new
        {
            nodeId = "nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowNumberOfUpdates"
        }
    }));
    var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId,
        methodInvocation).ConfigureAwait(false);
    var resopnseJson = JsonSerializer.Deserialize<JsonElement>(response.GetPayloadAsJson());
    return resopnseJson.GetProperty("value").GetInt32();
}

// Write value to the slow number of updates configuration node
async ValueTask WriteSlowNumberOfUpdatesValueAsync(int value)
{
    var methodInvocation = new CloudToDeviceMethod("ValueWrite")
    {
        ResponseTimeout = TimeSpan.FromSeconds(30),
    };
    methodInvocation.SetPayloadJson(JsonSerializer.Serialize(new
    {
        connection = new
        {
            endpoint = new
            {
                url = "opc.tcp://opcplc:50000",
                securityMode = "SignAndEncrypt"
            }
        },
        request = new
        {
            nodeId = "nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowNumberOfUpdates",
            value
        }
    }));
    await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInvocation).ConfigureAwait(false);
}
