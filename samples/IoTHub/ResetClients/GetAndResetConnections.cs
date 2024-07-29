// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using CommandLine;
using InvokeDeviceMethod;
using Microsoft.Azure.Devices;
using System.Net;
using System.Text.Json;

Parameters? parameters = null;
ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
    .WithParsed(parsedParams => parameters = parsedParams)
    .WithNotParsed(errors => Environment.Exit(1));

// This sample accepts the service connection string as a parameter, if present.
Parameters.ValidateConnectionStrings(parameters?.IoTHubOwnerConnectionString, parameters?.EdgeHubConnectionString,
    out var deviceId, out var moduleId);

// Create a ServiceClient to communicate with service-facing endpoint on your hub.
using var serviceClient = ServiceClient.CreateFromConnectionString(parameters!.IoTHubOwnerConnectionString);

Console.WriteLine("Active connections:");
var methodInvocation = new CloudToDeviceMethod("GetActiveConnections_V2")
{
    ResponseTimeout = TimeSpan.FromSeconds(30),
};
var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId,
    methodInvocation).ConfigureAwait(false);
var connections = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(response.GetPayloadAsJson()), Parameters.Indented);
Console.WriteLine(connections);

Console.WriteLine("Resetting all connections...");
methodInvocation = new CloudToDeviceMethod("ResetAllConnections_V2")
{
    ResponseTimeout = TimeSpan.FromSeconds(30),
};
response = await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId,
    methodInvocation).ConfigureAwait(false);
Console.WriteLine(response.Status);
