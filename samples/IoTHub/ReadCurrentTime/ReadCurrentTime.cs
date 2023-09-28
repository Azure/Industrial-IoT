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

while (true)
{
    var methodInvocation = new CloudToDeviceMethod("ValueRead_V2")
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
            nodeId = "i=2258"
        }
    }));

    var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInvocation).ConfigureAwait(false);
    var resopnseJson = JsonSerializer.Deserialize<JsonElement>(response.GetPayloadAsJson());
    Console.WriteLine("Current time on server:" + resopnseJson.GetProperty("value").GetString());
}
