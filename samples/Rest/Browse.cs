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

// Connect to publisher
using var httpClient = SamplesHelper.Shared.CreateClient();

var request = new HttpRequestMessage(HttpMethod.Post, SamplesHelper.Shared.OpcPublisher + "/v2/browse")
{
    Content = JsonContent.Create(new
    {
        connection = new
        {
            endpoint = new
            {
                url = SamplesHelper.Shared.OpcPlc,
                securityMode = "SignAndEncrypt"
            }
        },
        request = new { }
    }),
};
using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
response.EnsureSuccessStatusCode();
await foreach (var result in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(response.Content.ReadAsStream(), cancellationToken: cts.Token)!)
{
    var browseElementJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(browseElementJson);
}
