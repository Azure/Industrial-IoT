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
Parameters.ValidateConnectionStrings(parameters?.IoTHubOwnerConnectionString, parameters?.EdgeHubConnectionString, out var deviceId, out var moduleId);

// Create a ServiceClient to communicate with service-facing endpoint on your hub.
using var serviceClient = ServiceClient.CreateFromConnectionString(parameters!.IoTHubOwnerConnectionString);

// Get configured endpoints and then the nodes for each one of it
var methodInvocation = new CloudToDeviceMethod("GetConfiguredEndpoints")
{
    ResponseTimeout = TimeSpan.FromSeconds(30),
};
var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInvocation).ConfigureAwait(false);

var endpoints = JsonSerializer.Deserialize<JsonElement>(response.GetPayloadAsJson()).GetProperty("endpoints").EnumerateArray();
foreach (var endpoint in endpoints)
{
    var endpointJson = JsonSerializer.Serialize(endpoint, Parameters.Indented);
    Console.WriteLine(endpointJson);
    methodInvocation = new CloudToDeviceMethod("GetConfiguredNodesOnEndpoint")
    {
        ResponseTimeout = TimeSpan.FromSeconds(30),
    };
    methodInvocation.SetPayloadJson(endpointJson);
    response = await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId,
        methodInvocation).ConfigureAwait(false);

    // Response is too large, this will generate 413 when run against OPC PLC.

    var nodesJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(response.GetPayloadAsJson()),
        Parameters.Indented);
    Console.WriteLine(nodesJson);
}
