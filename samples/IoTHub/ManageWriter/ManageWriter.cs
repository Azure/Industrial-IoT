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

// Create a writer
var methodInvocation = new CloudToDeviceMethod("CreateOrUpdateDataSetWriterEntry")
{
    ResponseTimeout = TimeSpan.FromSeconds(30),
};
methodInvocation.SetPayloadJson(JsonSerializer.Serialize(new
{
    endpointUrl = "opc.tcp://opcplc:50000",
    securityMode = "SignAndEncrypt",
    dataSetWriterId = "MyWriterId",
    dataSetWriterGroup = "MyWriterGroup",
    dataSetPublishingInterval = 2000,
    OpcNodes = new[]
    {
        new
        {
            id = "i=2258",
            dataSetFieldId = "Time0",
            opcSamplingInterval = 1000
        },
    }
}));
await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInvocation).ConfigureAwait(false);

// Update the writer and add additional nodes
for (var i = 0; i < 10; i++)
{
    methodInvocation = new CloudToDeviceMethod("AddOrUpdateNodes")
    {
        ResponseTimeout = TimeSpan.FromSeconds(30),
    };
    methodInvocation.SetPayloadJson(JsonSerializer.Serialize(new
    {
        dataSetWriterId = "MyWriterId",
        dataSetWriterGroup = "MyWriterGroup",
        opcNodes = new[]
        {
            new
            {
                id = "i=2258",
                dataSetFieldId = "Time" + i,
                opcSamplingInterval = 1000
            }
        }
    }));
    await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId,
        methodInvocation).ConfigureAwait(false);
}

// Show the nodes in the writer
methodInvocation = new CloudToDeviceMethod("GetNodes")
{
    ResponseTimeout = TimeSpan.FromSeconds(30),
};
methodInvocation.SetPayloadJson(JsonSerializer.Serialize(new
{
    dataSetWriterId = "MyWriterId",
    dataSetWriterGroup = "MyWriterGroup"
}));
var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInvocation).ConfigureAwait(false);

var nodesJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(response.GetPayloadAsJson()),
    Parameters.Indented);
Console.WriteLine(nodesJson);
