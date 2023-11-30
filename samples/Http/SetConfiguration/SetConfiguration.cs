// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Text.Json;
using System.Net.Http.Json;

Console.WriteLine("Press key to exit");
Console.WriteLine();
using var cts = new CancellationTokenSource();
_ = Task.Run(() => { Console.ReadKey(); cts.Cancel(); });

using var parameters = await Parameters.Parse(args).ConfigureAwait(false);
// Connect to publisher
using var httpClient = parameters.CreateHttpClientWithAuth();

// Get original published nodes json configuration
var original = await httpClient.GetFromJsonAsync<JsonElement>(parameters.OpcPublisher + "/v2/configuration?IncludeNodes=true",
    JsonSerializerOptions.Default, cts.Token).ConfigureAwait(false);
var json = JsonSerializer.Serialize(original, Parameters.Indented);
Console.WriteLine("Original configuration: " + json);

// Clear all
using var clear = await httpClient.PutAsJsonAsync(parameters.OpcPublisher + "/v2/configuration", new { },
    JsonSerializerOptions.Default, cts.Token).ConfigureAwait(false);
clear.EnsureSuccessStatusCode();
var empty = await httpClient.GetFromJsonAsync<JsonElement>(parameters.OpcPublisher + "/v2/configuration?IncludeNodes=true",
    JsonSerializerOptions.Default, cts.Token).ConfigureAwait(false);
json = JsonSerializer.Serialize(empty, Parameters.Indented);
Console.WriteLine("Cleared to: " + json);

// Patch configuration to add new one

using var addOrUpdate = await httpClient.PatchAsJsonAsync(parameters.OpcPublisher + "/v2/configuration", new[]
{
    new
    {
        EndpointUrl = parameters.OpcPlc,
        DataSetWriterGroup = "Asset1",
        DataSetWriterId = "DataFlow1",
        DataSetPublishingInterval = 5000,
        OpcNodes = new []
        {
            new
            {
                Id = "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt0"
            }
        }
    }
}, JsonSerializerOptions.Default, cts.Token).ConfigureAwait(false);
addOrUpdate.EnsureSuccessStatusCode();

var configuration = await httpClient.GetFromJsonAsync<JsonElement>(parameters.OpcPublisher + "/v2/configuration?IncludeNodes=true",
    JsonSerializerOptions.Default, cts.Token).ConfigureAwait(false);
json = JsonSerializer.Serialize(configuration, Parameters.Indented);
Console.WriteLine("Patched to: " + json);

// Remove the endpoint and all its nodes again
using var unpublish = await httpClient.PostAsJsonAsync(parameters.OpcPublisher + "/v2/configuration/nodes/unpublish", new
{
    EndpointUrl = parameters.OpcPlc,
    DataSetWriterGroup = "Asset1",
    DataSetWriterId = "DataFlow1"
}, JsonSerializerOptions.Default, cts.Token).ConfigureAwait(false);
unpublish.EnsureSuccessStatusCode();

configuration = await httpClient.GetFromJsonAsync<JsonElement>(parameters.OpcPublisher + "/v2/configuration?IncludeNodes=true",
    JsonSerializerOptions.Default, cts.Token).ConfigureAwait(false);
json = JsonSerializer.Serialize(configuration, Parameters.Indented);
Console.WriteLine("Now cleared again: " + json);

// Re-apply original published nodes json configuration to the publisher
using var reset = await httpClient.PutAsJsonAsync(parameters.OpcPublisher + "/v2/configuration", original,
    JsonSerializerOptions.Default, cts.Token).ConfigureAwait(false);
reset.EnsureSuccessStatusCode();
configuration = await httpClient.GetFromJsonAsync<JsonElement>(parameters.OpcPublisher + "/v2/configuration?IncludeNodes=true",
    JsonSerializerOptions.Default, cts.Token).ConfigureAwait(false);
json = JsonSerializer.Serialize(configuration, Parameters.Indented);
Console.WriteLine("Now reset back to: " + json);
