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

var request = new HttpRequestMessage(HttpMethod.Post, parameters.OpcPublisher + "/v2/browse")
{
    Content = JsonContent.Create(new
    {
        connection = new
        {
            endpoint = new
            {
                url = parameters.OpcPlc,
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
    var browseElementJson = JsonSerializer.Serialize(result, Parameters.Indented);
    Console.WriteLine(browseElementJson);
}
