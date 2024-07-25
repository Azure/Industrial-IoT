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

while (!cts.IsCancellationRequested)
{
    Console.Clear();
    await foreach (var info in httpClient.GetFromJsonAsAsyncEnumerable<JsonElement>(
        parameters.OpcPublisher + "/v2/diagnostics/clients", cts.Token))
    {
        Console.WriteLine(JsonSerializer.Serialize(info, Parameters.Indented));
    }
    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false);
}
