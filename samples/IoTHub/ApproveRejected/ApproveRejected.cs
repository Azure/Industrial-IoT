// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Transactions;
using CommandLine;
using InvokeDeviceMethod;
using Microsoft.Azure.Devices;
using static System.Formats.Asn1.AsnWriter;

Parameters? parameters = null;
ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
    .WithParsed(parsedParams => parameters = parsedParams)
    .WithNotParsed(errors => Environment.Exit(1));

// This sample accepts the service connection string as a parameter, if present.
Parameters.ValidateConnectionStrings(parameters?.IoTHubOwnerConnectionString, parameters?.EdgeHubConnectionString, out var deviceId, out var moduleId);

// Create a ServiceClient to communicate with service-facing endpoint on your hub.
using var serviceClient = ServiceClient.CreateFromConnectionString(parameters!.IoTHubOwnerConnectionString);

// List rejected certs
var methodInvocation = new CloudToDeviceMethod("ListCertificates")
{
    ResponseTimeout = TimeSpan.FromSeconds(30),
};
methodInvocation.SetPayloadJson(JsonSerializer.Serialize("Rejected"));
var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInvocation).ConfigureAwait(false);
var certificates = JsonSerializer.Deserialize<JsonElement>(response.GetPayloadAsJson()).EnumerateArray()
    .Select(c => (Thumbprint: c.GetProperty("thumbprint").GetString(), Subject: c.GetProperty("subject").GetString()))
    .ToList();
for (var i = 0; i < certificates.Count; i++)
{
    var certificate = certificates[i];
    Console.WriteLine($"[{i}] {certificate.Subject} [{certificate.Thumbprint}]");
}
Console.WriteLine("Select index of rejected certificate to approve:");
var selected = certificates[int.Parse(Console.ReadLine()!, CultureInfo.CurrentCulture)];

// Approve
methodInvocation = new CloudToDeviceMethod("ApproveRejectedCertificate")
{
    ResponseTimeout = TimeSpan.FromSeconds(30),
};
methodInvocation.SetPayloadJson(JsonSerializer.Serialize(selected.Thumbprint));
await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInvocation).ConfigureAwait(false);
