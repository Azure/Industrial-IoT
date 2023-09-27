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
    using var response = await httpClient.PostAsJsonAsync(parameters.OpcPublisher + "/v2/read", new
    {
        connection = new
        {
            endpoint = new
            {
                url = parameters.OpcPlc,
                securityMode = "SignAndEncrypt"
            }
        },
        request = new
        {
            nodeId = "nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowNumberOfUpdates",
        }
    }).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
    var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
    return responseJson.GetProperty("value").GetInt32();
}

// Write value to the slow number of updates configuration node
async ValueTask WriteSlowNumberOfUpdatesValueAsync(int value)
{
    using var response = await httpClient.PostAsJsonAsync(parameters.OpcPublisher + "/v2/write", new
    {
        connection = new
        {
            endpoint = new
            {
                url = parameters.OpcPlc,
                securityMode = "SignAndEncrypt"
            }
        },
        request = new
        {
            nodeId = "nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowNumberOfUpdates",
            value
        }
    }).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
}
