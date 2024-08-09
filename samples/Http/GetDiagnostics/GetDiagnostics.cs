// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Text.Json;
using System.Net.Http.Json;

using var cts = new CancellationTokenSource();
_ = Task.Run(() => { Console.ReadKey(); cts.Cancel(); });

using var parameters = await Parameters.Parse(args).ConfigureAwait(false);
// Connect to publisher
using var httpClient = parameters.CreateHttpClientWithAuth();
while (!cts.IsCancellationRequested)
{
    Console.Clear();
    try
    {
        await foreach (var info in httpClient.GetFromJsonAsAsyncEnumerable<JsonElement>(
            parameters.OpcPublisher + "/v2/diagnostics/clients", cts.Token))
        {
            var str = JsonSerializer.Serialize(info, Parameters.Indented);
            Console.WriteLine(str);
        }
        Console.WriteLine();
        Console.WriteLine("Press key to exit");
        Console.SetCursorPosition(0, 0);
        await Task.Delay(TimeSpan.FromSeconds(1), cts.Token).ConfigureAwait(false);
    }
    catch (OperationCanceledException) { break; }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}
