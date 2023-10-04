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

foreach (var store in new[] { "Application", "Trusted", "Rejected", "Issuer", "User", "UserIssuer" })
{
    var methodInvocation = new CloudToDeviceMethod("ListCertificates")
    {
        ResponseTimeout = TimeSpan.FromSeconds(30),
    };
    // Get the application certificate and write to stdout
    methodInvocation.SetPayloadJson(JsonSerializer.Serialize(store));

    var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInvocation).ConfigureAwait(false);
    Console.WriteLine($"Certificates in {store} store:");
    Console.WriteLine("==============================");
    var certificates = JsonSerializer.Deserialize<JsonElement>(response.GetPayloadAsJson()).EnumerateArray().ToList();
    for (var i = 0; i < certificates.Count; i++)
    {
        var certificate = certificates[i];
        var thumbprint = certificate.GetProperty("thumbprint").GetString();
        var subject = certificate.GetProperty("subject").GetString();
        var notAfterUtc = certificate.GetProperty("notAfterUtc").GetDateTime();
        var notBeforeUtc = certificate.GetProperty("notBeforeUtc").GetDateTime();
        var serialNumber = certificate.GetProperty("serialNumber").GetString();
        var selfSigned = certificate.GetProperty("selfSigned").GetBoolean();
        Console.WriteLine($"[{thumbprint}] {subject} [SN:{serialNumber}] [Valid: {notBeforeUtc}-{notAfterUtc}] {(selfSigned ? "(Self-signed)" : "")}");
    }
    Console.WriteLine("==============================");
}

