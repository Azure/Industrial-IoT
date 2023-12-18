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

var configuration = await httpClient.GetFromJsonAsync<JsonElement>(parameters.OpcPublisher + "/v2/configuration",
    JsonSerializerOptions.Default, cts.Token).ConfigureAwait(false);
var endpoints = configuration.GetProperty("endpoints").EnumerateArray();
foreach (var endpoint in endpoints)
{
    var endpointJson = JsonSerializer.Serialize(endpoint, Parameters.Indented);
    Console.WriteLine(endpointJson);
    var response = await httpClient.PostAsJsonAsync(parameters.OpcPublisher + "/v2/configuration/endpoints/list/nodes",
        endpoint, cts.Token).ConfigureAwait(false);
    var nodesJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(
        await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false)),
        Parameters.Indented);
    Console.WriteLine(nodesJson);
}
